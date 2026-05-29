---
title: 'Rediseño flujo importación fundamentales — PDF-first con IA separada'
type: 'feature'
created: '2026-05-27'
status: 'done'
baseline_commit: 'fbc0effff69b92c83971efc646116b5e115b7c23'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El flujo actual mezcla creación del record, extracción de markdown y llamada a IA en un solo request sin persistencia intermedia. Si la IA falla, el PDF y el markdown se pierden. Los errores no llegan a `PipelineErrorLog`. El usuario no puede revisar ni editar los KPIs antes de confirmar.

**Approach:** Separar en dos pasos explícitos: Paso 1 crea el `FundamentalRecord`, guarda el PDF en disco y el markdown (con Compact) en BD — sin tocar la IA. Paso 2 lee el markdown guardado, llama a `kpiExtractorService`, guarda KPIs en el record y devuelve los valores al frontend para revisión editable antes de confirmar. Errores de ambos pasos van a `PipelineErrorLog` con pipeline `"Fundamentals"`.

## Boundaries & Constraints

**Always:**
- Flujo manual sin PDF (`POST /import`) permanece intacto.
- `ExtractionNotes` de la IA se guarda en `FundamentalRecord.ErrorReason` (no en FieldNotes).
- `PipelineErrorLog` se registra en Paso 1 (error de PDF/disk) y Paso 2 (error de IA).
- `processingMode = "ai"` en records creados por Paso 1.
- `MarkdownCompactor.Compact()` se aplica siempre antes de guardar `MarkdownContent`.

**Ask First:**
- Si al confirmar con valores editados hay un campo numérico con valor negativo (puede ser error de usuario).

**Never:**
- Llamar a la IA en Paso 1.
- Eliminar `POST /import` (flujo manual) ni `POST /{id}/confirm`.
- Modificar el comportamiento de `FundamentalsPreview.tsx` (se mantiene igual para el flujo manual).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output | Error Handling |
|----------|--------------|-----------------|----------------|
| Paso 1 exitoso | FibraId + Period válidos + PDF | Record creado, PDF guardado, MarkdownContent en BD, retorna `PdfUploadResultDto` | N/A |
| Paso 1 — FIBRA/Period ya procesado | Mismo FibraId+Period que record existente procesado | `IsPossibleUpdate=true`, `WarningMessage` en response | No bloquea |
| Paso 1 — PDF sin texto extraíble | PDF escaneado | MarkdownContent vacío, `markdownExtracted=false` en response | PipelineErrorLog |
| Paso 1 — error de disco | Falla al guardar el archivo | 500 + PipelineErrorLog | PipelineErrorLog |
| Paso 2 exitoso | Record con MarkdownContent | KPIs en BD, retorna `FundamentalRecordDto` con valores | N/A |
| Paso 2 — MarkdownContent vacío | Record sin markdown | 422 ValidationProblem | N/A |
| Paso 2 — IA no disponible | Falla kpiExtractor | 502 + PipelineErrorLog | PipelineErrorLog |
| PATCH kpis — edición antes de confirmar | id válido + valores editados | Record actualizado, retorna `FundamentalRecordDto` | N/A |

</frozen-after-approval>

## Code Map

**Backend:**
- `src/Server/Domain/Fundamentals/FundamentalRecord.cs:22` — `ErrorReason` tiene `init`, debe ser `set`
- `src/Server/Application/Fundamentals/IFundamentalRepository.cs` — agregar `UpdateKpisManualAsync`
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs:74` — `UpdateKpiExtractionAsync` guarda ExtractionNotes en FieldNotes actualmente; cambiar a `ErrorReason`
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs` — implementar `UpdateKpisManualAsync`
- `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs:134` — `POST /extract-kpis` (sin id) a eliminar
- `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs:377` — `POST /{id}/extract-kpis` agregar PipelineErrorLog
- `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` — nuevo `POST /upload-pdf` y `PATCH /{id}/kpis`
- `src/Server/SharedApiContracts/Fundamentals/` — nuevo `PdfUploadResultDto` y `PatchKpisRequest`

**Frontend:**
- `src/Web/Ops/src/api/fundamentalsApi.ts` — agregar `uploadPdfWithRecord`, `triggerKpiExtraction`, `patchKpis`; eliminar `extractKpisFromPdf`
- `src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx` — refactor flujo AI
- `scripts/codegen/Api.json` — regenerar tras cambios de backend

## Tasks & Acceptance

**Execution:**

- [ ] `src/Server/Domain/Fundamentals/FundamentalRecord.cs` -- Cambiar `ErrorReason` de `{ get; init; }` a `{ get; set; }` -- permite actualizar el campo tras la creación del record
- [ ] `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs` -- En `UpdateKpiExtractionAsync`: reemplazar `["extractionNotes"] = result.ExtractionNotes` en FieldNotes por `record.ErrorReason = result.ExtractionNotes` -- ExtractionNotes va a ErrorReason como acordado
- [ ] `src/Server/Application/Fundamentals/IFundamentalRepository.cs` -- Agregar `Task UpdateKpisManualAsync(Guid id, decimal? capRate, decimal? navPerCbfi, decimal? ltv, decimal? noiMargin, decimal? ffoMargin, decimal? quarterlyDistribution, string? summary, CancellationToken ct)` -- soporte para PATCH de valores editados por usuario
- [ ] `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs` -- Implementar `UpdateKpisManualAsync`: actualizar solo los campos KPI + Summary del record sin tocar Status ni FieldNotes -- el usuario edita valores, las notas IA quedan intactas
- [ ] `src/Server/SharedApiContracts/Fundamentals/PdfUploadResultDto.cs` -- Crear record `PdfUploadResultDto(Guid Id, string FibraTicker, string Period, bool MarkdownExtracted, bool IsPossibleUpdate, string? WarningMessage)` -- DTO de respuesta del Paso 1
- [ ] `src/Server/SharedApiContracts/Fundamentals/PatchKpisRequest.cs` -- Crear record `PatchKpisRequest` con los 6 KPIs decimales nullable + `Summary` string nullable -- DTO del body de PATCH /{id}/kpis
- [ ] `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` -- Eliminar el bloque completo `MapPost("/extract-kpis", ...)` (sin id, línea ~134) -- reemplazado por el nuevo flujo de dos pasos
- [ ] `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` -- Agregar `POST /upload-pdf` (recibe `IFormFile file`, `Guid fibraId`, `string period`): crear `FundamentalRecord` (processingMode="ai"), guardar PDF en disco, extraer markdown + `Compact()` + guardar en BD, registrar errores en `PipelineErrorLog`, retornar `PdfUploadResultDto` -- implementa el Paso 1
- [ ] `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` -- Agregar `PATCH /{id}/kpis` (recibe `PatchKpisRequest`): leer record, llamar `UpdateKpisManualAsync`, retornar `FundamentalRecordDto` actualizado -- permite guardar ediciones del usuario antes de confirmar
- [ ] `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` -- En `POST /{id}/extract-kpis`: inyectar `IPipelineErrorLogRepository`, envolver la llamada a `kpiExtractor.ExtractAsync` en try/catch que registre en `PipelineErrorLog` (Pipeline="Fundamentals") antes de devolver 502 -- errores IA quedan en bitácora
- [ ] `src/Web/Ops/src/api/fundamentalsApi.ts` -- Agregar `uploadPdfWithRecord(fibraId, period, file)` → `POST /upload-pdf` multipart; `triggerKpiExtraction(id)` → `POST /{id}/extract-kpis`; `patchKpis(id, values)` → `PATCH /{id}/kpis`; eliminar `extractKpisFromPdf` -- actualiza el cliente API al nuevo flujo
- [ ] `src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx` -- Refactorizar flujo AI: al click del botón con PDF → llamar `uploadPdfWithRecord` (Paso 1) → luego `triggerKpiExtraction` (Paso 2) → pre-llenar campos del form con los KPIs devueltos → cambiar botón a "Confirmar"; al confirmar con record ya creado → llamar `patchKpis` si hubo ediciones → llamar `confirmFundamentals`; mantener flujo manual (sin PDF) sin cambios -- implementa el nuevo UX en frontend
- [ ] `npm run codegen:api` (desde raíz del proyecto) -- Regenerar cliente TypeScript tras agregar los nuevos endpoints -- mantiene el cliente tipado en sync

**Acceptance Criteria:**

- Given FIBRA + Period + PDF válidos, when usuario sube PDF y hace click en "Importar", then se crea un record con `processingMode="ai"`, el PDF queda en disco y `MarkdownContent` con texto compactado queda en BD
- Given Paso 1 completado, when se llama a `POST /{id}/extract-kpis`, then los KPIs se guardan en el record y `ExtractionNotes` queda en `ErrorReason`
- Given IA falla en Paso 2, when el extractor lanza excepción, then se devuelve 502 Y la excepción queda registrada en `PipelineErrorLog` con Pipeline="Fundamentals"
- Given valores pre-llenados por IA, when usuario edita un KPI y confirma, then los valores editados se persisten antes del cambio de status a "processed"
- Given `POST /extract-kpis` (sin id), when cualquier cliente lo llama, then devuelve 404 (el endpoint fue eliminado)
- Given flujo manual (sin PDF), when usuario llena valores y hace click en "Importar y previsualizar", then el flujo existente (`POST /import`) se ejecuta sin cambios

## Design Notes

**Paso 1 — `POST /upload-pdf`:**  
Recibe `fibraId`, `period` como query params o form fields junto con el `IFormFile`. Genera un nuevo `Guid` para el record, guarda el PDF como `{id}.pdf`, extrae y compacta el markdown, persiste todo en una sola operación. Retorna `PdfUploadResultDto` que incluye el `id` para que el frontend encadene el Paso 2.

**PATCH /{id}/kpis:**  
Solo actualiza los 6 campos KPI + Summary. No toca `Status`, `FieldNotes` (notas IA), ni `ErrorReason`. El frontend llama a este endpoint solo si el usuario modificó algún valor antes de confirmar; si no hubo cambios puede llamar directamente a confirm.

**Flujo frontend — dos modos en `FundamentalsImportForm`:**
- Modo "import" (`pendingRecordId === null`): botón = "Importar y previsualizar", flujo normal o AI
- Modo "confirm" (`pendingRecordId !== null`): campos pre-llenados y editables, botón = "Confirmar", llama PATCH + confirm

## Verification

**Commands:**
- `dotnet build src/Server/Infrastructure/Infrastructure.csproj` -- expected: 0 errores
- `dotnet build src/Server/Api/Api.csproj` -- expected: 0 errores
- `dotnet test tests/Unit/Infrastructure.Tests/ --filter "FullyQualifiedName~MarkdownCompactor"` -- expected: 13 tests correctos (regresión)
- `npm run build --prefix src/Web/Ops` -- expected: 0 errores TypeScript

## Spec Change Log

