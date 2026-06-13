---
title: 'Banxico + DailySnapshot: dashboard ops y logging de pipeline'
type: 'feature'
created: '2026-06-12'
status: 'done'
baseline_commit: '8235ac5f6db7336d1c7009f2c19613d61ed1433f'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `BanxicoSyncJob`, `BanxicoMonthlySyncJob` y `DailySnapshotHistoricalJob` existen como procesos de fondo pero no aparecen en el dashboard operativo, no registran sus ejecuciones en `PipelineRunLog` ni sus errores en `PipelineErrorLog`, y no tienen botón "Ejecutar ahora" en Ops.

**Approach:** Añadir los tres procesos al dashboard operativo de Ops con el mismo patrón que `MarketPipelineJob` y `NewsPipelineJob`: logging de run (Queued al disparar manualmente, Completed/Failed al terminar el job), logging de errores, botones de ejecución en UI, y tarjetas en el dashboard con historial de últimas 5 corridas.

## Boundaries & Constraints

**Always:**
- Mismos nombres de pipeline en endpoints, jobs y dashboard: `"BanxicoSync"`, `"BanxicoInpc"`, `"DailySnapshot"`
- El bloque try/finally en jobs garantiza que `PipelineRunLog` siempre se escribe (incluso en crash inesperado)
- La escritura al log está envuelta en try/catch propio — un fallo al loggear no aborta el pipeline
- Los endpoints de Banxico ya existen en `OpsBanxicoEndpoints.cs`; solo agregar `TryLogQueuedRunAsync` (mismo helper que `OpsMarketEndpoints.cs`)
- `/daily-snapshot-historical/run` ya existe; agregarle `TryLogQueuedRunAsync` con pipeline `"DailySnapshot"`
- `DashboardPage.tsx`: usar colores no repetidos (violet para BanxicoSync, orange para BanxicoInpc, indigo para DailySnapshot)
- Después de modificar endpoints, ejecutar `npm run codegen:api` para regenerar el cliente tipado antes de tocar el frontend

**Ask First:**
- ¿La tarjeta de DailySnapshot debe mostrar `inserted` como `ItemsProcessed` (inserción incremental) o el total de items procesados (inserted + skipped)?

**Never:**
- No agregar nuevas migraciones de EF Core — no se añaden columnas ni tablas
- No cambiar la lógica de negocio de ningún job (solo añadir logging wrapper alrededor del código existente)
- No alterar `DailySnapshotReset` (pipeline name y endpoint separado que ya existe)
- No usar Task.WhenAll en BanxicoSyncJob (el HttpClient tipado no es thread-safe en el mismo job)

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Trigger manual Banxico CETES+TIIE | POST `/ops/banxico/sync-tiie/run` | 202, PipelineRunLog `"BanxicoSync"` status=Queued | TryLogQueuedRunAsync envuelto en try/catch |
| Trigger manual BanxicoInpc | POST `/ops/banxico/sync-inpc/run` | 202, PipelineRunLog `"BanxicoInpc"` status=Queued | Igual |
| Trigger manual DailySnapshot | POST `/ops/market/daily-snapshot-historical/run` | 202, PipelineRunLog `"DailySnapshot"` status=Queued | Igual |
| BanxicoSyncJob completa | Job ejecuta, cetes y tiie no son null | PipelineRunLog Completed, ItemsProcessed=2, ErrorCount=0 | |
| BanxicoSyncJob: una tasa nula | cetes OK, tiie=null | PipelineRunLog Completed, ItemsProcessed=1, ErrorCount=1; PipelineErrorLog entry | |
| BanxicoMonthlySyncJob: al día | `from > todayPeriod` (nada que sync) | PipelineRunLog Completed, ItemsProcessed=0, ErrorCount=0 | |
| BanxicoMonthlySyncJob: Banxico vacío | history.Count=0 | PipelineRunLog Completed, ErrorCount=1; PipelineErrorLog entry | |
| DailySnapshotHistoricalJob error de Yahoo | exception para una fibra | PipelineErrorLog entry por fibra; job continúa; ErrorCount acumula | |

</frozen-after-approval>

## Code Map

- `src/Server/Application/Jobs/BanxicoSyncJob.cs` — job que sincroniza CETES + TIIE 28d; añadir IPipelineRunLogRepository + IPipelineErrorLogRepository
- `src/Server/Application/Jobs/BanxicoMonthlySyncJob.cs` — job que sincroniza INPC mensual; mismo patrón
- `src/Server/Infrastructure/Jobs/Market/DailySnapshotHistoricalJob.cs` — job incremental de snapshots diarios; añadir logging de pipeline
- `src/Server/Api/Endpoints/Ops/OpsBanxicoEndpoints.cs` — endpoints `/sync-tiie/run` y `/sync-inpc/run`; añadir TryLogQueuedRunAsync
- `src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs` — endpoint `/daily-snapshot-historical/run`; añadir TryLogQueuedRunAsync (ya tiene el helper)
- `src/Server/Api/Endpoints/Ops/OpsDashboardEndpoints.cs` — Pipelines array; añadir `"BanxicoSync"`, `"BanxicoInpc"`, `"DailySnapshot"`
- `src/Web/Ops/src/api/dashboardApi.ts` — RunPipelineTarget; añadir 3 nuevos targets y sus casos en runPipeline
- `src/Web/Ops/src/pages/DashboardPage.tsx` — pipelineCards + mutationByTarget; añadir 3 nuevas tarjetas

## Tasks & Acceptance

**Execution:**
- [x] `src/Server/Application/Jobs/BanxicoSyncJob.cs` — añadir `IPipelineRunLogRepository` e `IPipelineErrorLogRepository` al constructor; envolver `ExecuteAsync` con try/finally que escribe PipelineRunLog (Completed/Failed, ItemsProcessed=successful updates 0-2, ErrorCount=null counts); por cada tasa nula escribir PipelineErrorLog con pipeline `"BanxicoSync"`
- [x] `src/Server/Application/Jobs/BanxicoMonthlySyncJob.cs` — mismo patrón; ItemsProcessed=entries.Count al hacer upsert (0 si ya al día); ErrorCount=1 si history.Count=0, 0 en otro caso; pipeline `"BanxicoInpc"`
- [x] `src/Server/Infrastructure/Jobs/Market/DailySnapshotHistoricalJob.cs` — añadir `IPipelineRunLogRepository` e `IPipelineErrorLogRepository`; envolver ExecuteAsync con try/finally; ItemsProcessed=inserted, ErrorCount=errors; pipeline `"DailySnapshot"`; por cada excepción de fibra escribir PipelineErrorLog con Context={ticker, yahooTicker}
- [x] `src/Server/Api/Endpoints/Ops/OpsBanxicoEndpoints.cs` — añadir a ambos endpoints los parámetros `IPipelineRunLogRepository runLogRepo, ILoggerFactory loggerFactory, IEmailEncryptor emailEncryptor, HttpContext ctx, CancellationToken ct`; llamar `TryLogQueuedRunAsync` (copiar el helper privado de `OpsMarketEndpoints.cs` o extraer a una clase compartida). Pipelines: `"BanxicoSync"` y `"BanxicoInpc"`
- [x] `src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs` — endpoint `/daily-snapshot-historical/run`: añadir los mismos parámetros que tienen los otros endpoints manuales y llamar `TryLogQueuedRunAsync("DailySnapshot", ...)`
- [x] `src/Server/Api/Endpoints/Ops/OpsDashboardEndpoints.cs` — agregar `"BanxicoSync"`, `"BanxicoInpc"`, `"DailySnapshot"` al array `Pipelines`
- [x] `npm run codegen:api` — regenerar cliente tipado; verificar que aparecen `/api/v1/ops/banxico/sync-tiie/run` y `/api/v1/ops/banxico/sync-inpc/run` en `paths`
- [x] `src/Web/Ops/src/api/dashboardApi.ts` — ampliar `RunPipelineTarget` con `'banxico-sync' | 'banxico-inpc' | 'daily-snapshot'`; añadir 3 casos en `runPipeline`
- [x] `src/Web/Ops/src/pages/DashboardPage.tsx` — añadir a `pipelineCards`: `{ name: 'BanxicoSync', target: 'banxico-sync', accent: 'from-violet-500/18 to-violet-100' }`, `{ name: 'BanxicoInpc', target: 'banxico-inpc', accent: 'from-orange-500/18 to-orange-100' }`, `{ name: 'DailySnapshot', target: 'daily-snapshot', accent: 'from-indigo-500/18 to-indigo-100' }`; añadir `useMutation` para cada uno; añadir a `mutationByTarget`

**Acceptance Criteria:**
- Dado que AdminOps hace clic en "Ejecutar ahora" en cualquier tarjeta de Banxico o DailySnapshot, cuando la petición procesa, entonces recibe 202, se crea PipelineRunLog con status=Queued y TriggeredBy con el email encriptado del actor
- Dado que `BanxicoSyncJob` completa, cuando ambas tasas se obtienen correctamente, entonces PipelineRunLog tiene Status=Completed, ItemsProcessed=2, ErrorCount=0
- Dado que `BanxicoSyncJob` no obtiene una tasa, cuando tiie o cetes es null, entonces PipelineRunLog tiene ErrorCount≥1 y hay un PipelineErrorLog entry con Pipeline=BanxicoSync
- Dado que `BanxicoMonthlySyncJob` ya está al día, cuando ejecuta y `from > todayPeriod`, entonces PipelineRunLog Status=Completed, ItemsProcessed=0, ErrorCount=0 (no hay PipelineErrorLog)
- Dado que `DailySnapshotHistoricalJob` falla para una fibra individual, cuando la excepción se captura por fibra, entonces hay un PipelineErrorLog entry con Pipeline=DailySnapshot y el job continúa con las demás fibras
- Dado que el dashboard carga, cuando existen runs de BanxicoSync/BanxicoInpc/DailySnapshot, entonces sus tarjetas muestran estado derivado, última ejecución y últimas 5 corridas en la tabla

## Spec Change Log

- patch post-review: corregido ID de serie TIIE en mensaje de error (SF43783 → SF60542) en `BanxicoSyncJob.cs:45`

## Design Notes

**TryLogQueuedRunAsync en OpsBanxicoEndpoints:** El helper privado actual está en `OpsMarketEndpoints.cs`. En lugar de duplicarlo, moverlo a una clase estática interna compartida o simplemente duplicarlo — dado que son dos archivos de endpoints de Ops, la duplicación es aceptable para evitar una abstracción prematura.

**ItemsProcessed para BanxicoSyncJob:** Se cuenta el número de tasas efectivamente actualizadas (0, 1 o 2). Una tasa nula es un error porque indica que Banxico no respondió correctamente para esa serie.

**PipelineErrorLog para rates nulas vs excepciones:** Una tasa nula (resultado de `LogWarning` en el job actual) se convierte en un PipelineErrorLog entry porque la tarjeta de dashboard necesita ErrorCount > 0 para mostrar estado "Fallando". El AiContext describe la serie que falló (CETES SF43783 o TIIE SF60542).

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: 0 errors
- `npm run codegen:api` (desde raíz del repo) -- expected: regenera `SharedApiClient` sin errores
- `npm run dev:ops` -- expected: inicia en puerto 5174 sin errores de compilación TypeScript
