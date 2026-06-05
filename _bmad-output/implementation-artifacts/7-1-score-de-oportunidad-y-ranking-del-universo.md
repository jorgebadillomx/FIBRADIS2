# Historia 7.1: Score de oportunidad y ranking del universo

Status: done

## Story

Como usuario,
quiero ver todas las FIBRAs activas clasificadas por un score de oportunidad configurable con cinco componentes (Descuento NAV 30%, Dividend Yield 30%, LTV invertido 20%, Margen NOI 10%, Precio vs AVG 52S 10%),
para que pueda identificar las FIBRAs más atractivas según mis propios criterios de inversión.

## Acceptance Criteria

### AC1 — Ranking principal con score y componentes

**Dado que** el universo activo tiene FIBRAs con datos completos,
**Cuando** abro Oportunidades,
**Entonces** las FIBRAs se muestran en orden descendente de score (0–100), con cada fila mostrando nombre/ticker, score total y el valor de cada componente.

### AC2 — Desglose visual al expandir fila

**Dado que** expando una fila de FIBRA,
**Entonces** veo un desglose visual mostrando la contribución de cada componente al score total.

### AC3 — Sección "datos limitados" para FIBRAs con 1-2 componentes

**Dado que** una FIBRA tiene datos para solo 2 de 5 componentes,
**Entonces** aparece en una sección separada de "datos limitados" debajo del ranking principal, con la advertencia: "Score referencial — datos insuficientes para el ranking principal."

### AC4 — Exclusión de FIBRAs sin ningún componente calculable

**Dado que** una FIBRA no tiene ningún componente calculable con precio actual,
**Entonces** queda excluida completamente tanto del ranking principal como de la sección de datos limitados.

### AC5 — Recálculo en tiempo real al mover sliders

**Dado que** arrastro el control deslizante de peso de Yield de 30% a 50%,
**Entonces** el ranking se recalcula en tiempo real sin recargar la página.

### AC6 — Perfiles de peso predefinidos y persistencia

**Dado que** selecciono el perfil "Renta",
**Entonces** los pesos se establecen en Yield 50%, NOI 20%, Descuento NAV 20%, LTV 10%, Precio vs 52S 0% y el ranking se actualiza inmediatamente. La configuración persiste en mi próximo inicio de sesión.

## Tasks / Subtasks

### T1 — Domain: entidad `UserOpportunityWeights`

- [x] T1.1 — Crear `src/Server/Domain/Portfolio/UserOpportunityWeights.cs` con campos: UserId (Guid, PK/FK), WeightsJson (string?), Profile (string), UpdatedAt (DateTimeOffset)

### T2 — SharedApiContracts: DTOs de oportunidades

- [x] T2.1 — Crear `src/Server/SharedApiContracts/Opportunities/OpportunityWeightsDto.cs` con record: NavDiscount, DividendYield, LtvInverted, NoiMargin, Pricevs52w (cada uno decimal 0-100), Profile (string)
- [x] T2.2 — Crear `src/Server/SharedApiContracts/Opportunities/OpportunityFibraRowDto.cs` con: FibraId, Ticker, Nombre, Score?, ComponentCount, IsLimitedData, + 5 pares (score percentil + valor raw) por componente
- [x] T2.3 — Crear `src/Server/SharedApiContracts/Opportunities/OpportunityRankingResponseDto.cs` con: Ranked, LimitedData, Weights

### T3 — Application: interfaz + calculador

- [x] T3.1 — Crear `src/Server/Application/Opportunities/IOpportunityWeightsRepository.cs` con: GetByUserIdAsync, UpsertAsync
- [x] T3.2 — Crear `src/Server/Application/Opportunities/OpportunityScoreCalculator.cs` con método estático `Calculate(fibras, snapshots, fundamentals, distributions, avg52w, weights)` → lista de `OpportunityFibraScore`
  - Algoritmo: calcular raw value por componente (null si no disponible), normalizar por percentil cross-FIBRA, score = Σ(percentil × peso)
  - Regla: ≥3 componentes → ranking principal; 1-2 → datos limitados; 0 o sin precio → excluir

### T4 — Infrastructure: EF config + repositorio + migración

- [x] T4.1 — Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Portfolio/UserOpportunityWeightsConfiguration.cs`
- [x] T4.2 — Agregar DbSet `UserOpportunityWeights` a `AppDbContext`
- [x] T4.3 — Crear `src/Server/Infrastructure/Persistence/Repositories/Opportunities/OpportunityWeightsRepository.cs`
- [x] T4.4 — Registrar en DI (`Program.cs` o `InfrastructureExtensions`)
- [x] T4.5 — Ejecutar `dotnet ef migrations add AddUserOpportunityWeights` y `dotnet ef database update`

### T5 — API endpoints

- [x] T5.1 — Crear `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs`:
  - `GET /api/v1/opportunities` — carga datos, calcula scores con pesos del usuario, devuelve `OpportunityRankingResponseDto`
  - `GET /api/v1/opportunities/weights` — devuelve pesos/perfil del usuario (default si no existen)
  - `PUT /api/v1/opportunities/weights` — persiste pesos/perfil del usuario
- [x] T5.2 — Registrar endpoints en `Program.cs`

### T6 — Unit tests

- [x] T6.1 — Crear `tests/Unit/Application.Tests/Opportunities/OpportunityScoreCalculatorTests.cs` con tests:
  - fibra con 5/5 componentes: score calculado correctamente
  - fibra con 2/5 componentes: IsLimitedData = true
  - fibra sin precio: excluida
  - un solo ticker en el universo: percentil = 50
  - perfil "Renta" redistribuye pesos correctamente

### T7 — Frontend: página /oportunidades

- [x] T7.1 — Regenerar cliente API tipado (`npm run codegen:api`)
- [x] T7.2 — Crear `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx`:
  - Tabla ranking (sortable) con filas expandibles mostrando desglose por componente
  - Sliders de peso (5 sliders, suma siempre 100%, live recálculo local)
  - Botones de perfil: Predeterminado, Renta, Crecimiento
  - Sección "datos limitados" con banner de advertencia
  - `PUT /api/v1/opportunities/weights` al guardar configuración
- [x] T7.3 — Agregar ruta `/oportunidades` (protected) en `routes.tsx`
- [x] T7.4 — Agregar enlace en nav principal (junto a Portafolio)

## Dev Notes

### Algoritmo de Score

**Componentes (5):**
| Componente | Campo | Fórmula raw | Default % |
|---|---|---|---|
| Descuento NAV | `navPerCbfi` | `(1 - price/nav) * 100` — mayor descuento = mejor | 30 |
| Dividend Yield | distribuciones anuales | `annualDist / price * 100` | 30 |
| LTV Invertido | `ltv` | `(1 - ltv) * 100` — menor LTV = mejor | 20 |
| Margen NOI | `noiMargin` | `noiMargin * 100` | 10 |
| Precio vs AVG 52S | DailySnapshot AVG | `(1 - price/avg52w) * 100` — precio bajo avg = mejor | 10 |

**Normalización por percentil:**
- Para cada componente C, recopilar todos los valores raw no-null en el universo elegible
- Ordenar ascendente, asignar rank 0..N-1
- percentil_i = rank_i / max(N-1, 1) * 100
- Caso N=1: percentil = 50

**Score total:**
`score = Σ(percentil_i × weight_i) / 100`
- Componentes ausentes contribuyen 0 (no se renormalizan los pesos)
- Esto significa fibras con menos componentes tienen score máximo < 100

**Elegibilidad:**
- Requiere precio actual (PriceSnapshot.LastPrice > 0)
- ≥3 componentes calculados → ranking principal
- 1-2 componentes → sección datos limitados
- 0 componentes o sin precio → excluida

**Perfiles:**
| Perfil | NavDiscount | DividendYield | LtvInverted | NoiMargin | Pricevs52w |
|---|---|---|---|---|---|
| default | 30 | 30 | 20 | 10 | 10 |
| renta | 20 | 50 | 10 | 20 | 0 |
| crecimiento | 40 | 15 | 25 | 10 | 10 |

### Arquitectura Backend

- `OpportunityScoreCalculator` es estático puro — sin efectos secundarios, solo transforma datos
- El endpoint carga datos con 3 queries paralelas: `GetLatestSnapshotPerFibraAsync`, `GetSummaryLatestAsync`, `GetDistributionsByFibrasAsync` + `GetWeek52AvgByFibrasAsync`
- Solo FIBRAs con `State = Active` entran al universo
- `UserOpportunityWeights` vive en schema `portfolio` para reutilizar la migración existente

### Arquitectura Frontend

- Los sliders operan en estado local (no llamada API en cada movimiento)
- El recálculo del ranking es 100% en el cliente: la respuesta del servidor incluye raw values, el cliente aplica pesos + normalización local
- Al guardar: `PUT /weights` persiste en servidor, luego invalida query `['opportunities']`
- Diseño: tabla con shadcn/ui + Tailwind, misma familia visual que PortafolioPage

### Patrones relevantes del proyecto

- `IClassFixture<ApiWebFactory>` para integration tests si se agregan — pero tests de calculador son unit tests puros
- `ExecuteUpdateAsync` no funciona en InMemory — usar change tracking (lección de épica 6)
- Los pesos del usuario en JSON: `{"navDiscount":30,"dividendYield":30,"ltvInverted":20,"noiMargin":10,"pricevs52w":10}`

## Dev Agent Record

### Completion Notes

- Branch: `story/7-1-score-de-oportunidad-y-ranking-del-universo`
- 6/6 unit tests nuevos (OpportunityScoreCalculatorTests): todos pasando ✅
- 245/250 integration tests: sin nuevas regresiones (5 pre-existentes) ✅
- Migración EF: `AddUserOpportunityWeights` aplicada a dev DB ✅
- Cliente API regenerado con openapi-typescript ✅
- Recálculo de scores local (cliente) — sin llamada API al mover sliders ✅

## File List

- `src/Server/Domain/Portfolio/UserOpportunityWeights.cs` (nuevo)
- `src/Server/SharedApiContracts/Opportunities/OpportunityWeightsDto.cs` (nuevo)
- `src/Server/SharedApiContracts/Opportunities/OpportunityFibraRowDto.cs` (nuevo)
- `src/Server/SharedApiContracts/Opportunities/OpportunityRankingResponseDto.cs` (nuevo)
- `src/Server/Application/Opportunities/IOpportunityWeightsRepository.cs` (nuevo)
- `src/Server/Application/Opportunities/OpportunityWeightsConfig.cs` (nuevo)
- `src/Server/Application/Opportunities/OpportunityScoreCalculator.cs` (nuevo)
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Portfolio/UserOpportunityWeightsConfiguration.cs` (nuevo)
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs` (modificado)
- `src/Server/Infrastructure/Persistence/Repositories/Opportunities/OpportunityWeightsRepository.cs` (nuevo)
- `src/Server/Infrastructure/Migrations/20260605145057_AddUserOpportunityWeights.cs` (nuevo)
- `src/Server/Infrastructure/Migrations/20260605145057_AddUserOpportunityWeights.Designer.cs` (nuevo)
- `src/Server/Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (modificado)
- `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs` (nuevo)
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` (modificado)
- `src/Server/Api/Program.cs` (modificado)
- `tests/Unit/Application.Tests/Opportunities/OpportunityScoreCalculatorTests.cs` (nuevo)
- `src/Web/SharedApiClient/schema.d.ts` (regenerado)
- `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx` (nuevo)
- `src/Web/Main/src/app/routes.tsx` (modificado)
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx` (modificado)

### Review Findings

- [x] **Review-Patch P1** — `DefaultWeightsDto` campo estático sin referencias, eliminado (`OpportunityEndpoints.cs:16`)
- [x] **Review-Patch P2** — `effectiveWeights` fallback `|| N` reemplazado por null-check explícito; evita mostrar 30% cuando un peso es 0 (ej. perfil Renta `pricevs52w=0`) (`OportunidadesPage.tsx:268`)
- [x] **Review-Patch P3** — `saveWeightsMutation` sin `onError`: agregado handler + mensaje de error inline (`OportunidadesPage.tsx:294`)
- [x] **Review-Patch P4** — `ComponentBar` ahora muestra puntos de contribución reales (`percentile × weight / 100`) en lugar de percentil bruto; encabezado actualizado a "Contribución al score por componente (puntos)" (`OportunidadesPage.tsx:64`)
- [x] **Review-Patch P5** — Botones `<button>` sin `type="button"` en perfiles y guardar configuración (`OportunidadesPage.tsx:376,432`)

## Change Log

- 2026-06-05: Implementación inicial historia 7-1 — score de oportunidad y ranking del universo
- 2026-06-05: Code review — 5 patches aplicados (P1-P5)
