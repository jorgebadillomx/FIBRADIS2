# Historia 5.11: Ingesta incremental automática de PDFs AMEFIBRA

Status: done

## Story

Como AdminOps,
quiero que el sistema descubra y procese automáticamente los reportes trimestrales nuevos publicados en AMEFIBRA por FIBRA,
para que el flujo de fundamentales deje de depender de una descarga/carga manual y solo procese novedades reales.

## Acceptance Criteria

### AC1 — Discovery AMEFIBRA extrae metadatos mínimos por reporte

**Dado que** el pipeline consulta `https://amefibra.com/reportes-de-fibras/`,
**Cuando** procesa una tarjeta o detalle de reporte,
**Entonces** obtiene y persiste al menos:
- `fibra`
- `period`
- `reportType`
- `packageUrl`
- `downloadUrl`
- `pdfUrl`
- `fileName`
- `sourcePublishedAt` o fecha equivalente disponible

**Y** el parser soporta el patrón actual del portal:
- listado paginado por `?cp=N`
- páginas detalle `/download/...`
- botón `Descargar` con `data-downloadurl`
- redirección `302` al PDF real en `wp-content/uploads/...`

### AC2 — El pipeline solo crea trabajo nuevo por FIBRA cuando detecta novedad

**Dado que** ya existen registros o manifiestos previos para una FIBRA,
**Cuando** el discovery vuelve a escanear AMEFIBRA,
**Entonces** no reprocesa todo el histórico;
**y** solo genera trabajo nuevo cuando detecta alguno de estos casos:
- período no registrado para esa FIBRA
- mismo período pero archivo distinto, marcado como `possibleUpdate`
- entrada nueva en el portal con `packageUrl` o `pdfUrl` no vistos

**Y** el pipeline puede seguir leyendo el índice central completo del portal,
pero la descarga y extracción pesada solo ocurre para novedades.

### AC3 — La identificación de período es determinística y rechaza ambiguos

**Dado que** el título del portal mezcla variantes como `2021 Reporte T3 Fibra Inn`, `Reporte Anual`, o nombres con sufijos de bolsa,
**Cuando** el parser intenta normalizar el período,
**Entonces**:
- los trimestrales válidos se convierten a formato interno `Q1-2024`, `Q2-2024`, etc.
- los anuales quedan clasificados explícitamente como `annual` y no entran al flujo trimestral por defecto
- cualquier título ambiguo queda en estado `error` o `pending-classification`, con observabilidad suficiente para revisión manual

### AC4 — El flujo automático reutiliza el motor vigente de fundamentales

**Dado que** el sistema ya cuenta con `FundamentalRecord`, almacenamiento de PDF, extracción de markdown y extracción de KPIs,
**Cuando** el pipeline detecta un PDF nuevo elegible,
**Entonces** reutiliza el mismo motor de persistencia del módulo `Fundamentals`
**y no** crea una segunda vía paralela de datos.

**Y** como mínimo preserva:
- guardado del PDF en el mismo esquema/ruta de fundamentales
- `MarkdownContent`
- `PdfReference`
- `ProcessingMode`
- `Status`
- trazabilidad de fuente y período

### AC5 — Integración con Ops y observabilidad

**Dado que** el proceso es automático,
**Cuando** corre el pipeline de discovery/procesamiento,
**Entonces** AdminOps puede ver:
- última corrida
- FIBRAs escaneadas
- reportes detectados
- reportes nuevos
- reportes omitidos por ya conocidos
- posibles updates
- errores por parseo, descarga o extracción

**Y** las fallas relevantes quedan registradas en observabilidad operativa (`PipelineRunLog` y/o `PipelineErrorLog`) con `Pipeline = "Fundamentals"`.

### AC5b — Las llamadas a IA quedan auditadas en el log existente

**Dado que** el flujo automático usa extracción o análisis con IA sobre un PDF detectado,
**Cuando** se invoca el proveedor configurado,
**Entonces** cada llamada queda registrada en el mecanismo existente de observabilidad de IA (`AiCallLog`),
**incluyendo** al menos proveedor, modelo, tipo de operación, resultado y referencia suficiente al `FundamentalRecord` o contexto procesado.

### AC6 — El flujo público y operativo conserva consistencia

**Dado que** el pipeline crea o actualiza un registro de fundamentales,
**Cuando** la corrida termina,
**Entonces** la historia por FIBRA en Ops sigue siendo visible en `FundamentalsHistory`
**y** la superficie pública consume el mismo registro final confirmado/publicado por el módulo de fundamentales, sin duplicados de período.

## Tasks / Subtasks

- [x] Definir modelo de source manifest para AMEFIBRA por FIBRA/período/archivo
  - [x] Decidir si el manifiesto vive como tabla nueva de `fundamentals` o como extensión de `FundamentalRecord`
  - [x] Persistir `packageUrl`, `pdfUrl`, `fileName` y fingerprint suficiente para deduplicación

- [x] Implementar integración `Infrastructure/Integrations/PdfDiscovery` para AMEFIBRA
  - [x] Parser de listado paginado
  - [x] Resolución de `data-downloadurl`
  - [x] Resolución del `302` al PDF real
  - [x] Normalización de título a `fibra`, `period`, `reportType`

- [x] Implementar job incremental de fundamentales automáticos
  - [x] Escaneo configurable
  - [x] Comparación contra manifiesto/records existentes
  - [x] Descarga solo de novedades
  - [x] Creación de records en el flujo actual de `Fundamentals`

- [x] Integrar con el pipeline operativo y dashboard
  - [x] Exponer corrida manual `Run now`
  - [x] Mostrar métricas del pipeline en Ops
  - [x] Registrar errores y decisiones de skip
  - [x] Registrar también todas las invocaciones a IA en el log existente de llamadas IA

- [x] Definir política de publicación del resultado
  - [x] Auto-publicar si extracción exitosa
  - [x] O dejar `pending` para confirmación humana
  - [x] O modelo híbrido según calidad/confianza

- [x] Cubrir con tests
  - [x] Unit tests del parser de AMEFIBRA
  - [x] Integration tests del flujo incremental
  - [x] Tests de no-regresión sobre `FundamentalRecord` por período

## Dev Notes

### Foundation

- El PRD ya contempla el modo `Api` para fundamentales: detección config-driven de PDFs nuevos, descarga, extracción IA y actualización del histórico por período. [Source: docs/req/prd.md#FR-18, FR-20, FR-21]
- El flujo vigente manual ya existe en backend y frontend Ops; esta historia debe extenderlo, no reemplazarlo. [Source: _bmad-output/implementation-artifacts/5-2-importacion-de-fundamentales-en-modo-manual.md]
- La arquitectura reserva explícitamente `Infrastructure/Integrations/PdfDiscovery` para este tipo de integración. [Source: docs/req/architecture.md#Integration Points, docs/req/architecture.md#Requirements to Structure Mapping]

### Current State To Preserve

- [src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs](C:/Users/jorge/source/repos/FIBRADIS/src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs) ya maneja:
  - `POST /upload-pdf`
  - `POST /{id}/extract-kpis`
  - `POST /{id}/confirm`
  - `GET /{id}/pdf`
  - historial por `fibraId`
- El flujo actual guarda PDF, extrae markdown, persiste KPIs y mantiene `possibleUpdate`; cualquier automatización debe reutilizar esas reglas en vez de duplicarlas.

- [src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx](C:/Users/jorge/source/repos/FIBRADIS/src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx) ya soporta:
  - importación manual sin PDF
  - upload de PDF + IA
  - confirmación de `possibleUpdate`
- El flujo automático no debe romper este formulario ni requerir otra semántica de período.

- [src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs](C:/Users/jorge/source/repos/FIBRADIS/src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs) y [src/Server/Domain/Ops/OperationalConfig.cs](C:/Users/jorge/source/repos/FIBRADIS/src/Server/Domain/Ops/OperationalConfig.cs) hoy manejan cadencia de noticias y otros parámetros operativos, pero no existe todavía configuración dedicada para PDFs/fuentes de fundamentales.

- [src/Web/Ops/src/pages/DashboardPage.tsx](C:/Users/jorge/source/repos/FIBRADIS/src/Web/Ops/src/pages/DashboardPage.tsx) hoy solo presenta `Market`, `News` y `Distribution`. Si este flujo se implementa como pipeline real, el dashboard deberá extenderse sin degradar esa UX.

### Suggested Implementation Shape

- Crear un origen AMEFIBRA explícito en `PdfDiscovery`, no un scraper genérico sin contrato.
- Separar estas responsabilidades:
  - `discover`: leer portal, detectar candidatos, normalizar metadatos
  - `decide`: comparar contra histórico/manifiesto y decidir `new`, `skip`, `possibleUpdate`, `error`
  - `ingest`: descargar PDF y crear/actualizar `FundamentalRecord`
  - `process`: extracción markdown + KPIs usando el motor existente

- La clave incremental no debe depender solo del nombre de archivo:
  - usar combinación de `fibra + period + reportType + pdfUrl/packageUrl`
  - si el mismo período aparece con otro PDF, registrar `possibleUpdate`

- El parser de AMEFIBRA debe apoyarse en el HTML real observado:
  - el listado ya contiene el `package title`
  - el detalle expone `data-downloadurl`
  - `data-downloadurl` redirige `302` al PDF final

### Constraints

- No agregar una segunda tabla pública de fundamentales ni una ruta pública nueva solo para AMEFIBRA.
- Mantener SQL Server como fuente de verdad y `FundamentalRecord` como registro canónico consumido por Main y Ops. [Source: AGENTS.md reglas críticas 1, 2, 3, 5]
- Toda acción manual posterior de Ops sobre este flujo debe quedar auditada. [Source: AGENTS.md regla crítica 6, docs/req/prd.md#SC-10]
- Si el pipeline corre en background, debe seguir el patrón de jobs persistentes y observabilidad ya usado en el proyecto. [Source: docs/req/architecture.md#Architectural Boundaries, _bmad-output/implementation-artifacts/5-1-dashboard-operativo-y-control-de-pipelines.md]
- Cualquier llamada a IA que haga este flujo debe reutilizar el log existente de observabilidad de IA; no crear un sistema paralelo de auditoría de prompts/respuestas.

### Project Structure Notes

- Backend probable:
  - `src/Server/Application/Fundamentals/`
  - `src/Server/Infrastructure/Integrations/PdfDiscovery/`
  - `src/Server/Infrastructure/Jobs/Fundamentals/` o equivalente
  - `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/`
  - `src/Server/Api/Endpoints/Ops/`

- Frontend Ops probable:
  - `src/Web/Ops/src/pages/`
  - `src/Web/Ops/src/modules/fundamentals/`
  - `src/Web/Ops/src/api/`

- Si se requiere configuración por fuente o por FIBRA, el lugar correcto es el módulo Ops/config operativa o una pantalla dedicada `pdf-config`, no incrustarlo en Main.

### Testing Requirements

- Unit:
  - parseo de títulos AMEFIBRA
  - detección de trimestre vs anual
  - resolución de `data-downloadurl`
  - clasificación `new/skip/possibleUpdate/error`

- Integration:
  - corrida incremental con reporte ya conocido
  - corrida incremental con período nuevo
  - mismo período con archivo distinto
  - error de descarga
  - error de parseo de período

- Regression:
  - `FundamentalsHistory` sigue mostrando registros manuales y automáticos
  - el endpoint público de fundamentales no duplica período vigente

### Open Questions / To Discuss

1. ¿El automático debe **publicar directo** al terminar, o debe dejar el registro en `pending` para confirmación humana?
2. ¿El alcance inicial es **solo AMEFIBRA** o debe respetar una futura configuración por FIBRA/fuente desde `pdf-config` desde el primer release?
3. ¿Se deben procesar **solo trimestrales** en esta historia y dejar anuales fuera, o los anuales también deben almacenarse aunque no alimenten KPIs?
4. Para `possibleUpdate`, ¿quieres que el sistema:
   - archive el registro anterior automáticamente,
   - cree work item para revisión,
   - o solo lo deje visible en Ops sin publicar?
5. ¿La detección incremental se dispara:
   - por schedule propio de fundamentos,
   - desde `Run now`,
   - o encadenada a otro pipeline existente?

## References

- [docs/req/prd.md](C:/Users/jorge/source/repos/FIBRADIS/docs/req/prd.md)
- [docs/req/architecture.md](C:/Users/jorge/source/repos/FIBRADIS/docs/req/architecture.md)
- [_bmad-output/planning-artifacts/epics.md](C:/Users/jorge/source/repos/FIBRADIS/_bmad-output/planning-artifacts/epics.md)
- [_bmad-output/implementation-artifacts/5-2-importacion-de-fundamentales-en-modo-manual.md](C:/Users/jorge/source/repos/FIBRADIS/_bmad-output/implementation-artifacts/5-2-importacion-de-fundamentales-en-modo-manual.md)
- [_bmad-output/implementation-artifacts/spec-fundamentals-pdf-first-flow.md](C:/Users/jorge/source/repos/FIBRADIS/_bmad-output/implementation-artifacts/spec-fundamentals-pdf-first-flow.md)
- [project-memory/seeds/02-modules.md](C:/Users/jorge/source/repos/FIBRADIS/project-memory/seeds/02-modules.md)
- [project-memory/seeds/04-api-patterns.md](C:/Users/jorge/source/repos/FIBRADIS/project-memory/seeds/04-api-patterns.md)

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Verificación real del portal AMEFIBRA el 2026-05-31: listado paginado `?cp=N`, fichas `/download/...`, ancla `a.wpdm-download-link`, `data-downloadurl` con parámetro transitorio `refresh`, y respuestas tanto `302` como `200 application/pdf`.
- Ajuste del flujo incremental para reutilizar `FundamentalRecord`, `MarkdownContent`, extracción IA y `AiCallLog`, sin crear un camino paralelo de persistencia.
- Corrección incidental de build en Ops: `AiModeSection.tsx` importaba de forma incompleta `setAiConfig`.

### Completion Notes List

- Se agregó `FundamentalSourceManifest` en esquema `fundamentals` para deduplicar por `packageUrl` y signature de descarga normalizada, preservando trazabilidad de detección/decisión por reporte.
- Se implementó `AmefibraDiscoveryClient` con sesión HTTP dedicada, warmup del portal y parser determinístico de títulos para clasificar `quarterly`, `annual` y `pending-classification`.
- Se agregó `FundamentalsAutomationService` y `FundamentalsPipelineJob` con schedule configurable desde `OperationalConfig`, endpoint `Run now`, métricas en dashboard y filtro de logs `Pipeline = "Fundamentals"`.
- Política aplicada: auto-confirmar cuando la extracción y KPIs regresan `Success`; anuales o títulos ambiguos quedan registrados en manifiesto pero no entran al flujo trimestral automático.
- Se generó la migración `20260531195830_AddFundamentalsAutomationManifest`, se regeneró `SharedApiClient` y se validó `dotnet build`, tests unitarios/integración relevantes y builds de `Main` y `Ops`.

### File List

- `_bmad-output/implementation-artifacts/5-11-amefibra-pdf-sync.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `scripts/codegen/Api.json`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs`
- `src/Server/Api/Endpoints/Ops/OpsDashboardEndpoints.cs`
- `src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Application/Fundamentals/AmefibraDiscoveryModels.cs`
- `src/Server/Application/Fundamentals/FundamentalsAutomationRunResult.cs`
- `src/Server/Application/Fundamentals/IAmefibraDiscoveryClient.cs`
- `src/Server/Application/Fundamentals/IFundamentalSourceManifestRepository.cs`
- `src/Server/Application/Fundamentals/IFundamentalsAutomationService.cs`
- `src/Server/Application/Ops/IOperationalConfigRepository.cs`
- `src/Server/Domain/Fundamentals/FundamentalSourceManifest.cs`
- `src/Server/Domain/Ops/OperationalConfig.cs`
- `src/Server/Infrastructure/Integrations/PdfDiscovery/AmefibraDiscoveryClient.cs`
- `src/Server/Infrastructure/Integrations/PdfDiscovery/AmefibraTitleParser.cs`
- `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs`
- `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsPipelineJob.cs`
- `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsPipelineSchedule.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260531195830_AddFundamentalsAutomationManifest.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260531195830_AddFundamentalsAutomationManifest.Designer.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalSourceManifestRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Fundamentals/FundamentalSourceManifestConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs`
- `src/Server/SharedApiContracts/Ops/OperationalConfigDto.cs`
- `src/Server/SharedApiContracts/Ops/UpdateOperationalConfigRequest.cs`
- `src/Web/Ops/src/api/dashboardApi.ts`
- `src/Web/Ops/src/api/pipelineLogsApi.ts`
- `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx`
- `src/Web/Ops/src/pages/ConfigPage.tsx`
- `src/Web/Ops/src/pages/DashboardPage.tsx`
- `src/Web/Ops/src/pages/PipelineLogsPage.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Fixtures/amefibra-sample.pdf`
- `tests/Integration/Api.Tests/Ops/DashboardEndpointTests.cs`
- `tests/Integration/Api.Tests/Ops/OpsConfigEndpointTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/PdfDiscovery/AmefibraTitleParserTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Fundamentals/FundamentalsAutomationServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/OperationalConfigRepositoryTests.cs`

## Change Log

- 2026-05-31 — Implementada historia 5.11: discovery incremental AMEFIBRA, manifiesto de fuentes, job/schedule de fundamentales, observabilidad Ops, cadencia configurable, endpoint manual y cobertura de tests/builds.

## Senior Developer Review (AI)

### Review Findings

**Patches:**

- [x] \[Review\]\[Patch\] P1 (HIGH) — Null dereference potencial en `TryLogPipelineErrorAsync`: parámetro `Fibra fibra` no-nullable pero `fibra.Id`/`fibra.Ticker` se acceden sin null-check; si un futuro caller pasa null, la excepción original se pierde silenciosamente dentro del catch. `FundamentalsAutomationService.cs:TryLogPipelineErrorAsync`
- [x] \[Review\]\[Patch\] P2 (HIGH) — `WarmupAsync` invocado en cada operación pública (`GetListingItemsAsync`, `GetPackageDetailsAsync`, `DownloadPdfAsync`): genera N×2+1 HEAD requests innecesarios y aborta todo el pipeline si AMEFIBRA responde con 405/403 a HEAD. Hacer el warmup lazy (una sola vez) y tolerante a fallos. `AmefibraDiscoveryClient.cs:WarmupAsync`
- [x] \[Review\]\[Patch\] P3 (HIGH) — `.GetAwaiter().GetResult()` en `GetPageCount`/`ParseListingItems`: bloquea sincrónicamente un thread del thread pool de Hangfire; riesgo de deadlock y saturación de workers con N páginas. Convertir a `async Task<>` con `await`. `AmefibraDiscoveryClient.cs:GetPageCount,ParseListingItems`
- [x] \[Review\]\[Patch\] P4 (MEDIUM) — Migración `AddFundamentalsAutomationManifest` con `defaultValue: 0` en `fundamentals_cadence_minutes`: cualquier fila con id != 1 quedaría con cadencia 0; cambiar a `defaultValue: 360` para consistencia con el seed. `20260531195830_AddFundamentalsAutomationManifest.cs:AddColumn`
- [x] \[Review\]\[Patch\] P5 (MEDIUM) — `IngestAsync` escribe el PDF a disco (`SavePdfAsync`) antes de insertar el record en BD (`AddAsync`): si la BD falla, el archivo queda huérfano sin registro. Invertir el orden: pre-computar la ruta, insertar record en BD, luego escribir el archivo. `FundamentalsAutomationService.cs:IngestAsync`
- [x] \[Review\]\[Patch\] P6 (MEDIUM) — `manifestRepo.AddAsync(manifest, ct)` post-catch usa el CT original: si el CT fue cancelado (timeout Hangfire, Run now cancelado), esta llamada lanza OCE fuera del try/catch y el run termina como "Failed" con estado inconsistente. Usar `CancellationToken.None` al igual que el `PipelineRunLog` en el finally. `FundamentalsAutomationService.cs:ExecuteAsync`
- [x] \[Review\]\[Patch\] P7 (MEDIUM) — `ShortYearRegex` `(?<!\d)(?<yy>\d{2})(?!\d)` convierte cualquier número de 2 dígitos en año (ej: "50" en un título → 2050): títulos AMEFIBRA con números no-año producirán períodos incorrectos que pasarán validación. Acotar el rango a 2018–2035 o exigir contexto previo (espacio + T/Q). `AmefibraTitleParser.cs:TryGetYear`
- [x] \[Review\]\[Patch\] P8 (MEDIUM) — `AiCallLog` no incluye referencia al `FundamentalRecord` (recordId, FibraId, Period): AC5b exige "referencia suficiente al FundamentalRecord o contexto procesado" para correlacionar llamadas IA con el registro que las disparó. `FundamentalsAutomationService.cs:IngestAsync`
- [x] \[Review\]\[Patch\] P9 (LOW) — Test faltante para el caso donde `GetListingItemsAsync` retorna lista vacía: el `PipelineRunLog` debería quedar con `ReportsDetected = 0` y `status = "success"`, no lanzar excepción. `FundamentalsAutomationServiceTests.cs`

**Deferred:**

- [x] \[Review\]\[Defer\] D1 (MEDIUM) — `DownloadPdfAsync` materializa el PDF completo en `byte[]` con `ReadAsByteArrayAsync` pese a usar `ResponseHeadersRead`: presión de memoria con PDFs grandes al procesar múltiples corridas concurrentes. `AmefibraDiscoveryClient.cs:DownloadPdfAsync`
- [x] \[Review\]\[Defer\] D2 (MEDIUM) — Inconsistencia estado manifest/record en error parcial: si `IngestAsync` falla después de `fundamentalRepo.AddAsync` (record ya en BD), el manifest queda con `LastDecision = "error"` y sin `LastProcessedRecordId`; corridas futuras lo marcarán como "skip" indefinidamente. Requiere transacción. `FundamentalsAutomationService.cs:ExecuteAsync,IngestAsync`
- [x] \[Review\]\[Defer\] D3 (LOW) — `GetCronExpression` devuelve default `"0 */6 * * *"` silenciosamente para valores no reconocidos en lugar de loggear warning. `FundamentalsPipelineSchedule.cs:GetCronExpression`
- [x] \[Review\]\[Defer\] D4 (MEDIUM) — `GetLatestByFibraAndPeriodAsync` filtra solo `reportType == "quarterly"`: `possibleUpdate` no detecta reportes anuales con distinto packageUrl; puede causar violación de unique constraint. `FundamentalSourceManifestRepository.cs:GetLatestByFibraAndPeriodAsync`
- [x] \[Review\]\[Defer\] D5 (LOW) — Manifiestos en path de skip no intentan hidratar `SourcePublishedAt` si quedó null en corrida anterior. `FundamentalsAutomationService.cs:ExecuteAsync`
- [x] \[Review\]\[Defer\] D6 (LOW) — Sin tests de regresión para `FundamentalsHistory` (Ops) y endpoint público (Main) con registros `ImportedBy = "system:amefibra"`. Agregar en próxima historia del módulo Fundamentals.
