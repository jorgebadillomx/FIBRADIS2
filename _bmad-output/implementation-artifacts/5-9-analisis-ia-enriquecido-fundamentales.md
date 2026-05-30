# Story 5.9: Análisis IA Enriquecido para Fundamentales

Status: done

## Story

Como AdminOps y como analista que consulta la ficha pública de una FIBRA,
quiero que el análisis IA de fundamentales devuelva un JSON enriquecido con señales operativas, señales financieras, alertas de riesgo, resumen en markdown e investor takeaway,
de modo que la ficha pública sea más interpretativa y el equipo Ops pueda corregir notas de KPI directamente sin reimportar.

## Acceptance Criteria

**AC1 — Prompt y schema JSON enriquecido:**
- El prompt de KPI extraction en `ai.AiPrompt` (content_type=`kpi_extraction`) es reemplazado por el nuevo prompt que devuelve: 6 KPIs con notas, `operationalSignals[]`, `financialSignals[]`, `riskFlags[]`, `summaryMarkdown`, `investorTakeaway`, `extractionNotes`.
- El seed de migración actualiza el registro existente en `ai.AiPrompt`. El fallback en `AiPromptTemplateDefaults` también se actualiza.

**AC2 — Parser JSON:**
- `KpiExtractionJsonParser` parsea los nuevos campos del JSON de la IA.
- Los arreglos vacíos `[]` se normalizan a `List<string>` vacía (nunca null).
- Si la IA omite un campo de arreglo o devuelve null, se asigna lista vacía.

**AC3 — Persistencia:**
- Se agrega columna `ai_analysis_json nvarchar(max) NULL` a `fundamentals.FundamentalRecord` vía migración EF Core.
- El JSON almacenado sigue el shape del record `FundamentalAiAnalysis` (ver Dev Notes).
- El campo `Summary` existente en `FundamentalRecord` se actualiza con el valor de `summaryMarkdown` para backward compat con displays que ya leen `Summary`.
- `ErrorReason` se reserva exclusivamente para errores reales de proceso. Las notas de extracción ya no se guardan en `ErrorReason` — van en `AiAnalysisJson.ExtractionNotes`. En records con status `partial` o `processed`, asignar `ErrorReason = null`.

**AC4 — DTOs y API:**
- `FundamentalesPublicDto` incluye nuevos campos: `operationalSignals`, `financialSignals`, `riskFlags`, `summaryMarkdown`, `investorTakeaway`.
- Los arreglos se exponen siempre como `string[]` (nunca null en el JSON de respuesta, puede ser `[]`).
- `FundamentalRecordDto` (Ops) también incluye los nuevos campos.

**AC5 — Display en Main (secciones condicionales):**
- El resumen analítico (`summaryMarkdown`) se renderiza como markdown. Si `react-markdown` no está disponible, usar texto plano con `white-space: pre-wrap`.
- Si `operationalSignals` no está vacío → sección "Señales operativas" como lista.
- Si `financialSignals` no está vacío → sección "Señales financieras" como lista.
- Si `riskFlags` no está vacío → sección "Alertas de riesgo" como lista con indicador visual de advertencia.
- Si `investorTakeaway` tiene contenido → callout destacado al final.
- Ninguna sección se renderiza si su contenido está vacío/null. Sin texto "Sin datos" ni headings vacíos.

**AC6 — Edición de notas de KPI en Ops:**
- En `FundamentalsHistory`, cada fila de record (procesado o parcial) tiene un botón "Notas" que abre un modal `<Dialog>` de shadcn/ui.
- El modal pre-carga las 6 notas actuales en textareas editables con label por KPI.
- El botón "Guardar notas" está disabled si no hubo cambios respecto al valor inicial.
- Al guardar, llama a `PATCH /api/v1/ops/fundamentals/{id}/field-notes` → cierra modal → invalida cache del historial.
- Si el request falla, muestra mensaje de error sin cerrar el modal.

**AC7 — Nuevo endpoint `PATCH /{id}/field-notes`:**
- `PATCH /api/v1/ops/fundamentals/{id:guid}/field-notes` — Auth: `[Authorize("AdminOps")]`.
- Body: `PatchFieldNotesRequest` con las 6 notas de KPI como strings nullable.
- Devuelve `FundamentalRecordDto` 200 | 404 si record no existe o está eliminado.
- El endpoint escribe sobre `FieldNotesJson` reemplazando el diccionario completo.

**AC8 — Tests:**
- Tests unitarios para `KpiExtractionJsonParser` con los nuevos campos: arreglos vacíos, null handling, strings en blanco filtrados.
- Tests de integración para `PATCH /{id}/field-notes`: happy path 200, 404 record inexistente, 401/403.

## Tasks / Subtasks

- [x] T1: Schema de extracción IA (AC1, AC2)
  - [x] T1.1: Extender `KpiExtractionResult` con `SummaryMarkdown`, `InvestorTakeaway`, `OperationalSignals`, `FinancialSignals`, `RiskFlags` (ver Dev Notes para firma completa)
  - [x] T1.2: Actualizar `KpiExtractionJsonParser.TryParse()` para leer nuevos campos; agregar helper `ReadStringArray()`
  - [x] T1.3: Actualizar `AiPromptTemplateDefaults.KpiExtractionBody` con el nuevo prompt (el texto completo está en Dev Notes)
  - [x] T1.4: Crear migración EF Core que haga seed UPDATE del registro `kpi_extraction` en `ai.AiPrompt`

- [x] T2: Persistencia — `AiAnalysisJson` (AC3)
  - [x] T2.1: Crear `FundamentalAiAnalysis` record en `src/Server/Domain/Fundamentals/`
  - [x] T2.2: Agregar `AiAnalysisJson string?` a `FundamentalRecord` + `SetAiAnalysis()` / `GetAiAnalysis()` siguiendo el patrón de `FieldNotesJson`
  - [x] T2.3: Agregar mapeo EF Core en `FundamentalRecordConfiguration`: `HasColumnName("ai_analysis_json")`, tipo `nvarchar(max)`, nullable
  - [x] T2.4: `dotnet ef migrations add AddFundamentalAiAnalysisJson --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [x] T2.5: Actualizar `FundamentalRepository.UpdateKpiExtractionAsync()`: serializar `FundamentalAiAnalysis`, asignar `Summary = result.SummaryMarkdown ?? result.Summary`, `ErrorReason = null` en success

- [x] T3: DTOs, API y nuevo endpoint (AC4, AC7)
  - [x] T3.1: Extender `FundamentalesPublicDto` con `OperationalSignals`, `FinancialSignals`, `RiskFlags`, `SummaryMarkdown`, `InvestorTakeaway`
  - [x] T3.2: Extender `FundamentalRecordDto` con los mismos campos
  - [x] T3.3: Crear `PatchFieldNotesRequest` en `src/Server/SharedApiContracts/Fundamentals/`
  - [x] T3.4: Agregar `UpdateFieldNotesAsync(Guid id, Dictionary<string, string?> notes)` en `IFundamentalRepository` e implementación
  - [x] T3.5: Agregar endpoint `PATCH /{id:guid}/field-notes` en `OpsFundamentalsEndpoints`
  - [x] T3.6: Actualizar mapping en `FundamentalsEndpoints.cs` para incluir nuevos campos en `FundamentalesPublicDto`
  - [x] T3.7: Regenerar cliente API tipado: `npm run codegen:api`

- [x] T4: Frontend Main — display enriquecido (AC5)
  - [x] T4.1: Verificar si `react-markdown` está en `src/Web/Main/package.json`; instalar si no está
  - [x] T4.2: Actualizar `FundamentalesSection.tsx`: renderizar `summaryMarkdown` como markdown
  - [x] T4.3: Agregar secciones condicionales: Señales operativas, Señales financieras, Alertas de riesgo
  - [x] T4.4: Agregar callout de `investorTakeaway` al final
  - [x] T4.5: Verificar en dev server que vacíos no generan secciones en blanco

- [x] T5: Frontend Ops — modal de edición de notas (AC6)
  - [x] T5.1: Crear `EditFieldNotesDialog.tsx` en `src/Web/Ops/src/modules/fundamentals/`
  - [x] T5.2: Agregar `patchFieldNotes(id, notes)` en `src/Web/Ops/src/api/fundamentalsApi.ts`
  - [x] T5.3: Integrar el modal en `FundamentalsHistory.tsx` con botón "Notas" por fila (solo en records no archivados)
  - [x] T5.4: Conectar con `useMutation` de TanStack Query + invalidación de cache

- [x] T6: Tests (AC8)
  - [x] T6.1: Tests unitarios del parser: arreglos vacíos, null, strings en blanco, happy path con todos los campos
  - [x] T6.2: Tests de integración para `PATCH /{id}/field-notes`

## Dev Notes

### Contexto: qué existe y qué cambia

El módulo de fundamentales tiene un flujo completo de extracción IA (historia 5-6, done) y notas por KPI (historia 5-7, done). El estado actual:

- **`KpiExtractionResult`** [`src/Server/Application/Fundamentals/KpiExtractionResult.cs`]: record con 6 KPIs + notas + `Summary` + `ExtractionNotes` + `Success`. NO tiene señales, flags ni takeaway.
- **`KpiExtractionJsonParser`** [`src/Server/Infrastructure/Integrations/Ai/KpiExtractionJsonParser.cs`]: parsea el JSON actual. El helper `ReadNullableString` y `ReadNullableDecimal` son reutilizables. Agregar `ReadStringArray` para los nuevos arreglos.
- **`FundamentalRecord`** [`src/Server/Domain/Fundamentals/FundamentalRecord.cs`]: tiene `AiAnalysisJson` como propiedad a AGREGAR. Ya tiene `FieldNotesJson` con el patrón `SetFieldNotes()`/`GetFieldNotes()` que se debe replicar.
- **Prompt actual** [`src/Server/Infrastructure/Integrations/Ai/AiPromptTemplateDefaults.cs`]: solo genera 6 KPIs + notas + summary. Reemplazar completamente.
- **`UpdateKpiExtractionAsync`** [`src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`]: actualmente guarda `ExtractionNotes` en `ErrorReason` (workaround). Esta historia limpia ese workaround.

La historia paralela **4-10** (análisis enriquecido de noticias, en review) usa exactamente el mismo patrón de columna `ai_analysis_json` — leer ese story file para consistencia:
[`_bmad-output/implementation-artifacts/4-10-analisis-ia-enriquecido-noticias.md`](_bmad-output/implementation-artifacts/4-10-analisis-ia-enriquecido-noticias.md)

### `FundamentalAiAnalysis` — nuevo record de dominio

```csharp
// src/Server/Domain/Fundamentals/FundamentalAiAnalysis.cs
namespace Domain.Fundamentals;

public sealed record FundamentalAiAnalysis(
    string? SummaryMarkdown,
    string? InvestorTakeaway,
    IReadOnlyList<string> OperationalSignals,
    IReadOnlyList<string> FinancialSignals,
    IReadOnlyList<string> RiskFlags,
    string? ExtractionNotes);
```

### `KpiExtractionResult` — firma actualizada

Agregar al final del record existente (no mover los campos existentes para no romper callers):

```csharp
public sealed record KpiExtractionResult(
    decimal? CapRate,
    string? CapRateNote,
    decimal? NavPerCbfi,
    string? NavPerCbfiNote,
    decimal? Ltv,
    string? LtvNote,
    decimal? NoiMargin,
    string? NoiMarginNote,
    decimal? FfoMargin,
    string? FfoMarginNote,
    decimal? QuarterlyDistribution,
    string? QuarterlyDistributionNote,
    string? Summary,
    string ExtractionNotes,
    bool Success,
    // NUEVOS — al final para no romper constructores existentes:
    string? SummaryMarkdown = null,
    string? InvestorTakeaway = null,
    IReadOnlyList<string>? OperationalSignals = null,
    IReadOnlyList<string>? FinancialSignals = null,
    IReadOnlyList<string>? RiskFlags = null);
```

Los callers existentes (incluyendo `KpiExtractionJsonParser` y los servicios Gemini/DeepSeek) usan constructores posicionales. Con parámetros opcionales al final, no se rompen. En `FillMissingNotes` y el constructor de error, no es necesario especificar los nuevos campos (toman null por defecto).

### `KpiExtractionJsonParser` — helper `ReadStringArray`

```csharp
private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName)
{
    if (!root.TryGetProperty(propertyName, out var property))
        return Array.Empty<string>();
    if (property.ValueKind != JsonValueKind.Array)
        return Array.Empty<string>();
    return property.EnumerateArray()
        .Where(e => e.ValueKind == JsonValueKind.String)
        .Select(e => e.GetString()!)
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .ToList();
}
```

En `TryParse`, agregar al final del constructor de `KpiExtractionResult`:
```csharp
SummaryMarkdown:       ReadNullableString(root, "summaryMarkdown"),
InvestorTakeaway:      ReadNullableString(root, "investorTakeaway"),
OperationalSignals:    ReadStringArray(root, "operationalSignals"),
FinancialSignals:      ReadStringArray(root, "financialSignals"),
RiskFlags:             ReadStringArray(root, "riskFlags"),
```

`HasAnyExtractedValue` debe actualizarse para incluir los nuevos campos en la condición de éxito:
```csharp
private static bool HasAnyExtractedValue(KpiExtractionResult result)
    => CountExtractedNumericFields(result) > 0
    || !string.IsNullOrWhiteSpace(result.Summary)
    || !string.IsNullOrWhiteSpace(result.SummaryMarkdown)
    || (result.OperationalSignals?.Count > 0)
    || (result.FinancialSignals?.Count > 0)
    || (result.RiskFlags?.Count > 0);
```

### Prompt nuevo — texto completo

El prompt completo que el usuario proporcionó para reemplazar el actual `kpi_extraction`. El placeholder del reporte es `{markdown_content}` — verificar que el código de interpolación en los servicios IA use ese placeholder. Si actualmente usa uno diferente (e.g., `{{content}}`), adaptar el prompt, no el código.

```
Eres un analista senior especializado en FIBRAs mexicanas, estados financieros, reportes trimestrales y anuales, métricas operativas inmobiliarias y análisis bursátil en México.

Tu tarea es leer un reporte financiero en formato markdown, extraer KPIs clave y devolver ÚNICAMENTE un objeto JSON válido, sin texto adicional y sin bloques de código.

Formato de salida obligatorio:
{
  "capRate": <número decimal o null>,
  "capRateNote": "<de dónde proviene, cómo se calculó o por qué es null>",
  "navPerCbfi": <número decimal o null>,
  "navPerCbfiNote": "<nota breve>",
  "ltv": <número decimal o null>,
  "ltvNote": "<nota breve>",
  "noiMargin": <número decimal o null>,
  "noiMarginNote": "<nota breve>",
  "ffoMargin": <número decimal o null>,
  "ffoMarginNote": "<nota breve>",
  "quarterlyDistribution": <número decimal o null>,
  "quarterlyDistributionNote": "<nota breve>",
  "operationalSignals": ["<señal operativa 1>", "<señal operativa 2>"],
  "financialSignals": ["<señal financiera 1>", "<señal financiera 2>"],
  "riskFlags": ["<riesgo 1>", "<riesgo 2>"],
  "summaryMarkdown": "<resumen analítico en markdown>",
  "investorTakeaway": "<conclusión breve y directa para inversionistas>",
  "extractionNotes": "<observaciones generales sobre calidad, consistencia o limitaciones de la extracción>"
}

Reglas de extracción:
- Devuelve solo JSON válido.
- Todos los valores numéricos deben ser números puros, sin comas de miles, sin símbolo de moneda y sin signo de porcentaje.
- capRate, ltv, noiMargin y ffoMargin deben expresarse como decimal. Ejemplo: 8.5% = 0.085.
- quarterlyDistribution debe ser la distribución por CBFI en pesos.
- navPerCbfi debe ser el NAV por CBFI en pesos.
- Si un KPI está explícitamente reportado, úsalo.
- Si no está explícito pero puede calcularse con certeza a partir de cifras del reporte, calcúlalo e indícalo brevemente en la nota.
- Si no puede determinarse con suficiente certeza, devuelve null.
- No inventes datos, no asumas cifras faltantes y no uses conocimiento externo al reporte.
- Si hay cifras ambiguas o contradictorias, prioriza el dato consolidado o más explícito y explícalo en extractionNotes.
- Las notas de KPI deben ser concisas, máximo 2 oraciones.
- operationalSignals, financialSignals y riskFlags deben contener frases breves y útiles; si no aplica, devuelve arreglos vacíos [].

Instrucciones para summaryMarkdown:
- Debe estar en español.
- Debe tener entre 3 y 5 párrafos cortos.
- Puede usar markdown simple: párrafos, **negritas** y listas cortas con guion.
- No uses tablas, HTML, encabezados tipo #, ni bloques de código.
- No te limites a repetir números: interpreta el desempeño.
- Debe cubrir, cuando exista evidencia suficiente: evolución operativa, rentabilidad y generación de flujo, balance y apalancamiento, sostenibilidad de la distribución, fortalezas y riesgos.
- Si hay comparativos trimestrales o anuales, incorpóralos.
- Si faltan datos para sostener una conclusión fuerte, dilo explícitamente.
- Señala con **negritas** el principal factor positivo y el principal foco de riesgo si se pueden identificar.

Criterios analíticos:
- Evalúa crecimiento o contracción de ingresos, NOI, FFO, AFFO o EBITDA si están disponibles.
- Evalúa márgenes y eficiencia operativa.
- Evalúa señales sobre ocupación, rentas, spreads, renovaciones, diversificación, cobranza o desempeño por segmento si el reporte lo permite.
- Evalúa deuda, LTV, perfil de vencimientos, costo financiero, tasa fija/variable, liquidez y refinanciamiento si existen.
- Evalúa la calidad y sostenibilidad de la distribución, no solo su monto.
- Mantén tono profesional, sobrio y orientado a inversionistas.

Reporte:
{markdown_content}
```

### `UpdateKpiExtractionAsync` — cambios en el repositorio

En `FundamentalRepository.cs`, el método actualmente hace:
```csharp
ErrorReason = result.ExtractionNotes  // WORKAROUND ACTUAL — eliminar
```

Cambiar a:
```csharp
AiAnalysisJson = JsonSerializer.Serialize(new FundamentalAiAnalysis(
    SummaryMarkdown:    result.SummaryMarkdown,
    InvestorTakeaway:   result.InvestorTakeaway,
    OperationalSignals: result.OperationalSignals ?? Array.Empty<string>(),
    FinancialSignals:   result.FinancialSignals ?? Array.Empty<string>(),
    RiskFlags:          result.RiskFlags ?? Array.Empty<string>(),
    ExtractionNotes:    result.ExtractionNotes)),
Summary:        result.SummaryMarkdown ?? result.Summary,
ErrorReason:    null,   // limpieza del workaround; solo para errores reales
```

### Endpoint `PATCH /{id}/field-notes`

```csharp
// PatchFieldNotesRequest.cs — nuevo DTO
namespace SharedApiContracts.Fundamentals;

public sealed record PatchFieldNotesRequest(
    string? CapRateNote,
    string? NavPerCbfiNote,
    string? LtvNote,
    string? NoiMarginNote,
    string? FfoMarginNote,
    string? QuarterlyDistributionNote);
```

En `OpsFundamentalsEndpoints`, seguir el mismo patrón del endpoint `PATCH /{id}/kpis`:
```csharp
fundamentalsGroup.MapPatch("/{id:guid}/field-notes", async (...) =>
{
    var record = await repo.GetByIdAsync(id, ct);
    if (record is null || record.DeletedAt is not null)
        return Results.NotFound();

    var notes = new Dictionary<string, string?>
    {
        ["capRateNote"]               = request.CapRateNote,
        ["navPerCbfiNote"]            = request.NavPerCbfiNote,
        ["ltvNote"]                   = request.LtvNote,
        ["noiMarginNote"]             = request.NoiMarginNote,
        ["ffoMarginNote"]             = request.FfoMarginNote,
        ["quarterlyDistributionNote"] = request.QuarterlyDistributionNote,
    };
    await repo.UpdateFieldNotesAsync(id, notes, ct);
    var updated = await repo.GetByIdAsync(id, ct);
    return Results.Ok(updated!.ToDto());
})
.RequireAuthorization("AdminOps");
```

`UpdateFieldNotesAsync` en el repositorio: cargar el record, actualizar `FieldNotesJson` via `SetFieldNotes()`, llamar `SaveChangesAsync`.

### Frontend Main — diseño de layout propuesto

Después de la tabla de KPIs existente, agregar las nuevas secciones en `FundamentalesSection.tsx`:

```tsx
{/* Resumen analítico */}
{data.summaryMarkdown && (
  <div className="mt-4">
    <p className="text-xs font-semibold text-muted-foreground uppercase mb-2">Resumen analítico</p>
    <ReactMarkdown className="prose prose-sm text-sm">{data.summaryMarkdown}</ReactMarkdown>
  </div>
)}

{/* Señales operativas */}
{(data.operationalSignals?.length ?? 0) > 0 && (
  <div className="mt-4">
    <p className="text-xs font-semibold text-muted-foreground uppercase mb-1">Señales operativas</p>
    <ul className="text-sm space-y-1 list-disc list-inside text-muted-foreground">
      {data.operationalSignals!.map((s, i) => <li key={i}>{s}</li>)}
    </ul>
  </div>
)}

{/* Señales financieras — mismo patrón */}
{/* Alertas de riesgo — mismo patrón con indicador naranja/rojo */}

{/* Investor Takeaway */}
{data.investorTakeaway && (
  <div className="mt-4 rounded-md border-l-4 border-primary bg-muted p-3">
    <p className="text-xs font-semibold text-primary uppercase mb-1">Perspectiva del analista</p>
    <p className="text-sm">{data.investorTakeaway}</p>
  </div>
)}
```

Clases de Tailwind v4 disponibles — usar las del design system existente. Si hay duda, revisar `design-system/MASTER.md`.

### Frontend Ops — `EditFieldNotesDialog`

```tsx
// src/Web/Ops/src/modules/fundamentals/EditFieldNotesDialog.tsx
// Props: record: FundamentalRecordDto, open: boolean, onClose: () => void
// Usa Dialog de shadcn/ui + 6 Textarea con label por KPI
// useMutation con patchFieldNotes()
// Botón "Guardar notas" disabled si !isDirty (comparar JSON.stringify de valores)
// onSuccess: queryClient.invalidateQueries(["fundamentals", fibraId]) + onClose()
// onError: mostrar mensaje de error inline dentro del modal (no toast — es más simple)
```

Las 6 claves del diccionario de notas que el dialog edita:
```
capRateNote, navPerCbfiNote, ltvNote, noiMarginNote, ffoMarginNote, quarterlyDistributionNote
```

Usar `kpi-definitions.ts` de Ops (`src/Web/Ops/src/lib/kpi-definitions.ts`) para los labels de cada campo.

### Convenciones críticas (no violar)

- **Nunca mostrar `0` para financieros nulos — siempre `—`** ([`fundamentales.ts:formatFundamentalValue()`])
- Los arreglos del DTO público deben ser `string[]` (nunca null en la respuesta JSON de la API)
- `noUnusedLocals: true` en tsconfig — no dejar imports sin usar
- `IReadOnlyList<string>` en dominio/application, `string[]` en SharedApiContracts DTOs
- **Migrations**: detener la API antes de `dotnet ef migrations add`
- **EF Core**: nunca `Task.WhenAll` con el mismo DbContext — operaciones secuenciales

### Verificar placeholder del prompt en servicios IA

Antes de actualizar el texto del prompt, verificar cuál es el placeholder actual en `GeminiKpiExtractorService` y `DeepSeekKpiExtractorService`. Buscar cómo se construye el prompt (probablemente `template.Replace("{markdown_content}", markdown)` o similar). El nuevo prompt usa `{markdown_content}` — si el código usa otro token, adaptar el prompt.

### Deferred work que esta historia resuelve

- **Workaround `ErrorReason = extractionNotes`** en `FundamentalRepository.UpdateKpiExtractionAsync()` — resuelto en T2.5 (AC3).

### References

- [KpiExtractionResult.cs](src/Server/Application/Fundamentals/KpiExtractionResult.cs)
- [KpiExtractionJsonParser.cs](src/Server/Infrastructure/Integrations/Ai/KpiExtractionJsonParser.cs)
- [FundamentalRecord.cs](src/Server/Domain/Fundamentals/FundamentalRecord.cs)
- [FundamentalRecordConfiguration.cs](src/Server/Infrastructure/Persistence/SqlServer/Configurations/Fundamentals/FundamentalRecordConfiguration.cs)
- [AiPromptTemplateDefaults.cs](src/Server/Infrastructure/Integrations/Ai/AiPromptTemplateDefaults.cs)
- [FundamentalesPublicDto.cs](src/Server/SharedApiContracts/Fundamentals/FundamentalesPublicDto.cs)
- [OpsFundamentalsEndpoints.cs](src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs)
- [FundamentalsEndpoints.cs](src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs)
- [FundamentalesSection.tsx](src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx)
- [FundamentalsHistory.tsx](src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx)
- [FundamentalsImportForm.tsx](src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx)
- [kpi-definitions.ts Ops](src/Web/Ops/src/lib/kpi-definitions.ts)
- [4-10-analisis-ia-enriquecido-noticias.md](_bmad-output/implementation-artifacts/4-10-analisis-ia-enriquecido-noticias.md) — patrón gemelo para noticias
- [convenciones-fibradis.md](_bmad-output/planning-artifacts/convenciones-fibradis.md)

## Dev Agent Record

### Agent Model Used

gpt-5-codex

### Debug Log References

- `dotnet build FIBRADIS.slnx`
- `dotnet ef migrations add AddFundamentalAiAnalysisJson --project src/Server/Infrastructure --startup-project src/Server/Api`
- `npm run codegen:api`
- `npm run build --workspace=src/Web/Main`
- `npm run build --workspace=src/Web/Ops`
- `dotnet test FIBRADIS.slnx --no-build`
- `npm test --workspace=src/Web/Main`

### Completion Notes List

- Se extendió la extracción IA de fundamentales con `summaryMarkdown`, `investorTakeaway`, `operationalSignals`, `financialSignals` y `riskFlags`; el parser ahora normaliza arreglos faltantes/null a listas vacías y filtra strings en blanco.
- Se agregó persistencia `ai_analysis_json` en `fundamentals.FundamentalRecord`, con record de dominio `FundamentalAiAnalysis`, migración EF `AddFundamentalAiAnalysisJson` y limpieza del workaround que guardaba `ExtractionNotes` en `ErrorReason` para registros exitosos/parciales.
- Se ampliaron `FundamentalesPublicDto` y `FundamentalRecordDto`, se agregó `PatchFieldNotesRequest`, `UpdateFieldNotesAsync()` y el endpoint `PATCH /api/v1/ops/fundamentals/{id}/field-notes`; además se restauró el endpoint stateless `POST /api/v1/ops/fundamentals/extract-kpis` para mantener compatibilidad con la suite existente.
- En Main, `FundamentalesSection` ahora renderiza markdown analítico y secciones condicionales de señales/riesgos/takeaway sin headings vacíos; en Ops se agregó `EditFieldNotesDialog` con invalidación de cache y botón `Notas` por fila.
- Validación ejecutada:
  - `dotnet build FIBRADIS.slnx` ✅
  - `npm run codegen:api` ✅
  - `npm run build --workspace=src/Web/Main` ✅
  - `npm run build --workspace=src/Web/Ops` ✅
  - `dotnet test FIBRADIS.slnx --no-build` ✅ (`Api.Tests` 189, `Infrastructure.Tests` 160, `Application.Tests` 35, `Domain.Tests` 9, `Jobs.Tests` 2)
  - `npm test --workspace=src/Web/Main` ✅ (62 passed)

### File List

- _bmad-output/implementation-artifacts/sprint-status.yaml
- scripts/codegen/Api.json
- src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs
- src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs
- src/Server/Application/Fundamentals/IFundamentalRepository.cs
- src/Server/Application/Fundamentals/KpiExtractionResult.cs
- src/Server/Domain/Fundamentals/FundamentalAiAnalysis.cs
- src/Server/Domain/Fundamentals/FundamentalRecord.cs
- src/Server/Infrastructure/Integrations/Ai/AiPromptTemplateDefaults.cs
- src/Server/Infrastructure/Integrations/Ai/KpiExtractionJsonParser.cs
- src/Server/Infrastructure/Persistence/Migrations/20260530204046_AddFundamentalAiAnalysisJson.cs
- src/Server/Infrastructure/Persistence/Migrations/20260530204046_AddFundamentalAiAnalysisJson.Designer.cs
- src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
- src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs
- src/Server/Infrastructure/Persistence/SqlServer/Configurations/Fundamentals/FundamentalRecordConfiguration.cs
- src/Server/SharedApiContracts/Fundamentals/FundamentalRecordDto.cs
- src/Server/SharedApiContracts/Fundamentals/FundamentalesPublicDto.cs
- src/Server/SharedApiContracts/Fundamentals/PatchFieldNotesRequest.cs
- src/Web/Main/src/modules/ficha-publica/FibraPage.tsx
- src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx
- src/Web/Main/src/modules/ficha-publica/sections/fundamentales.ts
- src/Web/Ops/src/api/fundamentalsApi.ts
- src/Web/Ops/src/modules/fundamentals/EditFieldNotesDialog.tsx
- src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx
- src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx
- src/Web/Ops/src/shared/ui/dialog.tsx
- src/Web/SharedApiClient/schema.d.ts
- tests/Integration/Api.Tests/Fundamentals/FundamentalsImportTests.cs
- tests/Unit/Infrastructure.Tests/Integrations/Ai/KpiExtractionJsonParserTests.cs
- tests/Unit/Infrastructure.Tests/Persistence/Repositories/FundamentalRepositoryTests.cs

## Change Log

- 2026-05-30 — Implementada historia 5.9: análisis IA enriquecido para fundamentales, persistencia `ai_analysis_json`, edición Ops de notas KPI, DTOs/API extendidos, compatibilidad `extract-kpis` restaurada y suites/builds validados.
- 2026-05-30 — Code review pasada 2 — 10 patches aplicados: P1 empty strings→null en UpdateFieldNotesAsync, P2 eliminado updated! (usa record directo), P3 ErrorReason=null en UpdateStatusAsync al procesar, P4 arrays non-null en error path del parser, P5 useEffect Dialog solo resetea al abrir, P6 urlTransform en ReactMarkdown, P7 GetAiAnalysis variable local, P8 keys compuestas en SignalsBlock, P9 null-forgiving removido de tests, P10 round-trip BD eliminado en PATCH /field-notes. 397 backend tests + 0 errores TS.
- 2026-05-30 — Fix hallazgo High de code review: `UpdateKpiExtractionAsync()` ahora usa `result.Success` (que ya evalúa todos los campos cualitativos via `HasAnyExtractedValue()`) en vez de `hasAnyKpi` (solo numéricos). Agregados 2 tests unitarios: cualitativo-only → `partial`/`ErrorReason=null`; nada extraído → `error`/`ErrorReason` relleno. 162 Infrastructure, 35 Application, 9 Domain — todos pasando.
 
## Senior Developer Review (AI)

### Hallazgos

- [x] [High] `UpdateKpiExtractionAsync()` sigue decidiendo `Status` y `ErrorReason` solo con KPIs numéricos, aunque el parser nuevo ya considera válidos `summaryMarkdown`, `operationalSignals`, `financialSignals` y `riskFlags`. Si la IA devuelve solo análisis cualitativo útil sin KPIs numéricos, el record queda en `error` y `ErrorReason` vuelve a llenarse con `ExtractionNotes`, contradiciendo el cambio de esta historia y dejando fuera ese registro de los flujos que esperan un resultado exitoso/parcial. El criterio de persistencia debe alinearse con `HasAnyExtractedValue()` o equivalente. Evidencia: [src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs](/C:/Users/jorge/source/repos/FIBRADIS/src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs:110), [src/Server/Infrastructure/Integrations/Ai/KpiExtractionJsonParser.cs](/C:/Users/jorge/source/repos/FIBRADIS/src/Server/Infrastructure/Integrations/Ai/KpiExtractionJsonParser.cs:116).

### Verificación

- Review sobre el working tree actual de `5.9`.
- No reejecuté tests en esta pasada; usé el diff local y el story file como fuente para validar AC vs implementación.

### Review Findings (Pasada 2 — 2026-05-30)

**Patch — High**
- [x] [Review][Patch] **P1: Empty strings en PATCH /field-notes se persisten como no-null — tooltips vacíos en lugar de "sin nota"** [`OpsFundamentalsEndpoints.cs`] — `EditFieldNotesDialog` inicializa todos los campos a `""` y envía esas cadenas al guardar. El servidor no normaliza `""` a `null` antes de `SetFieldNotes()`. `GetFieldNotes()` filtra por `kv.Value is not null` pero no filtra `""`, por lo que `fieldNotes.capRate = ""` se persiste y el frontend lee esa cadena vacía como nota existente. Fix: en el endpoint PATCH o en `UpdateFieldNotesAsync`, convertir strings vacíos a null antes de pasar al diccionario.
- [x] [Review][Patch] **P2: `updated!` NullReferenceException tras segundo `GetByIdAsync` en PATCH /field-notes** [`OpsFundamentalsEndpoints.cs:243`] — El primer `GetByIdAsync` valida existencia, pero entre `UpdateFieldNotesAsync` y el segundo `GetByIdAsync` un proceso concurrente puede ejecutar soft-delete, haciendo que `updated` retorne null. El `!` suprime el warning del compilador pero `ToDto(updated!, ...)` lanzaría NRE. Fix: agregar null check sobre `updated` y retornar 404 si es null.

**Patch — Medium**
- [x] [Review][Patch] **P3: ErrorReason no se limpia cuando /confirm transiciona el record a "processed" (AC3)** [`FundamentalRepository.cs:UpdateStatusAsync`] — Si un record pasó por estado `error` y quedó con `ErrorReason` no-null, y luego el operador lo confirma (→ `processed`), `UpdateStatusAsync` no toca `ErrorReason`. El AC3 requiere `ErrorReason = null` en records `partial` y `processed`. Fix: asignar `record.ErrorReason = null` cuando `status == "processed"` en `UpdateStatusAsync`.
- [x] [Review][Patch] **P4: `KpiExtractionResult` arrays nullable en el constructor del path de error (AC2 violation)** [`KpiExtractionResult.cs`] — Los parámetros `OperationalSignals`, `FinancialSignals`, `RiskFlags` tienen default `= null`. En el path de error de `KpiExtractionJsonParser` (cuando el JSON no parsea), se crea un `KpiExtractionResult` con 14 argumentos posicionales, dejando los tres arrays como `null`. El AC2 exige "nunca null". Fix: en `KpiExtractionJsonParser`, el path de fallo debe pasar `Array.Empty<string>()` para los tres arrays. Los unit tests confirman la brecha: usan `result.OperationalSignals!` con null-forgiving `!`.
- [x] [Review][Patch] **P5: `useEffect` en `EditFieldNotesDialog` borra edits en progreso cuando TanStack Query refetcha en segundo plano** [`EditFieldNotesDialog.tsx:46-50`] — `initialState` es un `useMemo` dependiente de `record`. Si la query de historial refetcha (p.ej. al recuperar el foco de ventana) mientras el modal está abierto, `record` cambia, `initialState` se recalcula, el efecto dispara y los cambios no guardados del usuario se pierden. Fix: eliminar `initialState` de las dependencias del efecto y solo resetear cuando `open` cambia de `false` a `true`.
- [x] [Review][Patch] **P6: ReactMarkdown sin sanitización de URLs — vector XSS por `javascript:` href en página pública** [`FundamentalesSection.tsx:87-89`] — `react-markdown` elimina HTML raw por defecto pero renderiza enlaces markdown como `<a href="...">` sin filtrar `javascript:` URLs. Aunque el prompt prohíbe links, una salida IA inesperada podría inyectar uno. Fix: agregar un `urlTransform` (o `rehypePlugins: [rehypeSanitize]`) que rechace URLs no-http/https.

**Patch — Low**
- [x] [Review][Patch] **P7: `GetAiAnalysis()` deserializa el JSON 5 veces por request en el endpoint público** [`FundamentalsEndpoints.cs:47-51`] — Cinco llamadas consecutivas a `record.GetAiAnalysis()` sin variable local, cada una ejecuta `JsonSerializer.Deserialize`. El endpoint de Ops usa correctamente `GetAiAnalysis(record)` con variable local. Fix: extraer a variable local antes del constructor del DTO.
- [x] [Review][Patch] **P8: `key={item}` en `SignalsBlock` — señales duplicadas de la IA causan warning de React y colapso de items** [`FundamentalesSection.tsx:157`] — Si la IA devuelve la misma cadena dos veces en un array, React ve keys duplicadas. Fix: usar `key={`${index}-${item}`}`.
- [x] [Review][Patch] **P9: Unit tests usan `!` null-forgiving en propiedades que el spec garantiza non-null** [`KpiExtractionJsonParserTests.cs`] — `Assert.Empty(result.OperationalSignals!)` etc. no detectarían una regresión que retorne `null` en lugar de lista vacía. Fix: quitar los `!` para que el test falle correctamente si la propiedad es null.
- [x] [Review][Patch] **P10: PATCH /field-notes ejecuta 3 round-trips a BD para una operación simple** [`OpsFundamentalsEndpoints.cs` + `FundamentalRepository.cs:UpdateFieldNotesAsync`] — El endpoint hace `GetByIdAsync` (1), `UpdateFieldNotesAsync` que internamente hace `FirstOrDefaultAsync` (2), y luego otro `GetByIdAsync` (3). Fix: pasar el record ya cargado a `UpdateFieldNotesAsync` o unificar en una sola operación de update + select.

**Deferred**
- [x] [Review][Defer] **D1: `UpdateStatusAsync` guard silencia re-confirmación por actor diferente** [`FundamentalRepository.cs:53-54`] — Introducido en esta historia para idempotencia. Si un segundo AdminOps confirma un registro ya procesado, su nombre no queda registrado. Comportamiento intencional; documentado como limitación. — deferred, decisión de diseño deliberada
- [x] [Review][Defer] **D2: `UpdateKpiExtractionAsync` sobreescribe notas editoriales de Ops al re-extraer** [`FundamentalRepository.cs:87-100`] — Limitación pre-existente: re-extracción reemplaza `FieldNotesJson` completo sin merge con ediciones previas del operador. — deferred, limitación de diseño pre-existente
- [x] [Review][Defer] **D3: Race condition DB — dos records `processed` para el mismo período (sin unique constraint)** [`FundamentalRepository.cs`] — Pre-existente. El índice único no fue añadido en ninguna historia. — deferred, pre-existing
- [x] [Review][Defer] **D4: Parser sigue leyendo campo legacy `summary` que el nuevo prompt ya no emite** [`KpiExtractionJsonParser.cs:84`] — Dead code inofensivo; el fallback `SummaryMarkdown ?? Summary` funciona correctamente. — deferred, dead code inofensivo

### Verificación (Pasada 2)

- Review sobre working tree y archivos nuevos (untracked) de `5.9`.
- Tres capas en paralelo: Blind Hunter, Edge Case Hunter, Acceptance Auditor.
- Tests no re-ejecutados en esta pasada — la pasada anterior validó builds y suites.
