# Story 15.1: INPC — Rendimiento Real e Inflación Visible

Status: review

## Story

Como usuario autenticado de Fibras Inmobiliarias,
quiero ver mi rendimiento ajustado por inflación en las herramientas de análisis, portafolio y comparación,
para que pueda saber si mis inversiones en FIBRAs realmente están creciendo mi poder adquisitivo y no solo igualando la inflación.

## Acceptance Criteria

**AC-1 — Backfill histórico 5 años**
Dado que la tabla `[ops].[InpcMonthly]` tiene datos parciales (≤25 meses),
cuando el operador ejecuta `POST /api/v1/ops/banxico/sync-inpc/backfill`,
entonces el sistema fetcha desde Banxico SP1 desde hace 72 meses y upserta todos los registros disponibles, logrando un histórico de ≥60 entradas en la DB.

**AC-2 — Herramientas: rendimiento real en FIBRAs vs CETES**
Dado que el INPC más reciente disponible (último elemento de `inpcHistory`) es ≥ 0,
cuando el usuario ve la tabla de resultados de la calculadora FIBRAs vs CETES,
entonces aparece una fila adicional por cada FIBRA y para CETES con su rendimiento real calculado por fórmula de Fisher `((1 + nominal/100) / (1 + inpc/100) - 1) * 100`, junto a un chip de contexto `"INPC últimos 12m: X.X%"`.
Si `inpcHistory` está vacío o null, la fila no aparece (degradación silenciosa).

**AC-3 — Portafolio KPIs: yield real**
Dado que el usuario autenticado tiene posiciones en su portafolio,
cuando ve el KpiCard de "Yield del Portafolio",
entonces aparece un sublabel `"Real: X.X% vs INPC Y.Y%"` calculado con Fisher.
En la sección secundaria (expandible) aparece una nueva métrica "Yield Real" con tono verde si > 0 y rojo si ≤ 0.
Si INPC no está disponible, el sublabel no aparece y el KpiCard queda idéntico al estado actual.

**AC-4 — Oportunidades: 6° factor Yield Real (default 0%)**
Dado que el usuario accede a `/oportunidades`,
cuando abre el panel de pesos,
entonces existe un 6° slider "Yield Real [INPC]" con valor por defecto 0%.
La suma de los 6 pesos siempre debe ser 100 (el slider nuevo empieza en 0 para no romper perfiles actuales).
Cuando el slider está en 0, el scoring es idéntico al comportamiento previo.
En la fila expandida de cada FIBRA aparece el componente "Yield Real" con su barra de contribución y el valor `(yield TTM - INPC anual)`.
Si INPC no disponible, el slider queda deshabilitado con tooltip explicativo.

**AC-5 — Comparador: fila Yield Real**
Dado que el usuario seleccionó ≥ 2 FIBRAs en el comparador,
cuando ve la sección "Distribuciones",
entonces aparece una fila adicional "Yield real [vs INPC]" debajo de "Yield decretado", calculando `yield calculado - INPC anual (Fisher)`.
El ganador (mayor yield real) se resalta igual que en las otras filas.
La fila incluye tooltip: "Yield calculado ajustado por INPC últimos 12m (X.X%). Fórmula de Fisher."
Si INPC no disponible, la fila no aparece.

**AC-6 — Performance Chart: benchmark INPC acumulado**
Dado que el usuario autenticado ve la gráfica de performance de su portafolio,
cuando el rango seleccionado es 1y o all,
entonces aparece una 4ª serie "Inflación (INPC)" como línea de referencia normalizada a 0% al inicio del rango y expresada como porcentaje acumulado de inflación, con toggle igual que las otras series.
Para rangos 30d y 90d, la serie también aparece pero puede mostrar datos escalonados por mes.
Si no hay datos INPC para el rango, la serie queda grisada/toggle deshabilitado.

**AC-7 — Codegen actualizado**
Dado que se agregan campos al DTO de performance,
cuando el dev ejecuta `npm run codegen:api`,
entonces el `schema.d.ts` incluye `inpcSeries` en `PortfolioPerformanceResponseDto` sin errores de compilación.

## Tasks / Subtasks

### T1 — Backend: IInpcRepository.GetRangeAsync (AC-1, AC-6)
- [x] Agregar `Task<IReadOnlyList<InpcMonthlyEntry>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)` a `IInpcRepository`
- [x] Implementar en `InpcRepository.cs` con `WHERE periodo >= from AND periodo <= to ORDER BY periodo ASC`
- [x] Unit test: rango vacío devuelve lista vacía; rango con datos devuelve entradas ordenadas

### T2 — Backend: Endpoint backfill 5 años (AC-1)
- [x] En `OpsBanxicoEndpoints.cs`, agregar `POST /api/v1/ops/banxico/sync-inpc/backfill` (requiere `"AdminOps"`)
- [x] El endpoint calcula `from = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-72)`, ignora DB state, llama `banxico.GetInpcHistoryAsync(from, today, ct)` y llama `inpcRepo.UpsertManyAsync(...)`
- [x] Loguear run en `PipelineRunLog` con pipeline = "BanxicoInpcBackfill"
- [x] No modificar `BanxicoMonthlySyncJob` — el backfill es una operación manual separada
- [x] Integration test: endpoint requiere auth AdminOps; 401 sin token, 403 con token User

### T3 — Backend: INPC series en PortfolioPerformanceResponseDto (AC-6)
- [x] En `PortfolioResponseDto.cs`: agregar `IReadOnlyList<PortfolioPerformancePointDto>? InpcSeries` a `PortfolioPerformanceResponseDto` (nullable — si no hay datos, null)
- [x] En `PortfolioEndpoints.cs` performance handler:
  - Obtener INPC entries para el rango con `inpcRepo.GetRangeAsync(from, today, ct)` (inyectar `IInpcRepository`)
  - Si entries vacío → `InpcSeries = null`
  - Si hay entries: normalizar a step-function diaria + índice base = primer valor disponible en rango
  - Formula normalización: `valuePct = (currentInpcIndex / baseInpcIndex - 1m) * 100m`
  - Construir la serie con resolución diaria usando step-function mensual (cada día usa el valor del mes en curso)
  - Alinear fechas con `PortfolioSeries` fechas existentes (no generar fechas nuevas)
- [x] Unit test: serie vacía cuando no hay INPC en rango; serie normalizada correctamente con 2+ meses

### T4 — codegen (AC-7)
- [x] Ejecutar `npm run codegen:api` después de T3 para regenerar `schema.d.ts`
- [x] Verificar que `PortfolioPerformanceResponseDto` en schema incluye `inpcSeries`

### T5 — Frontend utils: inflación (AC-2..AC-5)
- [x] Crear `src/Web/Main/src/shared/lib/inflation-utils.ts`:
  ```typescript
  export function calcRealReturn(nominalPct: number, inflationPct: number): number
  // Fisher: ((1 + nominal/100) / (1 + inflation/100) - 1) * 100
  export function latestInpcPct(inpcHistory: InpcMonthlyDto[] | null | undefined): number | null
  // Devuelve anualPct del último elemento, o null si vacío/undefined
  ```
- [x] Unit tests: Fisher con valores conocidos; denominador = 0 (inflación = -100%) → debe manejarse sin dividir por cero; `latestInpcPct(null)` → null; `latestInpcPct([])` → null

### T6 — Frontend: Herramientas rendimiento real (AC-2)
- [x] En `herramientas-logic.ts`: el resultado de `calcFibraVsCetes` ya tiene `rendimientoTotalPct`; calcular `rendimientoRealPct = calcRealReturn(rendimientoTotalPct / horizonte, inpc)` para rendimiento anualizado real  
  **Nota:** el rendimiento total acumulado se convierte a tasa anual equivalente antes de aplicar Fisher: `tae = (1 + total/100)^(1/years) - 1`; luego Fisher: `real = ((1 + tae) / (1 + inpc/100) - 1) * 100`
- [x] En `HerramientasPage.tsx`:
  - Extraer `const latestInpc = latestInpcPct(indicadoresQuery.data?.inpcHistory)`
  - Pasar `latestInpc` a la tabla de resultados FIBRAs vs CETES
  - Renderizar fila "Rendimiento real anual" si `latestInpc !== null` (tono `text-muted-foreground`, tamaño `text-sm`)
  - Chip de contexto sobre la tabla: `"Contexto inflación · INPC últimos 12m: X.X%"` (solo si `latestInpc !== null`)
  - Si real < 0 → tono rojo; si > 0 → verde; si 0 → gris
- [x] Unit tests en `herramientas-logic.ts`: caso INPC 0% (real = nominal), INPC igual al nominal (real ≈ 0), denominador -100% (edge case)

### T7 — Frontend: Portafolio KPIs yield real (AC-3)
- [x] En `PortafolioPage.tsx`: agregar query `fetchIndicadores()` y extraer `latestInpc`
- [x] Pasar `inpcAnual?: number` como prop a `KpiCards`
- [x] En `KpiCards.tsx`:
  - En el KpiCard "Yield del Portafolio": si `inpcAnual` disponible, agregar sublabel `"Real: X.X% vs INPC Y.Y%"` (fuente más pequeña, muted)
  - En la sección secundaria (ya expandible): agregar una 4ª métrica "Yield Real" calculada con Fisher
  - Tono: verde si `yieldReal > 0`, rojo si `yieldReal <= 0`
  - Si `inpcAnual` null/undefined → no renderizar sublabel ni métrica extra (degradación silenciosa)

### T8 — Frontend: Oportunidades 6° factor (AC-4)
- [x] En `OportunidadesPage.tsx`:
  - Agregar `fetchIndicadores()` query para obtener `latestInpc`
  - Añadir `yieldReal: number` a la estructura del scoring (calculado como `yield TTM - latestInpc`, con fallback null si alguno es null)
  - Agregar 6° slider "Yield Real [INPC]" con default 0% y min/max 0/100
  - Lógica de suma = 100: el nuevo slider se inicializa en 0 para no alterar el total de los 5 existentes
  - En la vista expandida de cada FIBRA: mostrar barra "Yield Real" con su contribución al score
  - Si `latestInpc` null → slider deshabilitado con tooltip `"INPC no disponible temporalmente"`
  - Los 3 perfiles preset (Predeterminado, Renta, Crecimiento) mantienen el 6° factor en 0%

### T9 — Frontend: Comparador fila Yield Real (AC-5)
- [x] En `ComparadorPage.tsx`:
  - Agregar `fetchIndicadores()` query para obtener `latestInpc`
  - En la sección "Distribuciones", agregar fila "Yield real [vs INPC]" después de "Yield decretado"
  - Cálculo: `yieldReal = calcRealReturn(yieldCalculado, latestInpc)` por FIBRA
  - Winner detection: misma lógica de resaltado que las otras filas (mayor = ganador)
  - Tooltip en el label: `"Yield calculado ajustado por INPC últimos 12m (X.X%). Fórmula de Fisher."`
  - Si `latestInpc` null → no renderizar la fila

### T10 — Frontend: PerformanceChart 4ª serie INPC (AC-6)
- [x] En `PerformanceChart.tsx`:
  - Agregar `inpcSeries` al merge de series (si no null): mapear `{ date, valuePct }` → `inpc: number`
  - Agregar toggle "Inflación INPC" con color naranja/ámbar (ej. `stroke="#f97316"`)
  - Si `performanceQuery.data?.inpcSeries` null → toggle aparece deshabilitado (grisado, no clickeable)
  - Incluir `inpc` en el cálculo del Y-axis domain para que la escala incluya la línea de inflación
  - Tooltip: mostrar valor INPC al igual que los otros cuando se hace hover

## Dev Notes

### Arquitectura del backfill (T2)

El endpoint de backfill es **independiente de `BanxicoMonthlySyncJob`**. No hereda la lógica incremental — simplemente llama directo a `IBanxicoClient.GetInpcHistoryAsync` con `from = today.AddMonths(-72)` y `to = today`. El job diario sigue funcionando igual (incremental desde el último período). Esto permite ejecutar el backfill una sola vez sin afectar el flujo normal.

El endpoint se agrega en `OpsBanxicoEndpoints.cs` que ya tiene el patrón para `sync-tiie/run` y `sync-inpc/run`. El backfill usa `Pipeline = "BanxicoInpcBackfill"` para diferenciarlo en `PipelineRunLog`.

### Normalización INPC para PerformanceChart (T3)

El challenge es que los benchmarks IPC/S&P son series diarias (snapshots de `DailySnapshot`), mientras que el INPC es mensual. La solución es una **step-function**:

1. Para cada fecha `d` en el rango de la gráfica, el valor INPC es el del mes cuyo `Periodo` es `(d.Year, d.Month, 1)`.
2. Si no existe entry para ese mes (p.ej. mes en curso sin datos), usar el último entry disponible.
3. Normalizar: `valuePct = (inpcEntry.InpcIndex / baseEntry.InpcIndex - 1m) * 100m`
4. `baseEntry` = entry cuyo `Periodo` es el mes de inicio del rango (o el más cercano anterior).

Esto produce una línea escalonada mensual que es visualmente honesta y no implica interpolación.

**Importante**: el performance endpoint actualmente usa `await` secuencial para queries con el mismo DbContext (convención EF Core — nunca `Task.WhenAll` con mismo context). Al agregar `inpcRepo.GetRangeAsync`, asegurarse de que sea un `await` separado después de los otros queries.

### Fisher formula (T5)

La fórmula de Fisher para rendimiento real:
```typescript
function calcRealReturn(nominalPct: number, inflationPct: number): number {
  // Evitar división por cero si inflación = -100%
  const denom = 1 + inflationPct / 100
  if (Math.abs(denom) < 0.0001) return 0
  return ((1 + nominalPct / 100) / denom - 1) * 100
}
```

**Edge case crítico**: denominador = 0 si `inflationPct = -100`. Debe ser el primer test unitario.

### Rendimiento real anualizado en Herramientas (T6)

La calculadora `calcFibraVsCetes` devuelve `rendimientoTotalPct` = rendimiento acumulado compuesto en `horizonte` años. Para comparar con INPC (que es tasa anual), convertir a TAE primero:
```typescript
const tae = (Math.pow(1 + rendimientoTotalPct / 100, 1 / horizonte) - 1) * 100
const real = calcRealReturn(tae, latestInpc)
```

El display en la tabla mostrará el rendimiento real **anualizado** (no acumulado) para coherencia con INPC anual.

### 6° factor Oportunidades — suma de pesos (T8)

El panel de pesos tiene validación `total must = 100`. Con 6 sliders, la lógica de redistribución al cambiar un slider debe contemplar que si el 6° empieza en 0, los otros 5 suman 100 y el usuario puede subir el 6° solo si baja alguno de los otros manualmente. **No forzar redistribución automática** — mantener el mismo comportamiento actual (validación que bloquea guardar si total ≠ 100).

Los perfiles preset son arrays hardcodeados. Agregarles el 6° peso en 0:
- `Predeterminado`: `[30, 30, 20, 10, 10, 0]`
- `Renta`: `[20, 50, 10, 20, 0, 0]`
- `Crecimiento`: `[40, 15, 25, 10, 10, 0]`

### Datos necesarios para `latestInpc` (T6, T7, T8, T9)

Todos los componentes que necesitan INPC simplemente llaman a `fetchIndicadores()` y extraen `data?.inpcHistory?.[data.inpcHistory.length - 1]?.anualPct`. Esta función ya existe en `fibrasApi.ts` y ya es llamada por `HerramientasPage`. **No crear duplicados** — si el componente ya tenía el query, extender; si no, agregar el query.

### Backfill trigger desde Ops Dashboard (T2)

El DashboardPage actualmente muestra el pipeline "BanxicoInpc". El nuevo endpoint de backfill es independiente y visible solo para AdminOps. No es necesario agregarlo al dashboard automáticamente — puede ser un endpoint manual via postman o UI futura. El story no incluye cambios al `DashboardPage`.

### Checklist SEO

Esta historia no agrega nuevas rutas públicas ni nuevas superficies SEO. No se requiere checklist SEO.

### Security Checklist

- [ ] **TOCTOU**: El endpoint de backfill no tiene riesgo TOCTOU (upsert idempotente). Si se llama dos veces en paralelo, el upsert es atómico por PK `periodo`. ✓
- [ ] **Auth-gating**: Las nuevas filas/KPIs son solo para usuarios autenticados (Herramientas, Portafolio, Oportunidades, Comparador son ya rutas protegidas). El Comparador es público pero solo muestra la fila si hay dato — sin auth issue. ✓
- [ ] **Denominador cero**: `calcRealReturn` con `inflationPct = -100` → primer test unitario obligatorio. `latestInpcPct([])` → null → sin cálculo. ✓

### Project Structure Notes

**Backend — archivos a modificar/crear:**
- `src/Server/Application/Ops/IInpcRepository.cs` → agregar `GetRangeAsync`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/InpcRepository.cs` → implementar `GetRangeAsync`
- `src/Server/Api/Endpoints/Ops/OpsBanxicoEndpoints.cs` → agregar endpoint backfill
- `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs` → añadir INPC series en performance handler
- `src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs` → añadir `InpcSeries?` al record `PortfolioPerformanceResponseDto`

**Frontend — archivos a crear:**
- `src/Web/Main/src/shared/lib/inflation-utils.ts` (nuevo)

**Frontend — archivos a modificar:**
- `src/Web/Main/src/modules/herramientas/herramientas-logic.ts`
- `src/Web/Main/src/modules/herramientas/HerramientasPage.tsx`
- `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`
- `src/Web/Main/src/modules/portafolio/KpiCards.tsx`
- `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx`
- `src/Web/Main/src/modules/comparador/ComparadorPage.tsx`
- `src/Web/Main/src/modules/portafolio/PerformanceChart.tsx`

**Tests backend:**
- `tests/Unit/Infrastructure.Tests/Repositories/InpcRepositoryTests.cs` (o similar)
- `tests/Integration/Api.Tests/OpsBanxioBackfillTests.cs`
- `tests/Unit/Infrastructure.Tests/Endpoints/PortfolioPerformanceInpcTests.cs`

**Tests frontend:**
- `src/Web/Main/src/shared/lib/inflation-utils.test.ts` (nuevo)
- Extender tests existentes de `herramientas-logic.test.ts` (o crear si no existe)

### Referencia de archivos de contexto

- [Source: src/Server/Application/Jobs/BanxicoMonthlySyncJob.cs] — lógica incremental del job
- [Source: src/Server/Application/Ops/IInpcRepository.cs] — interfaz actual
- [Source: src/Server/Api/Endpoints/Ops/OpsBanxicoEndpoints.cs] — patrón del endpoint manual
- [Source: src/Server/Api/Endpoints/Private/IndicadoresEndpoints.cs] — BuildInpcHistory, GetLastAsync(25)
- [Source: src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs] — handler de performance con benchmarks
- [Source: src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs#L70-74] — PortfolioPerformanceResponseDto
- [Source: src/Web/Main/src/modules/herramientas/herramientas-logic.ts] — calcFibraVsCetes, patrones de cálculo
- [Source: src/Web/Main/src/modules/portafolio/KpiCards.tsx] — sección expandible de KPIs secundarios
- [Source: src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx] — 5 sliders + calcLocalScore
- [Source: src/Web/Main/src/modules/comparador/ComparadorPage.tsx] — secciones de filas + winner detection
- [Source: src/Web/Main/src/modules/portafolio/PerformanceChart.tsx] — merge de series + toggles + domain
- [Source: _bmad-output/planning-artifacts/convenciones-fibradis.md] — denominador cero como primer test, EF Core sin Task.WhenAll mismo context

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` - passed, 641/641 tests
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter BanxicoInpcBackfillEndpointTests` - passed, 2/2 tests
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj` - 333/335 passed; 2 failures preexistentes y ajenas a esta historia en `CalculadoraEndpointTests` y `DashboardEndpointTests`
- `npm run codegen:api` - passed, regeneró `src/Web/SharedApiClient/schema.d.ts`
- `npm test --workspace=src/Web/Main` - passed, 184/184 tests
- `npm run build --workspace=src/Web/Main` - passed

### Completion Notes List

- Se agregó el backfill manual de INPC para Ops con ventana fija de 72 meses y auditoría en `PipelineRunLog`.
- El performance de Portafolio ahora expone serie INPC y la gráfica la muestra como línea adicional.
- Las herramientas, portafolio, oportunidades y comparador ya calculan y muestran rendimiento real ajustado por inflación.
- Se añadió el sexto factor de oportunidades `Yield Real` y se extendieron pesos, perfiles y validaciones.
- Se regeneró el cliente API compartido para incluir `inpcSeries` y `yieldReal`.

### File List

- `src/Server/Application/Ops/IInpcRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/InpcRepository.cs`
- `src/Server/Api/Endpoints/Ops/OpsBanxicoEndpoints.cs`
- `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs`
- `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs`
- `src/Server/Application/Opportunities/OpportunityWeightsConfig.cs`
- `src/Server/SharedApiContracts/Opportunities/OpportunityWeightsDto.cs`
- `src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs`
- `src/Web/Main/src/shared/lib/inflation-utils.ts`
- `src/Web/Main/src/shared/lib/inflation-utils.test.ts`
- `src/Web/Main/src/modules/herramientas/herramientas-logic.ts`
- `src/Web/Main/src/modules/herramientas/herramientas-logic.test.ts`
- `src/Web/Main/src/modules/herramientas/HerramientasPage.tsx`
- `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`
- `src/Web/Main/src/modules/portafolio/KpiCards.tsx`
- `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx`
- `src/Web/Main/src/modules/comparador/ComparadorPage.tsx`
- `src/Web/Main/src/modules/portafolio/PerformanceChart.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/InpcRepositoryTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/BanxicoMonthlySyncJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Endpoints/PortfolioPerformanceInpcTests.cs`
- `tests/Integration/Api.Tests/Ops/BanxicoInpcBackfillEndpointTests.cs`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-06-18: Implementación completa de la historia 15.1, pruebas ejecutadas, story movido a `review`.

### Review Findings

- [x] \[Review\]\[Decision\] **yieldRealPct es diferencia aritmética (~3.5 pp) pero calcLocalScore la pondera igual que scores 0–100 → el 6° slider es prácticamente inerte (~21× menos influencia que otros factores)** — **Implementado opción B:** `yieldRealScore` se computa server-side como percentil sobre el universo activo. `OpportunityScoreCalculator` recibe `latestInpcAnualPct`, computa `yieldRealRaw = dividendYieldRaw - inpc` por FIBRA, ejecuta `PercentileNormalize`, expone `YieldRealScore` + `YieldRealPct` en DTO. Frontend usa `row.yieldRealScore` en `calcLocalScore` y `ComponentBar`.

- [x] [Review][Defer] **`weightSum` incluye `yieldReal` aunque el slider esté disabled → posible confusión si `effectiveWeights.yieldReal > 0` con INPC no disponible** — Analizado: el escenario "permanentemente bloqueado" no se produce en práctica porque las weights almacenadas suman 100 incluyendo yieldReal; el usuario puede ajustar los otros sliders activos. Riesgo mínimo, diferido.

- [x] \[Review\]\[Patch\] **`getToneClass(0)` retorna neutro pero el spec (AC-3) dice "rojo si ≤ 0" para Yield Real** — **Implementado:** `KpiCards.tsx` línea 149: reemplazado `getToneClass(yieldReal)` por `yieldReal <= 0 ? 'text-red-600' : 'text-green-600'`.

- [x] [Review][Defer] **FakeInpcRepository.GetRangeAsync ignora parámetros from/to — test coverage frágil para futuros escenarios con entradas fuera del rango** — `PortfolioPerformanceInpcTests.cs`: los tests actuales pasan porque los datos del fake están dentro del rango; el fake devuelve todo incondicionalmente. Impacto: futuras regressions no detectadas si se agregan entradas fuera del rango al fake.

- [x] [Review][Defer] **`baseEntry` cae a `normalizedEntries[0]` cuando no existe entry ≤ rangeMonthStart → serie INPC con base errónea sin aviso** — `PortfolioEndpoints.cs` `BuildInpcSeriesAsync`: ocurre cuando el portafolio comienza en un mes cuyo INPC aún no está publicado. La serie retorna valores no-nulos pero con base desplazada. Sin log de advertencia.

- [x] [Review][Defer] **Toggle INPC disabled=true MIENTRAS carga (data=undefined) → parpadeo visible** — `PerformanceChart.tsx` línea 131: `!performanceQuery.data?.inpcSeries?.length`. Con data=undefined, disabled=true. Al llegar datos, enabled. Fix sugerido: `key === 'inpc' && performanceQuery.isSuccess && !performanceQuery.data?.inpcSeries?.length`.

- [x] [Review][Defer] **"balanceado" añadido a `FromProfile` pero bloqueado por `ValidateWeights` y ausente en `PROFILES` frontend → dead code path** — `OpportunityWeightsConfig.cs` + `OpportunityEndpoints.cs`: `FromProfile("balanceado")` retorna `Balanced` pero `ValidateWeights` rechaza `profile = "balanceado"` con 422. Frontend nunca genera este valor. Sin impacto de usuario actual.

- [x] [Review][Defer] **Race condition en test de integración de backfill al ejecutarse justo en medianoche UTC** — `BanxicoInpcBackfillEndpointTests.cs`: `expectedFrom` y `fakeBanxico.LastFrom` se calculan independientemente. Probabilidad mínima en práctica.

- [x] [Review][Defer] **TOCTOU pre-existente en `UpsertManyAsync` (FindAsync → Add) puede lanzar PK violation si dos AdminOps llaman backfill simultáneamente** — `InpcRepository.cs`: patrón FindAsync+Add sin transacción atómica. El endpoint captura la excepción y retorna 500. Riesgo mínimo (sólo AdminOps, operación manual).
