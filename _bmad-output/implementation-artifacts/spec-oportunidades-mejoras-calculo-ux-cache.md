---
title: 'Oportunidades — correcciones cálculo, caché y UX (grupos A C D E)'
type: 'feature'
created: '2026-06-05'
status: 'done'
baseline_commit: '39fdd9a3d5045c1d0cadc9a3f6d4dbb7ae5c9c41'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El módulo Oportunidades tiene cuatro defectos y mejoras accionables: (1) valores brutos negativos en NAV Discount y Price vs 52w distorsionan percentiles; (2) ausencia de guard en el threshold de degradación permite estado perpetuamente "Degraded" si el valor llega a 0 en DB (D5 deferred 7-3); (3) el catch de race-condition en el upsert de pesos captura cualquier violación de unique constraint, no solo la PK esperada; (4) el endpoint recarga todos los datos en cada petición sin caché; y la UX no da retroalimentación en tiempo real al configurar pesos ni indica el rank en PromediarTab.

**Approach:** Cuatro grupos en un solo PR: (A) correcciones de cálculo backend — floor 0 en negativos, clamp threshold, catch tipado; (C) `IMemoryCache` 15 min para datos crudos compartidos; (D) indicador contextual "Faltan/Sobran N%" en sliders; (E) columna de rank ordinal en PromediarTab.

## Boundaries & Constraints

**Always:**
- NAV Discount y Price vs 52w: `Math.Max(0m, raw)` — componente sigue participando en percentiles con valor 0, nunca se excluye por ser negativo
- `UniverseCoverageCalculator.Calculate()`: `Math.Clamp(degradationThresholdPct, 1, 49)` al inicio — nunca lanzar excepción al caller
- Catch del upsert: añadir `&& pg.ConstraintName == "PK_UserOpportunityWeights"` — otro 23505 debe re-lanzarse (nombre confirmado en migration `20260605145057`)
- Cache key `"opp:raw:v1"` comparte datos crudos entre todos los usuarios; scores se recalculan por request con pesos del usuario — no cachear el ranking final
- TTL de caché: 15 minutos absolutos (no sliding)

**Ask First:**
- Nada identificado — scope completamente claro.

**Never:**
- No cachear el ranking final (depende de pesos del usuario)
- No cambiar `SuspensionThresholdPct = 50m`
- Grupo B (snapshots históricos + deltas ComponentBar) → deferred-work

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| NAV Discount negativo | Precio=35, NAV=30 → raw=−16.67 | `navDiscountRaw = 0`, componente disponible | — |
| Price > Avg52w | Precio=12, Avg52w=10 → raw=−20 | `pricevs52wRaw = 0` | — |
| threshold=0 en DB vía SQL | `degradationThresholdPct=0`, missing=5% | status="Normal" (threshold clamped a 1) | — |
| Race condition PK upsert | Dos PUT concurrentes de pesos | catch solo si `ConstraintName == "PK_UserOpportunityWeights"` | Otro 23505: re-lanza `DbUpdateException` |
| Cache hit | Segunda petición ≤15 min | no hits DB para fibras/snapshots/fundamentals/distributions/avg52w | — |
| Cache miss / expirado | Primera petición o TTL vencido | carga desde DB, almacena en cache | — |
| Suma pesos = 85 | Usuario mueve slider | UI muestra "Faltan 15%" en amber | — |
| Suma pesos = 110 | Usuario mueve slider | UI muestra "Sobran 10%" en destructive | — |

</frozen-after-approval>

## Code Map

- `src/Server/Application/Opportunities/OpportunityScoreCalculator.cs:159,186` — floor a 0 en `navDiscountRaw` y `pricevs52wRaw`
- `src/Server/Application/Opportunities/UniverseCoverageCalculator.cs:15` — clamp `degradationThresholdPct` a [1,49]
- `src/Server/Infrastructure/Persistence/Repositories/Opportunities/OpportunityWeightsRepository.cs:33` — narrow catch a `ConstraintName == "PK_UserOpportunityWeights"`
- `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs` — inyectar `IMemoryCache`; cachear datos crudos 15 min bajo `"opp:raw:v1"`; `configRepo` y `weightsRepo` quedan fuera del cache (son por-usuario o de config)
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registrar `AddMemoryCache()` si no existe
- `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx:516-519` — suma de pesos: reemplazar texto estático por "Faltan N%" / "Sobran N%" / "✓ 100%"
- `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx:112-122` — añadir columna `#` (rank 1-based) como primera columna
- `tests/Unit/Application.Tests/Opportunities/OpportunityScoreCalculatorTests.cs` — 2 tests nuevos: floor NAV negativo, floor 52w negativo
- `tests/Unit/Application.Tests/Opportunities/UniverseCoverageCalculatorTests.cs` — 1 test nuevo: threshold=0 clamped a Normal

## Tasks & Acceptance

**Execution:**
- [x] `src/Server/Application/Opportunities/OpportunityScoreCalculator.cs` -- aplicar `Math.Max(0m, ...)` en L159 (`navDiscountRaw`) y L186 (`pricevs52wRaw`) -- valores negativos distorsionan percentiles; 0 significa "sin descuento en esta métrica"
- [x] `src/Server/Application/Opportunities/UniverseCoverageCalculator.cs` -- primera línea de `Calculate()`: `degradationThresholdPct = Math.Clamp(degradationThresholdPct, 1, 49)` -- D5 deferred 7-3; guard contra configuración 0 en DB
- [x] `src/Server/Infrastructure/Persistence/Repositories/Opportunities/OpportunityWeightsRepository.cs` -- cambiar `when` del catch a: `ex.InnerException is Npgsql.PostgresException { SqlState: "23505" } pg && pg.ConstraintName == "PK_UserOpportunityWeights"` -- catch genérico enmascara otras violaciones de constraint
- [x] `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs` -- inyectar `IMemoryCache cache` como parámetro del handler GET `/`; intentar `cache.TryGetValue("opp:raw:v1", out RawOpportunityData? raw)`; si miss: ejecutar los 5 awaits de datos crudos y guardar con `cache.Set("opp:raw:v1", raw, TimeSpan.FromMinutes(15))`; `configRepo.GetAsync` y `weightsRepo` permanecen fuera del cache
- [x] `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` -- añadir `builder.Services.AddMemoryCache()` si no existe
- [x] `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx` -- en L516-519, reemplazar el `<span>Suma: {weightSum}%…</span>` por lógica condicional: `weightSum === 100` → texto muted `"✓ 100%"`; `weightSum < 100` → texto amber `"Faltan {100 - weightSum}%"`; `weightSum > 100` → texto destructive `"Sobran {weightSum - 100}%"`
- [x] `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` -- añadir `<th>#</th>` como primera columna en `<thead>` y `<td className="px-3 py-2 text-muted-foreground tabular-nums text-xs">#{idx + 1}</td>` en cada fila del `<tbody>`
- [x] `tests/Unit/Application.Tests/Opportunities/OpportunityScoreCalculatorTests.cs` -- añadir `Calculate_NavDiscountNegative_FlooredToZero` y `Calculate_Pricevs52wNegative_FlooredToZero`
- [x] `tests/Unit/Application.Tests/Opportunities/UniverseCoverageCalculatorTests.cs` -- añadir `Calculate_ThresholdZero_ClampedToOne_ZeroMissingIsNormal`: threshold=0 clamped, 0% missing → Normal

**Acceptance Criteria:**
- Given FIBRA con precio > NAV, when `Calculate()`, then `navDiscountRaw = 0` y el componente cuenta como disponible (no null)
- Given FIBRA con precio > avg52w, when `Calculate()`, then `pricevs52wRaw = 0`
- Given `degradationThresholdPct = 0`, when `Calculate()` con 0% missing (todas las FIBRAs con precio), then `status = "Normal"` (threshold clamped a 1; 0% < 1% → Normal)
- Given upsert con 23505 en otra constraint (no PK), when catch, then `DbUpdateException` se re-lanza
- Given segunda petición al ranking en < 15 min, then no hits DB para los 5 datos crudos
- Given weightSum = 85 al mover slider, then UI muestra "Faltan 15%" en color amber
- Given tabla PromediarTab, then primera columna muestra "#1", "#2"… en orden descendente por score
- `dotnet test` pasa incluyendo los 3 nuevos tests

## Design Notes

**Floor a 0 en negativos:** `Math.Max(0m, raw)` es la corrección semánticamente correcta. Una FIBRA que cotiza sobre NAV (premium) merece percentil bajo en esa métrica, no un valor negativo que comprime la curva entera de las demás FIBRAs.

**Caché raw data:** Crear un `private sealed record RawOpportunityData(...)` con los 5 diccionarios/listas como tipo del cache entry. `IMemoryCache` es singleton — no hay riesgo de DbContext en el cache; solo se almacenan los datos ya materializados.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: 0 errors
- `dotnet test tests/Unit/Application.Tests --no-build` -- expected: todos verdes, incluyendo 3 nuevos

## Suggested Review Order

**Caché de datos crudos (Grupo C — entry point)**

- Forma del payload cacheado; todos los campos son datos crudos compartidos entre usuarios.
  [`OpportunityEndpoints.cs:18`](../../src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs#L18)

- Cache miss: carga los 5 awaits y almacena; hit: los omite. `config`/`weights` fuera del bloque.
  [`OpportunityEndpoints.cs:54`](../../src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs#L54)

- Registro del singleton `IMemoryCache` en el contenedor.
  [`ApiServiceExtensions.cs:45`](../../src/Server/Api/CompositionRoot/ApiServiceExtensions.cs#L45)

**Correcciones de cálculo — floor de negativos (Grupo A)**

- `Math.Max(0m, ...)` en `navDiscountRaw`: FIBRA sobre NAV recibe percentil 0, no negativo.
  [`OpportunityScoreCalculator.cs:159`](../../src/Server/Application/Opportunities/OpportunityScoreCalculator.cs#L159)

- Mismo floor para `pricevs52wRaw`: precio > promedio 52s también se clipa en 0.
  [`OpportunityScoreCalculator.cs:186`](../../src/Server/Application/Opportunities/OpportunityScoreCalculator.cs#L186)

**Guards de defensividad (Grupo A)**

- `Math.Clamp(1, 49)`: threshold=0 en DB ya no causa estado perpetuamente "Degraded".
  [`UniverseCoverageCalculator.cs:21`](../../src/Server/Application/Opportunities/UniverseCoverageCalculator.cs#L21)

- Catch tipado: otro 23505 (distinta constraint) ahora se re-lanza correctamente.
  [`OpportunityWeightsRepository.cs:33`](../../src/Server/Infrastructure/Persistence/Repositories/Opportunities/OpportunityWeightsRepository.cs#L33)

**UX — retroalimentación en sliders y rank (Grupos D y E)**

- "Faltan N%" / "Sobran N%" / "✓ 100%" con colores contextuales por estado.
  [`OportunidadesPage.tsx:517`](../../src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx#L517)

- Columna `#` en cabecera de PromediarTab — hace explícito el rank ordinal.
  [`PromediarTab.tsx:113`](../../src/Web/Main/src/modules/oportunidades/PromediarTab.tsx#L113)

- Celda `#{idx + 1}` en cada fila; orden descendente por score ya existía.
  [`PromediarTab.tsx:155`](../../src/Web/Main/src/modules/oportunidades/PromediarTab.tsx#L155)

**Tests**

- Precio > NAV → `navDiscountRaw = 0`, componente disponible (no null).
  [`OpportunityScoreCalculatorTests.cs:229`](../../tests/Unit/Application.Tests/Opportunities/OpportunityScoreCalculatorTests.cs#L229)

- Precio > avg52w → `pricevs52wRaw = 0`.
  [`OpportunityScoreCalculatorTests.cs:249`](../../tests/Unit/Application.Tests/Opportunities/OpportunityScoreCalculatorTests.cs#L249)

- threshold=0 clamped a 1, 0% missing → "Normal" (cubre el bug exacto del guard).
  [`UniverseCoverageCalculatorTests.cs:101`](../../tests/Unit/Application.Tests/Opportunities/UniverseCoverageCalculatorTests.cs#L101)
