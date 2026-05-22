# Story 3.5: DailySnapshot histórico e idempotente + retención de PriceSnapshot

Status: done

## Story

Como AdminOps,
quiero que el sistema rellene el histórico de precios diarios (5 años) desde Yahoo Finance de forma idempotente, y que `PriceSnapshot` conserve únicamente los registros de los últimos 2 días,
para que las gráficas de la ficha pública tengan datos desde el primer día de operación y la tabla de snapshots no crezca indefinidamente.

## Acceptance Criteria

1. **AC1 — Limpieza PriceSnapshot (retención 2 días):**
   Dado que `MarketPipelineJob` termina su ejecución normal, cuando persiste los snapshots del ciclo, entonces elimina automáticamente todos los registros de `market.PriceSnapshot` cuyo `captured_at` corresponda a una fecha anterior a ayer (UTC).

2. **AC2 — Nuevo job `DailySnapshotHistoricalJob`:**
   Dado que AdminOps dispara el job manualmente, cuando se ejecuta por primera vez con todas las fibras activas, entonces inserta registros de `market.DailySnapshot` para los últimos 5 años por cada fibra, sin errores de duplicado.

3. **AC3 — Idempotencia del job histórico:**
   Dado que el job ya se ejecutó anteriormente, cuando se vuelve a ejecutar, entonces no inserta duplicados (UX_DailySnapshot_FibraId_Date impide colisiones) y el log reporta `inserted=0, skipped=N`.

4. **AC4 — Valores nulos / cero en candles de Yahoo:**
   Dado que Yahoo Finance retorna un candle donde `Open`, `High` o `Low` son cero (baja liquidez), cuando el job procesa ese candle, entonces sustituye el campo cero por el valor de `Close` del mismo candle. Si `Close` también es cero, el candle se descarta.

5. **AC5 — Falla aislada por fibra:**
   Dado que Yahoo falla al obtener candles de una fibra específica, cuando el job procesa el resto de fibras, entonces esas fibras se procesan correctamente y el error de la fibra fallida queda registrado en el log sin detener el job.

6. **AC6 — Endpoint manual Ops:**
   Dado que AdminOps hace `POST /api/v1/ops/market/daily-snapshot-historical/run` (auth `AdminOps`), entonces el job se encola en Hangfire y la respuesta retorna `202 Accepted`.

7. **AC7 — Sin cron automático (DailySnapshotHistoricalJob):**
   El job histórico NO tiene `RecurringJob.AddOrUpdate`; solo se ejecuta por trigger manual.

8. **AC8 — Endpoint manual para DistributionPipelineJob:**
   Dado que AdminOps hace `POST /api/v1/ops/market/distribution/run` (auth `AdminOps`), entonces el job se encola en Hangfire y la respuesta retorna `202 Accepted`. El cron automático diario existente (`"0 6 * * *"`) se conserva sin cambios.

## Tasks / Subtasks

- [x] **T1 — YahooOhlcvResult record** (AC2, AC4)
  - [x] Crear `src/Server/Infrastructure/Integrations/Yahoo/YahooOhlcvResult.cs`
  - [x] Record: `(DateOnly Date, decimal Open, decimal High, decimal Low, decimal Close, long Volume)`

- [x] **T2 — IYahooFinanceClient: GetOhlcvHistoryAsync** (AC2)
  - [x] Agregar método a `src/Server/Infrastructure/Integrations/Yahoo/IYahooFinanceClient.cs`
  - [x] Firma: `Task<IReadOnlyList<YahooOhlcvResult>> GetOhlcvHistoryAsync(string yahooTicker, DateOnly from, CancellationToken ct = default)`

- [x] **T3 — YahooFinanceClient: implementar GetOhlcvHistoryAsync** (AC2, AC4)
  - [x] Implementar en `src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs`
  - [x] Usar `historyClient.Inner.GetHistoryAsync(yahooTicker)` → extraer `result.Value.Candles`
  - [x] Filtrar: descartar candles donde `Close == 0`
  - [x] Carry-forward: si `Open/High/Low == 0`, usar `Close`
  - [x] Filtrar por fecha `>= cutoff` y ordenar por `Date`

- [x] **T4 — IMarketRepository: DeleteOldPriceSnapshotsAsync** (AC1)
  - [x] Agregar a `src/Server/Application/Market/IMarketRepository.cs`
  - [x] Firma: `Task DeleteOldPriceSnapshotsAsync(DateOnly cutoff, CancellationToken ct = default)`

- [x] **T5 — MarketRepository: implementar DeleteOldPriceSnapshotsAsync** (AC1)
  - [x] Implementar en `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs`
  - [x] `DELETE FROM market.PriceSnapshot WHERE CAST(captured_at AS DATE) < cutoff`
  - [x] Usar `ExecuteDeleteAsync` (EF Core bulk delete, sin cargar entidades)

- [x] **T6 — MarketPipelineJob: agregar limpieza al final** (AC1)
  - [x] Al final de `ExecuteAsync`, antes del log final, llamar `DeleteOldPriceSnapshotsAsync`
  - [x] `cutoff = DateOnly.FromDateTime(timeService.UtcNow.UtcDateTime)` (borra todo antes de hoy; conserva hoy y ayer)
  - [x] Encapsular en try/catch para no abortar si falla la limpieza; log warning si falla

- [x] **T7 — DailySnapshotHistoricalJob** (AC2-AC5)
  - [x] Crear `src/Server/Infrastructure/Jobs/Market/DailySnapshotHistoricalJob.cs`
  - [x] Patrón idéntico a `DistributionPipelineJob`
  - [x] `historyStart = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5))`
  - [x] Por fibra: `GetOhlcvHistoryAsync(ticker, historyStart)` → loop candles → `UpsertDailySnapshotAsync`
  - [x] Contadores: `inserted` / `skipped` / `errors` (inferir inserted/skipped del comportamiento de Upsert)
  - [x] `[DisableConcurrentExecution(timeoutInSeconds: 0)]`

- [x] **T8 — Registro en DI y endpoint Ops** (AC6, AC7)
  - [x] `ApiServiceExtensions.cs`: `builder.Services.AddScoped<DailySnapshotHistoricalJob>()`
  - [x] `Program.cs`: SIN `RecurringJob.AddOrUpdate`
  - [x] Agregar endpoint en `src/Server/Api/Endpoints/Ops/` (o archivo existente de Ops endpoints):
    `POST /api/v1/ops/market/daily-snapshot-historical/run` → `BackgroundJob.Enqueue<DailySnapshotHistoricalJob>(...)`
  - [x] Proteger con `RequireAuthorization("AdminOps")`
  - [x] Retornar `202 Accepted`

- [x] **T10 — Endpoint manual para DistributionPipelineJob** (AC8)
  - [x] En `src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs` (ya existe por T8), agregar:
    `POST /api/v1/ops/market/distribution/run` → `BackgroundJob.Enqueue<DistributionPipelineJob>(...)`
  - [x] Proteger con `RequireAuthorization("AdminOps")`
  - [x] Retornar `202 Accepted`
  - [x] El cron `"0 6 * * *"` en `Program.cs` se mantiene intacto

- [x] **T9 — Unit tests** (AC1-AC5)
  - [x] `DailySnapshotHistoricalJobTests.cs`:
    - `WhenYahooReturnsCandles_UpsertsAllSnapshots`
    - `WhenCandlesAlreadyExist_ReportsSkipped` (fake Upsert devuelve false)
    - `WhenCandleHasZeroClose_DiscardedCandle`
    - `WhenCandleHasZeroOpen_UsesCloseAsFallback`
    - `WhenOneTickerFails_OtherTickersAreProcessed`
    - `WhenNoActiveFibras_DoesNotCallYahoo`
  - [x] `MarketPipelineJobTests.cs`: agregar test que verifica que `DeleteOldPriceSnapshotsAsync` se llama con la fecha correcta

## Dev Notes

### Contexto previo crítico (historia 3.4)

La historia 3.4 implementó `DistributionPipelineJob` que es el patrón exacto a replicar. Todos los fakes, la estructura del job, y la forma de registro en DI están ya establecidos en:
- `src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs`

El agente debe usar esos archivos como referencia directa.

### Yahoo Finance — API de candles OHLCV

La biblioteca `YahooQuotesApi` (ya registrada como singleton `YahooQuotesHistory`) devuelve el mismo objeto para dividendos y candles. El método `GetHistoryAsync(ticker)` retorna `Security` con:
- `result.Value.Dividends` → ya usado en `GetDividendHistoryAsync`
- `result.Value.Candles` → `ImmutableArray<Candle>` — **usar aquí**

Estructura de `Candle` (YahooQuotesApi):
- `Date` (`NodaTime.Instant`) → convertir con `.ToDateTimeUtc()`
- `Open`, `High`, `Low`, `Close`, `AdjustedClose` → `decimal`
- `Volume` → `long`

**IMPORTANTE**: Usar `Close`, no `AdjustedClose`, para DailySnapshot (consistencia con los datos intraday que ya se almacenan).

La instancia `YahooQuotesHistory` singleton ya está configurada con `WithHistoryStartDate(2020-01-01)`, que cubre los 5 años requeridos desde hoy (2021-05-21 en adelante).

```csharp
// Implementación GetOhlcvHistoryAsync en YahooFinanceClient
if (historyClient is null) return [];

var result = await historyClient.Inner.GetHistoryAsync(yahooTicker);
if (!result.HasValue) return [];

var candles = result.Value.Candles;
if (candles.IsDefaultOrEmpty) return [];

var cutoff = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

return candles
    .Where(c => c.Date.ToDateTimeUtc() >= cutoff && c.Close > 0)
    .Select(c => new YahooOhlcvResult(
        DateOnly.FromDateTime(c.Date.ToDateTimeUtc()),
        c.Open > 0 ? c.Open : c.Close,
        c.High > 0 ? c.High : c.Close,
        c.Low > 0 ? c.Low : c.Close,
        c.Close,
        c.Volume))
    .OrderBy(c => c.Date)
    .ToList();
```

### PriceSnapshot — limpieza con EF Core ExecuteDeleteAsync

No cargar entidades para borrar. Usar la API de bulk delete de EF Core 7+:

```csharp
public async Task DeleteOldPriceSnapshotsAsync(DateOnly cutoff, CancellationToken ct = default)
{
    var cutoffDt = cutoff.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    await db.PriceSnapshots
        .Where(p => p.CapturedAt < cutoffDt)
        .ExecuteDeleteAsync(ct);
}
```

Llamar en `MarketPipelineJob` con `cutoff = DateOnly.FromDateTime(timeService.UtcNow.UtcDateTime)`:
- Borra todo lo anterior a HOY (UTC)
- Conserva hoy y ayer (el job corre durante horario BMV, por lo que "hoy" siempre tiene datos)

### DailySnapshotHistoricalJob — inferir inserted/skipped

`UpsertDailySnapshotAsync` no retorna bool (a diferencia de `UpsertDistributionAsync`). Para contar inserted/skipped, hay dos opciones:

**Opción A (recomendada):** Cambiar `UpsertDailySnapshotAsync` para retornar `bool` (true=inserted, false=updated):
```csharp
public async Task<bool> UpsertDailySnapshotAsync(DailySnapshot snapshot, CancellationToken ct = default)
{
    var existing = await db.DailySnapshots.FirstOrDefaultAsync(...);
    if (existing is null) { db.DailySnapshots.Add(snapshot); await db.SaveChangesAsync(ct); return true; }
    existing.MergeUpdate(snapshot); await db.SaveChangesAsync(ct); return false;
}
```

**Opción B:** Dejar `UpsertDailySnapshotAsync` sin cambios y solo contar `candles.Count` vs `0` (log más simple).

**El agente debe usar Opción A** — mantiene consistencia con `UpsertDistributionAsync` y da mejor observabilidad. Requiere actualizar `IMarketRepository`, `MarketRepository`, y los fakes en tests existentes (solo añadir `return true/false`; los tests existentes seguirán compilando porque el método ya no es `Task` sino `Task<bool>`).

> ⚠️ **BREAKING CHANGE**: Cambiar `Task` → `Task<bool>` en `UpsertDailySnapshotAsync` afecta:
> - `IMarketRepository.cs`
> - `MarketRepository.cs`
> - `MarketPipelineJob.cs` (ignorar el bool: `await marketRepo.UpsertDailySnapshotAsync(...)`)
> - `FakeMarketRepository` en `MarketPipelineJobTests.cs`
> - `FakeDistMarketRepository` en `DistributionPipelineJobTests.cs`

### Endpoint Ops manual

`OpsMarketEndpoints.cs` (ya creado por T8) tiene el endpoint del job histórico. T10 agrega un segundo endpoint en el mismo archivo para distribuciones. Ambos siguen el mismo patrón:

```csharp
// Histórico (ya implementado — T8)
group.MapPost("/market/daily-snapshot-historical/run", (IBackgroundJobClient jobClient) =>
{
    jobClient.Enqueue<DailySnapshotHistoricalJob>(j => j.ExecuteAsync(CancellationToken.None));
    return Results.Accepted();
})
.RequireAuthorization("AdminOps")
.Produces(StatusCodes.Status202Accepted);

// Distribuciones (T10 — nuevo)
group.MapPost("/market/distribution/run", (IBackgroundJobClient jobClient) =>
{
    jobClient.Enqueue<DistributionPipelineJob>(j => j.ExecuteAsync(CancellationToken.None));
    return Results.Accepted();
})
.RequireAuthorization("AdminOps")
.Produces(StatusCodes.Status202Accepted);
```

**IMPORTANTE para T10**: `DistributionPipelineJob` ya está registrado como scoped en DI y ya tiene un cron automático (`"0 6 * * *"` en `Program.cs`). T10 solo agrega el endpoint; NO modificar el cron ni el registro en DI.

### Compatibilidad con `DailySnapshot.MergeUpdate()`

El método `MergeUpdate()` en `DailySnapshot.cs` tiene esta lógica:
- `Open` NUNCA se sobreescribe (si ya existe, el histórico de Yahoo no lo pisa)
- `High` = MAX(existente, nuevo) — el histórico puede mejorar un High parcial
- `Low` = MIN(existente, nuevo) — ídem
- `Close` y `Volume` siempre se actualizan (el histórico de Yahoo es el valor correcto EOD)

Esto significa que si el job intraday ya capturó datos del día, el histórico de Yahoo los complementa correctamente.

### Registro en DI

```csharp
// ApiServiceExtensions.cs — agregar junto a DistributionPipelineJob
builder.Services.AddScoped<DailySnapshotHistoricalJob>();
```

```csharp
// Program.cs — SIN RecurringJob.AddOrUpdate; solo el job intraday y de distribuciones siguen como están
// NO agregar: RecurringJob.AddOrUpdate<DailySnapshotHistoricalJob>(...)
```

### Project Structure Notes

```
src/Server/
  Domain/Market/                        — SIN cambios (entidades existentes suficientes)
  Application/Market/
    IMarketRepository.cs                UPDATE — añadir DeleteOldPriceSnapshotsAsync + cambiar UpsertDailySnapshotAsync a Task<bool>
  Infrastructure/
    Integrations/Yahoo/
      YahooOhlcvResult.cs               NEW
      IYahooFinanceClient.cs            UPDATE — añadir GetOhlcvHistoryAsync
      YahooFinanceClient.cs             UPDATE — implementar GetOhlcvHistoryAsync
    Jobs/Market/
      DailySnapshotHistoricalJob.cs     NEW
      MarketPipelineJob.cs              UPDATE — agregar llamada a DeleteOldPriceSnapshotsAsync al final
    Persistence/Repositories/Market/
      MarketRepository.cs               UPDATE — implementar DeleteOldPriceSnapshotsAsync + Task<bool> en UpsertDailySnapshot
  Api/
    CompositionRoot/
      ApiServiceExtensions.cs           UPDATE — AddScoped<DailySnapshotHistoricalJob>
    Endpoints/Ops/
      OpsMarketEndpoints.cs             NEW (o UPDATE si ya existe archivo Ops de mercado)
    Program.cs                          UPDATE — app.MapOpsMarket() (o equivalente)

tests/Unit/Infrastructure.Tests/Jobs/Market/
  DailySnapshotHistoricalJobTests.cs    NEW
  MarketPipelineJobTests.cs             UPDATE — test para DeleteOldPriceSnapshotsAsync
  DistributionPipelineJobTests.cs       UPDATE — fix FakeDistMarketRepository.UpsertDailySnapshotAsync → Task<bool>
```

### Sin migraciones de schema

No se requieren nuevas migraciones:
- `market.PriceSnapshot` ya tiene el índice necesario para el delete eficiente (`IX_PriceSnapshot_FibraId_CapturedAt`)
- `market.DailySnapshot` ya tiene índice único `UX_DailySnapshot_FibraId_Date` — idempotencia garantizada
- No hay columnas nuevas en ninguna tabla

### Referencias

- Story 3.4 patrón job: [3-4-pipeline-historico-distribuciones.md](_bmad-output/implementation-artifacts/3-4-pipeline-historico-distribuciones.md)
- Entidad DailySnapshot: [DailySnapshot.cs](src/Server/Domain/Market/DailySnapshot.cs)
- Entidad PriceSnapshot: [PriceSnapshot.cs](src/Server/Domain/Market/PriceSnapshot.cs)
- Job de mercado actual: [MarketPipelineJob.cs](src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs)
- Job de distribuciones (patrón a seguir): [DistributionPipelineJob.cs](src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs)
- Cliente Yahoo: [YahooFinanceClient.cs](src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs)
- Repositorio de mercado: [MarketRepository.cs](src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs)
- Tests de distribuciones (patrón fakes): [DistributionPipelineJobTests.cs](tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs)

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~Infrastructure.Tests.Jobs.Market"` → 24 passed, 0 failed
- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj` → 35 passed, 0 failed
- `dotnet test tests/Unit/Domain.Tests/Domain.Tests.csproj` → 9 passed, 0 failed
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` → 61 passed, 0 failed
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~OpsMarketEndpointTests"` → 2 passed, 0 failed
- `dotnet build src/Server/Api/Api.csproj` → build OK, 0 errors, 0 warnings

### Completion Notes List

- Implementado `YahooOhlcvResult` y `GetOhlcvHistoryAsync` en `YahooFinanceClient`, usando `History.Ticks` de `YahooQuotesApi` 7.0.8 y normalizando `Open/High/Low` con fallback a `Close` mientras se descartan candles con `Close <= 0`.
- Cambiado `IMarketRepository.UpsertDailySnapshotAsync` a `Task<bool>` para observabilidad de inserciones vs skips, y agregado `DeleteOldPriceSnapshotsAsync` con `ExecuteDeleteAsync`.
- `MarketPipelineJob` ahora ejecuta retención de `PriceSnapshot` al final del ciclo con `try/catch` y warning no bloqueante.
- Creado `DailySnapshotHistoricalJob` con ejecución manual, idempotencia basada en `UpsertDailySnapshotAsync`, manejo aislado de errores por fibra y `DisableConcurrentExecution`.
- Registrado el job en DI y agregado endpoint protegido `POST /api/v1/ops/market/daily-snapshot-historical/run` sin cron automático en `Program.cs`.
- Agregados tests unitarios para el job histórico y para la limpieza de snapshots al final del pipeline.
- Agregado endpoint protegido `POST /api/v1/ops/market/distribution/run` para disparo manual de `DistributionPipelineJob`, manteniendo intacto el cron diario existente.
- Agregados tests de integración para ambos triggers manuales de mercado en Ops.

### File List

- src/Server/Application/Market/IMarketRepository.cs
- src/Server/Infrastructure/Integrations/Yahoo/IYahooFinanceClient.cs
- src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs
- src/Server/Infrastructure/Integrations/Yahoo/YahooOhlcvResult.cs
- src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs
- src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs
- src/Server/Infrastructure/Jobs/Market/DailySnapshotHistoricalJob.cs
- src/Server/Api/CompositionRoot/ApiServiceExtensions.cs
- src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs
- src/Server/Api/Program.cs
- tests/Integration/Api.Tests/OpsMarketEndpointTests.cs
- tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs
- tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs
- tests/Unit/Infrastructure.Tests/Jobs/Market/DailySnapshotHistoricalJobTests.cs

## Senior Developer Review (AI)

### Review Findings

#### Patches

- [x] [Review][Patch] **P1 — Cutoff de retención borra ayer; AC1 exige conservar hoy Y ayer** [MarketPipelineJob.cs, MarketRepository.cs]
  `retentionCutoff = DateOnly.FromDateTime(timeService.UtcNow.UtcDateTime)` → `WHERE CapturedAt < hoy_midnight` elimina todos los registros de ayer. AC1 dice "anterior a ayer" (retención 2 días = conservar hoy y ayer). Fix: `retentionCutoff = DateOnly.FromDateTime(timeService.UtcNow.UtcDateTime).AddDays(-1)`. También actualizar la assertion del test de `2026-05-19` a `2026-05-18` (un día antes del fake time).

- [x] [Review][Patch] **P2 — `UpsertDailySnapshotAsync` no captura excepción de llave duplicada** [MarketRepository.cs:UpsertDailySnapshotAsync]
  El patrón SELECT-then-INSERT puede racear con `MarketPipelineJob` si ambos jobs procesan el mismo `(FibraId, Date)` simultáneamente. La segunda inserción golpea `UX_DailySnapshot_FibraId_Date` y lanza `DbUpdateException` sin ser capturada, abortando todos los candles restantes de la fibra. `UpsertDistributionAsync` ya tiene el catch correcto (SqlException 2627/2601). Fix: agregar el mismo patrón de catch + `db.Entry(snapshot).State = EntityState.Detached; return false;`.

- [x] [Review][Patch] **P3 — `ChangeTracker.Clear()` demasiado amplio en `UpsertDistributionAsync`** [MarketRepository.cs:UpsertDistributionAsync]
  En la rama de catch duplicado, `db.ChangeTracker.Clear()` desvincula **todas** las entidades rastreadas en el scope, no solo el `dist` fallido. Si algún código futuro compartiera el scope (o en tests de integración que reutilicen el contexto), esto puede silenciosamente perder entidades pendientes. Fix: `db.Entry(dist).State = EntityState.Detached;` en lugar de `db.ChangeTracker.Clear()`.

#### Deferred

- [x] [Review][Defer] **D1 — `SaveChangesAsync` por candle en backfill histórico** [DailySnapshotHistoricalJob.cs] — deferred, pre-existing design choice
- [x] [Review][Defer] **D2 — `RecurringJob.AddOrUpdate<DistributionPipelineJob>` añadido en story 3.5 (omisión de story 3.4)** [Program.cs] — deferred, pre-existing; comportamiento final correcto
- [x] [Review][Defer] **D3 — `DailySnapshotHistoricalJob` usa `DateTime.UtcNow` directo en lugar de `ITimeService`** [DailySnapshotHistoricalJob.cs:19] — deferred, spec Dev Notes lo especifica explícitamente; no afecta correctitud en producción
- [x] [Review][Defer] **D4 — Endpoint manual sin guard contra re-enqueueing** [OpsMarketEndpoints.cs] — deferred, concern operacional de baja prioridad para job de única ejecución

## Change Log

- 2026-05-21: Implementado job manual `DailySnapshotHistoricalJob`, retención de `PriceSnapshot` a 2 días, endpoint Ops protegido y cobertura unitaria asociada.
- 2026-05-21: Patch T10 — agregar endpoint manual `POST /api/v1/ops/market/distribution/run` para `DistributionPipelineJob`; cron automático diario se conserva.
