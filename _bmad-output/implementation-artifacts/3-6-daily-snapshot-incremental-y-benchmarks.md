# Story 3.6: DailySnapshot incremental diario + benchmarks IPC BMV / S&P 500

Status: done

## Story

Como sistema,
quiero que `DailySnapshotHistoricalJob` corra diariamente de forma incremental (solo desde el último día registrado) y que los índices de referencia `^MXX` y `^GSPC` estén disponibles como benchmarks en la gráfica de portafolio,
para que el gráfico "Rendimiento vs Benchmarks" tenga datos históricos precisos basados en precios de cierre oficiales de Yahoo Finance y se actualice automáticamente cada día.

## Acceptance Criteria

1. **AC1 — Lógica incremental por fibra:**
   Dado que `DailySnapshotHistoricalJob` se ejecuta para una fibra que ya tiene datos hasta el 2026-06-10, cuando procesa esa fibra, entonces llama `GetOhlcvHistoryAsync` con `from = 2026-06-10` (último día registrado) en lugar de `today - 4 años`.

2. **AC2 — Primer run (tabla vacía o fibra sin datos):**
   Dado que una fibra no tiene ningún registro en `DailySnapshot`, cuando el job la procesa, entonces llama `GetOhlcvHistoryAsync` con `from = today - 4 años` (backfill completo).

3. **AC3 — Benchmarks incluidos en el job incremental:**
   Dado que `^MXX` y `^GSPC` existen en la tabla `Fibras` con `State = Inactive`, cuando `DailySnapshotHistoricalJob` corre, entonces procesa los benchmarks con la misma lógica incremental que las fibras activas, usando `GetByTickerAsync` para resolverlos.

4. **AC4 — Benchmarks NO aparecen en catálogo ni operaciones de fibras activas:**
   Dado que `^MXX` y `^GSPC` tienen `State = Inactive`, entonces NO aparecen en ninguna llamada a `GetAllActiveAsync` (catálogo, oportunidades, comparador, news pipeline, fundamentales, ni en `MarketPipelineJob`).

5. **AC5 — Job registrado como RecurringJob diario post-cierre:**
   El job corre automáticamente a las 16:15 CST de lunes a viernes (`"15 22 * * 1-5"` UTC), cubriendo el cierre de ambos mercados (BMV cierra 15:00 CST, NYSE 16:00 EST = 22:00 UTC).

6. **AC6 — `MarketPipelineJob` ya no upserta `DailySnapshot`:**
   Dado que `MarketPipelineJob` corre en horario BMV, cuando guarda datos intradiarios, entonces solo persiste `PriceSnapshot` — ya no llama `UpsertDailySnapshotAsync`.

7. **AC7 — Endpoint Ops para reset y repoblado:**
   Dado que AdminOps hace `POST /api/v1/ops/market/daily-snapshot-reset/run`, entonces se eliminan todos los registros de `market.DailySnapshot` y se encola `DailySnapshotHistoricalJob` (que al correr con tabla vacía hace backfill de 4 años). Retorna `202 Accepted`.

8. **AC8 — Repositorio: `GetLatestDailySnapshotDateAsync`:**
   `IMarketRepository` expone `Task<DateOnly?> GetLatestDailySnapshotDateAsync(Guid fibraId, CancellationToken ct)` que retorna `MAX(Date)` de `DailySnapshot` para esa fibra, o `null` si no hay registros.

## Tasks / Subtasks

- [x] **T1 — Seed benchmarks en `CatalogSeed`** (AC3, AC4)
  - [x] Agregar en `CatalogSeed.cs` dos entradas usando el mismo helper `F(...)` pero con `State = FibraState.Inactive`:
    - `^MXX` → `YahooTicker = "^MXX"`, `FullName = "IPC BMV"`, `ShortName = "IPC"`, `Sector = "Índice"`, `Market = "BMV"`, `Currency = "MXN"`
    - `^GSPC` → `YahooTicker = "^GSPC"`, `FullName = "S&P 500"`, `ShortName = "S&P 500"`, `Sector = "Índice"`, `Market = "NYSE"`, `Currency = "USD"`
  - [x] El helper `F(...)` usa `State = FibraState.Active` por defecto → crear variante o sobreescribir `State` después de crear la instancia
  - [x] Agregar migración EF Core: `dotnet ef migrations add AddBenchmarkFibras --project src/Server/Infrastructure --startup-project src/Server/Api`

- [x] **T2 — `IMarketRepository`: nuevo método `GetLatestDailySnapshotDateAsync`** (AC8)
  - [x] Agregar a `src/Server/Application/Market/IMarketRepository.cs`:
    ```csharp
    Task<DateOnly?> GetLatestDailySnapshotDateAsync(Guid fibraId, CancellationToken ct = default);
    ```

- [x] **T3 — `MarketRepository`: implementar `GetLatestDailySnapshotDateAsync`** (AC8)
  - [x] Implementar en `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs`:
    ```csharp
    public async Task<DateOnly?> GetLatestDailySnapshotDateAsync(Guid fibraId, CancellationToken ct = default)
    {
        var max = await db.DailySnapshots
            .Where(d => d.FibraId == fibraId)
            .MaxAsync(d => (DateOnly?)d.Date, ct);
        return max;
    }
    ```

- [x] **T4 — `IMarketRepository`: nuevo método `DeleteAllDailySnapshotsAsync`** (AC7)
  - [x] Agregar a `IMarketRepository.cs`:
    ```csharp
    Task DeleteAllDailySnapshotsAsync(CancellationToken ct = default);
    ```

- [x] **T5 — `MarketRepository`: implementar `DeleteAllDailySnapshotsAsync`** (AC7)
  - [x] Implementar usando `ExecuteDeleteAsync` (bulk, sin cargar entidades):
    ```csharp
    public async Task DeleteAllDailySnapshotsAsync(CancellationToken ct = default)
        => await db.DailySnapshots.ExecuteDeleteAsync(ct);
    ```

- [x] **T6 — Convertir `DailySnapshotHistoricalJob` a modo incremental** (AC1, AC2, AC3, AC5)
  - [x] Reemplazar `historyStart = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5))` (fijo) por lógica incremental por fibra:
    ```csharp
    var lastDate = await marketRepo.GetLatestDailySnapshotDateAsync(fibra.Id, ct);
    var historyStart = lastDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-4));
    ```
  - [x] Después del loop de fibras activas, agregar loop de benchmarks (hardcodeados):
    ```csharp
    var benchmarkTickers = new[] { "^MXX", "^GSPC" };
    foreach (var ticker in benchmarkTickers)
    {
        await Task.Delay(TimeSpan.FromSeconds(1.5), ct);
        var benchmark = await fibraRepo.GetByTickerAsync(ticker, ct);
        if (benchmark is null) continue;
        // misma lógica incremental que fibras activas
    }
    ```
  - [x] Registrar en `Program.cs` como `RecurringJob.AddOrUpdate` con cron `"15 22 * * 1-5"` y timezone `mexicoTz` (ya usado para otros jobs)

- [x] **T7 — Remover upsert `DailySnapshot` de `MarketPipelineJob`** (AC6)
  - [x] Eliminar bloque completo (líneas 136-147 aprox.):
    ```csharp
    var daily = new DailySnapshot { ... };
    await marketRepo.UpsertDailySnapshotAsync(daily, ct);
    ```
  - [x] Conservar íntegro el bloque de `PriceSnapshot` — solo eliminar el fragmento de `DailySnapshot`

- [x] **T8 — Endpoint Ops `daily-snapshot-reset/run`** (AC7)
  - [x] En `src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs`, agregar:
    ```csharp
    group.MapPost("/market/daily-snapshot-reset/run", async (
        IMarketRepository marketRepo,
        IBackgroundJobClient jobClient,
        CancellationToken ct) =>
    {
        await marketRepo.DeleteAllDailySnapshotsAsync(ct);
        jobClient.Enqueue<DailySnapshotHistoricalJob>(j => j.ExecuteAsync(CancellationToken.None));
        return Results.Accepted();
    })
    .RequireAuthorization("AdminOps")
    .Produces(StatusCodes.Status202Accepted);
    ```

- [x] **T9 — Registrar nuevo cron en `Program.cs`** (AC5)
  - [x] Agregar junto al bloque `RecurringJob.AddOrUpdate` existente (después de los market pipeline jobs):
    ```csharp
    RecurringJob.AddOrUpdate<DailySnapshotHistoricalJob>(
        "daily-snapshot-incremental",
        j => j.ExecuteAsync(CancellationToken.None),
        "15 22 * * 1-5",
        new RecurringJobOptions { TimeZone = mexicoTz });
    ```
  - [x] **No eliminar** el endpoint manual existente `POST /api/v1/ops/market/daily-snapshot-historical/run` de T8 de la historia 3.5 — sigue siendo útil para disparos manuales

- [x] **T10 — Unit tests** (AC1-AC8)
  - [x] `DailySnapshotHistoricalJobTests.cs` — actualizar/agregar:
    - `WhenFibraHasExistingSnapshots_FetchesFromLastDate` (AC1)
    - `WhenFibraHasNoSnapshots_FetchesFrom4YearsAgo` (AC2)
    - `WhenBenchmarkExistsInactive_IsProcessedByJob` (AC3)
    - `WhenBenchmarkNotInDb_IsSkippedGracefully` (AC3)
  - [x] `MarketPipelineJobTests.cs` — agregar:
    - `Execute_DoesNotCallUpsertDailySnapshot` (AC6)
  - [x] `MarketRepositoryTests.cs` — agregar:
    - `GetLatestDailySnapshotDateAsync_WhenRecordsExist_ReturnsMaxDate`
    - `GetLatestDailySnapshotDateAsync_WhenNoRecords_ReturnsNull`
    - `DeleteAllDailySnapshotsAsync_DeletesAllRows`

### Review Findings

- [x] [Review][Decision→Patch] Cron timezone — cambiado a `TimeZoneInfo.Utc`; dispara a las 22:15 UTC (15 min post-cierre NYSE) en lugar de 22:15 hora México (~03-04 UTC). [`Program.cs:140`]
- [x] [Review][Patch] Reset endpoint sin audit log — agregado `TryLogQueuedRunAsync("DailySnapshotReset", ...)` con los parámetros de DI correspondientes. [`OpsMarketEndpoints.cs:80`]
- [x] [Review][Patch] `MarketRepositoryTests` no limpia la BD en teardown — refactorizado a `DbScope : IAsyncDisposable` que llama `EnsureDeletedAsync` + `DisposeAsync` al salir (incluso en fallo). [`tests/Unit/Infrastructure.Tests/Market/MarketRepositoryTests.cs:11`]
- [x] [Review][Defer] `DeleteAllDailySnapshotsAsync` + enqueue no son atómicos — si el proceso muere entre el DELETE y el enqueue, la tabla queda vacía hasta el siguiente cron o trigger manual [`OpsMarketEndpoints.cs:85`] — deferred, riesgo bajo y recuperable
- [x] [Review][Defer] Early-return salta benchmarks cuando `fibras.Count == 0` — en setup inicial sin FIBRAs activas, ^MXX/^GSPC no se procesan en ese ciclo [`DailySnapshotHistoricalJob.cs:23`] — deferred, edge case extremo, early-return pre-existente

## Dev Notes

### Contexto crítico: qué existe y qué cambia

La historia 3.5 implementó `DailySnapshotHistoricalJob` como un **job manual one-shot** de 5 años. Esta historia lo convierte en un job **diario incremental** con alcance de 4 años.

El problema de diseño actual:
- `MarketPipelineJob` hace upsert de `DailySnapshot` con `Close = LastPrice` capturado en el momento (no el cierre oficial de mercado). Sobreescribe el registro hasta 20 veces por día con datos intradiarios, dejando un OHLCV aproximado.
- `GetOhlcvHistoryAsync` de Yahoo da el OHLCV oficial de cierre — ese es el dato correcto para la gráfica de rendimiento histórico.

### Benchmarks: `FibraState.Inactive` es la clave

`GetAllActiveAsync` filtra por `FibraState.Active` — los 15 callers que lo usan (catálogo, comparador, oportunidades, jobs de noticias/fundamentales, etc.) **automáticamente ignorarán** los benchmarks si tienen `State = Inactive`.

`GetByTickerAsync` no filtra por estado (devuelve cualquier fibra por ticker) — el endpoint de portfolio performance ya lo usa así y seguirá funcionando.

### Cron explicado: `"15 22 * * 1-5"` UTC

- BMV cierra ~15:00 CST = 21:00 UTC
- NYSE cierra ~16:00 EST = 22:00 UTC (en verano)
- 22:15 UTC = ambos mercados cerrados → Yahoo ya tiene el OHLCV final del día
- Lunes a viernes únicamente (1-5)

Para invierno (EST → +1h) el cron aún es válido porque Yahoo suele publicar los cierres con retraso que compensa la diferencia.

### Seed benchmarks — patrón de `F(...)` existente

El helper privado `F(...)` en `CatalogSeed.cs` setea `State = FibraState.Active` hardcodeado (línea 51). Para los benchmarks, la forma más limpia sin modificar el helper es crear la instancia con initializer:

```csharp
// En CatalogSeed.Seed():
modelBuilder.Entity<Fibra>().HasData(
    // ... 19 fibras existentes ...
    BenchmarkF("^MXX",  "^MXX",  "IPC BMV", "IPC",     "BMV", "MXN"),
    BenchmarkF("^GSPC", "^GSPC", "S&P 500",  "S&P 500", "NYSE", "USD")
);

// Helper nuevo (privado, debajo de F()):
private static Fibra BenchmarkF(string ticker, string yahooTicker, string fullName,
    string shortName, string market, string currency)
    => new()
    {
        Id = GuidFromTicker(ticker),
        Ticker = ticker,
        YahooTicker = yahooTicker,
        FullName = fullName,
        ShortName = shortName,
        Sector = "Índice",
        Market = market,
        Currency = currency,
        State = FibraState.Inactive,
        NameVariants = [],
        CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
    };
```

`GuidFromTicker("^MXX")` y `GuidFromTicker("^GSPC")` generan GUIDs deterministas — sin colisiones con las 19 fibras existentes.

### Loop benchmarks en `DailySnapshotHistoricalJob`

El job actualmente itera `GetAllActiveAsync()`. Los benchmarks son `Inactive`, por lo que hay que procesarlos en un bloque separado después del loop principal. El throttle de 1.5s entre tickers aplica igual para no presionar a Yahoo:

```csharp
// Después del loop de fibras activas:
var benchmarkTickers = new[] { "^MXX", "^GSPC" };
foreach (var (benchmarkTicker, bIdx) in benchmarkTickers.Select((t, i) => (t, i)))
{
    await Task.Delay(TimeSpan.FromSeconds(1.5), ct);
    try
    {
        ct.ThrowIfCancellationRequested();
        var benchmark = await fibraRepo.GetByTickerAsync(benchmarkTicker, ct);
        if (benchmark is null)
        {
            logger.LogWarning("Benchmark {Ticker} not found in Fibras table, skipping", benchmarkTicker);
            continue;
        }
        var lastDate = await marketRepo.GetLatestDailySnapshotDateAsync(benchmark.Id, ct);
        var historyStart = lastDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-4));
        var candles = await yahooClient.GetOhlcvHistoryAsync(benchmarkTicker, historyStart, ct);
        foreach (var candle in candles)
        {
            if (candle.Close <= 0) continue;
            var snapshot = new DailySnapshot
            {
                FibraId = benchmark.Id,
                Ticker = benchmark.Ticker,
                Date = candle.Date,
                Open = candle.Open > 0 ? candle.Open : candle.Close,
                High = candle.High > 0 ? candle.High : candle.Close,
                Low = candle.Low > 0 ? candle.Low : candle.Close,
                Close = candle.Close,
                Volume = candle.Volume,
            };
            var wasInserted = await marketRepo.UpsertDailySnapshotAsync(snapshot, ct);
            if (wasInserted) inserted++;
            else skipped++;
        }
    }
    catch (OperationCanceledException) { throw; }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to fetch OHLCV history for benchmark {Ticker}", benchmarkTicker);
        errors++;
    }
}
```

### `MarketPipelineJob` — qué eliminar exactamente

Solo eliminar el bloque `DailySnapshot` (líneas ~136-148 en la versión actual). El bloque `PriceSnapshot` se conserva íntegro. La variable `processed++` ya está fuera del bloque de DailySnapshot, no tocarla.

```csharp
// ELIMINAR este bloque completo:
var daily = new DailySnapshot
{
    FibraId = fibra.Id,
    Ticker = fibra.Ticker,
    Date = DateOnly.FromDateTime(capturedAt.UtcDateTime),
    Open = quote.Open,
    High = quote.DayHigh,
    Low = quote.DayLow,
    Close = quote.LastPrice,
    Volume = quote.Volume,
};
await marketRepo.UpsertDailySnapshotAsync(daily, ct);
```

### Sin cambios en `PortfolioEndpoints.cs`

El endpoint `GET /api/v1/portfolio/performance` ya busca benchmarks con `fibraRepo.GetByTickerAsync(benchmarkTicker, ct)` — sin filtro de estado. No requiere cambios: encontrará `^MXX` y `^GSPC` una vez insertados por la migración.

### Migración EF Core

La migración solo agrega dos rows de seed en `catalog.Fibras`. No hay cambios de schema — `FibraState` ya existe con valores `Active = 0`, `Inactive = 1`. La migración será de tipo `OnModelCreating HasData` (datos de seed), no DDL.

```bash
dotnet ef migrations add AddBenchmarkFibras \
  --project src/Server/Infrastructure \
  --startup-project src/Server/Api
dotnet ef database update \
  --project src/Server/Infrastructure \
  --startup-project src/Server/Api
```

### Project Structure — archivos afectados

```
src/Server/
  Infrastructure/
    Persistence/
      Seed/
        CatalogSeed.cs                          UPDATE — BenchmarkF helper + 2 entradas
      Migrations/
        XXXXXX_AddBenchmarkFibras.cs            NEW (generado por EF)
      Repositories/Market/
        MarketRepository.cs                     UPDATE — GetLatestDailySnapshotDateAsync + DeleteAllDailySnapshotsAsync
    Jobs/Market/
      DailySnapshotHistoricalJob.cs             UPDATE — lógica incremental + loop benchmarks
      MarketPipelineJob.cs                      UPDATE — eliminar bloque DailySnapshot upsert
  Application/Market/
    IMarketRepository.cs                        UPDATE — 2 métodos nuevos
  Api/
    Endpoints/Ops/
      OpsMarketEndpoints.cs                     UPDATE — nuevo endpoint reset/run
    Program.cs                                  UPDATE — RecurringJob diario 22:15 UTC

tests/Unit/Infrastructure.Tests/
  Jobs/Market/
    DailySnapshotHistoricalJobTests.cs          UPDATE — tests incrementales + benchmarks
    MarketPipelineJobTests.cs                   UPDATE — assert no DailySnapshot upsert
  Repositories/Market/ (nuevo archivo o existente)
    MarketRepositoryTests.cs                    UPDATE/NEW — GetLatestDailySnapshot + DeleteAll
```

### Fakes en tests existentes

`FakeMarketRepository` (usado en `DailySnapshotHistoricalJobTests`) necesita implementar los dos métodos nuevos:
- `GetLatestDailySnapshotDateAsync` → retornar `null` por defecto en el fake base; tests específicos pueden sobrescribir
- `DeleteAllDailySnapshotsAsync` → no-op en el fake

Los fakes de otros tests (`MarketPipelineJobTests`, `DistributionPipelineJobTests`) también necesitan implementar los dos métodos nuevos si implementan `IMarketRepository`.

### Referencias

- Job actual a modificar: [DailySnapshotHistoricalJob.cs](src/Server/Infrastructure/Jobs/Market/DailySnapshotHistoricalJob.cs)
- MarketPipelineJob (bloque a eliminar): [MarketPipelineJob.cs](src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs) líneas ~136-148
- Seed a actualizar: [CatalogSeed.cs](src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs)
- Endpoint portfolio performance (sin cambios): [PortfolioEndpoints.cs](src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs) líneas 87-109
- IMarketRepository: [IMarketRepository.cs](src/Server/Application/Market/IMarketRepository.cs)
- MarketRepository: [MarketRepository.cs](src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs)
- OpsMarketEndpoints: [OpsMarketEndpoints.cs](src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs)
- History 3.5 (patrón base): [3-5-daily-snapshot-historico-y-limpieza-price-snapshots.md](_bmad-output/implementation-artifacts/3-5-daily-snapshot-historico-y-limpieza-price-snapshots.md)

## Dev Agent Record

### Agent Model Used
gpt-5

### Completion Notes List
- Seedé `^MXX` y `^GSPC` como `FibraState.Inactive` en `CatalogSeed` y generé la migración `AddBenchmarkFibras`.
- `IMarketRepository` y `MarketRepository` ahora exponen `GetLatestDailySnapshotDateAsync` y `DeleteAllDailySnapshotsAsync`; el borrado masivo usa `ExecuteDeleteAsync`.
- `DailySnapshotHistoricalJob` pasó a incremental por fibra, reutiliza el último `DailySnapshot` como punto de arranque, procesa benchmarks inactivos por ticker y quedó registrado como recurring job diario a las 22:15 UTC de lunes a viernes.
- `MarketPipelineJob` ya no persiste `DailySnapshot`, solo `PriceSnapshot`.
- Agregué `POST /api/v1/ops/market/daily-snapshot-reset/run` para vaciar `market.DailySnapshot` y reencolar el backfill histórico.
- Añadí tests unitarios para la lógica incremental, los benchmarks inactivos, el borrado masivo y la eliminación del upsert diario del pipeline de mercado.
- Validé el cambio con `dotnet test` completo de `Domain`, `Application`, `Infrastructure`, `Api`, `Jobs` y `Contract`, además del filtro `OpsMarketEndpointTests`.

### File List
- _bmad-output/implementation-artifacts/sprint-status.yaml
- scripts/codegen/Api.json
- src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs
- src/Server/Api/Program.cs
- src/Server/Application/Market/IMarketRepository.cs
- src/Server/Infrastructure/Jobs/Market/DailySnapshotHistoricalJob.cs
- src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs
- src/Server/Infrastructure/Migrations/SqlServer/20260613000042_AddBenchmarkFibras.cs
- src/Server/Infrastructure/Migrations/SqlServer/20260613000042_AddBenchmarkFibras.Designer.cs
- src/Server/Infrastructure/Migrations/SqlServer/AppDbContextModelSnapshot.cs
- src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs
- src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs
- tests/Unit/Infrastructure.Tests/Jobs/Market/DailySnapshotHistoricalJobTests.cs
- tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs
- tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs
- tests/Unit/Infrastructure.Tests/Market/MarketRepositoryTests.cs

## Change Log

- 2026-06-12: Convertí `DailySnapshotHistoricalJob` a incremental, agregué benchmarks inactivos `^MXX/^GSPC`, eliminé el upsert diario del pipeline intradía y añadí el reset Ops con migración EF y cobertura completa de pruebas.
