# Story 3.2: Clasificación de frescura y estados en UI

Status: done

## Story

Como visitante público,
quiero ver indicadores de frescura precisos (Fresh / Stale / fuera-de-horario / crítico) sobre los precios en el carrusel de la Home, top movers y ficha pública,
para que siempre sepa si el precio que estoy viendo es actual.

## Acceptance Criteria

1. **Fresh:** Un precio actualizado hace menos de 20 min durante horario de mercado muestra indicador verde "Fresh" con timestamp de última actualización.
2. **Stale:** Un precio actualizado entre 20 min y 6 h atrás durante horario de mercado muestra indicador ámbar "Stale".
3. **Fuera de horario:** Cuando el mercado está cerrado (después de las 3:15pm CDMX o fin de semana), el precio muestra "fuera-de-horario" — no "Stale" ni "crítico".
4. **Crítico:** Un precio sin actualizar por 6 h o más durante una sesión de mercado abierta (o con `MarketDataStatus.Critical`) muestra indicador rojo "crítico".
5. **Sin datos:** Cuando no existe dato de precio para una FIBRA, los campos de precio muestran `—` y no se muestra indicador de frescura.

## Tasks / Subtasks

- [x] Task 1: Backend — FreshnessClassifier (AC: #1, #2, #3, #4, #5)
  - [x] 1.1 Crear `src/Server/Application/Market/FreshnessClassifier.cs` como clase estática con método `Classify(PriceSnapshot? snapshot, bool isMarketOpen, DateTimeOffset utcNow) → string?`
  - [x] 1.2 Crear `tests/Unit/Application.Tests/Market/FreshnessClassifierTests.cs` con tests para los 5 casos de los AC (fresh, stale, critical-by-age, critical-by-status, off-hours, no-data)

- [x] Task 2: Backend — IMarketRepository + implementación (AC: #1–#5)
  - [x] 2.1 Agregar `Task<IReadOnlyList<PriceSnapshot>> GetLatestSnapshotPerFibraAsync(CancellationToken ct)` a `src/Server/Application/Market/IMarketRepository.cs`
  - [x] 2.2 Implementar en `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs` (ver patrón en Dev Notes)

- [x] Task 3: Backend — Contrato API (AC: #1–#5)
  - [x] 3.1 Crear carpeta `src/Server/SharedApiContracts/Market/` y archivo `MarketSnapshotDto.cs`

- [x] Task 4: Backend — Endpoint GET /api/v1/market/snapshots (AC: #1–#5)
  - [x] 4.1 Crear `src/Server/Api/Endpoints/Public/MarketEndpoints.cs` con `MapMarket()` extension method
  - [x] 4.2 Registrar `app.MapMarket()` en `src/Server/Api/Program.cs` junto a `app.MapCatalog()`

- [x] Task 5: Backend — Verificación de build y tests
  - [x] 5.1 `dotnet build FIBRADIS.slnx` sin errores
  - [x] 5.2 `dotnet test tests/Unit/` — todos los tests pasan (incluyendo los nuevos de FreshnessClassifier)

- [x] Task 6: Frontend — Regenerar cliente API tipado
  - [x] 6.1 `npm run codegen:api` — verificar que la ruta `/api/v1/market/snapshots` aparece en los tipos generados

- [x] Task 7: Frontend — fibrasApi.ts (AC: #1–#5)
  - [x] 7.1 Agregar `fetchMarketSnapshots()` usando `apiClient.GET('/api/v1/market/snapshots')`

- [x] Task 8: Frontend — Helpers de formato
  - [x] 8.1 Crear `src/Web/Main/src/shared/lib/format-time.ts` con `formatRelativeTime(isoString: string): string`

- [x] Task 9: Frontend — PrecioSection.tsx (AC: #1–#5)
  - [x] 9.1 Agregar interfaz de props con campos de market data (lastPrice, dailyChange, dailyChangePct, capturedAt, freshnessStatus)
  - [x] 9.2 Mostrar precio con dos decimales, cambio% con color positivo/negativo, FreshnessBadge con timestamp relativo
  - [x] 9.3 Mostrar `—` sin badge cuando `freshnessStatus` es null (AC #5)

- [x] Task 10: Frontend — FibraPage.tsx (AC: #1–#5)
  - [x] 10.1 Agregar query `useQuery({ queryKey: ['market-snapshots'], queryFn: fetchMarketSnapshots })` con `staleTime: 60_000` y `refetchInterval: 5 * 60_000`
  - [x] 10.2 Derivar `marketData = snapshots.find(s => s.ticker === fibra?.ticker) ?? null`
  - [x] 10.3 Reemplazar bloque "Precio placeholder — Épica 3" en el sticky header con precio y badge reales
  - [x] 10.4 Pasar market data como props a `<PrecioSection />`

- [x] Task 11: Frontend — MercadoSection.tsx (AC: #1–#5)
  - [x] 11.1 Recibir market data como prop
  - [x] 11.2 Agregar cards de métricas: 52W High, 52W Low, Volumen (formato numérico apropiado)
  - [x] 11.3 Mantener el selector de período y el placeholder de gráfica — el chart llega en Story 3.3

- [x] Task 12: Frontend — Componentes de Home (AC: #1–#5)
  - [x] 12.1 `PriceCarousel.tsx`: reemplazar skeletons con cards reales (ticker, shortName, lastPrice, dailyChangePct, FreshnessBadge). Mantener skeleton mientras `isLoading`
  - [x] 12.2 `TopMovers.tsx`: mostrar top 5 por `|dailyChangePct|` descendente; verde para positivo, rojo para negativo
  - [x] 12.3 `QuickRanking.tsx`: tabla real con columnas Ticker / Precio / Cambio / Volumen, ordenada por ticker

- [x] Task 13: Frontend — Build final
  - [x] 13.1 `npm run build --workspace=src/Web/Main` sin errores (obligatorio por convenciones antes de `done`)

### Review Follow-ups (AI)

- [x] [AI-Review] Fix High #1: Agregar FreshnessBadge en TopMovers y pasar lastUpdated en PriceCarousel
- [x] [AI-Review] Fix High #2: Conversión segura de number|string antes de formatear en todos los componentes
- [x] [AI-Review] Fix Medium #3: Fallback en TopMovers — sort por ticker cuando dailyChangePct es null en todos
- [x] [AI-Review] Re-verificar build final tras correcciones

## Dev Notes

### Backend: FreshnessClassifier — lógica exacta

Umbrales según NFR-04:

| Condición | Estado devuelto |
|-----------|----------------|
| `snapshot == null` o `LastPrice == null` | `null` (sin badge) |
| `IBmvSchedule.IsTradingHours() == false` | `"off-hours"` |
| `snapshot.Status == MarketDataStatus.Critical` | `"critical"` |
| `utcNow - CapturedAt >= 6h` | `"critical"` |
| `utcNow - CapturedAt >= 20min` | `"stale"` |
| resto | `"fresh"` |

```csharp
// src/Server/Application/Market/FreshnessClassifier.cs
public static class FreshnessClassifier
{
    private static readonly TimeSpan FreshThreshold    = TimeSpan.FromMinutes(20);
    private static readonly TimeSpan CriticalThreshold = TimeSpan.FromHours(6);

    // Devuelve null cuando no hay dato (la UI no muestra badge).
    // Los valores de retorno deben coincidir exactamente con FreshnessStatus en freshness-badge.tsx:
    //   "fresh" | "stale" | "off-hours" | "critical"
    public static string? Classify(PriceSnapshot? snapshot, bool isMarketOpen, DateTimeOffset utcNow)
    {
        if (snapshot is null || !snapshot.LastPrice.HasValue)
            return null;

        if (!isMarketOpen)
            return "off-hours";

        if (snapshot.Status == MarketDataStatus.Critical)
            return "critical";

        var age = utcNow - snapshot.CapturedAt;
        if (age >= CriticalThreshold)
            return "critical";
        if (age >= FreshThreshold)
            return "stale";

        return "fresh";
    }
}
```

**Importante:** `FreshnessClassifier` vive en la capa `Application` pero sus tests van en `tests/Unit/Application.Tests/Market/` (el proyecto ya existe). No crear un proyecto de tests nuevo.

### Backend: Contrato API

```csharp
// src/Server/SharedApiContracts/Market/MarketSnapshotDto.cs
namespace SharedApiContracts.Market;

public record MarketSnapshotDto(
    Guid FibraId,
    string Ticker,
    decimal? LastPrice,
    decimal? DailyChange,
    decimal? DailyChangePct,
    long? Volume,
    decimal? Week52High,
    decimal? Week52Low,
    string? CapturedAt,        // ISO 8601 UTC, ej. "2026-05-19T14:30:00Z", null si no hay dato
    string? FreshnessStatus    // "fresh" | "stale" | "off-hours" | "critical" | null (null = sin badge)
);
```

### Backend: Nuevo endpoint

```
GET /api/v1/market/snapshots
Auth: anonymous
Response: IReadOnlyList<MarketSnapshotDto>  (una entrada por FIBRA activa)
```

Patrón del handler — seguir exactamente el estilo de `CatalogEndpoints.cs`:

```csharp
// src/Server/Api/Endpoints/Public/MarketEndpoints.cs
public static class MarketEndpoints
{
    public static IEndpointRouteBuilder MapMarket(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/market").WithTags("Market");

        group.MapGet("/snapshots", async (
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            IBmvSchedule bmvSchedule,
            CancellationToken ct) =>
        {
            var utcNow = DateTimeOffset.UtcNow;
            var isMarketOpen = bmvSchedule.IsTradingHours(utcNow);

            var fibras = await fibraRepo.GetAllActiveAsync(ct);
            var latestSnapshots = await marketRepo.GetLatestSnapshotPerFibraAsync(ct);
            var snapshotByFibra = latestSnapshots.ToDictionary(s => s.FibraId);

            var results = fibras.Select(fibra =>
            {
                snapshotByFibra.TryGetValue(fibra.Id, out var snap);
                var freshnessStatus = FreshnessClassifier.Classify(snap, isMarketOpen, utcNow);

                return new MarketSnapshotDto(
                    fibra.Id,
                    fibra.Ticker,
                    snap?.LastPrice,
                    snap?.DailyChange,
                    snap?.DailyChangePct,
                    snap?.Volume,
                    snap?.Week52High,
                    snap?.Week52Low,
                    snap?.CapturedAt.ToString("O"),
                    freshnessStatus);
            }).ToList();

            return Results.Ok(results);
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<MarketSnapshotDto>>(StatusCodes.Status200OK);

        return app;
    }
}
```

Registrar en `Program.cs` después de `app.MapCatalog()`:
```csharp
app.MapMarket();
```

`IFibraRepository.GetAllActiveAsync()` ya existe (fue agregado en Story 3.1 para el pipeline job).
`IBmvSchedule` ya está registrado como Singleton en `ApiServiceExtensions.cs`.

### Backend: IMarketRepository.GetLatestSnapshotPerFibraAsync

Agregar a la interfaz:
```csharp
// IMarketRepository.cs — agregar:
Task<IReadOnlyList<PriceSnapshot>> GetLatestSnapshotPerFibraAsync(CancellationToken ct = default);
```

Implementación recomendada en `MarketRepository.cs` (EF Core):
```csharp
public async Task<IReadOnlyList<PriceSnapshot>> GetLatestSnapshotPerFibraAsync(CancellationToken ct = default)
{
    // Subquery: máximo captured_at por fibra_id
    var latestByFibra = db.PriceSnapshots
        .GroupBy(p => p.FibraId)
        .Select(g => new { FibraId = g.Key, MaxDate = g.Max(p => p.CapturedAt) });

    return await db.PriceSnapshots
        .Where(p => latestByFibra
            .Any(l => l.FibraId == p.FibraId && l.MaxDate == p.CapturedAt))
        .ToListAsync(ct);
}
```

Si EF Core no traduce la query anterior correctamente (verificar con SQL Server Profiler o logs), usar el fallback:
```csharp
// Fallback — correcto para ~10 FIBRAs (no escalaría con cientos):
var fibraIds = await db.PriceSnapshots.Select(p => p.FibraId).Distinct().ToListAsync(ct);
var results = new List<PriceSnapshot>(fibraIds.Count);
foreach (var id in fibraIds)
{
    var latest = await db.PriceSnapshots
        .Where(p => p.FibraId == id)
        .OrderByDescending(p => p.CapturedAt)
        .FirstOrDefaultAsync(ct);
    if (latest is not null) results.Add(latest);
}
return results;
```

### Frontend: Patrón TanStack Query compartido

Usar la misma `queryKey: ['market-snapshots']` en TODOS los componentes. TanStack Query deduplicará la llamada HTTP — solo se hace UNA request aunque PriceCarousel, TopMovers, QuickRanking y FibraPage llamen a la misma función.

```typescript
// Patrón estándar para todos los componentes que consumen market data:
const { data: snapshots = [], isLoading } = useQuery({
  queryKey: ['market-snapshots'],
  queryFn: fetchMarketSnapshots,
  staleTime: 60_000,           // 1 min — el pipeline actualiza c/15 min
  refetchInterval: 5 * 60_000, // refetch automático cada 5 min
})
```

Para FibraPage (un snapshot específico):
```typescript
const marketData = snapshots.find(s => s.ticker === fibra?.ticker) ?? null
```

### Frontend: Estado actual de los archivos a modificar

**`PrecioSection.tsx`** — actualmente sin props, muestra `—` hardcodeado con `<FreshnessBadge status="off-hours" />`. Reemplazar con props reales. La `FreshnessBadge` existente acepta `status` y `lastUpdated?: string`.

**`FibraPage.tsx`** — hay DOS lugares con precio placeholder que deben actualizarse:
1. Sticky header (~línea 88): bloque con comentario "Precio placeholder — Épica 3 reemplaza este bloque con precio real"
2. Render de `<PrecioSection />` (actualmente sin props)

**`MercadoSection.tsx`** — tiene selector de período (1M/3M/6M/1A) y placeholder de gráfica. Para esta historia: agregar grid de métricas (52W High, 52W Low, Volumen) debajo del selector. El gráfico de historial de precios llega en Story 3.3 — mantener el placeholder.

**`PriceCarousel.tsx`** — 9 skeleton cards con `animate-pulse`. Reemplazar con datos reales; mantener skeleton mientras `isLoading`.

**`TopMovers.tsx`** — 5 skeleton rows. Reemplazar con los 5 mayores movimientos del día (`|dailyChangePct|` descendente). Si todos tienen `dailyChangePct == null` (sin datos), mostrar los que existan ordenados por ticker.

**`QuickRanking.tsx`** — skeleton grid 4 columnas. Reemplazar con tabla real: Ticker / Precio / Cambio / Volumen. Default: ordenar por ticker alfabéticamente.

### Frontend: Helper formatRelativeTime

```typescript
// src/Web/Main/src/shared/lib/format-time.ts
export function formatRelativeTime(isoString: string): string {
  const diffMs = Date.now() - new Date(isoString).getTime()
  const minutes = Math.floor(diffMs / 60_000)
  if (minutes < 1) return 'ahora'
  if (minutes < 60) return `hace ${minutes} min`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `hace ${hours} h`
  return `hace ${Math.floor(hours / 24)} días`
}
```

Pasar como `lastUpdated` prop de `FreshnessBadge` cuando `capturedAt != null`.

### Frontend: Reglas de presentación (convenciones del proyecto)

- Precios nulos → `—` (nunca `0`, nunca `null.toFixed()`)
- `DailyChangePct` positivo → clase `text-positive`; negativo → `text-negative`
- Importar `FreshnessBadge` desde `@/shared/ui/freshness-badge` (alias `@/`, no rutas relativas)
- `react-router` v7: `import { useParams } from 'react-router'` — nunca `react-router-dom`
- NO añadir dependencias npm nuevas (no date-fns, no swiper/carousel library)
- Tailwind v4 — verificar que las clases existen en v4 antes de usarlas

### Anti-patrones críticos a evitar

1. **NO calcular freshness en el frontend** — el backend devuelve `freshnessStatus` calculado usando `IBmvSchedule` (que conoce el horario BMV)
2. **NO crear un componente FreshnessBadge nuevo** — usar el existente en `@/shared/ui/freshness-badge`
3. **NO ejecutar `npx shadcn@latest add`** sin aprobación explícita
4. **NO olvidar `npm run codegen:api`** antes de escribir código frontend — el tipo `paths` debe incluir `/api/v1/market/snapshots`
5. **NO pasar `status="off-hours"` hardcodeado** en los componentes actualizados — siempre usar el valor del API

### Project Structure Notes

**Archivos nuevos:**
```
src/Server/Application/Market/FreshnessClassifier.cs
src/Server/SharedApiContracts/Market/MarketSnapshotDto.cs
src/Server/Api/Endpoints/Public/MarketEndpoints.cs
src/Web/Main/src/shared/lib/format-time.ts
tests/Unit/Application.Tests/Market/FreshnessClassifierTests.cs
```

**Archivos modificados:**
```
src/Server/Application/Market/IMarketRepository.cs
  → agregar GetLatestSnapshotPerFibraAsync

src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs
  → implementar GetLatestSnapshotPerFibraAsync

src/Server/Api/Program.cs
  → registrar app.MapMarket()

src/Web/Main/src/api/fibrasApi.ts
  → agregar fetchMarketSnapshots()

src/Web/Main/src/modules/ficha-publica/sections/PrecioSection.tsx
src/Web/Main/src/modules/ficha-publica/FibraPage.tsx
src/Web/Main/src/modules/ficha-publica/sections/MercadoSection.tsx
src/Web/Main/src/modules/home/PriceCarousel.tsx
src/Web/Main/src/modules/home/TopMovers.tsx
src/Web/Main/src/modules/home/QuickRanking.tsx
```

**NO tocar:**
```
src/Web/Main/src/shared/ui/freshness-badge.tsx   ← componente completo, no modificar
src/Server/Domain/Market/                         ← no requiere cambios de dominio
src/Server/Infrastructure/Jobs/Market/            ← no requiere cambios en el pipeline
```

### Testing

**Backend (obligatorio — workflow-rules.md):**

`tests/Unit/Application.Tests/Market/FreshnessClassifierTests.cs` debe cubrir:
- Fresh: `capturedAt = utcNow - 10min`, mercado abierto → `"fresh"`
- Stale: `capturedAt = utcNow - 90min`, mercado abierto → `"stale"`
- Crítico por edad: `capturedAt = utcNow - 7h`, mercado abierto → `"critical"`
- Crítico por status: `Status = MarketDataStatus.Critical`, mercado abierto, edad < 20min → `"critical"`
- Fuera de horario: mercado cerrado → `"off-hours"` (independiente de la edad)
- Sin datos: `snapshot = null` → `null`

Ejecutar: `dotnet test tests/Unit/` — resultado esperado: todos los tests pasan.

**Frontend:**
- No se requieren tests de componentes React para MVP.

### Referencias

- [Source: _bmad-output/planning-artifacts/epics.md#Historia 3.2] — user story y ACs originales
- [Source: _bmad-output/planning-artifacts/epics.md#NFR-04] — umbrales: fresh ≤20min, stale >20min <6h, crítico ≥6h
- [Source: docs/req/architecture.md#Data freshness strategy] — estados explícitos fresh/stale/partial/error/null
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md] — FreshnessBADGE por ítem en carrusel, nunca precio sin estado; ficha `$XX.XX +1.23% [Fresh · hace N min]`
- [Source: src/Web/Main/src/shared/ui/freshness-badge.tsx] — `FreshnessStatus = 'fresh' | 'stale' | 'off-hours' | 'critical'`; prop `lastUpdated?: string`
- [Source: src/Server/Application/Market/IBmvSchedule.cs] — `IsTradingHours(DateTimeOffset utcNow): bool`
- [Source: src/Server/Api/Endpoints/Public/CatalogEndpoints.cs] — patrón exacto de endpoint (extension method, AllowAnonymous, Produces)
- [Source: src/Server/Api/Program.cs] — punto de registro: `app.MapMarket()` va junto a `app.MapCatalog()`
- [Source: _bmad-output/implementation-artifacts/3-1-pipeline-de-mercado-ingesta-y-snapshots.md] — `IFibraRepository.GetAllActiveAsync()` ya existe; patrón EF Core de MarketRepository
- [Source: _bmad-output/planning-artifacts/convenciones-fibradis.md] — react-router v7, openapi-fetch, sin deps nuevas, alias @/, no rutas relativas

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- `FakeMarketRepository` en Infrastructure.Tests no implementaba `GetLatestSnapshotPerFibraAsync` → agregado con retorno vacío.
- Build del Api bloqueado por DLLs en uso (proceso Api corriendo en Debug) → se usó `--configuration Release` para regenerar `Api.json`.
- Props de PrecioSection/MercadoSection usaban `number | null | undefined` pero el schema genera `string | number | null | undefined` → actualizadas interfaces.

### Completion Notes List

- FreshnessClassifier implementado como clase estática en Application.Market con lógica exacta del story file. 7 tests unitarios cubren todos los ACs (fresh, stale, critical-por-edad, critical-por-status, off-hours, sin-datos, lastPrice-null).
- GetLatestSnapshotPerFibraAsync implementado con subquery EF Core (GROUP BY + MAX).
- FakeMarketRepository en Infrastructure.Tests actualizado para implementar la nueva interfaz.
- Endpoint GET /api/v1/market/snapshots creado y registrado en Program.cs. Llama a FreshnessClassifier en el servidor — el frontend solo muestra el valor recibido.
- codegen:api regenerado con Release build; `MarketSnapshotDto` y ruta `/api/v1/market/snapshots` presentes en schema.d.ts.
- fetchMarketSnapshots() agregado a fibrasApi.ts.
- formatRelativeTime helper creado en shared/lib/format-time.ts.
- PrecioSection, FibraPage, MercadoSection actualizados con props reales de market data. Placeholder eliminado del sticky header.
- PriceCarousel, TopMovers, QuickRanking usan queryKey ['market-snapshots'] para deduplicación; mantienen skeleton durante isLoading.
- `npm run build --workspace=src/Web/Main` exitoso sin errores ni advertencias de tipo.
- Total tests: 8 Application.Tests + 16 Infrastructure.Tests = 24 tests, todos pasan.
- ✅ Resolved review finding [High]: TopMovers sin FreshnessBadge → agregados badge + lastUpdated en cada fila.
- ✅ Resolved review finding [High]: Casts inseguros number|string → helper toNum() en format-time.ts; aplicado en PrecioSection, FibraPage, MercadoSection, PriceCarousel, TopMovers, QuickRanking.
- ✅ Resolved review finding [Medium]: Fallback sort TopMovers — cuando todos tienen dailyChangePct == null, se ordena por ticker; si alguno tiene datos, se ordena por |changePct| descendente.

### File List

**Nuevos:**
- src/Server/Application/Market/FreshnessClassifier.cs
- src/Server/SharedApiContracts/Market/MarketSnapshotDto.cs
- src/Server/Api/Endpoints/Public/MarketEndpoints.cs
- src/Web/Main/src/shared/lib/format-time.ts
- tests/Unit/Application.Tests/Market/FreshnessClassifierTests.cs

**Modificados:**
- src/Server/Application/Market/IMarketRepository.cs
- src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs
- src/Server/Api/Program.cs
- src/Web/Main/src/api/fibrasApi.ts
- src/Web/SharedApiClient/schema.d.ts
- src/Web/Main/src/modules/ficha-publica/sections/PrecioSection.tsx
- src/Web/Main/src/modules/ficha-publica/FibraPage.tsx
- src/Web/Main/src/modules/ficha-publica/sections/MercadoSection.tsx
- src/Web/Main/src/modules/home/PriceCarousel.tsx
- src/Web/Main/src/modules/home/TopMovers.tsx
- src/Web/Main/src/modules/home/QuickRanking.tsx
- tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs
- _bmad-output/implementation-artifacts/sprint-status.yaml

**Modificados en review follow-up (2026-05-19):**
- src/Web/Main/src/shared/lib/format-time.ts (agregado toNum helper)
- src/Web/Main/src/modules/home/TopMovers.tsx (FreshnessBadge, lastUpdated, toNum, fallback sort)
- src/Web/Main/src/modules/home/PriceCarousel.tsx (lastUpdated en FreshnessBadge, toNum)
- src/Web/Main/src/modules/ficha-publica/sections/PrecioSection.tsx (toNum)
- src/Web/Main/src/modules/ficha-publica/sections/MercadoSection.tsx (toNum)
- src/Web/Main/src/modules/ficha-publica/FibraPage.tsx (toNum en sticky header)
- src/Web/Main/src/modules/home/QuickRanking.tsx (toNum)

### Change Log

- 2026-05-18: Historia 3.2 implementada — FreshnessClassifier backend, endpoint GET /api/v1/market/snapshots, regeneración codegen, y actualización de 9 componentes frontend (PrecioSection, FibraPage, MercadoSection, PriceCarousel, TopMovers, QuickRanking).
- 2026-05-19: Resueltos 3 findings del code review — helper toNum para conversión segura number|string, FreshnessBadge+lastUpdated en TopMovers, fallback sort alfabético en TopMovers. Build OK, 24 tests pasan.

## Senior Developer Review (AI)

### Findings

1. **High** — Los indicadores de frescura no cumplen el alcance de los AC en Home. La historia exige indicadores sobre "el carrusel de la Home, top movers y ficha pública" y además AC #1 pide timestamp para el estado Fresh. `TopMovers.tsx` no renderiza `FreshnessBadge` en ningún caso, y `PriceCarousel.tsx` sí renderiza badge pero nunca pasa `lastUpdated`, por lo que el timestamp nunca aparece ahí. Esto deja parte del flujo principal sin la señal de frescura requerida. Referencias: `src/Web/Main/src/modules/home/TopMovers.tsx:44-64`, `src/Web/Main/src/modules/home/PriceCarousel.tsx:62-69`, AC #1 y Story.

2. **High** — La UI sigue tratando los decimales OpenAPI como `number` por cast, aunque el cliente tipado los declara como `number | string`. Si el backend serializa `decimal` como string, expresiones como `(snap.lastPrice as number).toFixed(2)` y `Math.abs(a.dailyChangePct as number)` fallan en runtime o producen resultados incorrectos. El cambio reconoció esta unión en las props, pero no hizo conversión real antes de formatear. Referencias: `src/Web/SharedApiClient/schema.d.ts:355-368`, `src/Web/Main/src/modules/ficha-publica/sections/PrecioSection.tsx:21-35`, `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx:123-130`, `src/Web/Main/src/modules/ficha-publica/sections/MercadoSection.tsx:12-15,28,34,40`, `src/Web/Main/src/modules/home/PriceCarousel.tsx:59-66`, `src/Web/Main/src/modules/home/TopMovers.tsx:14-16,53-58`, `src/Web/Main/src/modules/home/QuickRanking.tsx:4-9,57,67`.

3. **Medium** — El fallback pedido para `TopMovers` cuando no hay `dailyChangePct` no está implementado. Las Dev Notes dicen: "Si todos tienen `dailyChangePct == null` (sin datos), mostrar los que existan ordenados por ticker". El código actual siempre ordena por `Math.abs(dailyChangePct)` usando `-1` para null y luego hace `slice(0, 5)`, así que en ese escenario devuelve el orden original del API, no orden alfabético por ticker. Referencia: `src/Web/Main/src/modules/home/TopMovers.tsx:12-18`.

### Verification

- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj --filter FreshnessClassifierTests` → 7 passed, 0 failed
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter MarketPipelineJobTests` → 5 passed, 0 failed
- `npm run build --workspace=src/Web/Main` → OK

### Review Outcome

- Result: changes requested
- Story status returned to `in-progress` pending fixes for the findings above

### Follow-up Review (2026-05-19)

No findings. Los 3 hallazgos previos quedaron resueltos:

- `TopMovers` ahora renderiza `FreshnessBadge` con `lastUpdated`.
- `PriceCarousel` ahora pasa `lastUpdated` al badge.
- La UI convierte `number | string` de forma segura con `toNum()` antes de formatear o comparar.
- El fallback de `TopMovers` ahora ordena por ticker cuando todos los `dailyChangePct` son `null`.

### Follow-up Verification

- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj --filter FreshnessClassifierTests` → 7 passed, 0 failed
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter MarketPipelineJobTests` → 5 passed, 0 failed
- `npm run build --workspace=src/Web/Main` → OK

### Final Review Outcome

- Result: approved
- Story status moved to `done`
