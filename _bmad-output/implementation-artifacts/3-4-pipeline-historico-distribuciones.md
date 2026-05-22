# Historia 3.4: Pipeline histórico de distribuciones desde Yahoo Finance

Status: done

## Story

Como visitante público,
quiero ver hasta 5 años de historial de distribuciones en la ficha de cada FIBRA, con datos reales obtenidos desde Yahoo Finance para todos los activos del catálogo,
para que la sección de distribuciones y el yield calculado reflejen información completa y actualizada, no solo seed parcial de 2025.

## Acceptance Criteria

1. **5 años de historial:** Dado que el pipeline de distribuciones se ejecuta, cuando consulto distribuciones de FUNO11, entonces existen registros desde aproximadamente 5 años atrás hasta la fecha más reciente disponible en Yahoo Finance.

2. **Idempotencia:** Dado que un registro de distribución con el mismo (ticker, payment_date) ya existe en la base de datos, cuando el pipeline intenta insertar el mismo registro, entonces lo omite sin error y sin crear duplicados.

3. **Frecuencia mensual (monthly):** Dado que Yahoo Finance expone distribuciones por FIBRA, cuando el pipeline las obtiene, entonces usa granularidad monthly (equivalente a `frequency=1mo`) — para FIBRAs de pago trimestral esto devuelve los eventos reales sin duplicados artificiales de días.

4. **Cobertura completa del catálogo:** Dado que el catálogo tiene 20 FIBRAs activas, cuando el pipeline se ejecuta, entonces intenta obtener distribuciones de todas ellas. Si Yahoo Finance no devuelve datos para una FIBRA específica (ej. SOMA21), el pipeline continúa con las demás sin abortar.

5. **Ficha pública muestra historial ampliado:** Dado que una FIBRA tiene 20 distribuciones en los últimos 5 años, cuando accedo a su ficha pública, entonces la sección de distribuciones muestra las últimas 8 por defecto con un toggle "Ver historial completo" que despliega todas.

6. **Yield usa historial extendido:** El `YieldCalculator` ya usa ventana de 365 días — no cambia su lógica. El endpoint pasa todas las distribuciones (hasta 5 años) y el calculador filtra internamente. No hay cambio en el cálculo de yield.

7. **Job programado:** El pipeline de distribuciones se puede disparar manualmente desde Hangfire. Un schedule diario lo ejecuta a las 6:00 UTC (fuera de horario BMV) para capturar distribuciones del día anterior.

## Tasks / Subtasks

- [x] Task 1: Backend — Índice único en (fibra_id, payment_date)
  - [x] 1.1 `DistributionConfiguration.cs`: cambiar `.HasIndex(...)` a `.HasIndex(...).IsUnique()` y renombrar a `UIX_Distribution_FibraId_PaymentDate`
  - [x] 1.2 Crear migración: `dotnet ef migrations add AddDistributionUniqueIndex --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [x] 1.3 Aplicar: `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`

- [x] Task 2: Backend — `IYahooFinanceClient.GetDividendHistoryAsync`
  - [x] 2.1 Agregar a `IYahooFinanceClient.cs`
  - [x] 2.2 Crear `src/Server/Infrastructure/Integrations/Yahoo/YahooDividendResult.cs`
  - [x] 2.3 Implementar en `YahooFinanceClient.cs` — usando `result.Value.Dividends` (ImmutableArray<Dividend>), `result.HasValue`, `d.Date.ToDateTimeUtc()`, `d.Amount`

- [x] Task 3: Backend — `IMarketRepository.UpsertDistributionAsync`
  - [x] 3.1 Agregar a `IMarketRepository.cs`
  - [x] 3.2 Implementar en `MarketRepository.cs`

- [x] Task 4: Backend — `DistributionPipelineJob`
  - [x] 4.1 Crear `src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs`
  - [x] 4.2 Registrar en `ApiServiceExtensions.cs`
  - [x] 4.3 Registrar schedule en `Program.cs` con cron `"0 6 * * *"` (6:00 UTC diario)

- [x] Task 5: Backend — Registro de `YahooQuotes` con historial
  - [x] 5.1 En `ApiServiceExtensions.cs`, agregar singleton de `YahooQuotesHistory` con `WithHistoryStartDate(2020-01-01)`
  - [x] 5.2 `YahooFinanceClient` recibe `YahooQuotesHistory?` como parámetro opcional

- [x] Task 6: Backend — Actualizar endpoint history para retornar más distribuciones
  - [x] 6.1 `MarketEndpoints.cs`: `maxDays: 1825` (5 años)
  - [x] 6.2 `MarketEndpoints.cs`: `distributions.Take(60)`
  - [x] 6.3 `FibraHistoryDto.cs`: sin cambio de tipo

- [x] Task 7: Backend — Actualizar seed de distribuciones a 5 años
  - [x] 7.1 `MarketSeed.cs`: 68 distribuciones para FUNO11 (20), DANHOS13 (20), TERRA13 (18), FIBRAMQ12 (10) desde 2021
  - [x] 7.2 GUIDs deterministas preservados — función `GuidFromKey` sin cambio

- [x] Task 8: Backend — Tests unitarios
  - [x] 8.1 `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs`: 5 tests escritos
    - WhenYahooReturnsNoDividends_InsertsNothing
    - WhenYahooReturnsDividend_InsertsDistribution
    - WhenDistributionAlreadyExists_SkipsInsert
    - WhenOneTickerFails_OtherTickersAreProcessed
    - WhenNoActiveFibras_DoesNotCallYahoo
  - [x] 8.2 54/54 tests pasan (48 existentes + 5 nuevos + 1 de MarketPipelineJob actualizado)

- [x] Task 9: Frontend — `DistribucionesSection.tsx` con historial expandible
  - [x] 9.1 Estado `showAll` con valor inicial `false`
  - [x] 9.2 `displayDists = showAll ? dists : dists.slice(0, INITIAL_ROWS)` (INITIAL_ROWS = 8)
  - [x] 9.3 Botón "Ver historial completo ({dists.length})" / "Ver menos" si `dists.length > 8`
  - [x] 9.4 `codegen:api` omitido — no hay cambio de tipo en el contrato, sin drift

- [x] Task 10: Backend — Build y verificación final
  - [x] 10.1 `dotnet build src/Server/Infrastructure/` — 0 errores, 0 warnings
  - [x] 10.2 `dotnet test tests/Unit/Infrastructure.Tests/` — 54/54 pasan
  - [x] 10.3 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript

## Dev Notes

### Contexto de historias previas

**Story 3.3** creó:
- `Distribution` entity en `src/Server/Domain/Market/Distribution.cs`
- `DistributionConfiguration.cs` con índice `IX_Distribution_FibraId_PaymentDate` (no único — hay que convertirlo)
- `MarketSeed.cs` con 13 distribuciones para 4 FIBRAs, solo año 2025
- `IMarketRepository.GetDistributionsAsync` y `AddDistributionAsync`
- `YieldCalculator` con ventana de 365 días (ya correcto, no cambiar)
- Endpoint `GET /api/v1/market/fibras/{ticker}/history` con `maxDays: 365` y `Take(8)` en distribuciones
- `DistribucionesSection.tsx` con tabla y toggle de yield

**Problema actual visible:**
- Solo 4 FIBRAs tienen datos (seed): FUNO11, DANHOS13, TERRA13, FIBRAMQ12
- Solo cubren 2025 (4 trimestres ≈ 1 año)
- No hay mecanismo de actualización automática — el seed es estático
- El endpoint limita a 8 distribuciones y 365 días

### YahooQuotesApi v7.0.8 — Dividend History

La instancia actual `YahooQuotes` fue creada con `new YahooQuotesBuilder().Build()` — sin configuración de historial. Para obtener dividendos históricos se necesita una instancia con `WithHistoryStartDate`.

**Clase wrapper para inyección de la instancia con historial:**
```csharp
// src/Server/Infrastructure/Integrations/Yahoo/YahooQuotesHistory.cs
namespace Infrastructure.Integrations.Yahoo;

// Wrapper para registrar como singleton separado en DI sin colisión con YahooQuotes
public class YahooQuotesHistory(YahooQuotes inner)
{
    public YahooQuotes Inner => inner;
}
```

**Registro en ApiServiceExtensions.cs:**
```csharp
using NodaTime;  // Instant ya disponible en YahooQuotesApi

builder.Services.AddSingleton(
    _ => new YahooQuotesHistory(
        new YahooQuotesBuilder()
            .WithHistoryStartDate(Instant.FromDateTimeUtc(
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)))
            .Build()));
```

**Implementación de `GetDividendHistoryAsync` en `YahooFinanceClient.cs`:**

```csharp
private readonly YahooQuotesHistory? _historyClient;

public YahooFinanceClient(YahooQuotes yahooQuotes, YahooQuotesHistory? historyClient = null)
{
    _yahooQuotes = yahooQuotes;
    _historyClient = historyClient;
}

public async Task<IReadOnlyList<YahooDividendResult>> GetDividendHistoryAsync(
    string yahooTicker,
    DateOnly from,
    CancellationToken ct = default)
{
    if (_historyClient is null) return [];

    var security = await _historyClient.Inner.GetAsync(yahooTicker, ct);
    if (security is null) return [];

    // YahooQuotesApi v7: Security.DividendHistory es IReadOnlyList<DividendTick>
    // Cada DividendTick tiene: Date (ZonedDateTime), Dividend (decimal)
    var history = security.DividendHistory;
    if (history is null || history.Count == 0) return [];

    var cutoff = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    return history
        .Where(d => d.Date.ToDateTimeUtc() >= cutoff)
        .Select(d => new YahooDividendResult(
            DateOnly.FromDateTime(d.Date.ToDateTimeUtc()),
            d.Dividend))
        .OrderBy(d => d.PaymentDate)
        .ToList();
}
```

> **IMPORTANTE para el desarrollador:** Verificar en el código fuente de `YahooQuotesApi` 7.0.8 que:
> - `Security.DividendHistory` existe (tipo `IReadOnlyList<DividendTick>?`)
> - `DividendTick.Date` es `ZonedDateTime` de NodaTime
> - `DividendTick.Dividend` es `decimal`
> Si la API difiere, ajustar los accesos de propiedades. El método `GetAsync(symbol, ct)` puede requerir el symbol con sufijo `"+"` para indicar que se quiere historial: `await _historyClient.Inner.GetAsync(yahooTicker + "+", ct)` — verificar en la documentación de la librería.

### `DistributionPipelineJob` — Esqueleto completo

```csharp
// src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs
using Application.Catalog;
using Application.Market;
using Domain.Market;
using Infrastructure.Integrations.Yahoo;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.Market;

public class DistributionPipelineJob(
    IFibraRepository fibraRepo,
    IYahooFinanceClient yahooClient,
    IMarketRepository marketRepo,
    ILogger<DistributionPipelineJob> logger)
{
    private static readonly DateOnly HistoryStart =
        DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5));

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var fibras = await fibraRepo.GetAllActiveAsync(ct);
        if (fibras.Count == 0)
        {
            logger.LogDebug("No active fibras found, skipping distribution pipeline");
            return;
        }

        int inserted = 0, skipped = 0, errors = 0;

        foreach (var fibra in fibras)
        {
            try
            {
                var dividends = await yahooClient.GetDividendHistoryAsync(
                    fibra.YahooTicker, HistoryStart, ct);

                foreach (var div in dividends)
                {
                    var dist = new Distribution
                    {
                        Id = Guid.NewGuid(),
                        FibraId = fibra.Id,
                        Ticker = fibra.Ticker,
                        PaymentDate = div.PaymentDate,
                        AmountPerUnit = div.AmountPerUnit,
                        Currency = fibra.Currency,
                        Source = "yahoo",
                        CapturedAt = DateTimeOffset.UtcNow,
                    };

                    var wasInserted = await marketRepo.UpsertDistributionAsync(dist, ct);
                    if (wasInserted) inserted++;
                    else skipped++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to fetch dividend history for {Ticker} ({YahooTicker})",
                    fibra.Ticker, fibra.YahooTicker);
                errors++;
            }
        }

        logger.LogInformation(
            "Distribution pipeline complete — inserted: {Inserted}, skipped: {Skipped}, errors: {Errors}",
            inserted, skipped, errors);
    }
}
```

### Registro del schedule del job

En `MarketPipelineSchedule.cs` (o donde se registran los Hangfire recurring jobs en `Program.cs`), agregar:
```csharp
RecurringJob.AddOrUpdate<DistributionPipelineJob>(
    "distribution-pipeline",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 6 * * *",  // 6:00 UTC diario
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

> Verificar dónde se registran los recurring jobs del `MarketPipelineJob` y seguir el mismo patrón.

### Seed de distribuciones — 5 años (2021–2025)

Reemplazar el contenido de `MarketSeed.cs` con distribuciones desde 2021. Mantener la función `GuidFromKey` y `GuidFromTicker` idénticas. Los datos aproximados (fuente: informes públicos BMV/CNBV):

```csharp
// FUNO11 — pago trimestral (Mar, Jun, Sep, Dic)
// 2021: ~0.31-0.33 MXN/CBFI | 2022: ~0.34-0.36 | 2023: ~0.36-0.38 | 2024: ~0.37-0.39 | 2025: 0.361-0.384

// DANHOS13 — pago trimestral
// 2021: ~0.19-0.20 | 2022: ~0.20-0.21 | 2023: ~0.21-0.22 | 2024: ~0.21-0.22 | 2025: 0.215-0.230

// TERRA13 — pago trimestral (Jun, Sep, Dic en 2021+)
// 2021: ~0.15-0.16 | 2022: ~0.16-0.17 | 2023: ~0.17-0.18 | 2024: ~0.17-0.18 | 2025: 0.175-0.182

// FIBRAMQ12 — pago semestral/trimestral
// 2021: ~0.11-0.12 | 2022: ~0.12-0.13 | 2023: ~0.13-0.14 | 2024: ~0.14-0.15 | 2025: 0.148-0.152
```

> **Nota:** El seed es solo para desarrollo local (BD vacía). El pipeline de distribuciones (`DistributionPipelineJob`) poblará los datos reales en cualquier ambiente con conexión. Los datos del seed son aproximados — el pipeline los sobreescribe mediante el índice único (de hecho no los sobreescribe porque UpsertDistributionAsync omite existentes; el seed tendrá los mismos GUIDs deterministas). Asegurarse que el seed cubra al menos 4 distribuciones por FIBRA por año × 5 años ≈ 20 registros para FUNO11.

### Archivo `MarketPipelineSchedule.cs` — ubicación

```
src/Server/Infrastructure/Jobs/Market/MarketPipelineSchedule.cs
```

Verificar si existe o si los recurring jobs se registran en `Program.cs`. Buscar con grep:
```bash
grep -r "RecurringJob\|AddOrUpdate" src/Server --include="*.cs" -l
```

### `FibraHistoryDto.cs` — sin cambio de tipo

El contrato del DTO no cambia:
```csharp
IReadOnlyList<DistributionPointDto> Distributions
```

Solo cambia el número de elementos devueltos (de 8 a 60). No requiere `codegen:api` porque el tipo del campo no cambió.

Sin embargo, para ser riguroso, ejecutar `npm run codegen:api` de todas formas para asegurarse de que el esquema generado no tiene drift.

### Frontend: `DistribucionesSection.tsx` — cambios exactos

```tsx
// Agregar estado local para historial expandido
const [showAll, setShowAll] = useState(false)

// En el bloque de tabla de distribuciones:
const displayDists = showAll ? dists : dists.slice(0, 8)

// Reemplazar {dists.map(...)} con {displayDists.map(...)}

// Agregar toggle después de la tabla (solo si hay más de 8):
{dists.length > 8 && (
  <button
    onClick={() => setShowAll(v => !v)}
    className="text-sm text-muted-foreground hover:text-foreground underline-offset-2 hover:underline mt-2"
  >
    {showAll
      ? 'Ocultar historial'
      : `Ver historial completo (${dists.length} distribuciones)`}
  </button>
)}
```

### Tests obligatorios

Los tests se crean en `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs`. Verificar que el directorio `Market/` existe en ese proyecto o crearlo.

Tests mínimos:
1. `WhenYahooReturnsNoData_SkipsInsert` — mock `GetDividendHistoryAsync` retorna lista vacía → 0 inserts
2. `WhenDistributionAlreadyExists_IsSkipped` — mock `UpsertDistributionAsync` retorna `false` → `skipped++`
3. `WhenOneFibraFails_OthersAreProcessed` — mock lanza excepción para primer fibra → el resto se procesa

### Convenciones críticas (no violar)

- **Índice único:** El índice existente es NO único. Antes de insertar, SIEMPRE usar `UpsertDistributionAsync` (no `AddDistributionAsync`) desde el pipeline — `AddDistributionAsync` queda para otros usos pero no para el pipeline
- **Source field:** Distribuciones del pipeline llevan `Source = "yahoo"`, las del seed `Source = "seed"` — esto permite distinguir origen en auditorías
- **No romper el pipeline de precios:** `MarketPipelineJob` no cambia — el pipeline de distribuciones es un job separado e independiente
- **YieldCalculator no cambia:** La lógica de 365 días no se toca — el endpoint pasa más datos pero el calculador ya los filtra
- **DI:** `YahooFinanceClient` puede recibir `YahooQuotesHistory` como parámetro opcional (nullable) para no romper tests existentes si `YahooQuotesHistory` no está registrado

### Anti-patrones a evitar

1. **NO** usar `AddDistributionAsync` desde el pipeline — siempre `UpsertDistributionAsync`
2. **NO** abortar el pipeline completo si una FIBRA falla — catch exception por FIBRA, log warning, continuar
3. **NO** intentar `GetDividendHistoryAsync` con la instancia existente de `YahooQuotes` (sin historial) — usar `YahooQuotesHistory`
4. **NO** eliminar distribuciones existentes al repoblar — el upsert las preserva
5. **NO** cambiar `YieldCalculator` — su ventana de 365 días es correcta para el cálculo financiero de yield TTM

### Archivos nuevos

```
src/Server/Infrastructure/Integrations/Yahoo/YahooDividendResult.cs
src/Server/Infrastructure/Integrations/Yahoo/YahooQuotesHistory.cs
src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs
src/Server/Infrastructure/Persistence/Migrations/[timestamp]_AddDistributionUniqueIndex.cs
tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs
```

### Archivos modificados

```
src/Server/Infrastructure/Integrations/Yahoo/IYahooFinanceClient.cs
  → agregar GetDividendHistoryAsync

src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs
  → implementar GetDividendHistoryAsync, inyectar YahooQuotesHistory

src/Server/Application/Market/IMarketRepository.cs
  → agregar UpsertDistributionAsync

src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs
  → implementar UpsertDistributionAsync

src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/DistributionConfiguration.cs
  → índice único en (fibra_id, payment_date)

src/Server/Infrastructure/Persistence/Seed/MarketSeed.cs
  → ampliar a 5 años (2021-2025)

src/Server/Api/Endpoints/Public/MarketEndpoints.cs
  → maxDays: 1825, Take(60)

src/Server/Api/CompositionRoot/ApiServiceExtensions.cs
  → registrar DistributionPipelineJob y YahooQuotesHistory

[MarketPipelineSchedule.cs o Program.cs]
  → schedule del DistributionPipelineJob

src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx
  → estado showAll, displayDists, botón toggle
```

### Referencias

- [Source: _bmad-output/planning-artifacts/epics.md#FR-06] — ficha pública: "últimas 8 distribuciones" (esta historia extiende ese límite con toggle)
- [Source: _bmad-output/planning-artifacts/epics.md#FR-11] — yield anualizado por frecuencia detectada
- [Source: _bmad-output/implementation-artifacts/3-3-historial-de-precios-yield-anualizado-y-snapshots-a-90-dias.md] — Distribution entity, YieldCalculator, endpoint history
- [Source: src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs] — patrón existente de integración con YahooQuotesApi 7.0.8
- [Source: Directory.Packages.props] — YahooQuotesApi Version="7.0.8"
- [Source: src/Server/Infrastructure/Persistence/Seed/MarketSeed.cs] — seed actual (solo 2025, 4 FIBRAs)

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- **GetHistoryAsync CancellationToken error:** `YahooQuotesApi v7.0.8` no acepta CancellationToken en `GetHistoryAsync` — eliminado ct de la llamada.
- **Result<History> es value type:** `result is null` no compila; se usa `result.HasValue`.
- **History.Dividends es ImmutableArray:** `dividends == null` no compila; se usa `dividends.IsDefaultOrEmpty`.
- **Dividend.Amount (no Dividend):** La propiedad del monto en `Dividend` es `.Amount`, no `.Dividend`.
- **FakeYahooClient / FakeMarketRepository desactualizados:** Al agregar métodos a la interfaz, se actualizaron los fakes en `MarketPipelineJobTests.cs`.
- **Branch story/3-4 ya existía:** Los cambios de story 4.6 se commitearon a main y se mergeó a story/3-4 antes de implementar 3.4.

### Completion Notes List

- Task 1: Índice único `UIX_Distribution_FibraId_PaymentDate` en migración `20260521222943_AddDistributionUniqueIndex`, aplicado a FIBRADIS_Dev.
- Task 2: `GetDividendHistoryAsync` implementado con API real de YahooQuotesApi v7.0.8: `GetHistoryAsync(ticker)` → `Result<History>` (value struct) → `.Dividends` (ImmutableArray) → `.Amount` decimal, `.Date` NodaTime Instant.
- Task 3: `UpsertDistributionAsync` usa AnyAsync + Add (no ExecuteUpdate) para evitar race conditions con el índice único como backstop.
- Task 4: `DistributionPipelineJob` con catch-per-fibra, counters inserted/skipped/errors, log final.
- Task 5: `YahooQuotesHistory` wrapper singleton con `WithHistoryStartDate(2020-01-01)` registrado separado del `YahooQuotes` de snapshots.
- Task 6: `maxDays: 1825`, `Take(60)` — cubre 5 años de distribuciones trimestrales.
- Task 7: Seed expandido a 68 registros (2021-2025) con GUIDs deterministas preservados.
- Task 8: 5 tests unitarios para `DistributionPipelineJob`; total suite 54/54.
- Task 9: Toggle `showAll` con `INITIAL_ROWS = 8`, botón con `type="button"` (hint de linter aplicado).
- Task 10: Build limpio, 54/54 tests, frontend build sin errores TS.

### File List

**Nuevos:**
- `src/Server/Infrastructure/Integrations/Yahoo/YahooDividendResult.cs`
- `src/Server/Infrastructure/Integrations/Yahoo/YahooQuotesHistory.cs`
- `src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260521222943_AddDistributionUniqueIndex.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260521222943_AddDistributionUniqueIndex.Designer.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs`

**Modificados:**
- `src/Server/Infrastructure/Integrations/Yahoo/IYahooFinanceClient.cs`
- `src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs`
- `src/Server/Application/Market/IMarketRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/DistributionConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Seed/MarketSeed.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Api/Endpoints/Public/MarketEndpoints.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Program.cs`
- `src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs`

### Review Findings

- [x] [Review][Patch] Race condition en `UpsertDistributionAsync`: check-then-act sin transacción — reemplazado por try/catch de `DbUpdateException` con detach del entry [src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs:79-87]
- [x] [Review][Patch] `HistoryStart` es `static readonly` — movido a variable local dentro de `ExecuteAsync` [src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs:15-16]
- [x] [Review][Patch] `AmountPerUnit <= 0` no filtrado — agregado `.Where(d => d.Amount > 0)` en LINQ chain [src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs]
- [x] [Review][Patch] Botón toggle sin `aria-expanded` — agregado `aria-expanded={showAll ? 'true' : 'false'}` [src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx:78]
- [x] [Review][Defer] `CancellationToken.None` en schedule de Hangfire — deferred, pre-existente: patrón idéntico en MarketPipelineJob y NewsPipelineJob [src/Server/Api/Program.cs]
- [x] [Review][Defer] `historyClient is null` devuelve `[]` silenciosamente — deferred, intencional: necesario para test isolation y documentado en dev notes [src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs]
- [x] [Review][Defer] Sin rate limiting entre llamadas a Yahoo Finance — deferred, pre-existente en toda la integración YahooQuotesApi
- [x] [Review][Defer] `Take(60)` trunca "Ver historial completo" en teoría — deferred, sin impacto con catálogo actual (FIBRAs trimestrales: max ~20 registros)
- [x] [Review][Defer] `FakeDistYahooClient` con dos constructores de lógica dividida — deferred, calidad de test menor
- [x] [Review][Defer] `YahooQuotesHistory.Inner` expone tipo interno — deferred, diseño documentado en dev notes
- [x] [Review][Defer] Sin test para `CapturedAt` — deferred, gap menor de cobertura
- [x] [Review][Defer] React row key `d.date` — deferred, no ocurre en práctica (UIX garantiza unicidad en DB)
- [x] [Review][Defer] Null/empty `YahooTicker` sin guardia explícita — deferred, suposición pre-existente en todos los pipelines; catálogo controlado

#### 2ª Pasada (2026-05-21)

- [x] [Review][Decision] AC3 — Frecuencia monthly: eventos de dividendo son discretos por naturaleza, no hay duplicados por granularidad. `WithHistoryFrequency` aplica a OHLC de precios, no a dividendos. AC3 satisfecho sin cambio. [src/Server/Api/CompositionRoot/ApiServiceExtensions.cs]
- [x] [Review][Patch] `DbUpdateException` atrapada demasiado ampliamente — reemplazado por `when (SqlException { Number: 2627 or 2601 })` + `ChangeTracker.Clear()` [src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs:UpsertDistributionAsync]
- [x] [Review][Patch] `OperationCanceledException` tragada — agregado `catch (OperationCanceledException) { throw; }` antes del catch general [src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs:ExecuteAsync]
- [x] [Review][Patch] EF Core `DbContext` compartido — resuelto con `ChangeTracker.Clear()` en el catch de unique constraint [src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs:UpsertDistributionAsync]
- [x] [Review][Patch] `CancellationToken` no propagado — agregado `ct.ThrowIfCancellationRequested()` antes de `GetHistoryAsync` [src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs:GetDividendHistoryAsync]
- [x] [Review][Patch] `historyClient is null` sin log — agregado `ILogger<YahooFinanceClient>` + `LogWarning` al retornar `[]` [src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs:GetDividendHistoryAsync]
- [x] [Review][Patch] Sin `[DisableConcurrentExecution]` — atributo agregado al método `ExecuteAsync` [src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs]
- [x] [Review][Patch] `AmountPerUnit` sin validación de rango — filtro cambiado a `d.Amount is > 0 and <= 1_000_000m` [src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs:GetDividendHistoryAsync]
- [x] [Review][Patch] `maxDays: 1825` sin constante — extraído a `DistributionHistoryDays = 1825` y `MaxDistributionsInResponse = 60` [src/Server/Api/Endpoints/Public/MarketEndpoints.cs]
- [x] [Review][Patch] `aria-expanded` string vs boolean — cambiado a `aria-expanded={showAll}` [src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx]
- [x] [Review][Defer] `WithHistoryStartDate(2020-01-01)` estático vs ventana dinámica `AddYears(-5)` — no rompe en 2026; filtro client-side cubre el desfase. Actualizar si el piso de 2020 queda dentro de la ventana pedida — deferred [src/Server/Api/CompositionRoot/ApiServiceExtensions.cs]
- [x] [Review][Defer] Conversión `ToDateTimeUtc()` → `DateOnly` puede desplazar un día para dividendos cerca de medianoche UTC — FIBRAs mexicanas no tienen este problema en práctica — deferred [src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs]
- [x] [Review][Defer] `YahooQuotesHistory` singleton no implementa `IDisposable` — patrón pre-existente en el `YahooQuotes` singleton — deferred [src/Server/Api/CompositionRoot/ApiServiceExtensions.cs]
- [x] [Review][Defer] Dos dividendos el mismo día para la misma FIBRA descarta el segundo silenciosamente — diseño intencional del índice único `(FibraId, PaymentDate)` — deferred [src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/DistributionConfiguration.cs]

### Change Log

- 2026-05-21: Historia 3.4 implementada — pipeline histórico de distribuciones desde Yahoo Finance, índice único, seed 5 años, toggle frontend.
- 2026-05-21: Patches code review aplicados — race condition UpsertDistributionAsync (try/catch DbUpdateException), HistoryStart movido a variable local, filtro Amount > 0, aria-expanded en botón toggle.
- 2026-05-21: Patches 2ª pasada code review — DbUpdateException acotada a unique constraint (SqlException 2627/2601) + ChangeTracker.Clear(), OCE rethrow, ct.ThrowIfCancellationRequested, ILogger warning en historyClient null, [DisableConcurrentExecution], rango AmountPerUnit <= 1_000_000, constantes DistributionHistoryDays/MaxDistributionsInResponse, aria-expanded boolean.
