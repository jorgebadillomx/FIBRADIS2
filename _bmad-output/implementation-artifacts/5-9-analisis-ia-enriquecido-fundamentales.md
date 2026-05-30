# Story 5.9: Análisis IA Enriquecido para Fundamentales

Status: ready-for-dev

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

- [ ] T1: Schema de extracción IA (AC1, AC2)
  - [ ] T1.1: Extender `KpiExtractionResult` con `SummaryMarkdown`, `InvestorTakeaway`, `OperationalSignals`, `FinancialSignals`, `RiskFlags` (ver Dev Notes para firma completa)
  - [ ] T1.2: Actualizar `KpiExtractionJsonParser.TryParse()` para leer nuevos campos; agregar helper `ReadStringArray()`
  - [ ] T1.3: Actualizar `AiPromptTemplateDefaults.KpiExtractionBody` con el nuevo prompt (el texto completo está en Dev Notes)
  - [ ] T1.4: Crear migración EF Core que haga seed UPDATE del registro `kpi_extraction` en `ai.AiPrompt`

- [ ] T2: Persistencia — `AiAnalysisJson` (AC3)
  - [ ] T2.1: Crear `FundamentalAiAnalysis` record en `src/Server/Domain/Fundamentals/`
  - [ ] T2.2: Agregar `AiAnalysisJson string?` a `FundamentalRecord` + `SetAiAnalysis()` / `GetAiAnalysis()` siguiendo el patrón de `FieldNotesJson`
  - [ ] T2.3: Agregar mapeo EF Core en `FundamentalRecordConfiguration`: `HasColumnName("ai_analysis_json")`, tipo `nvarchar(max)`, nullable
  - [ ] T2.4: `dotnet ef migrations add AddFundamentalAiAnalysisJson --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [ ] T2.5: Actualizar `FundamentalRepository.UpdateKpiExtractionAsync()`: serializar `FundamentalAiAnalysis`, asignar `Summary = result.SummaryMarkdown ?? result.Summary`, `ErrorReason = null` en success

- [ ] T3: DTOs, API y nuevo endpoint (AC4, AC7)
  - [ ] T3.1: Extender `FundamentalesPublicDto` con `OperationalSignals`, `FinancialSignals`, `RiskFlags`, `SummaryMarkdown`, `InvestorTakeaway`
  - [ ] T3.2: Extender `FundamentalRecordDto` con los mismos campos
  - [ ] T3.3: Crear `PatchFieldNotesRequest` en `src/Server/SharedApiContracts/Fundamentals/`
  - [ ] T3.4: Agregar `UpdateFieldNotesAsync(Guid id, Dictionary<string, string?> notes)` en `IFundamentalRepository` e implementación
  - [ ] T3.5: Agregar endpoint `PATCH /{id:guid}/field-notes` en `OpsFundamentalsEndpoints`
  - [ ] T3.6: Actualizar mapping en `FundamentalsEndpoints.cs` para incluir nuevos campos en `FundamentalesPublicDto`
  - [ ] T3.7: Regenerar cliente API tipado: `npm run codegen:api`

- [ ] T4: Frontend Main — display enriquecido (AC5)
  - [ ] T4.1: Verificar si `react-markdown` está en `src/Web/Main/package.json`; instalar si no está
  - [ ] T4.2: Actualizar `FundamentalesSection.tsx`: renderizar `summaryMarkdown` como markdown
  - [ ] T4.3: Agregar secciones condicionales: Señales operativas, Señales financieras, Alertas de riesgo
  - [ ] T4.4: Agregar callout de `investorTakeaway` al final
  - [ ] T4.5: Verificar en dev server que vacíos no generan secciones en blanco

- [ ] T5: Frontend Ops — modal de edición de notas (AC6)
  - [ ] T5.1: Crear `EditFieldNotesDialog.tsx` en `src/Web/Ops/src/modules/fundamentals/`
  - [ ] T5.2: Agregar `patchFieldNotes(id, notes)` en `src/Web/Ops/src/api/fundamentalsApi.ts`
  - [ ] T5.3: Integrar el modal en `FundamentalsHistory.tsx` con botón "Notas" por fila (solo en records no archivados)
  - [ ] T5.4: Conectar con `useMutation` de TanStack Query + invalidación de cache

- [ ] T6: Tests (AC8)
  - [ ] T6.1: Tests unitarios del parser: arreglos vacíos, null, strings en blanco, happy path con todos los campos
  - [ ] T6.2: Tests de integración para `PATCH /{id}/field-notes`

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

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

### File List
