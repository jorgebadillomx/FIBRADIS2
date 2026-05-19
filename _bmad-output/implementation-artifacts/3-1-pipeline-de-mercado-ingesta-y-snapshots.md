# Historia 3.1: Pipeline de Mercado — Ingesta y Snapshots

Status: done

## Historia

Como AdminOps,
quiero que el pipeline de mercado obtenga Last Price, cambio diario, volumen y datos de 52 semanas del proveedor externo cada 15 minutos, solo durante el horario BMV (8:15am–3:15pm CDMX, días hábiles), y persista snapshots diarios,
para que los precios de las FIBRAs se mantengan frescos durante el horario de mercado y los ciclos no se ejecuten cuando el mercado está cerrado.

## Criterios de Aceptación

**CA-1: Ciclo exitoso durante horario BMV**
Dado que el reloj del sistema es 10:00am CDMX un martes,
Cuando se ejecuta un ciclo del pipeline,
Entonces se persisten registros `PriceSnapshot` para todas las FIBRAs activas con `captured_at` actual y `status=processed`.

**CA-2: No se ejecuta fuera del horario BMV**
Dado que el reloj del sistema es 4:00pm CDMX cualquier día hábil,
Entonces no se dispara ningún ciclo del pipeline — Hangfire no programa trabajo fuera del horario BMV.

**CA-3: Error parcial aislado por FIBRA**
Dado que el proveedor externo retorna un error para FMTY14 pero tiene éxito para todas las demás,
Entonces FMTY14 obtiene `status=error` con `error_reason` poblado, mientras que todas las demás FIBRAs se actualizan con `status=processed`.

**CA-4: Snapshot diario persistido**
Dado que transcurre un día completo de operaciones,
Entonces se persiste un registro de snapshot diario por FIBRA activa en el esquema `market` con campos OHLC (open, high, low, close) y volume.

**CA-5: Clasificación `crítico` por fallos consecutivos**
Dado que dos ciclos consecutivos del pipeline fallan para una FIBRA específica,
Entonces el sistema clasifica el precio de esa FIBRA como `crítico` (NFR-04) — el nuevo `PriceSnapshot` tiene `status=crítico`.

## Tareas / Subtareas

- [x] Task 1: Entidades de dominio — módulo Market (AC: CA-1, CA-3, CA-5)
  - [x] Crear `src/Server/Domain/Market/MarketDataStatus.cs`: enum `Processed`, `Error`, `Critical` (string-backed)
  - [x] Crear `src/Server/Domain/Market/PriceSnapshot.cs`: `Guid Id`, `Guid FibraId`, `string Ticker`, `decimal? LastPrice`, `decimal? DailyChange`, `decimal? DailyChangePct`, `long? Volume`, `decimal? Week52High`, `decimal? Week52Low`, `DateTimeOffset CapturedAt`, `MarketDataStatus Status`, `string? ErrorReason`
  - [x] Crear `src/Server/Domain/Market/DailySnapshot.cs`: `Guid Id`, `Guid FibraId`, `string Ticker`, `DateOnly Date`, `decimal? Open`, `decimal? High`, `decimal? Low`, `decimal? Close`, `long? Volume`
  - [x] Eliminar `.gitkeep` de `src/Server/Domain/Market/`

- [x] Task 2: Integración Yahoo Finance (AC: CA-1, CA-3)
  - [x] Crear `src/Server/Infrastructure/Integrations/Yahoo/YahooQuoteResult.cs`: record con `string Symbol`, `decimal? LastPrice`, `decimal? DailyChange`, `decimal? DailyChangePct`, `long? Volume`, `decimal? Week52High`, `decimal? Week52Low`, `decimal? Open`, `decimal? DayHigh`, `decimal? DayLow`
  - [x] Crear `src/Server/Infrastructure/Integrations/Yahoo/IYahooFinanceClient.cs`: `Task<IReadOnlyList<YahooQuoteResult>> GetQuotesAsync(IEnumerable<string> yahooTickers, CancellationToken ct)`
  - [x] Crear `src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs`: implementación typed client con HttpClient; deserializa `quoteResponse.result[]`; maneja campos null con `?.`
  - [x] Registrar `AddHttpClient<IYahooFinanceClient, YahooFinanceClient>` con BaseAddress `https://query1.finance.yahoo.com` y timeout 15s en `ApiServiceExtensions.cs`
  - [x] Registrar `IYahooFinanceClient → YahooFinanceClient` como typed client en DI
  - [x] Eliminar `.gitkeep` de `src/Server/Infrastructure/Integrations/Yahoo/`

- [x] Task 3: Servicio de tiempo y guard BMV (AC: CA-2)
  - [x] Crear `src/Server/Infrastructure/Time/ITimeService.cs`: `DateTimeOffset UtcNow { get; }`
  - [x] Crear `src/Server/Infrastructure/Time/SystemTimeService.cs`: `DateTimeOffset.UtcNow`
  - [x] Crear `src/Server/Application/Market/IBmvSchedule.cs`: `bool IsTradingHours(DateTimeOffset utcNow)`
  - [x] Crear `src/Server/Infrastructure/Jobs/Market/BmvSchedule.cs`: timezone `America/Mexico_City` (con fallback `"Central Standard Time"` en Windows via `RuntimeInformation.IsOSPlatform`); L-V 8:15am–3:15pm CDMX; sábado/domingo retorna false; NO verifica festivos en MVP
  - [x] Registrar `ITimeService → SystemTimeService` (Singleton) y `IBmvSchedule → BmvSchedule` (Singleton) en DI

- [x] Task 4: Repositorio de mercado e interfaces de aplicación (AC: CA-1, CA-3, CA-4, CA-5)
  - [x] Crear `src/Server/Application/Market/IMarketRepository.cs`:
    - `Task AddPriceSnapshotAsync(PriceSnapshot snapshot, CancellationToken ct)`
    - `Task<IReadOnlyList<PriceSnapshot>> GetLastSnapshotsAsync(Guid fibraId, int count, CancellationToken ct)` — ordena por `captured_at DESC`
    - `Task UpsertDailySnapshotAsync(DailySnapshot snapshot, CancellationToken ct)`
  - [x] Crear `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs`: implementación EF Core; upsert de DailySnapshot usando `FirstOrDefaultAsync` + add-or-update (sin raw SQL)
  - [x] Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/PriceSnapshotConfiguration.cs`: schema `market`, tabla `PriceSnapshot`, columnas snake_case, status como string, índice en `(fibra_id, captured_at)`
  - [x] Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/DailySnapshotConfiguration.cs`: schema `market`, tabla `DailySnapshot`, unique constraint `(fibra_id, date)`
  - [x] Agregar `DbSet<PriceSnapshot> PriceSnapshots` y `DbSet<DailySnapshot> DailySnapshots` a `AppDbContext.cs`
  - [x] Registrar `IMarketRepository → MarketRepository` como Scoped en DI
  - [x] Eliminar `.gitkeep` de `src/Server/Application/Market/` y `src/Server/Infrastructure/Jobs/Market/`

- [x] Task 5: Migración EF Core — schema market (AC: CA-1, CA-4)
  - [x] Ejecutar: `dotnet ef migrations add AddMarketSchema --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [x] Verificar migración generada: esquema `market`, tablas `PriceSnapshot` y `DailySnapshot`, índices correctos; no hay cambios en tablas de `auth` o `catalog`
  - [x] Ejecutar: `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`

- [x] Task 6: Job Hangfire — MarketPipelineJob (AC: CA-1, CA-2, CA-3, CA-5)
  - [x] Crear `src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs`:
    - Inyectar: `IBmvSchedule`, `ITimeService`, `IFibraRepository`, `IYahooFinanceClient`, `IMarketRepository`, `ILogger<MarketPipelineJob>`
    - Método `ExecuteAsync(CancellationToken ct)` con guard BMV, batch Yahoo, lógica Error/Critical
  - [x] Registrar job recurrente en `Program.cs`: `RecurringJob.AddOrUpdate<MarketPipelineJob>` cron `*/15 * * * 1-5`

- [x] Task 7: Actualizar PipelineFreshnessHealthCheck (AC: CA-1)
  - [x] `PipelineFreshnessHealthCheck.cs` ya es funcional — verifica failed jobs en Hangfire; compila correctamente con los nuevos DbSets

- [x] Task 8: Unit tests (AC: todos)
  - [x] Crear `tests/Unit/Infrastructure.Tests/Jobs/Market/BmvScheduleTests.cs`: 6 tests de horario (martes, límites, sábado)
  - [x] Crear `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs`: 5 tests con fakes manuales (fuera horario, éxito total, fallo parcial, primer fallo=Error, segundo fallo=Critical)
  - [x] Ejecutar `dotnet test` — 15/15 Infrastructure.Tests pasando, 0 regresiones en Domain.Tests y Application.Tests

- [x] Task 9: Build final
  - [x] `dotnet build FIBRADIS.slnx` — 0 errores, 0 advertencias
  - [x] Actualizar File List y Change Log en este story file

## Dev Notes

### Contexto del módulo Market

Esta es la primera historia del módulo `Market`. Los directorios ya existen con `.gitkeep` — eliminarlos al crear los primeros archivos reales. No agregar código en `Class1.cs` de Domain o Application.

**No hay frontend en esta historia.** La UI de frescura (carrusel, indicadores Fresh/Stale/crítico) es Story 3.2. Esta historia es puramente backend.

### Yahoo Finance — Detalles críticos de integración

**Endpoint en batch (recomendado):**
```
GET https://query1.finance.yahoo.com/v7/finance/quote?symbols=FUNO11.MX,DANHOS13.MX,...
```

**Formato de ticker BMV en Yahoo Finance:** `{TICKER}.MX`

| Ticker | Yahoo ticker |
|--------|-------------|
| FUNO11 | FUNO11.MX |
| DANHOS13 | DANHOS13.MX |
| TERRA13 | TERRA13.MX |
| FIBRAMQ12 | FIBRAMQ12.MX |
| FMTY14 | FMTY14.MX |
| FINN13 | FINN13.MX |
| FIHO12 | FIHO12.MX |
| VESTA15 | VESTA15.MX |
| HCITY17 | HCITY17.MX |
| PLUS18 | PLUS18.MX |

**Campos JSON del response:**
```json
{
  "quoteResponse": {
    "result": [
      {
        "symbol": "FUNO11.MX",
        "regularMarketPrice": 21.50,
        "regularMarketChange": -0.20,
        "regularMarketChangePercent": -0.92,
        "regularMarketVolume": 1500000,
        "fiftyTwoWeekHigh": 25.10,
        "fiftyTwoWeekLow": 18.40,
        "regularMarketOpen": 21.70,
        "regularMarketDayHigh": 21.90,
        "regularMarketDayLow": 21.30
      }
    ]
  }
}
```

**Consideraciones:**
- Si `query1` devuelve 401/403, probar con `query2.finance.yahoo.com` — misma URL, diferente host
- El response puede omitir símbolos que no encuentra — no es un error HTTP, simplemente no aparecen en `result[]`
- Manejar todos los campos numéricos como nullable en el DTO (`decimal?`) — Yahoo puede omitirlos fuera de horario
- No reintentar en el mismo ciclo ante fallo HTTP — el siguiente ciclo de 15 min actúa como reintento natural

### Horario BMV y timezone

```csharp
// Detectar TZ según OS (Windows vs Linux)
var tzId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? "Central Standard Time"
    : "America/Mexico_City";
var mexicoTz = TimeZoneInfo.FindSystemTimeZoneById(tzId);

// Convertir UTC → CDMX
var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow.UtcDateTime, mexicoTz);
var isWeekday = localNow.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
var openTime  = new TimeOnly(8, 15);
var closeTime = new TimeOnly(15, 15);
var localTime = TimeOnly.FromDateTime(localNow);
return isWeekday && localTime >= openTime && localTime < closeTime;
```

MVP: no se verifican días festivos mexicanos (se ejecuta en festivos — acceptable).

### Registro del job recurrente en Program.cs

Registrar después de `app.Build()` y antes de `app.Run()`, en el bloque donde ya existe el registro de Hangfire:

```csharp
var mexicoTz = ...; // misma lógica de TZ
RecurringJob.AddOrUpdate<MarketPipelineJob>(
    "market-pipeline",
    j => j.ExecuteAsync(CancellationToken.None),
    "*/15 * * * 1-5",   // cada 15 min, solo L-V (guard interno maneja ventana exacta)
    new RecurringJobOptions { TimeZone = mexicoTz }
);
```

El guard `IBmvSchedule.IsTradingHours()` dentro del job es la barrera precisa. El cron excluye sábado/domingo como optimización.

### Lógica de `crítico` — invariante clave

```csharp
// Al procesar un fallo en una FIBRA:
var recentSnapshots = await _marketRepo.GetLastSnapshotsAsync(fibraId, count: 1, ct);
var prevFailed = recentSnapshots.FirstOrDefault()?.Status
    is MarketDataStatus.Error or MarketDataStatus.Critical;
var statusForNew = prevFailed ? MarketDataStatus.Critical : MarketDataStatus.Error;
```

- `GetLastSnapshotsAsync` NO incluye el snapshot que se está creando; consulta solo registros ya persistidos
- Si no existe snapshot previo (primera corrida y falla) → `Error`, no `Critical`
- Dos fallos consecutivos bastan para `Critical` — no importa cuántos éxitos hubo antes

### Schema y tablas EF Core

Ver `FibraConfiguration.cs` como referencia del patrón de configuración EF Core. Aplicar el mismo patrón:

```csharp
// PriceSnapshotConfiguration.cs
builder.ToTable("PriceSnapshot", schema: "market");
builder.HasKey(x => x.Id);
builder.Property(x => x.Ticker).HasMaxLength(20).IsRequired();
builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
builder.Property(x => x.ErrorReason).HasMaxLength(500);
// LastPrice, DailyChange, etc. → HasColumnType("decimal(18,4)") o dejar inferencia
builder.HasIndex(x => new { x.FibraId, x.CapturedAt }).HasDatabaseName("IX_PriceSnapshot_FibraId_CapturedAt");

// DailySnapshotConfiguration.cs
builder.ToTable("DailySnapshot", schema: "market");
builder.HasAlternateKey(x => new { x.FibraId, x.Date }); // unique constraint
```

**DailySnapshot — upsert sin raw SQL:**
```csharp
var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoTz));
var existing = await _ctx.DailySnapshots
    .FirstOrDefaultAsync(d => d.FibraId == snap.FibraId && d.Date == today, ct);
if (existing is null)
{
    _ctx.DailySnapshots.Add(snap); // Open = snap.LastPrice (primer precio del día)
}
else
{
    existing.High   = Max(existing.High, snap.LastPrice);
    existing.Low    = Min(existing.Low, snap.LastPrice);
    existing.Close  = snap.LastPrice;
    existing.Volume = snap.Volume;  // usar el volumen más reciente
}
await _ctx.SaveChangesAsync(ct);
```

### Archivos a crear (NUEVOS)

```
src/Server/Domain/Market/
├── MarketDataStatus.cs           (enum)
├── PriceSnapshot.cs              (entidad)
└── DailySnapshot.cs              (entidad)

src/Server/Application/Market/
├── IMarketRepository.cs
└── IBmvSchedule.cs

src/Server/Infrastructure/
├── Integrations/Yahoo/
│   ├── IYahooFinanceClient.cs
│   ├── YahooQuoteResult.cs
│   └── YahooFinanceClient.cs
├── Jobs/Market/
│   ├── MarketPipelineJob.cs
│   └── BmvSchedule.cs
├── Persistence/
│   ├── Repositories/Market/
│   │   └── MarketRepository.cs
│   └── SqlServer/Configurations/Market/
│       ├── PriceSnapshotConfiguration.cs
│       └── DailySnapshotConfiguration.cs
└── Time/
    ├── ITimeService.cs
    └── SystemTimeService.cs

tests/Unit/Infrastructure/Jobs/Market/
├── BmvScheduleTests.cs
└── MarketPipelineJobTests.cs
```

### Archivos a modificar (UPDATE)

```
src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs
  → agregar DbSet<PriceSnapshot> PriceSnapshots y DbSet<DailySnapshot> DailySnapshots
  → registrar configuraciones en OnModelCreating

src/Server/Api/CompositionRoot/ApiServiceExtensions.cs
  → AddHttpClient("yahoo") con BaseAddress y timeout
  → Scoped: IYahooFinanceClient → YahooFinanceClient
  → Scoped: IMarketRepository → MarketRepository
  → Singleton: ITimeService → SystemTimeService
  → Singleton: IBmvSchedule → BmvSchedule

src/Server/Api/Program.cs
  → RecurringJob.AddOrUpdate<MarketPipelineJob> con cron L-V

src/Server/Api/HealthChecks/PipelineFreshnessHealthCheck.cs
  → leer y actualizar si es stub; inyectar AppDbContext para verificar frescura real
```

### Project Structure Notes

- Seguir Clean Architecture: `Domain → Application → Infrastructure → Api`; nunca cruzar capas hacia abajo
- El módulo `Market` no debe acceder directamente a tablas de `catalog` — pasar `IFibraRepository` por DI
- `IFibraRepository.GetAllActiveAsync()` ya existe en `Application/Catalog/` — no duplicar
- Los test de `BmvSchedule` deben construir la instancia con fechas UTC concretas, no `DateTime.Now` — el `ITimeService` en los tests del job debe ser un mock

### Referencias

- [Source: docs/req/architecture.md#Data freshness strategy] — estados fresh/stale/crítico, snapshots diarios
- [Source: docs/req/architecture.md#Naming] — convenciones de tablas `PascalCase`, columnas `snake_case`, schema `market`
- [Source: docs/req/architecture.md#Line 355] — `PriceSnapshot`, `DailySnapshot` son los nombres exactos de tablas
- [Source: _bmad-output/planning-artifacts/epics.md#Historia 3.1] — criterios de aceptación
- [Source: _bmad-output/planning-artifacts/epics.md#NFR-03] — cada 15 min, horario BMV, clasificación crítico en 2 ciclos fallidos
- [Source: _bmad-output/planning-artifacts/epics.md#NFR-04] — fresh ≤20min, stale >20min-6h, crítico >6h
- [Source: _bmad-output/planning-artifacts/convenciones-fibradis.md] — reglas de stack y código
- [Source: src/Server/Infrastructure/Persistence/SqlServer/Configurations/Catalog/FibraConfiguration.cs] — patrón EF Core configuration
- [Source: src/Server/Api/CompositionRoot/ApiServiceExtensions.cs] — patrón DI + Hangfire

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Yahoo Finance typed-client: Se usó patrón `AddHttpClient<IYahooFinanceClient, YahooFinanceClient>` en lugar de named client para evitar dependencia de `Microsoft.Extensions.Http` en Infrastructure.
- `IMarketRepository`: nombre de métodos ajustado de `AddSnapshotAsync/GetLatestSnapshotAsync` a `AddPriceSnapshotAsync/GetLastSnapshotsAsync` para coincidir exactamente con el story spec.
- Tests con `file sealed class`: tipo file-local no puede usarse en firmas de métodos de tipos no-file-local; se cambió a `internal sealed class`.

### Completion Notes List

- Todos los CAs implementados y verificados mediante unit tests con fakes manuales.
- `BmvSchedule` usa `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` para timezone cross-platform.
- `MarketPipelineJob` captura excepciones de batch Yahoo a nivel de todo el lote y marca todas las FIBRAs como Error/Critical según historial previo.
- El job recurrente se registra en `Program.cs` solo cuando Hangfire tiene SQL storage configurado.
- `PipelineFreshnessHealthCheck` ya era funcional — verifica Hangfire failed job count.
- ✅ Resuelto [Review][Patch]: `Program.cs` ahora usa timezone Mexico City (`Central Standard Time` / `America/Mexico_City`) en lugar de UTC para el RecurringJob.
- ✅ Resuelto [Review][Patch]: `DailySnapshot` tiene método `MergeUpdate` que preserva `Open` y toma max/min de High/Low; `MarketRepository.UpsertDailySnapshotAsync` lo usa. 8 tests nuevos en `Domain.Tests/Market/DailySnapshotTests.cs` verifican todos los casos.

### File List

**Nuevos:**
- tests/Unit/Domain.Tests/Market/DailySnapshotTests.cs
- src/Server/Domain/Market/MarketDataStatus.cs
- src/Server/Domain/Market/PriceSnapshot.cs
- src/Server/Domain/Market/DailySnapshot.cs
- src/Server/Application/Market/IBmvSchedule.cs
- src/Server/Application/Market/IMarketRepository.cs
- src/Server/Infrastructure/Time/ITimeService.cs
- src/Server/Infrastructure/Time/SystemTimeService.cs
- src/Server/Infrastructure/Integrations/Yahoo/YahooQuoteResult.cs
- src/Server/Infrastructure/Integrations/Yahoo/IYahooFinanceClient.cs
- src/Server/Infrastructure/Integrations/Yahoo/YahooFinanceClient.cs
- src/Server/Infrastructure/Jobs/Market/BmvSchedule.cs
- src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs
- src/Server/Infrastructure/Jobs/Market/MarketPipelineSchedule.cs
- src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs
- src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/PriceSnapshotConfiguration.cs
- src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/DailySnapshotConfiguration.cs
- src/Server/Infrastructure/Persistence/Migrations/20260519030126_AddMarketSchema.cs
- src/Server/Infrastructure/Persistence/Migrations/20260519030126_AddMarketSchema.Designer.cs
- tests/Unit/Infrastructure.Tests/Jobs/Market/BmvScheduleTests.cs
- tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs
- tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineScheduleTests.cs

**Modificados:**
- src/Server/Infrastructure/Infrastructure.csproj
- src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs
- src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
- src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs
- src/Server/Application/Catalog/IFibraRepository.cs
- src/Server/Api/CompositionRoot/ApiServiceExtensions.cs
- src/Server/Api/Program.cs
- src/Server/Domain/Market/DailySnapshot.cs

### Change Log

- 2026-05-19: Implementación completa de historia 3.1 — pipeline de mercado backend (claude-sonnet-4-6)
- 2026-05-18: Resolución de findings de code review — timezone CDMX en RecurringJob + MergeUpdate OHLC en DailySnapshot (claude-sonnet-4-6)

## Senior Developer Review (AI)

### Review Findings

- [x] [Review][Patch] El job recurrente sigue programado fuera del horario BMV [src/Server/Api/Program.cs:35]
- [x] [Review][Patch] `DailySnapshot` puede degradarse o perder OHLC al sobrescribir siempre con el último payload [src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs:24]
