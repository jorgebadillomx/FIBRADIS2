# Historia 8.3: Comparador público de FIBRAs en /comparar

Status: ready-for-dev

## Story

Como visitante público,
quiero seleccionar entre 2 y 4 FIBRAs en `/comparar` y verlas lado a lado en cuatro bloques (Mercado, Fundamentales, Distribuciones, Score público),
para poder evaluar valor relativo sin abrir varias fichas ni autenticarme.

## Acceptance Criteria

### AC1 — Selector de FIBRAs con URL state

**Dado que** navego a `/comparar`,
**Entonces** veo un selector de FIBRAs (autocomplete por ticker o nombre) que permite elegir de 2 a 4 FIBRAs.
**Y** la selección se refleja inmediatamente en el query param: `/comparar?fibras=FUNO11,FMTY14`.
**Y** al cargar `/comparar?fibras=FUNO11,FMTY14` directamente, las 2 FIBRAs ya están seleccionadas y los datos cargados.

### AC2 — Bloque Mercado

**Dado que** tengo 2+ FIBRAs seleccionadas,
**Cuando** el endpoint `/api/v1/compare` responde,
**Entonces** veo una fila por métrica:
- Precio actual (MXN)
- Cambio día (%)
- Promedio 52S (MXN)
- Volumen

**Y** cualquier valor `null` del servidor se muestra como `—` sin desplazar la columna.

### AC3 — Bloque Fundamentales

**Entonces** veo las siguientes filas de fundamentales:
- Período del reporte
- Cap Rate (%)
- NAV por CBFI (MXN)
- LTV (%)
- Margen NOI (%)
- Margen FFO (%)

**Y** valores `null` se muestran como `—`.

### AC4 — Bloque Distribuciones

**Entonces** veo las siguientes filas de distribuciones:
- Distribución trimestral (MXN)
- Yield calculado anual (%)
- Yield decretado anual (%)

**Y** valores `null` se muestran como `—`.

### AC5 — Bloque Score público (perfil Balanceado)

**Entonces** veo el Score de oportunidad calculado con perfil Balanceado (pesos iguales: 20/20/20/20/20) como referencia neutral, sin personalización.
**Y** se muestran los 5 componentes individuales (NAV Descuento, Dividend Yield, LTV, NOI Margin, Price vs 52S).
**Y** si una FIBRA tiene `isLimitedData = true`, se muestra el score con indicación visual de "datos limitados".
**Y** si una FIBRA no tiene precio (`isExcluded = true`), la celda muestra `—`.

### AC6 — Quitar FIBRA y límites

**Dado que** tengo 3 FIBRAs seleccionadas,
**Cuando** quito una,
**Entonces** la columna desaparece y la URL se actualiza.
**Y** no es posible quitar una FIBRA si solo quedan 2 (el botón de quitar se deshabilita con tooltip).
**Y** no es posible agregar una quinta FIBRA (el selector se deshabilita con tooltip).

### AC7 — Sin autenticación

**Dado que** soy un visitante no autenticado,
**Entonces** `/comparar` carga correctamente sin solicitar login.
**Y** el endpoint `GET /api/v1/compare` es público (no requiere JWT).

### AC8 — Responsividad

**Dado que** accedo en 360px,
**Entonces** la tabla usa scroll horizontal sin overflow no intencional y cada columna es mínimamente legible.
**Y** en 768px y 1280px la tabla se despliega sin scroll horizontal cuando hay 2-3 FIBRAs.

### AC9 — SEO y enlace de navegación

**Entonces** `<title>Comparador de FIBRAs — FIBRADIS</title>` está presente.
**Y** hay `<meta name="description">` con descripción útil de 120-160 chars.
**Y** hay una entrada "Comparar" en el nav principal de `PublicLayout.tsx`.

### AC10 — Backend: endpoint público /api/v1/compare

**Dado que** hago `GET /api/v1/compare?tickers=FUNO11,FMTY14`,
**Entonces** recibo 200 con un array de `ComparadorFibraDto` por cada ticker.
**Y** si los tickers son menos de 2 o más de 4, recibo 400 con mensaje claro.
**Y** si un ticker no existe en el catálogo, recibo 400 con el ticker inválido identificado.
**Y** el endpoint no requiere token (AllowAnonymous).

## Tasks / Subtasks

### T1 — Contrato API: ComparadorFibraDto (AC: 2, 3, 4, 5, 10)

- [ ] T1.1 — Crear `src/Web/SharedApiClient/` no aplica — el contrato vive en el backend; crear `src/Server/Application/Compare/`:
  - Crear `src/Server/Application/Compare/` (directorio)
  - No se necesita clase de aplicación; el cálculo ocurre en el endpoint usando repositorios y `OpportunityScoreCalculator`
  
- [ ] T1.2 — Crear contrato en SharedApiContracts. Buscar dónde viven los otros DTOs:
  ```
  src/Web/SharedApiClient/schema.d.ts  → generado por codegen, no editar
  ```
  El contrato real está en el backend. Crear `src/Server/Api/Contracts/Compare/ComparadorFibraDto.cs`:
  ```csharp
  namespace Api.Contracts.Compare;

  public sealed record ComparadorMercadoDto(
      decimal? PrecioActual,
      decimal? CambiaDiaPct,
      decimal? Avg52S,
      long? Volumen,
      string? FreshnessStatus);

  public sealed record ComparadorFundamentalesDto(
      string? Periodo,
      decimal? CapRate,
      decimal? NavPerCbfi,
      decimal? Ltv,
      decimal? NoiMargin,
      decimal? FfoMargin);

  public sealed record ComparadorDistribucionesDto(
      decimal? DistribucionTrimestral,
      decimal? YieldCalculadoPct,
      decimal? YieldDecretadoPct);

  public sealed record ComparadorScoreDto(
      decimal? Score,
      bool IsLimitedData,
      bool IsExcluded,
      decimal? NavDescuentoScore,
      decimal? DividendYieldScore,
      decimal? LtvScore,
      decimal? NoiMarginScore,
      decimal? PriceVs52wScore);

  public sealed record ComparadorFibraDto(
      Guid FibraId,
      string Ticker,
      string Nombre,
      ComparadorMercadoDto Mercado,
      ComparadorFundamentalesDto Fundamentales,
      ComparadorDistribucionesDto Distribuciones,
      ComparadorScoreDto Score);
  ```
  
  **IMPORTANTE**: Verificar si el proyecto tiene una carpeta `SharedApiContracts` o si los DTOs viven en la capa Api. Buscar patrones en `OpportunityEndpoints.cs` → usa `SharedApiContracts.Opportunities`. Si existe ese patrón, crear en la ubicación correcta. De lo contrario crear bajo `src/Server/Api/Endpoints/Public/`.

### T2 — Backend: endpoint GET /api/v1/compare (AC: 7, 10)

- [ ] T2.1 — Buscar si existe `src/Server/Application/Opportunities/OpportunityWeights.cs` para usar `OpportunityWeights.Default`. Verificar que el perfil balanceado sea 20/20/20/20/20.

- [ ] T2.2 — Crear `src/Server/Api/Endpoints/Public/CompareEndpoints.cs`:
  ```csharp
  using Application.Catalog;
  using Application.Fundamentals;
  using Application.Market;
  using Application.Opportunities;
  using Microsoft.AspNetCore.Mvc;
  // ... imports según contratos elegidos en T1.2

  namespace Api.Endpoints.Public;

  public static class CompareEndpoints
  {
      private const int MinFibras = 2;
      private const int MaxFibras = 4;

      public static IEndpointRouteBuilder MapCompare(this IEndpointRouteBuilder app)
      {
          var group = app.MapGroup("/api/v1/compare").WithTags("Compare");

          group.MapGet("/", async (
              [FromQuery] string tickers,
              IFibraRepository fibraRepo,
              IMarketRepository marketRepo,
              IFundamentalRepository fundamentalRepo,
              IBmvSchedule bmvSchedule,
              CancellationToken ct) =>
          {
              var tickerList = tickers
                  .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                  .Select(t => t.ToUpperInvariant())
                  .Distinct()
                  .ToList();

              if (tickerList.Count < MinFibras)
                  return Results.Problem($"Se requieren al menos {MinFibras} FIBRAs.", statusCode: 400);
              if (tickerList.Count > MaxFibras)
                  return Results.Problem($"Se permiten como máximo {MaxFibras} FIBRAs.", statusCode: 400);

              // Validar que todos los tickers existen en catálogo
              var fibras = new List<Domain.Catalog.Fibra>();
              foreach (var ticker in tickerList)
              {
                  var fibra = await fibraRepo.GetByTickerAsync(ticker, ct);
                  if (fibra is null)
                      return Results.Problem(
                          $"Ticker no encontrado: '{ticker}'.",
                          statusCode: StatusCodes.Status400BadRequest,
                          extensions: new Dictionary<string, object?> { ["domainCode"] = "FIBRA_NOT_FOUND", ["ticker"] = ticker });
                  fibras.Add(fibra);
              }

              var fibraIds = fibras.Select(f => f.Id).ToList();
              var utcNow = DateTimeOffset.UtcNow;
              var isMarketOpen = bmvSchedule.IsTradingHours(utcNow);

              // DbContext no es thread-safe — awaits secuenciales obligatorios
              var allSnapshots = await marketRepo.GetLatestSnapshotPerFibraAsync(ct);
              var snapshotByFibra = allSnapshots.Where(s => fibraIds.Contains(s.FibraId)).ToDictionary(s => s.FibraId);

              var latestFundamentals = await fundamentalRepo.GetSummaryLatestAsync(ct);
              var fundamentalByFibra = latestFundamentals
                  .Where(r => fibraIds.Contains(r.Record.FibraId))
                  .ToDictionary(r => r.Record.FibraId, r => r.Record);

              var distributions = await marketRepo.GetDistributionsByFibrasAsync(fibraIds, 365, ct);
              var distsByFibra = distributions.GroupBy(d => d.FibraId).ToDictionary(g => g.Key, g => g.ToList());

              var avg52wByFibra = await marketRepo.GetWeek52AvgByFibrasAsync(fibraIds, 365, ct);

              // Score público con pesos balanceados (20/20/20/20/20)
              var allActiveFibras = await fibraRepo.GetAllActiveAsync(ct);
              var allSnapForScore = await marketRepo.GetLatestSnapshotPerFibraAsync(ct);
              var allFundamentalsForScore = await fundamentalRepo.GetSummaryLatestAsync(ct);
              var allDistsForScore = await marketRepo.GetDistributionsByFibrasAsync(
                  allActiveFibras.Select(f => f.Id).ToList(), 365, ct);
              var allAvg52wForScore = await marketRepo.GetWeek52AvgByFibrasAsync(
                  allActiveFibras.Select(f => f.Id).ToList(), 365, ct);

              var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
              var annualDistForScore = allDistsForScore
                  .GroupBy(d => d.FibraId)
                  .ToDictionary(g => g.Key, g => g.Where(d => d.PaymentDate >= cutoff).Sum(d => d.AmountPerUnit));

              var scores = OpportunityScoreCalculator.Calculate(
                  allActiveFibras,
                  allSnapForScore.ToDictionary(s => s.FibraId),
                  allFundamentalsForScore.ToDictionary(r => r.Record.FibraId, r => r.Record),
                  annualDistForScore,
                  allAvg52wForScore,
                  OpportunityWeights.Default);

              var scoreByFibra = scores.ToDictionary(s => s.FibraId);

              var result = fibras.Select(fibra =>
              {
                  snapshotByFibra.TryGetValue(fibra.Id, out var snap);
                  fundamentalByFibra.TryGetValue(fibra.Id, out var fund);
                  distsByFibra.TryGetValue(fibra.Id, out var dists);
                  avg52wByFibra.TryGetValue(fibra.Id, out var avg52w);
                  scoreByFibra.TryGetValue(fibra.Id, out var score);

                  var freshnessStatus = FreshnessClassifier.Classify(snap, isMarketOpen, utcNow);

                  // Yield calculado: distribuciones anuales / precio actual
                  var annualDist = dists?
                      .Where(d => d.PaymentDate >= cutoff)
                      .Sum(d => d.AmountPerUnit);
                  decimal? yieldCalculado = snap?.LastPrice is > 0m && annualDist is > 0m
                      ? Math.Round(annualDist.Value / snap.LastPrice.Value * 100m, 2)
                      : null;

                  // Yield decretado: distribución trimestral * 4 / precio
                  var trimestreDist = fund?.QuarterlyDistribution;
                  decimal? yieldDecretado = snap?.LastPrice is > 0m && trimestreDist is > 0m
                      ? Math.Round(trimestreDist.Value * 4m / snap.LastPrice.Value * 100m, 2)
                      : null;

                  return new ComparadorFibraDto(
                      fibra.Id,
                      fibra.Ticker,
                      fibra.ShortName,
                      new ComparadorMercadoDto(snap?.LastPrice, snap?.DailyChangePct, avg52w > 0 ? avg52w : null, snap?.Volume, freshnessStatus),
                      new ComparadorFundamentalesDto(fund?.Period, fund?.CapRate, fund?.NavPerCbfi, fund?.Ltv, fund?.NoiMargin, fund?.FfoMargin),
                      new ComparadorDistribucionesDto(trimestreDist, yieldCalculado, yieldDecretado),
                      score is null
                          ? new ComparadorScoreDto(null, false, true, null, null, null, null, null)
                          : new ComparadorScoreDto(score.Score, score.IsLimitedData, score.IsExcluded,
                              score.NavDiscountScore, score.DividendYieldScore, score.LtvInvertedScore,
                              score.NoiMarginScore, score.Pricevs52wScore));
              }).ToList();

              return Results.Ok(result);
          })
          .AllowAnonymous()
          .Produces<IReadOnlyList<ComparadorFibraDto>>(StatusCodes.Status200OK)
          .ProducesProblem(StatusCodes.Status400BadRequest);

          return app;
      }
  }
  ```

  **Nota sobre el score**: El cálculo de percentiles requiere el universo COMPLETO activo para ser válido. Cargar solo las FIBRAs seleccionadas produce percentiles incorrectos. Por eso se carga el universo completo y luego se filtra el resultado por las FIBRAs solicitadas. Esto implica ~7 queries adicionales. Es aceptable dado el contexto público de baja frecuencia.

- [ ] T2.3 — Registrar el endpoint en `Program.cs` o donde se registran los demás endpoints públicos. Buscar el patrón de registro en el archivo de bootstrap.

  Buscar: `app.MapMarket()` o `app.MapFundamentalsPublic()` → añadir `app.MapCompare()` en el mismo bloque.

- [ ] T2.4 — Verificar que `ComparadorFibraDto` sea serializable correctamente (records → JSON camelCase por convención del proyecto).

### T3 — Backend: tests de integración (AC: 10)

- [ ] T3.1 — Crear `tests/Integration/Api.Tests/Public/CompareEndpointTests.cs`:
  ```csharp
  // Tests requeridos:
  // 1. GET /compare?tickers=FUNO11,FMTY14 → 200, array de 2 items
  // 2. GET /compare?tickers=FUNO11 → 400 (menos de 2)
  // 3. GET /compare?tickers=FUNO11,FMTY14,DANHOS13,TERRA13,FMTY14X → 400 (más de 4)
  // 4. GET /compare?tickers=FUNO11,XXXXXX → 400 con "XXXXXX" identificado
  // 5. GET /compare?tickers=FUNO11,FMTY14 sin JWT → 200 (endpoint público, no requiere auth)
  // 6. Fundamentales null → campo correspondiente devuelve null (no excepción)
  ```

  Patrones del proyecto para integration tests:
  - Usar `WebApplicationFactory` del proyecto de tests
  - Seed FIBRAs y snapshots desde el fixture compartido
  - Ver `tests/Integration/Api.Tests/Public/` para ejemplos similares

### T4 — Codegen: regenerar SharedApiClient (AC: todos)

- [ ] T4.1 — Con el backend compilando y el servidor levantado (o con el spec generado), ejecutar:
  ```bash
  npm run codegen:api
  ```
  Verificar que `schema.d.ts` en `src/Web/SharedApiClient/` incluye el nuevo endpoint `/api/v1/compare`.

### T5 — Frontend: API client y hook (AC: 1, 2, 3, 4, 5)

- [ ] T5.1 — Crear `src/Web/Main/src/modules/comparador/comparadorApi.ts`:
  ```typescript
  import { fibrasApi } from '@/shared/lib/fibrasApi'
  import type { paths } from '@shared-api/schema'

  type ComparadorFibraDto = paths['/api/v1/compare']['get']['responses']['200']['content']['application/json'][number]

  export async function fetchComparacion(tickers: string[]): Promise<ComparadorFibraDto[]> {
    const { data, error } = await fibrasApi.GET('/api/v1/compare', {
      params: { query: { tickers: tickers.join(',') } },
    })
    if (error) throw new Error('Error al cargar comparación')
    return data ?? []
  }
  ```

  **Verificar** cómo el resto de módulos importa `fibrasApi` — buscar el patrón en `CatalogoPage.tsx` o `FundamentalesPage.tsx` para copiar el import correcto.

- [ ] T5.2 — Crear hook `src/Web/Main/src/modules/comparador/useComparacion.ts`:
  ```typescript
  import { useQuery } from '@tanstack/react-query'
  import { fetchComparacion } from './comparadorApi'

  export function useComparacion(tickers: string[]) {
    return useQuery({
      queryKey: ['comparacion', tickers],
      queryFn: () => fetchComparacion(tickers),
      enabled: tickers.length >= 2,
      staleTime: 60_000,
    })
  }
  ```

### T6 — Frontend: ComparadorPage (AC: 1, 2, 3, 4, 5, 6, 7, 8, 9)

- [ ] T6.1 — Crear `src/Web/Main/src/modules/comparador/ComparadorPage.tsx`:

  **Estado URL**: usar `useSearchParams()` de `react-router` para leer/escribir `?fibras=`.
  ```typescript
  import { useSearchParams } from 'react-router'
  // const [searchParams, setSearchParams] = useSearchParams()
  // const tickers = searchParams.get('fibras')?.split(',').filter(Boolean) ?? []
  ```

  **Estructura de la página**:
  1. Header de la página: título H1, descripción
  2. Selector de FIBRAs (autocomplete usando el catálogo completo de `/api/v1/fibras?pageSize=100`)
     - Chips para FIBRAs seleccionadas con botón X (deshabilitado si quedan 2)
     - Input de búsqueda deshabilitado si ya hay 4 FIBRAs
  3. Tabla de comparación (solo visible cuando `tickers.length >= 2`):
     - Primera columna: etiquetas de métricas
     - Columnas 2–5: una por FIBRA seleccionada, con header = Ticker + Nombre
     - Filas agrupadas por bloque con subheader (Mercado, Fundamentales, Distribuciones, Score)
  4. Loading state: skeleton o spinner mientras carga
  5. Error state: mensaje de error con botón de retry

  **Valor nulo → `—`**: seguir la convención del proyecto (buscar en `FundamentalesPage.tsx` o `OportunidadesPage.tsx` cómo se formatea `null` → `"—"`).

  **Scroll horizontal en móvil**: envolver la tabla en `<div className="overflow-x-auto">`.

  **Score badge**: para `isLimitedData`, agregar un indicador visual (badge, tooltip) — seguir el estilo de `ScoreBadge` en `OportunidadesPage.tsx` si existe.

- [ ] T6.2 — SEO tags (AC9):
  ```tsx
  <title>Comparador de FIBRAs — FIBRADIS</title>
  <meta name="description" content="Compara hasta 4 FIBRAs mexicanas lado a lado: mercado, fundamentales, distribuciones y score de oportunidad." />
  <meta property="og:title" content="Comparador de FIBRAs — FIBRADIS" />
  <meta property="og:description" content="..." />
  ```

### T7 — Frontend: routing y navegación (AC: 9)

- [ ] T7.1 — Agregar ruta en `src/Web/Main/src/app/routes.tsx`:
  ```typescript
  import { ComparadorPage } from '@/modules/comparador/ComparadorPage'
  // En el array de children de PublicLayout:
  { path: '/comparar', element: <ComparadorPage /> },
  ```

- [ ] T7.2 — Agregar enlace en `src/Web/Main/src/shared/layouts/PublicLayout.tsx`:
  ```tsx
  // Dentro del <nav> junto a Catálogo, Noticias, Fundamentales:
  <Link to="/comparar" className="hover:text-foreground transition-colors duration-150">Comparar</Link>
  ```
  Posición sugerida: entre "Catálogo" y "Noticias" (orden alfabético aproximado).

### T8 — Build y tests frontend (AC: todos)

- [ ] T8.1 — Ejecutar `npm run build --workspace=src/Web/Main` — 0 errores TypeScript.
- [ ] T8.2 — Verificar en dev server (`npm run dev:main`) que:
  - `/comparar` carga sin error
  - Seleccionar 2 FIBRAs y ver datos
  - URL se actualiza con `?fibras=`
  - Recarga directa con URL `/comparar?fibras=FUNO11,FMTY14` funciona

## Dev Notes

### Arquitectura: endpoint vs client-side calculation

El score de oportunidad requiere percentile normalization sobre el **universo completo** de FIBRAs activas para ser significativo. Si se calculara solo con las 2-4 FIBRAs del comparador, los percentiles serían incorrectos (por ejemplo, la mejor de 2 FIBRAs siempre tendría score 100).

Por eso T2.2 carga el universo completo para el cálculo del score y luego filtra los resultados para devolver solo las FIBRAs solicitadas. Esto implica ~7 queries totales en el endpoint — aceptable para un endpoint público de baja frecuencia.

### DbContext thread-safety

Este endpoint NO debe usar `Task.WhenAll`. Todas las queries son secuenciales. Ver convenciones-fibradis.md sección "EF Core — nunca Task.WhenAll".

### Yield calculado vs decretado

- **Yield calculado**: suma de distribuciones en los últimos 365 días / precio actual × 100
- **Yield decretado**: `QuarterlyDistribution` del último fundamental × 4 / precio actual × 100
- Ambos son `null` si `LastPrice` es null o ≤ 0 (guard contra denominador cero — ver convenciones).

### Formato de nulos en frontend

La convención del proyecto es: **NUNCA mostrar `0` para datos financieros nulos — siempre `—`**.
El valor `null` del servidor se muestra como `"—"`. Usar un helper como:
```typescript
const fmt = (v: number | null | undefined, decimals = 2) =>
  v == null ? '—' : v.toFixed(decimals)
```

### URL state con useSearchParams

React Router v7. Leer y escribir query params:
```typescript
import { useSearchParams } from 'react-router'
const [searchParams, setSearchParams] = useSearchParams()
const tickers = searchParams.get('fibras')?.split(',').filter(Boolean) ?? []

function addTicker(ticker: string) {
  const next = [...new Set([...tickers, ticker])]
  setSearchParams({ fibras: next.join(',') })
}
function removeTicker(ticker: string) {
  const next = tickers.filter(t => t !== ticker)
  if (next.length === 0) setSearchParams({})
  else setSearchParams({ fibras: next.join(',') })
}
```

### Autocomplete de FIBRAs

Usar `GET /api/v1/fibras?pageSize=100` (ya existe, AllowAnonymous) para obtener el catálogo completo de FIBRAs activas. Cachear con `staleTime: Infinity` dado que el catálogo cambia raramente. Filtrar client-side por input del usuario. Ver `CatalogoPage.tsx` para el patrón de carga del catálogo.

### Contratos existentes a reutilizar

- `OpportunityWeights.Default` → en `src/Server/Application/Opportunities/OpportunityWeights.cs`
- `OpportunityScoreCalculator.Calculate()` → en `src/Server/Application/Opportunities/OpportunityScoreCalculator.cs`
- `FreshnessClassifier.Classify()` → en `src/Server/Application/Market/FreshnessClassifier.cs` (o similar)
- `IBmvSchedule` → ya inyectado en otros endpoints públicos
- `IFibraRepository.GetByTickerAsync()` → ya usado en `CatalogEndpoints.cs`
- `IMarketRepository.GetDistributionsByFibrasAsync()` → ya usado en `OpportunityEndpoints.cs`
- `IMarketRepository.GetWeek52AvgByFibrasAsync()` → ya usado en `OpportunityEndpoints.cs`

### Ubicación de contratos DTO

Antes de crear el directorio de contratos en T1.2, verificar la estructura real del proyecto:
```bash
# Buscar dónde viven los DTOs compartidos
find src/Server -name "*Dto.cs" | head -10
```
Si existen en `src/Server/Application/*/Dtos/` o en `SharedApiContracts/`, seguir ese patrón.

### Security Checklist (nuevo en esta historia)

- [ ] **TOCTOU**: No aplica — este endpoint es read-only, no escribe en BD.
- [ ] **Auth-gating**: No aplica — endpoint completamente público (`AllowAnonymous`). No hay componentes interactivos que requieran auth (el StarButton de favoritos no aparece en el comparador — es una superficie pública sin preferencias de usuario).
- [ ] **Denominador cero**: `yieldCalculado` y `yieldDecretado` guardan explícitamente contra `LastPrice <= 0` y `distribution <= 0`. Verificar en tests.

### Project Structure Notes

- Módulo frontend: `src/Web/Main/src/modules/comparador/` (directorio ya existe con `.gitkeep`)
- Endpoint backend: `src/Server/Api/Endpoints/Public/CompareEndpoints.cs` (nuevo)
- Ruta: `/comparar` — pública, sin `ProtectedRoute`
- Nav: `PublicLayout.tsx` — agregar enlace en el `<nav>` existente

### References

- FR-08 y FR-09 del PRD en `docs/req/prd.md`
- `OpportunityEndpoints.cs` → patrón de uso de `OpportunityScoreCalculator`
- `CatalogEndpoints.cs` → patrón de `GetByTickerAsync` y manejo de 404
- `MarketEndpoints.cs` → patrón de endpoints públicos con `AllowAnonymous`
- `routes.tsx` y `PublicLayout.tsx` → dónde agregar ruta y enlace de nav
- convenciones-fibradis.md → reglas de stack y código obligatorias

## Dev Agent Record

### Agent Model Used

(por completar al implementar)

### Debug Log References

### Completion Notes List

### File List
