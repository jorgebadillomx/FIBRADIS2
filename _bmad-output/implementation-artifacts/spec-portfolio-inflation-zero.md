---
title: 'Inflación (INPC) plana en cero en la gráfica de rendimiento del portafolio'
type: 'bugfix'
created: '2026-06-23'
status: 'done'
baseline_commit: '2373cdc9408346e66ba9c9e3edd4d7a5f3757bd0'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** En la sección privada de portafolio, la línea "Inflación (INPC)" de la gráfica de rendimiento aparece plana en cero, sobre todo en el rango por defecto **30D**. La serie INPC se construye como una función escalonada mensual con rezago: cada punto diario resuelve al último INPC con `Periodo <= mes-del-punto` y se normaliza contra el INPC del mes del primer punto. En rangos cortos todos los puntos caen en el mismo mes base, y el INPC del mes en curso aún no se publica, por lo que `current == baseEntry` para todos los puntos → `valuePct = 0`. Los datos INPC en BD son correctos (65 registros, ene-2021 → may-2026); el defecto está en el cálculo, no en la ingesta.

**Approach:** Reemplazar la resolución escalonada mensual por un **índice INPC interpolado a valor diario**, con **proyección del mes en curso (aún no publicado)** usando la última tasa mensual observada (MoM = `v[n-1]/v[n-2]`). La base pasa a ser el índice interpolado del primer día del rango, y cada punto se normaliza contra esa base. La línea sube de forma suave y coherente en todos los rangos, incluido 30D. Cambio aislado en el backend; el frontend ya renderiza `inpcSeries` sin cambios.

## Boundaries & Constraints

**Always:** Mantener la firma y el contrato de `BuildInpcSeriesAsync` (entrada `portfolioSeries` + `IInpcRepository`, salida `IReadOnlyList<PortfolioPerformancePointDto>?`). Conservar el filtro `InpcIndex > 0`. Devolver `null` cuando no haya entradas en rango (toggle se deshabilita). `valuePct` redondeado a 4 decimales. La serie resultante debe tener exactamente un punto por fecha de `portfolioSeries`.

**Ask First:** Etiquetar visualmente el tramo proyectado del mes en curso como estimación (cambio de UI/leyenda) — fuera de alcance salvo que se pida.

**Never:** No tocar la ingesta/migraciones de INPC ni `InpcRepository`. No modificar las series de portfolio/IPC/S&P500 ni `BuildNormalizedPoints`. No cambiar el componente de gráfica `PerformanceChart.tsx`. No extrapolar más allá del mes que necesiten los puntos del rango.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Rango 30D dentro de mes en curso sin publicar | Puntos diarios de may–jun; último INPC = mayo | Serie estrictamente creciente (no todo 0); jun proyectado con MoM de may | N/A |
| Interpolación intramensual | Puntos en distintos días del mismo mes con mes siguiente publicado | Valor diario interpolado linealmente entre anclas mensuales | N/A |
| Proyección multi-mes | Punto en mes > último publicado | Índice = último ancla × MoM^(meses después) | N/A |
| Sin INPC en rango | `GetRangeAsync` devuelve vacío | Retorna `null` | Toggle INPC deshabilitado |
| Una sola entrada publicada | `normalizedEntries.Count == 1` | MoM=1 (degradación suave, sin crash) | N/A |
| `portfolioSeries` vacío | Count == 0 | Retorna `null` | N/A |

</frozen-after-approval>

## Code Map

- `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs` -- contiene `BuildInpcSeriesAsync` (líneas ~530-568) y el record `InpcStepEntry`; aquí va el cambio de algoritmo.
- `src/Server/Application/Ops/IInpcRepository.cs` -- contrato del repo (`GetRangeAsync`), no se modifica.
- `tests/Unit/Infrastructure.Tests/Endpoints/PortfolioPerformanceInpcTests.cs` -- tests por reflexión de `BuildInpcSeriesAsync`; actualizar expectativas (escalonado → interpolado) y añadir casos.
- `src/Web/Main/src/modules/portafolio/PerformanceChart.tsx` -- consumidor de `inpcSeries`; sin cambios (referencia).

## Tasks & Acceptance

**Execution:**
- [x] `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs` -- Reescribir `BuildInpcSeriesAsync`: construir anclas mensuales (índice por mes) a partir de `normalizedEntries`, extender con meses proyectados (MoM = `v[n-1]/v[n-2]`, MoM=1 si <2 entradas) hasta cubrir el mes superior del último punto; helper `InterpolatedIndexAt(DateOnly)` que interpola linealmente entre el ancla del primer día del mes y la del mes siguiente según `(día-1)/díasDelMes`; clamp al primer ancla para fechas anteriores; `base = InterpolatedIndexAt(firstPortfolioDate)`; `valuePct = round((idx/base - 1)*100, 4)`. Conservar guardas de `null` existentes.
- [x] `tests/Unit/Infrastructure.Tests/Endpoints/PortfolioPerformanceInpcTests.cs` -- Actualizar `WhenEntriesExist_NormalizesFromBaseMonth` a valores interpolados; añadir test del bug (30D, todos los puntos en mes base + mes en curso sin publicar → serie estrictamente creciente, primer punto = 0); añadir test de proyección multi-mes; conservar `WhenNoInpcInRange_ReturnsNull`.

**Acceptance Criteria:**
- Given un portafolio con datos y rango 30D donde el INPC del mes en curso no está publicado, when se solicita `/api/v1/portfolio/performance?range=30d`, then `inpcSeries` tiene valores estrictamente crecientes (primer punto 0, último > 0) en vez de todos cero.
- Given cualquier rango con ≥2 entradas INPC, when se construye la serie, then cada fecha de `portfolioSeries` tiene exactamente un punto y los valores son monótonos no decrecientes mientras el INPC suba.
- Given que no hay entradas INPC en el rango, when se construye la serie, then retorna `null` y el toggle INPC se muestra deshabilitado (comportamiento actual preservado).

## Design Notes

Anclas: diccionario `mes(1° día) → índice`. Ejemplo interpolación (mar=110, abr=121): para `2026-03-15`, fracción `(15-1)/31≈0.4516` → `110 + (121-110)*0.4516 ≈ 114.97`. Proyección: si último publicado = mayo y se requiere junio, `junio = mayo × (mayo/abril)`. La interpolación lineal sobre índices es suficiente; no usar compuesto diario (sobre-ingeniería para el rango de tasas mexicano).

## Verification

**Commands:**
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter FullyQualifiedName~PortfolioPerformanceInpc` -- expected: todos verdes.
- `dotnet build FIBRADIS.slnx` -- expected: compila sin errores.

**Manual checks:**
- Con BD dev (LAPBADIS/FIBRADIS_Dev), abrir la gráfica del portafolio en 30D: la línea naranja "Inflación (INPC)" debe moverse (no quedar pegada en 0%). Nota: con los datos actuales mayo-2026 bajó vs abril, por lo que el tramo proyectado del mes en curso puede inclinarse levemente hacia abajo — refleja la última tasa real observada.

## Suggested Review Order

- Entrada: nueva estrategia interpolada+proyectada que sustituye el escalón mensual (raíz del bug).
  [`PortfolioEndpoints.cs:530`](../../src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs#L530)

- Construcción de anclas mensuales y proyección del mes en curso con MoM (`v[n-1]/v[n-2]`).
  [`PortfolioEndpoints.cs:560`](../../src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs#L560)

- Proyección acotada al mes posterior al último punto (no extrapola de más).
  [`PortfolioEndpoints.cs:574`](../../src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs#L574)

- Interpolación lineal diaria entre anclas, con clamps y guarda de división.
  [`PortfolioEndpoints.cs:583`](../../src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs#L583)

- Normalización contra la base interpolada del primer día del rango.
  [`PortfolioEndpoints.cs:605`](../../src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs#L605)

**Tests**

- Repro del bug 30D: serie estrictamente creciente, ya no plana en cero.
  [`PortfolioPerformanceInpcTests.cs:55`](../../tests/Unit/Infrastructure.Tests/Endpoints/PortfolioPerformanceInpcTests.cs#L55)

- Interpolación intramensual (~10% sobre base interpolada) y proyección multi-mes (~21%).
  [`PortfolioPerformanceInpcTests.cs:28`](../../tests/Unit/Infrastructure.Tests/Endpoints/PortfolioPerformanceInpcTests.cs#L28)
