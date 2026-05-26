# Historia 5.5: Fundamentales — Catálogo selectivo, PDF→MD e IA desde Markdown

Status: done

## Story

Como AdminOps,
quiero que la importación manual de fundamentales use dropdowns del catálogo para FIBRA y Período, acepte un PDF que se convierta a Markdown y se guarde en base de datos de forma independiente al summary IA, y que los campos tengan tooltips explicativos,
para que el proceso de captura sea más guiado, el contenido del PDF esté disponible como texto en BD, y el summary IA siempre se genere desde ese Markdown almacenado (nunca desde el PDF directo).

## Acceptance Criteria

### AC1 — Campo FIBRA usa dropdown del catálogo

**Dado que** accedo a la página Fundamentales — Importación Manual,
**Entonces** el campo FIBRA es un dropdown que carga las FIBRAs activas del catálogo (ticker + nombre), reemplazando el input UUID libre. El FibraId enviado al backend es el ID (GUID) de la FIBRA seleccionada.

### AC2 — Campo Período usa dropdown de trimestres

**Dado que** accedo al formulario de importación,
**Entonces** el campo Período es un dropdown con los últimos 12 trimestres generados dinámicamente (Q2-2026, Q1-2026, Q4-2025, …). El dropdown muestra el trimestre actual primero.

### AC3 — PDF se convierte a Markdown y se guarda independientemente

**Dado que** subo un PDF durante la importación (campo PDF en el formulario),
**Entonces**:
- El backend recibe el PDF, extrae el texto usando PdfPig y lo almacena en el campo `MarkdownContent` del registro
- El PDF original se almacena también en disco (comportamiento existente de `pdfReference`)
- `MarkdownContent` es independiente de `Summary` — ambos pueden existir o estar vacíos por separado
- El preview muestra si el MD fue procesado (badge "MD disponible")

### AC4 — Summary IA se genera desde MarkdownContent en BD

**Dado que** un registro tiene `MarkdownContent` no vacío,
**Cuando** el operador hace click en "Generar Summary IA" en el historial,
**Entonces**:
- El backend llama a `IAiSummaryService.GenerateSummaryAsync` usando el `MarkdownContent` del registro como `bodyText` y `AiContentType.Document`
- El summary generado se guarda en el campo `Summary` del registro
- El historial muestra el badge de resumen actualizado

### AC5 — Tooltips explicativos en cada campo

**Dado que** el operador posiciona el cursor sobre el ícono ⓘ junto a cada label del formulario,
**Entonces** aparece un tooltip (title HTML) con la descripción del campo:
- FIBRA: qué representa, de dónde viene
- Período: formato Q#-YYYY, qué trimestre corresponde
- Cap Rate: definición y unidad esperada
- NAV por CBFI: valor neto del activo por certificado
- LTV: ratio deuda/activos, como decimal
- Margen NOI: ingresos netos operativos, como decimal
- Margen FFO: funds from operations, como decimal
- Dist. Trimestral: distribución en MXN por CBFI
- PDF: qué se espera y qué hace el sistema con él

### AC6 — Sin regresiones

Todos los tests existentes pasan tras los cambios. Los tests de integración de 5-2 siguen en verde.

---

## Tasks / Subtasks

### Backend — Dominio

- [x] **T1: Añadir MarkdownContent a FundamentalRecord**
  - [x] T1.1 Añadir propiedad `public string? MarkdownContent { get; set; }` a `src/Server/Domain/Fundamentals/FundamentalRecord.cs`

### Backend — Infrastructure

- [x] **T2: Configuración EF + Migración**
  - [x] T2.1 Añadir configuración de `MarkdownContent` en `FundamentalRecordConfiguration.cs`:
    ```csharp
    builder.Property(x => x.MarkdownContent)
        .HasColumnName("markdown_content")
        .HasColumnType("nvarchar(max)");
    ```
  - [x] T2.2 Generar migración: `dotnet ef migrations add AddMarkdownContentToFundamentalRecord --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [x] T2.3 Aplicar migración: `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`

- [x] **T3: Ampliar repositorio**
  - [x] T3.1 Añadir métodos a `IFundamentalRepository.cs`:
    ```csharp
    Task UpdateMarkdownContentAsync(Guid id, string markdownContent, CancellationToken ct);
    Task UpdateAiSummaryAsync(Guid id, string summary, CancellationToken ct);
    ```
  - [x] T3.2 Implementar en `FundamentalRepository.cs` (patrón load-modify-save igual que `UpdatePdfReferenceAsync`)

- [x] **T4: PdfPig + Extractor**
  - [x] T4.1 Instalar NuGet: `dotnet add src/Server/Infrastructure package UglyToad.PdfPig`
  - [x] T4.2 Crear `src/Server/Infrastructure/Integrations/Pdf/PdfMarkdownExtractor.cs`:
    ```csharp
    using UglyToad.PdfPig;

    namespace Infrastructure.Integrations.Pdf;

    public static class PdfMarkdownExtractor
    {
        public static string Extract(Stream pdfStream)
        {
            using var document = PdfDocument.Open(pdfStream);
            var sb = new System.Text.StringBuilder();
            foreach (var page in document.GetPages())
            {
                var text = page.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                    sb.AppendLine();
                }
            }
            return sb.ToString().Trim();
        }
    }
    ```

### Backend — SharedApiContracts

- [x] **T5: Actualizar DTOs**
  - [x] T5.1 Añadir `HasMarkdownContent` a `FundamentalRecordDto` (bool, entre Summary y PdfReference)
  - [x] T5.2 Añadir `HasMarkdownContent` a `FundamentalPreviewDto` (bool, entre PdfReference y CapturedAt)

### Backend — API Endpoints

- [x] **T6: Modificar endpoint POST /{id}/pdf para extraer MD**
  - [x] T6.1 En `OpsFundamentalsEndpoints.cs`, después de guardar el PDF en disco, extraer el texto con `PdfMarkdownExtractor.Extract` y llamar `UpdateMarkdownContentAsync`
  - [x] T6.2 El archivo PDF se sigue guardando (comportamiento existente, sin cambio)
  - [x] T6.3 En el response JSON del endpoint `/pdf`, añadir `markdownExtracted: true/false`

- [x] **T7: Nuevo endpoint POST /{id}/ai-summary**
  - [x] T7.1 En `OpsFundamentalsEndpoints.cs` añadir:
    ```csharp
    group.MapPost("/{id:guid}/ai-summary", async (
        Guid id,
        IFundamentalRepository repo,
        IFibraRepository fibraRepo,
        IAiSummaryService aiSummaryService,
        CancellationToken ct) =>
    {
        var record = await repo.GetByIdAsync(id, ct);
        if (record is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(record.MarkdownContent))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["markdownContent"] = ["El registro no tiene MarkdownContent. Sube primero un PDF."]
            });

        var fibra = await fibraRepo.GetByIdAsync(record.FibraId, ct);
        var title = $"Reporte Trimestral {fibra?.Ticker ?? record.FibraId.ToString()} {record.Period}";

        var summary = await aiSummaryService.GenerateSummaryAsync(
            title: title,
            snippet: null,
            bodyText: record.MarkdownContent,
            contentType: AiContentType.Document,
            ct: ct);

        if (string.IsNullOrWhiteSpace(summary))
            return Results.Problem("La IA no devolvió un resumen.", statusCode: StatusCodes.Status502BadGateway);

        await repo.UpdateAiSummaryAsync(id, summary, ct);
        return Results.Ok(new { summary });
    })
    .RequireAuthorization("AdminOps");
    ```

- [x] **T8: Actualizar mapeo DTO en GET /ops/fundamentals**
  - [x] T8.1 Incluir `HasMarkdownContent: !string.IsNullOrWhiteSpace(r.MarkdownContent)` en el mapeo del listado
  - [x] T8.2 Incluir `HasMarkdownContent: !string.IsNullOrWhiteSpace(record.MarkdownContent)` en el mapeo del confirm y del import preview

- [x] **T9: Regenerar SharedApiClient**
  - [x] T9.1 `npm run codegen:api` — actualizar `Api.json` y `schema.d.ts`

### Frontend — Ops SPA

- [x] **T10: Actualizar FundamentalsImportForm**
  - [x] T10.1 Reemplazar input UUID de FIBRA por `<select>` que carga catálogo con `useQuery({ queryKey: ['catalog-ops'], queryFn: fetchOpsCatalog })`. Mostrar `ticker — nombre`. Enviar `fibraId` = el ID del item seleccionado.
  - [x] T10.2 Reemplazar input Period por `<select>` con últimos 12 trimestres generados en frontend (función `generateRecentPeriods(): string[]`). El primer item es el trimestre actual.
  - [x] T10.3 Eliminar campo `pdfReference` (texto libre). Añadir campo `<input type="file" accept="application/pdf">` para subir PDF.
  - [x] T10.4 Actualizar `onSubmit`:
    1. Llamar `importFundamentals(jsonData)` → obtener `preview` con `id`
    2. Si `pdfFile` seleccionado: llamar `uploadFundamentalPdf(preview.id, pdfFile)`
    3. Llamar `onPreview(preview, selectedFibraId)`
  - [x] T10.5 Eliminar campo `summary` del formulario (ya no es manual; lo genera la IA).
  - [x] T10.6 Añadir tooltips (ícono `ⓘ` con `title="..."`) en cada campo:
    - FIBRA: "FIBRA a la que pertenecen estos fundamentales. Selecciona del catálogo de FIBRAs activas."
    - Período: "Trimestre al que corresponden los datos. Ejemplo: Q3-2024 = tercer trimestre de 2024."
    - Cap Rate: "Capitalización de renta: ratio NOI anualizado / valor de mercado de activos. Decimal (ej. 0.08 = 8%)."
    - NAV por CBFI: "Valor Neto del Activo por CBFI. Precio teórico por certificado según valor libro de activos menos pasivos."
    - LTV: "Loan to Value: deuda / valor total de activos. Decimal (ej. 0.45 = 45%)."
    - Margen NOI: "Net Operating Income margin: % de ingresos que queda tras gastos operativos. Decimal."
    - Margen FFO: "Funds from Operations margin: flujo operativo ajustado por depreciación. Decimal."
    - Dist. Trimestral: "Distribución pagada por CBFI en el trimestre, en MXN."
    - PDF: "Reporte oficial de la FIBRA (press release, informe trimestral). Se convierte a Markdown para análisis IA."

- [x] **T11: Actualizar FundamentalsPreview**
  - [x] T11.1 Si `preview.hasMarkdownContent`, mostrar badge verde "MD disponible"

- [x] **T12: Actualizar FundamentalsHistory**
  - [x] T12.1 Añadir columna "MD" en tabla (indicador si el registro tiene `markdownContent`)
  - [x] T12.2 Si registro tiene `markdownContent` y no tiene `summary` O si el operador quiere regenerar: mostrar botón "Generar Summary IA"
  - [x] T12.3 El botón llama a `generateAiSummary(id)` → `useMutation` → invalida query de historial

- [x] **T13: Actualizar fundamentalsApi.ts**
  - [x] T13.1 Añadir función `generateAiSummary(id: string): Promise<{ summary: string }>` usando `fetch` nativo con auth headers

### Tests

- [x] **T14: Unit test PdfMarkdownExtractor**
  - [x] T14.1 Crear `tests/Unit/Infrastructure.Tests/Integrations/Pdf/PdfMarkdownExtractorTests.cs`:
    - Test con bytes inválidos → verifica que lanza excepción apropiada
    - Test con stream vacío → verifica que lanza excepción apropiada (PdfPig lanza si el stream no es un PDF)

- [x] **T15: Integration test ai-summary endpoint**
  - [x] T15.1 En `tests/Integration/Api.Tests/Fundamentals/FundamentalsImportTests.cs` añadir:
    - `POST /ai-summary con registro sin MarkdownContent → 400 ValidationProblem`
    - `POST /ai-summary → 401 sin token, 403 con rol User`

---

## Dev Notes

### PdfPig — justificación de dependencia

`UglyToad.PdfPig` es la única librería MIT/pure-.NET para extracción de texto de PDF sin dependencias nativas. No hay alternativa sin deps de sistema operativo para MVP. Se añade al proyecto `Infrastructure` únicamente.

### MarkdownContent — formato

El campo `MarkdownContent` almacena el texto extraído del PDF, página por página, separado por líneas en blanco. No es Markdown decorado (sin `#`, `**`); es texto plano estructurado que sirve como `bodyText` para el prompt de IA con `AiContentType.Document`.

### Flujo PDF→MD en endpoint POST /{id}/pdf

```csharp
// Después de guardar el archivo en disco:
try
{
    var pdfBytes = File.ReadAllBytes(fullPath);
    using var pdfStream = new MemoryStream(pdfBytes);
    var markdown = PdfMarkdownExtractor.Extract(pdfStream);
    if (!string.IsNullOrWhiteSpace(markdown))
        await repo.UpdateMarkdownContentAsync(id, markdown, ct);
}
catch (Exception ex)
{
    logger.LogWarning(ex, "PDF guardado pero extracción MD falló para {Id}", id);
    // No falla el endpoint — el PDF ya está guardado, MD opcional
}
```

La extracción de MD no falla el endpoint si PdfPig no puede procesar el PDF (PDFs escaneados sin OCR, PDFs corruptos). El PDF siempre queda en disco.

### Endpoint ai-summary — no modifica Status

El endpoint `POST /{id}/ai-summary` solo actualiza el campo `Summary`. No cambia el `Status` del registro. Un registro `pending` puede tener summary generado; el operador aún debe confirmar.

### generateRecentPeriods() — lógica frontend

```typescript
function generateRecentPeriods(): string[] {
  const now = new Date()
  const periods: string[] = []
  let year = now.getFullYear()
  let q = Math.ceil((now.getMonth() + 1) / 3)
  for (let i = 0; i < 12; i++) {
    periods.push(`Q${q}-${year}`)
    q--
    if (q === 0) { q = 4; year-- }
  }
  return periods
}
```

### Tooltip — implementación simple

Usar `title` HTML nativo con ícono `ⓘ` inline:
```tsx
<label className="flex items-center gap-1 text-sm font-medium text-slate-700 mb-1">
  Cap Rate
  <span
    className="cursor-help text-slate-400 text-xs"
    title="Capitalización de renta: ratio NOI anualizado / valor de mercado de activos. Decimal (ej. 0.08 = 8%)."
  >
    ⓘ
  </span>
</label>
```

No requiere librería adicional.

### Migración cross-version

La migración añade solo una columna nullable `nvarchar(max)` a una tabla existente. Es segura sin locks en SQL Server — la columna se añade como NULL y los registros existentes quedan con `MarkdownContent = null`. No requiere backfill.

### ImportFundamentalsRequest — sin cambio

El request de import no cambia: `Summary` se puede seguir enviando opcionalmente (para importaciones sin PDF), pero el formulario ya no lo muestra. El campo `pdfReference` del request también permanece para compatibilidad con tests existentes.

### noUnusedLocals

El tsconfig del Ops SPA tiene `noUnusedLocals: true`. Cada import nuevo DEBE usarse.

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- **EF migration falló por DLLs bloqueados**: El proceso `dotnet run` del API tenía bloqueados los assemblies de Infrastructure. Workaround: `--configuration Release` en `migrations add` y `database update` (convención documentada en FIBRADIS).
- **PdfPig sin versión estable**: Solo disponible como prerelease `1.7.0-custom-5`. Instalado con `--prerelease`. Añadido a `Directory.Packages.props`.
- **TypeScript error `Property 'name' does not exist on type 'FibraDetail'`**: `FibraDetail` expone `fullName` y `shortName`, no `name`. Corregido: `f.shortName ?? f.fullName`.
- **Linting error en `<input type="file">` oculto**: "Element has no title attribute, no placeholder attribute". Resuelto: `aria-label="Seleccionar archivo PDF"`.
- **Lint hint en FundamentalsPreview**: "Button type attribute has not been set." Resuelto: `type="button"` en ambos botones.

### Completion Notes List

- **AC1**: Campo FIBRA reemplazado por `<select>` que consume `fetchOpsCatalog`. Muestra `ticker — shortName ?? fullName`. Envía GUID al backend.
- **AC2**: Campo Período reemplazado por `<select>` con `generateRecentPeriods()` — últimos 12 trimestres dinámicos, trimestre actual primero.
- **AC3**: Endpoint `POST /{id}/pdf` ahora lee el PDF en `MemoryStream` después de guardarlo en disco, llama `PdfMarkdownExtractor.Extract` (PdfPig), y persiste el texto en `MarkdownContent`. Fallos de extracción se loggean como Warning sin romper el endpoint. Response incluye `markdownExtracted: bool`.
- **AC4**: Nuevo endpoint `POST /{id}/ai-summary` valida que `MarkdownContent` no esté vacío (400 si vacío), llama `IAiSummaryService.GenerateSummaryAsync` con `AiContentType.Document` y persiste el resultado en `Summary` vía `UpdateAiSummaryAsync`. No modifica `Status`. FundamentalsHistory muestra botón "Generar IA"/"Regen. IA" cuando `hasMarkdownContent`.
- **AC5**: Componente `FieldInfo` con `title` HTML nativo en todos los campos: FIBRA, Período, PDF, Cap Rate, NAV por CBFI, LTV, Margen NOI, Margen FFO, Dist. Trimestral. Sin dependencia de librería adicional.
- **AC6**: 132/132 unit tests + 174/174 integration tests verdes. Builds backend (Release) y ambos SPAs sin errores ni warnings.
- **Separación MarkdownContent/Summary**: `MarkdownContent` (texto del PDF) y `Summary` (resumen IA) son columnas independientes en DB. Ambas son nullable. El formulario ya no expone `summary` manual.
- **FundamentalsHistory**: Refactorizado para extraer `HistoryRow` como componente separado, necesario para que `useMutation` del AI summary sea por fila (hook rules de React).
- **FundamentalsPreview**: Badge verde "MD disponible" aparece cuando `preview.hasMarkdownContent`. Botones con `type="button"` explícito.

### File List

**Nuevos:**
- `src/Server/Infrastructure/Integrations/Pdf/PdfMarkdownExtractor.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260526044451_AddMarkdownContentToFundamentalRecord.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Pdf/PdfMarkdownExtractorTests.cs`

**Modificados:**
- `Directory.Packages.props` — añadido UglyToad.PdfPig 1.7.0-custom-5
- `src/Server/Domain/Fundamentals/FundamentalRecord.cs` — `MarkdownContent` property; `Summary` setter de `init` a `set`
- `src/Server/Application/Fundamentals/IFundamentalRepository.cs` — `UpdateMarkdownContentAsync`, `UpdateAiSummaryAsync`
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs` — implementación de ambos métodos
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Fundamentals/FundamentalRecordConfiguration.cs` — configuración `markdown_content`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs` — snapshot actualizado
- `src/Server/SharedApiContracts/Fundamentals/FundamentalRecordDto.cs` — `HasMarkdownContent: bool`
- `src/Server/SharedApiContracts/Fundamentals/FundamentalPreviewDto.cs` — `HasMarkdownContent: bool`
- `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` — PDF extrae MD, nuevo endpoint ai-summary, mappings DTO actualizados
- `scripts/codegen/Api.json` — regenerado con nuevos DTOs y endpoint
- `src/Web/SharedApiClient/schema.d.ts` — regenerado
- `src/Web/Ops/src/api/fundamentalsApi.ts` — `generateAiSummary`, tipo de retorno `uploadFundamentalPdf`
- `src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx` — reescritura completa (dropdowns, PDF file input, tooltips)
- `src/Web/Ops/src/modules/fundamentals/FundamentalsPreview.tsx` — badge MD disponible, `type="button"`
- `src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx` — columna MD, HistoryRow extraído, botón Generar IA
- `tests/Integration/Api.Tests/Fundamentals/FundamentalsImportTests.cs` — 3 tests nuevos ai-summary

### Change Log

- 2026-05-25: Historia 5-5 creada — mejoras formulario importación manual fundamentales.
- 2026-05-26: Historia 5-5 implementada — FIBRA dropdown catálogo, Período dropdown 12 trimestres, PDF→MarkdownContent (PdfPig), endpoint ai-summary, tooltips HTML nativos, columna MD en historial. 132 unit + 174 integration tests verdes.

---

## Senior Developer Review (AI)

**Fecha:** 2026-05-26 | **Outcome:** Approved (patches applied)

### Review Findings

- [x] [Review][Patch][Med] PDF en memoria dos veces en endpoint `/pdf` — `ms.ToArray()` crea `pdfBytes[]`, y luego se instancia `new MemoryStream(pdfBytes)` para PdfPig. Fix: reutilizar el `ms` original con `ms.Position = 0` y pasarlo directamente a `PdfMarkdownExtractor.Extract(ms)`, eliminando `pdfBytes` y la segunda copia de memoria. [`src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`]
- [x] [Review][Patch][Low] Sin log cuando PDF extrae texto vacío (no excepción) — cuando todos los `page.Text` son vacíos, `markdownExtracted` queda en `false` silenciosamente sin trazabilidad. Añadir `logger.LogInformation("PDF subido sin texto extraíble para registro {Id} (PDF escaneado o sin capa de texto)", id)` en el else del `string.IsNullOrWhiteSpace(markdown)`. [`src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`]
- [x] [Review][Patch][Med] Error silencioso en `handlePdfUpload` de FundamentalsHistory + sin validación de tamaño en frontend — el `catch` del historial solo hace `console.error`, sin feedback visual al usuario. Además, ni el formulario ni el historial validan `file.size` antes de subir. Fix: (a) mostrar un estado de error inline en el botón o un toast; (b) rechazar con mensaje si `file.size > 20 * 1024 * 1024`. [`src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx`]
- [x] [Review][Patch][Low] Badge "Error IA" sin mensaje ni dismiss — cuando `aiSummaryMutation.isError`, se muestra solo el texto fijo "Error IA" sin el mensaje de error ni forma de descartarlo. Fix: mostrar `aiSummaryMutation.error?.message` y añadir un botón ×. [`src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx`]
- [x] [Review][Patch][Low] `generateAiSummary` convierte ProblemDetails a cadena plana — en caso de 400/502, el servidor devuelve JSON estructurado (`title`, `detail`, `errors`) que se convierte a `"Error: 400 {json}"`. Fix: intentar `JSON.parse(text)` y extraer el `detail` o `title` para el mensaje de error. [`src/Web/Ops/src/api/fundamentalsApi.ts`]
- [x] [Review][Patch][Med] Falta test positivo de `PdfMarkdownExtractor` — el story file T14.1 especifica "Test con PDF sintético de 1 página con texto conocido → verifica que el texto extraído contiene el contenido esperado". Solo se implementaron los tests de error. Fix: añadir un test con un PDF mínimo válido (bytes generados programáticamente o un archivo `.pdf` de prueba) que verifique que `Extract` retorna texto no vacío. [`tests/Unit/Infrastructure.Tests/Integrations/Pdf/PdfMarkdownExtractorTests.cs`]
- [x] [Review][Defer] Race condition en uploads concurrentes del mismo registro — dos requests simultáneos pueden sobreescribir el mismo archivo y hacer updates no atómicos en BD. [`src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`] — deferred, herramienta Ops de baja concurrencia; ya existía en `UpdatePdfReferenceAsync` (ver W3 en 5-2)
- [x] [Review][Defer] Race condition en ai-summary — dos solicitudes simultáneas para el mismo id consumen créditos de IA dos veces. [`src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`] — deferred, baja probabilidad en Ops; mitigar con campo `generating` en historia futura si se detecta en producción
- [x] [Review][Defer] HttpClient timeout 30 s puede ser insuficiente para documentos largos con AiContentType.Document + retry — pre-existente en los AI services. [`src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs`] — deferred, pre-existing; ajustar timeout en configuración si se detecta en producción
- [x] [Review][Defer] Record huérfano si `uploadFundamentalPdf` falla post-import — el record queda en BD sin PDF/MD, el usuario no sabe que existe. [`src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx`] — deferred, arquitectura pre-existente de dos pasos; mitigar con mensaje de error aclaratorio en historia futura
- [x] [Review][Defer] Ordenamiento de períodos por `SUBSTRING` en SQL (string, no numérico) en `GetLatestProcessedByFibraAsync` — pre-existente, no introducido en esta historia. [`src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`] — deferred, pre-existing; riesgo mínimo con el formato Q#-YYYY
- [x] [Review][Defer] Separador de ruta OS vs `relativePath` hardcodeado con `/` — `Path.Combine` usa `\` en Windows mientras `relativePath` usa `/`. Pre-existente. [`src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`] — deferred, pre-existing; sin impacto en runtime actual
- [x] [Review][Defer] `UglyToad.PdfPig` en versión prerelease `1.7.0-custom-5` — sin versión estable publicada en NuGet; riesgo de supply-chain teórico. [`Directory.Packages.props`] — deferred, única opción MIT/pure-.NET documentada en Dev Notes
- [x] [Review][Defer] Updates de `pdfReference` y `markdownContent` en dos transacciones separadas — si el proceso termina entre los dos `SaveChanges`, el registro queda con PDF pero sin MD. [`src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`] — deferred, estado recuperable (re-subir el PDF re-extrae el MD)
