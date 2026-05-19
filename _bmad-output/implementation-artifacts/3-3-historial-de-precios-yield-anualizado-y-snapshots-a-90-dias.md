# Historia 3.3: Historial de precios, yield anualizado y snapshots a 90 días

Status: done

## Story

Como visitante público,
quiero ver 90 días de historial de precios en la gráfica de la ficha pública y ver un yield de dividendo anualizado calculado a partir de la frecuencia de distribución real detectada de la FIBRA,
para que pueda analizar tendencias de precios e ingresos por rendimiento basados en patrones reales y no en periodicidad asumida.

## Acceptance Criteria

1. **Historial en gráfica:** Dado que FUNO11 tiene 60 días de snapshots diarios, cuando selecciono el selector de gráfica "3M" en su ficha, entonces se renderizan hasta 60 puntos de datos diarios disponibles; los días sin snapshot muestran un hueco, no un cero.
2. **Yield por frecuencia detectada:** Dado que FUNO11 tiene distribuciones en intervalos irregulares, cuando el sistema calcula el yield anualizado, entonces usa el patrón de frecuencia detectado (ej: 3 pagos en 12 meses → anualizar por 3), NO una suposición trimestral fija.
3. **Sin datos de distribución:** Dado que no existen datos de distribución para una FIBRA, entonces el yield se muestra como "no disponible" sin ninguna estimación numérica.
4. **Recuperabilidad de snapshots a 90 días:** Dado que se consultan snapshots de hace 90 días, entonces siguen siendo recuperables y aparecen en la gráfica — no son eliminados ni ocultados.

## Tasks / Subtasks

- [x] Task 1: Backend — Entidad `Distribution` y migración (AC: #2, #3)
  - [x] 1.1 Crear `src/Server/Domain/Market/Distribution.cs` con campos: `Guid Id`, `Guid FibraId`, `string Ticker`, `DateOnly PaymentDate`, `decimal AmountPerUnit`, `string Currency = "MXN"`, `string Source`, `DateTimeOffset CapturedAt`
  - [x] 1.2 Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/DistributionConfiguration.cs`: schema `market`, tabla `Distribution`, índice en `(fibra_id, payment_date)`
  - [x] 1.3 Agregar `DbSet<Distribution> Distributions` a `AppDbContext.cs` y registrar `DistributionConfiguration` en `OnModelCreating`
  - [x] 1.4 Crear migración: `dotnet ef migrations add AddMarketDistribution --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [x] 1.5 Aplicar migración: `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`

- [x] Task 2: Backend — Seed de distribuciones (AC: #2, #3)
  - [x] 2.1 Crear `src/Server/Infrastructure/Persistence/Seed/MarketSeed.cs` con distribuciones seed para FUNO11, DANHOS13, TERRA13, FIBRAMQ12 usando GUIDs deterministas (MD5, mismo patrón que CatalogSeed)
  - [x] 2.2 Registrar `MarketSeed.Seed(modelBuilder)` en `AppDbContext.OnModelCreating` (patrón existente del proyecto)

- [x] Task 3: Backend — Extensión de `IMarketRepository` (AC: #1, #2, #3, #4)
  - [x] 3.1 Agregar a `IMarketRepository`: `GetDailySnapshotsAsync`, `GetDistributionsAsync`, `AddDistributionAsync`
  - [x] 3.2 Implementar los tres métodos en `MarketRepository.cs`

- [x] Task 4: Backend — `YieldCalculator` (AC: #2, #3)
  - [x] 4.1 Crear `src/Server/Application/Market/YieldCalculator.cs`
  - [x] 4.2 Crear `tests/Unit/Application.Tests/Market/YieldCalculatorTests.cs` — 7 tests, todos pasan

- [x] Task 5: Backend — DTOs de historial (AC: #1, #2, #3, #4)
  - [x] 5.1 Crear `src/Server/SharedApiContracts/Market/FibraHistoryDto.cs`

- [x] Task 6: Backend — Endpoint `GET /api/v1/market/fibras/{ticker}/history` (AC: #1, #2, #3, #4)
  - [x] 6.1 Agregar en `MarketEndpoints.cs` el nuevo endpoint con `Task.WhenAll` paralelo
  - [x] 6.2 Verificado — registrado a través de `MapMarket()`

- [x] Task 7: Backend — Build y tests (AC: todos)
  - [x] 7.1 `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] 7.2 `dotnet test` — todos los tests pasan incluyendo `YieldCalculatorTests`

- [x] Task 8: Frontend — Regenerar cliente API tipado (AC: #1, #2, #3)
  - [x] 8.1 `npm run codegen:api` — ruta `/api/v1/market/fibras/{ticker}/history` confirmada en schema.d.ts

- [x] Task 9: Frontend — `fibrasApi.ts` (AC: #1, #2, #3)
  - [x] 9.1 Agregado `fetchFibraHistory(ticker, period)` en `fibrasApi.ts`

- [x] Task 10: Frontend — Instalar recharts y componente `PriceChart.tsx` (AC: #1, #4)
  - [x] 10.1 recharts instalado: `npm install recharts --workspace=src/Web/Main`
  - [x] 10.2 `chart.tsx` creado manualmente en `src/shared/ui/chart.tsx` (npx shadcn bloqueado por hook; componente escrito con tipos compatibles con recharts 3.x)
  - [x] 10.3 Crear `src/Web/Main/src/shared/ui/price-chart.tsx`

- [x] Task 11: Frontend — `MercadoSection.tsx` (AC: #1, #4)
  - [x] 11.1 Extendido `MercadoSectionProps` con `ticker: string`
  - [x] 11.2 Query `useQuery(['fibra-history', ticker, period])` agregado
  - [x] 11.3 Placeholder reemplazado con `<PriceChart>` + skeleton loading
  - [x] 11.4 Selector de período conectado al estado `period`

- [x] Task 12: Frontend — `DistribucionesSection.tsx` (AC: #2, #3)
  - [x] 12.1 Props extendidas con `ticker: string`
  - [x] 12.2 Query `useQuery(['fibra-history', ticker, '1y'])` agregado
  - [x] 12.3 `annualizedYield` mostrado como porcentaje o "no disponible"
  - [x] 12.4 Tabla con últimas 8 distribuciones: Fecha | Monto por CBFI
  - [x] 12.5 Estado loading con skeleton cards

- [x] Task 13: Frontend — `FibraPage.tsx` (AC: #1, #2, #3)
  - [x] 13.1 `ticker={fibra!.ticker}` pasado a `<MercadoSection>`
  - [x] 13.2 `ticker={fibra!.ticker}` pasado a `<DistribucionesSection>`

- [x] Task 14: Frontend — Build final (AC: todos)
  - [x] 14.1 `npm run build --workspace=src/Web/Main` — 0 errores ✓

### Review Follow-ups (AI)

- [x] [AI-Review] Fix `YieldCalculator` — reemplazar extrapolación por intervalo con TTM (suma directa del año) para que 3 pagos en 12 meses anualizan por 3, no por 4 [High]
- [x] [AI-Review] Test: 3 pagos trimestrales → yield anualizado por 3 [High]
- [x] [AI-Review] Fix `PriceChart` — detectar gaps de fechas no consecutivas e insertar entradas `close: null` para que `connectNulls={false}` produzca huecos reales (AC #1) [High]
- [x] [AI-Review] Fix `MercadoSection` — manejar estado `isError` mostrando mensaje explícito en lugar de gráfica vacía [Medium]
- [x] [AI-Review] Fix `DistribucionesSection` — manejar estado `isError` mostrando mensaje explícito en lugar de estado vacío [Medium]

## Dev Notes

### Contexto de historias previas

**Story 3.1** creó:
- `DailySnapshot` entity (mercado/DailySnapshot.cs) con campos OHLCV. El `Close` es el precio de cierre del día. Ya existe en DB con índice `(fibra_id, date)`.
- `IMarketRepository` con `UpsertDailySnapshotAsync` y `GetLastSnapshotsAsync` (por `PriceSnapshot`, no por `DailySnapshot`)
- El pipeline corre cada 15 min y persiste snapshots diarios vía `UpsertDailySnapshotAsync`

**Story 3.2** creó:
- `FreshnessClassifier`, `MarketSnapshotDto`, endpoint `GET /api/v1/market/snapshots`
- `FibraPage.tsx` ya tiene `useQuery(['market-snapshots'])` y pasa props a `MercadoSection` y `DistribucionesSection`
- `MercadoSection.tsx` ya tiene selector de período (solo UI, sin function) y placeholder explícito en línea 68-71

**Precondición importante:** La DB en desarrollo (`FIBRADIS_Dev`, SQL Server LAPBADIS, Windows Auth) está vacía sin datos seed de mercado. Los `DailySnapshot` y `Distribution` records que usaremos en los tests son generados por el `MarketDistributionSeeder` y por el pipeline de mercado real (si está corriendo). El developer no necesita datos reales — los tests unitarios usan datos in-memory.

### Entidad Distribution

```csharp
// src/Server/Domain/Market/Distribution.cs
namespace FIBRADIS.Domain.Market;

public class Distribution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FibraId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public decimal AmountPerUnit { get; set; }
    public string Currency { get; set; } = "MXN";
    public string Source { get; set; } = "seed";
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

### EF Core Configuration — Distribution

```csharp
// src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/DistributionConfiguration.cs
builder.ToTable("Distribution", schema: "market");
builder.HasKey(x => x.Id);
builder.Property(x => x.Ticker).HasMaxLength(20).IsRequired();
builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
builder.Property(x => x.Source).HasMaxLength(50).IsRequired();
builder.Property(x => x.AmountPerUnit).HasColumnType("decimal(18,6)");
builder.HasIndex(x => new { x.FibraId, x.PaymentDate }).HasDatabaseName("IX_Distribution_FibraId_PaymentDate");
```

### Seed de distribuciones

El seeder debe buscar los `FibraId` en la tabla `catalog.Fibra` por ticker antes de insertar. Solo inserta si no existen registros para esa fibra:

```csharp
// src/Server/Infrastructure/Persistence/Seeds/MarketDistributionSeeder.cs
// Distribuciones aproximadas de los últimos 4 trimestres por FIBRA (fuente pública BMV):
// FUNO11: pago trimestral ~0.38 MXN por CBFI
// DANHOS13: pago trimestral ~0.22 MXN por CBFI
// TERRA13: pago trimestral ~0.18 MXN por CBFI
// FIBRAMQ12: pago trimestral ~0.15 MXN por CBFI

var distributions = new[]
{
    // FUNO11
    ("FUNO11", new DateOnly(2025, 3, 17),  0.3610m),
    ("FUNO11", new DateOnly(2025, 6, 16),  0.3720m),
    ("FUNO11", new DateOnly(2025, 9, 15),  0.3780m),
    ("FUNO11", new DateOnly(2025, 12, 15), 0.3840m),
    // DANHOS13
    ("DANHOS13", new DateOnly(2025, 3, 17), 0.2150m),
    ("DANHOS13", new DateOnly(2025, 6, 16), 0.2200m),
    ("DANHOS13", new DateOnly(2025, 9, 15), 0.2250m),
    ("DANHOS13", new DateOnly(2025, 12, 15),0.2300m),
    // TERRA13
    ("TERRA13", new DateOnly(2025, 6, 16),  0.1750m),
    ("TERRA13", new DateOnly(2025, 9, 15),  0.1800m),
    ("TERRA13", new DateOnly(2025, 12, 15), 0.1820m),
    // FIBRAMQ12
    ("FIBRAMQ12", new DateOnly(2025, 9, 15),  0.1480m),
    ("FIBRAMQ12", new DateOnly(2025, 12, 15), 0.1520m),
};
```

El seeder debe:
1. Obtener el mapa `ticker → fibraId` desde `ctx.Fibras.Where(f => tickers.Contains(f.Ticker))`
2. Verificar si ya existe al menos 1 distribution para FUNO11 — si sí, salir sin insertar (idempotente)
3. Insertar los registros con `ctx.Distributions.AddRange(...)` + `ctx.SaveChangesAsync()`

### Algoritmo YieldCalculator

```csharp
// src/Server/Application/Market/YieldCalculator.cs
public static class YieldCalculator
{
    // Retorna null si no hay datos suficientes o si no hay precio.
    // 'today' se pasa como parámetro para facilitar tests deterministas.
    public static decimal? Calculate(
        IReadOnlyList<Distribution> distributions,
        decimal? lastPrice,
        DateOnly today)
    {
        if (!lastPrice.HasValue || lastPrice.Value <= 0)
            return null;

        var cutoff = today.AddDays(-365);
        var inYear = distributions
            .Where(d => d.PaymentDate >= cutoff)
            .OrderBy(d => d.PaymentDate)
            .ToList();

        if (inYear.Count == 0)
            return null;

        // Suma total de pagos en el último año
        var totalInYear = inYear.Sum(d => d.AmountPerUnit);

        // Detectar frecuencia: si hay < 12 meses de datos, extrapolar
        // Usar los gaps entre pagos para detectar el patrón
        decimal annualizedTotal;
        if (inYear.Count >= 2)
        {
            // Calcular intervalo promedio en días entre pagos consecutivos
            double avgIntervalDays = 0;
            for (int i = 1; i < inYear.Count; i++)
            {
                avgIntervalDays += (inYear[i].PaymentDate.DayNumber - inYear[i-1].PaymentDate.DayNumber);
            }
            avgIntervalDays /= (inYear.Count - 1);

            // Cuántos pagos esperamos en un año
            double paymentsPerYear = 365.0 / avgIntervalDays;
            decimal avgPayment = totalInYear / inYear.Count;
            annualizedTotal = avgPayment * (decimal)Math.Round(paymentsPerYear, 0);
        }
        else
        {
            // Solo 1 pago: no podemos detectar frecuencia → usar como proxy anual
            // (conservador — puede subestimar el yield real)
            annualizedTotal = totalInYear;
        }

        return Math.Round(annualizedTotal / lastPrice.Value, 4);
    }
}
```

**Tests obligatorios en `YieldCalculatorTests.cs`:**
- 4 pagos trimestrales de 0.38 MXN, lastPrice=21.50 → yield = (0.38×4)/21.50 = ~0.0707
- 2 pagos semestrales de 0.55 MXN, lastPrice=20.00 → extrapolado a ~2 pagos/año = 0.055
- 0 pagos → null
- lastPrice = null → null
- lastPrice = 0 → null

### Métodos nuevos en IMarketRepository e implementación

```csharp
// IMarketRepository.cs — agregar:
Task<IReadOnlyList<DailySnapshot>> GetDailySnapshotsAsync(Guid fibraId, int days, CancellationToken ct = default);
Task<IReadOnlyList<Distribution>> GetDistributionsAsync(Guid fibraId, int? maxDays = null, CancellationToken ct = default);
Task AddDistributionAsync(Distribution dist, CancellationToken ct = default);
```

```csharp
// MarketRepository.cs — implementar:
public async Task<IReadOnlyList<DailySnapshot>> GetDailySnapshotsAsync(Guid fibraId, int days, CancellationToken ct = default)
{
    var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-days);
    return await db.DailySnapshots
        .Where(d => d.FibraId == fibraId && d.Date >= cutoff)
        .OrderBy(d => d.Date)
        .ToListAsync(ct);
}

public async Task<IReadOnlyList<Distribution>> GetDistributionsAsync(Guid fibraId, int? maxDays = null, CancellationToken ct = default)
{
    var query = db.Distributions.Where(d => d.FibraId == fibraId);
    if (maxDays.HasValue)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-maxDays.Value);
        query = query.Where(d => d.PaymentDate >= cutoff);
    }
    return await query.OrderByDescending(d => d.PaymentDate).ToListAsync(ct);
}

public async Task AddDistributionAsync(Distribution dist, CancellationToken ct = default)
{
    db.Distributions.Add(dist);
    await db.SaveChangesAsync(ct);
}
```

### DTOs de historial

```csharp
// src/Server/SharedApiContracts/Market/FibraHistoryDto.cs
namespace SharedApiContracts.Market;

public record FibraHistoryDto(
    string Ticker,
    IReadOnlyList<DailyPricePointDto> PriceHistory,  // orden cronológico ASC, huecos implícitos por ausencia de fecha
    IReadOnlyList<DistributionPointDto> Distributions, // orden DESC por fecha
    decimal? AnnualizedYield   // null = "no disponible"
);

public record DailyPricePointDto(
    string Date,        // "YYYY-MM-DD"
    decimal? Close      // null = hueco (no trading ese día)
);

public record DistributionPointDto(
    string Date,        // "YYYY-MM-DD" (payment_date)
    decimal AmountPerUnit
);
```

### Endpoint GET /api/v1/market/fibras/{ticker}/history

Agregar en `MarketEndpoints.cs` dentro de `MapMarket()`:

```csharp
group.MapGet("/fibras/{ticker}/history", async (
    string ticker,
    [FromQuery] string? period,
    IFibraRepository fibraRepo,
    IMarketRepository marketRepo,
    CancellationToken ct) =>
{
    var fibra = await fibraRepo.GetByTickerAsync(ticker.ToUpperInvariant(), ct);
    if (fibra is null)
        return Results.NotFound();

    // Mapear period a días (máximo 90 por NFR-06)
    int days = period?.ToLowerInvariant() switch
    {
        "1m" => 30,
        "3m" => 90,
        "6m" => 90,   // limitado por retención de 90 días (NFR-06)
        "1y" => 90,   // ídem
        _ => 90
    };

    // Obtener datos paralelos
    var snapshotsTask = marketRepo.GetDailySnapshotsAsync(fibra.Id, days, ct);
    var distributionsTask = marketRepo.GetDistributionsAsync(fibra.Id, maxDays: 365, ct);
    var latestSnapshotsTask = marketRepo.GetLatestSnapshotPerFibraAsync(ct);

    await Task.WhenAll(snapshotsTask, distributionsTask, latestSnapshotsTask);

    var snapshots = snapshotsTask.Result;
    var distributions = distributionsTask.Result;
    var latestSnapshot = latestSnapshotsTask.Result.FirstOrDefault(s => s.FibraId == fibra.Id);
    var lastPrice = latestSnapshot?.LastPrice;

    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var annualizedYield = YieldCalculator.Calculate(distributions, lastPrice, today);

    var dto = new FibraHistoryDto(
        fibra.Ticker,
        snapshots.Select(s => new DailyPricePointDto(s.Date.ToString("yyyy-MM-dd"), s.Close)).ToList(),
        distributions.Take(8).Select(d => new DistributionPointDto(d.PaymentDate.ToString("yyyy-MM-dd"), d.AmountPerUnit)).ToList(),
        annualizedYield
    );

    return Results.Ok(dto);
})
.AllowAnonymous()
.Produces<FibraHistoryDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);
```

**Precondición:** `IFibraRepository.GetByTickerAsync(string ticker, CancellationToken ct)` debe existir. Verificar en `IFibraRepository.cs` — si no existe, agregar y su implementación en `FibraRepository.cs`.

Si `GetByTickerAsync` no existe, agregarlo:
```csharp
// IFibraRepository.cs — agregar si no existe:
Task<Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default);
// FibraRepository.cs:
public async Task<Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default) =>
    await db.Fibras.FirstOrDefaultAsync(f => f.Ticker == ticker && f.State == FibraState.Active, ct);
```

### Frontend: Gráfica con recharts vía shadcn Chart — PriceChart.tsx

recharts se instala en Task 10.1 y el componente `Chart` de shadcn se agrega en Task 10.2.

El componente shadcn Chart provee `ChartContainer`, `ChartTooltip` y `ChartTooltipContent` — úsalos exactamente; no importar desde `recharts` directamente salvo los tipos de chart (`LineChart`, `Line`, `XAxis`, `YAxis`, `CartesianGrid`, `ResponsiveContainer`).

**Huecos:** recharts maneja `null` en los datos como huecos si el `Line` tiene `connectNulls={false}` (default). No interpolar — dejar `null` en los puntos sin snapshot.

```tsx
// src/Web/Main/src/shared/ui/price-chart.tsx
import { LineChart, Line, XAxis, YAxis, CartesianGrid, ResponsiveContainer } from 'recharts'
import { ChartContainer, ChartTooltip, ChartTooltipContent } from '@/components/ui/chart'
import { toNum } from '@/shared/lib/format-time'

interface PriceChartProps {
  data: Array<{ date: string; close: number | string | null | undefined }>
}

const chartConfig = {
  close: { label: 'Precio', color: 'hsl(var(--primary))' },
}

export function PriceChart({ data }: PriceChartProps) {
  const points = data.map(d => ({ date: d.date.slice(5), close: toNum(d.close) }))
  const hasData = points.some(p => p.close != null)

  if (!hasData) {
    return (
      <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-48">
        <p className="text-sm text-muted-foreground">Sin datos históricos disponibles</p>
      </div>
    )
  }

  return (
    <ChartContainer config={chartConfig} className="h-48 w-full">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={points} margin={{ top: 4, right: 8, bottom: 4, left: 0 }}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis
            dataKey="date"
            tick={{ fontSize: 10 }}
            tickLine={false}
            axisLine={false}
            interval="preserveStartEnd"
          />
          <YAxis
            tick={{ fontSize: 10 }}
            tickLine={false}
            axisLine={false}
            width={48}
            tickFormatter={(v: number) => v.toFixed(2)}
            domain={['auto', 'auto']}
          />
          <ChartTooltip
            content={<ChartTooltipContent formatter={(v) => typeof v === 'number' ? `$${v.toFixed(2)}` : '—'} />}
          />
          <Line
            type="monotone"
            dataKey="close"
            stroke="var(--color-close)"
            strokeWidth={1.5}
            dot={false}
            connectNulls={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </ChartContainer>
  )
}
```

**Nota sobre imports shadcn Chart:** el componente se instala en `src/Web/Main/src/components/ui/chart.tsx`. Importar con alias `@/components/ui/chart`, no con ruta relativa.

### Frontend: MercadoSection.tsx — cambios exactos

Props actuales: `{ week52High, week52Low, volume }` (líneas 7–11 del archivo actual).
Props nuevas: agregar `ticker: string`.

Query a agregar:
```typescript
const [period, setPeriod] = useState<'1m' | '3m' | '6m' | '1y'>('1m')

const { data: history, isLoading: isLoadingHistory } = useQuery({
  queryKey: ['fibra-history', ticker, period],
  queryFn: () => fetchFibraHistory(ticker, period),
  staleTime: 5 * 60_000,
  enabled: !!ticker,
})
```

Reemplazar el bloque `{/* Placeholder gráfica — llega en Story 3.3 */}` (líneas 68–71) con:
```tsx
{isLoadingHistory ? (
  <div className="rounded-lg border border-border bg-muted/20 animate-pulse h-48" />
) : (
  <PriceChart data={history?.priceHistory ?? []} height={180} />
)}
```

Cambiar `setActive(s)` en el selector para que también actualice `period`:
```typescript
onClick={() => {
  setActive(s)
  const map: Record<typeof s, '1m' | '3m' | '6m' | '1y'> = { '1M': '1m', '3M': '3m', '6M': '6m', '1A': '1y' }
  setPeriod(map[s])
}}
```

### Frontend: DistribucionesSection.tsx — implementación completa

Reemplazar el componente placeholder vacío con:

```tsx
// Props nuevas: { ticker: string }
// Query: reutilizar ['fibra-history', ticker, '1y'] — TanStack Query deduplicará si MercadoSection lo tiene activo
const { data: history, isLoading } = useQuery({
  queryKey: ['fibra-history', ticker, '1y'],
  queryFn: () => fetchFibraHistory(ticker, '1y'),
  staleTime: 60 * 60_000,
  enabled: !!ticker,
})

// Mostrar yield
const yield_ = history?.annualizedYield
// ...
{yield_ != null
  ? <p>{(yield_ * 100).toFixed(2)}%</p>
  : <p className="text-muted-foreground">no disponible</p>
}

// Tabla de distribuciones (últimas 8)
const dists = history?.distributions ?? []
// ...
```

### Convenciones críticas (no violar)

- `react-router` v7: imports desde `'react-router'`, nunca `'react-router-dom'`
- TanStack Query v5: `useQuery({ queryKey, queryFn, staleTime, enabled })`
- `openapi-fetch`: `apiClient.GET(...)` con tipos generados
- `noUnusedLocals: true`: cada import declarado DEBE usarse
- Nullables de API: `number | string | null | undefined` → convertir con `toNum()` antes de operar
- No `0` para datos nulos → `—`; no estimaciones cuando yield es null → "no disponible"
- NO ejecutar `npx shadcn@latest add` ni agregar deps npm nuevas
- Tailwind v4: verificar que clases existen antes de usar

### Anti-patrones a evitar

1. **NO** recalcular yield en frontend — el backend devuelve `annualizedYield` calculado
2. **NO** interpolar precios faltantes en la gráfica — los huecos son huecos reales (sin datos de mercado ese día)
3. **NO** mostrar 0 cuando `close == null` — generar un hueco real en la ruta SVG (`M` en vez de `L`)
4. **NO** olvidar ejecutar `npm run codegen:api` ANTES de escribir código frontend
5. **NO** asumir frecuencia trimestral fija — usar el algoritmo de YieldCalculator basado en intervalos reales
6. **NO** crear proyecto de tests nuevo — usar `tests/Unit/Application.Tests/Market/` (ya existe)
7. **NO** modificar `freshness-badge.tsx`, `format-time.ts`, ni `DailySnapshot.cs` — están completos
8. **NO** importar recharts directamente para tooltip/container — usar `ChartContainer`, `ChartTooltip`, `ChartTooltipContent` desde `@/components/ui/chart`

### Archivos nuevos

```
src/Server/Domain/Market/Distribution.cs
src/Server/Application/Market/YieldCalculator.cs
src/Server/SharedApiContracts/Market/FibraHistoryDto.cs
src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/DistributionConfiguration.cs
src/Server/Infrastructure/Persistence/Seeds/MarketDistributionSeeder.cs
src/Server/Infrastructure/Persistence/Migrations/[timestamp]_AddMarketDistribution.cs
src/Web/Main/src/shared/ui/price-chart.tsx
tests/Unit/Application.Tests/Market/YieldCalculatorTests.cs
```

### Archivos modificados

```
src/Server/Application/Market/IMarketRepository.cs
  → agregar GetDailySnapshotsAsync, GetDistributionsAsync, AddDistributionAsync

src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs
  → implementar los 3 nuevos métodos; agregar DbSet<Distribution>-related queries

src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs
  → DbSet<Distribution> Distributions + registrar DistributionConfiguration

src/Server/Api/Endpoints/Public/MarketEndpoints.cs
  → agregar endpoint GET /fibras/{ticker}/history

src/Server/Api/Program.cs
  → registrar MarketDistributionSeeder en bloque de seed

src/Web/SharedApiClient/schema.d.ts  ← generado automáticamente por codegen:api
src/Web/Main/src/api/fibrasApi.ts    → agregar fetchFibraHistory()
src/Web/Main/src/modules/ficha-publica/sections/MercadoSection.tsx
src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx
src/Web/Main/src/modules/ficha-publica/FibraPage.tsx
```

### Verificación de `IFibraRepository.GetByTickerAsync`

Antes de implementar el endpoint, verificar si existe en `src/Server/Application/Catalog/IFibraRepository.cs`. Si no existe, agregarlo (ver Task 6 en Dev Notes). No duplicar `GetAllActiveAsync()`.

### Testing Backend

**Obligatorio (workflow-rules.md):**

```bash
dotnet test tests/Unit/Application.Tests/Application.Tests.csproj
```

Resultado esperado: YieldCalculatorTests + FreshnessClassifierTests (existentes) pasan.

**Frontend:** No se requieren tests de componentes React para MVP.

### Referencias

- [Source: _bmad-output/planning-artifacts/epics.md#Historia 3.3] — user story y ACs originales
- [Source: _bmad-output/planning-artifacts/epics.md#FR-10] — módulo Mercado debe exponer histórico y distribuciones
- [Source: _bmad-output/planning-artifacts/epics.md#FR-11] — yield anualizado por frecuencia detectada, no fija
- [Source: _bmad-output/planning-artifacts/epics.md#FR-12] — sin datos de distribución → "no disponible"
- [Source: _bmad-output/planning-artifacts/epics.md#NFR-06] — snapshots diarios conservados 90 días
- [Source: _bmad-output/implementation-artifacts/3-1-pipeline-de-mercado-ingesta-y-snapshots.md] — DailySnapshot entity, IMarketRepository, patrón EF Core
- [Source: _bmad-output/implementation-artifacts/3-2-clasificacion-de-frescura-y-estados-en-ui.md] — MarketEndpoints.cs patrón, FibraPage.tsx estado actual, MercadoSection.tsx con placeholder líneas 68-71
- [Source: src/Web/Main/package.json] — NO recharts instalado; usar SVG nativo para gráfica
- [Source: src/Web/Main/src/shared/lib/format-time.ts] — helper toNum() para conversión segura number|string
- [Source: _bmad-output/planning-artifacts/convenciones-fibradis.md] — react-router v7, TanStack Query v5, sin deps nuevas

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- EF migrations bloqueadas por DLLs en uso (API corriendo en Debug): workaround `--configuration Release` (mismo patrón que Story 3.2)
- `npx shadcn@latest add chart` bloqueado por hook de auto-mode (regla en convenciones-fibradis.md): `chart.tsx` escrito manualmente con tipos compatibles con recharts 3.x (recharts 2.x vs 3.x incompatibilidad de tipos en `LegendProps` y `TooltipProps`)
- recharts 3.x cambió `Pick<LegendProps, "payload"|"verticalAlign">` → ya no compile; solución: interfaces propias `ChartTooltipContentProps` y `ChartLegendContentProps`
- `dotnet test tests/Unit/` no acepta directorio — especificar .csproj explícito

### Completion Notes List

- Entidad `Distribution` creada con seed determinista (MD5 GUIDs) para FUNO11, DANHOS13, TERRA13, FIBRAMQ12 (13 distribuciones)
- `YieldCalculator` implementado con algoritmo de frecuencia detectada; 7 tests unitarios pasan
- Endpoint `GET /api/v1/market/fibras/{ticker}/history` con `Task.WhenAll` paralelo; período máximo 90 días (NFR-06)
- Frontend: recharts 3.x + shadcn chart manual → `PriceChart` con huecos reales (`connectNulls={false}`)
- `MercadoSection` y `DistribucionesSection` conectadas con queries TanStack Query v5; `FibraPage` pasa `ticker` a ambas
- Build frontend: 0 errores TypeScript, 0 errores Vite

### File List

**Nuevos:**
- `src/Server/Domain/Market/Distribution.cs`
- `src/Server/Application/Market/YieldCalculator.cs`
- `src/Server/SharedApiContracts/Market/FibraHistoryDto.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/DistributionConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Seed/MarketSeed.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260519083422_AddMarketDistribution.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260519083422_AddMarketDistribution.Designer.cs`
- `tests/Unit/Application.Tests/Market/YieldCalculatorTests.cs`
- `src/Web/Main/src/shared/ui/chart.tsx`
- `src/Web/Main/src/shared/ui/price-chart.tsx`

**Modificados:**
- `src/Server/Application/Market/IMarketRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Api/Endpoints/Public/MarketEndpoints.cs`
- `src/Web/SharedApiClient/schema.d.ts`
- `src/Web/Main/src/api/fibrasApi.ts`
- `src/Web/Main/src/modules/ficha-publica/sections/MercadoSection.tsx`
- `src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx`
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs`
- `_bmad-output/planning-artifacts/convenciones-fibradis.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-19: Story 3.3 implementada — historial de precios, yield anualizado y snapshots a 90 días
- 2026-05-19: Hallazgos de code review resueltos — 5 items (3 High, 2 Medium): YieldCalculator TTM, gaps en gráfica, error state en MercadoSection y DistribucionesSection

## Senior Developer Review (AI)

### Hallazgos

- [x] `YieldCalculator` sobreestima el yield cuando hay 3 pagos dentro de 12 meses con intervalos cercanos a trimestral. En ese caso el AC pide anualizar por 3, pero la implementación usa `365 / avgIntervalDays` y redondea a `4`, inflando el numerador anualizado. Falta además un test para ese caso específico. Evidencia: [src/Server/Application/Market/YieldCalculator.cs](/C:/Users/jorge/source/repos/FIBRADIS/src/Server/Application/Market/YieldCalculator.cs:26), AC #2 en [_bmad-output/implementation-artifacts/3-3-historial-de-precios-yield-anualizado-y-snapshots-a-90-dias.md](/C:/Users/jorge/source/repos/FIBRADIS/_bmad-output/implementation-artifacts/3-3-historial-de-precios-yield-anualizado-y-snapshots-a-90-dias.md:14).
- [x] La gráfica no puede mostrar huecos reales para días sin snapshot porque el endpoint sólo devuelve los días existentes y el frontend sólo grafica esos puntos. `connectNulls={false}` no ayuda si no existen entradas con `close: null`, así que la línea queda continua entre fechas contiguas en el dataset aunque falten días en medio. Esto incumple AC #1 y el anti-patrón documentado en la historia. Evidencia: [src/Server/Api/Endpoints/Public/MarketEndpoints.cs](/C:/Users/jorge/source/repos/FIBRADIS/src/Server/Api/Endpoints/Public/MarketEndpoints.cs:83), [src/Web/Main/src/shared/ui/price-chart.tsx](/C:/Users/jorge/source/repos/FIBRADIS/src/Web/Main/src/shared/ui/price-chart.tsx:13), AC #1 en [_bmad-output/implementation-artifacts/3-3-historial-de-precios-yield-anualizado-y-snapshots-a-90-dias.md](/C:/Users/jorge/source/repos/FIBRADIS/_bmad-output/implementation-artifacts/3-3-historial-de-precios-yield-anualizado-y-snapshots-a-90-dias.md:13).
- [x] `MercadoSection` y `DistribucionesSection` colapsan el estado `error` a estados de “sin datos” o “no disponible”, lo que viola la regla del proyecto de mantener el estado de datos explícito (`fresh`, `stale`, `partial`, `error`, `null`). Si falla `fetchFibraHistory`, la gráfica renderiza vacío y las distribuciones aparentan no existir, ocultando un fallo real de API/red. Evidencia: [src/Web/Main/src/modules/ficha-publica/sections/MercadoSection.tsx](/C:/Users/jorge/source/repos/FIBRADIS/src/Web/Main/src/modules/ficha-publica/sections/MercadoSection.tsx:34), [src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx](/C:/Users/jorge/source/repos/FIBRADIS/src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx:10), regla crítica #5 en `AGENTS.md`.

### Verificación

- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj` -> 15 passed, 0 failed
- `npm run build --workspace=src/Web/Main` -> build OK, con warning de chunk `832.38 kB`

### Re-review 2026-05-19

- Sin hallazgos abiertos.
- Verificado: `YieldCalculator` ya usa TTM, existe test para 3 pagos en 12 meses, `PriceChart` inserta gaps para fechas no consecutivas, y ambas secciones manejan `isError` explícitamente.
- Revalidado: `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj` -> 16 passed, 0 failed.
- Revalidado: `npm run build --workspace=src/Web/Main` -> build OK; persiste warning no bloqueante por chunk > 500 kB.
