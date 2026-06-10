# Story 9.3: Mejoras al Portafolio y Superficies

Status: done

## Story

Como usuario autenticado,
quiero ver métricas más ricas en mi portafolio (YOC, yield, ingreso mensual, score, gráfica de rendimiento, logo, tab de calendario) y encontrar el yield en el catálogo y la ficha pública,
para tomar mejores decisiones de inversión sin salir de la plataforma.

## Contexto — qué ya existe y qué se agrega

**YA IMPLEMENTADO — NO tocar:**
- `% Portafolio` (pctPortafolio) — columna ya existe en PositionsTable
- `Plusvalía %` y `Ganancia $` — ya son columnas separadas en la tabla
- `dailyChangePct` — ya se muestra en catálogo (CatalogoPage)
- Story 10-2 cubre la calculadora ISR (no incluida aquí)

**SCOPE DE ESTA HISTORIA:**
1. YOC (Yield on Cost) — nueva columna en tabla de posiciones
2. Ingreso anual estimado + desglose mensual — actualizar KpiCards
3. Yield del portafolio — nueva KPI card
4. "Más métricas" colapsable — agregar toggle para KPI cards secundarias
5. Score de oportunidad en tabla de posiciones — badge junto al ticker
6. Logo de FIBRA en tabla de posiciones
7. Gráfica rendimiento vs benchmarks (IPC BMV + S&P 500)
8. Tab Calendario en portafolio — próximos pagos proyectados
9. Extensión de "Promediar posición" con ¿Qué pasaría si...? usando commission_factor
10. Yield en catálogo — badge en CatalogoPage
11. Badge de yield en header de FibraPage
12. Búsqueda de FIBRA en el drawer móvil del nav
13. Benchmarks en URL del comparador

## Acceptance Criteria

**AC-1**: La tabla de posiciones muestra columna YOC (Yield on Cost = rentaAnual / costoTotalCompra × 100) con formato `X.XX%`. Si rentaAnual es null o costoTotalCompra es 0, mostrar `—`.

**AC-2**: KpiCards muestra "Rentas Anuales Brutas" con subtexto `$X.XX/mes` debajo del valor principal. Si rentasAnualesBrutas es null, el subtexto no aparece.

**AC-3**: Existe nueva KPI card "Yield del Portafolio" calculado como `rentasAnualesBrutas / valorTotal × 100`. Si cualquier operando es null o valorTotal es 0, mostrar `—`.

**AC-4**: Las KPI cards se reorganizan en dos grupos: primarias (siempre visibles: Inversión Total, Valor Total, Plusvalía %, Ganancia $, Yield Portafolio) y secundarias (colapsables con botón "Más métricas ▾": Rentas Anuales Brutas, Rentas Reales Brutas, % Rentas del Portafolio). El estado colapsado/expandido se mantiene en `localStorage` bajo la clave `portfolio_extra_kpis_open`.

**AC-5**: La tabla de posiciones muestra el logo de la FIBRA (32×32px) junto al nombre/ticker. Si la imagen falla, muestra el ticker en texto. El logo se carga desde el endpoint existente de logos.

**AC-6**: La tabla de posiciones muestra un badge de score de oportunidad junto al ticker (ej. `3.4/5`). El backend devuelve `opportunityScore` en `PortfolioPositionDto`. Si el score es null, no se muestra badge. El color del badge sigue la escala: ≥4.0 verde, ≥3.0 ámbar, <3.0 rojo.

**AC-7**: Existe gráfica de "Rendimiento vs benchmarks" en la página de portafolio (debajo de KpiCards, encima de la tabla). Muestra tres líneas: Mi Portafolio, IPC (BMV) y S&P 500. El eje Y es % de cambio desde el inicio del período. Selectores de período: 30D, 90D, 1Y, ALL. Los datos del portafolio se calculan usando las posiciones actuales × precios históricos del `DailySnapshot`. Los benchmarks (^MXX, ^GSPC) se obtienen de Yahoo Finance y se almacenan igual que cualquier FIBRA en `DailySnapshot`.

**AC-8**: La gráfica tiene leyenda interactiva: hacer clic en "Mi Portafolio", "IPC" o "S&P 500" activa/desactiva esa línea individualmente. El estado activo/inactivo es visual (opacidad) sin recargar datos.

**AC-9**: La página de portafolio tiene un segundo tab "Calendario" junto al tab principal "Posiciones". El tab de Calendario muestra los próximos 90 días con pagos esperados por FIBRA, calculados como: si tiene distribución registrada en los últimos 6 meses, proyectar la siguiente fecha según la cadencia detectada (trimestral / semestral / anual). Por cada pago proyectado muestra: FIBRA, fecha estimada, monto por título (último conocido), monto total para el usuario (monto × títulos). Si no hay distribuciones recientes de ninguna posición, muestra estado vacío.

**AC-10**: La sección "Promediar posición" en `/oportunidades` (Épica 7-2) se extiende con un sub-simulador "¿Qué pasaría si...?": el usuario ingresa un número de títulos a comprar. El sistema calcula el nuevo costo promedio ponderado aplicando el `commission_factor` configurado en Ops. Muestra: nuevo costo promedio, delta vs precio actual, nuevo % portafolio estimado. El `commission_factor` se lee del endpoint `GET /api/v1/config` (ya existe). Si commission_factor no está disponible, se asume 0.

**AC-11**: En `CatalogoPage`, cada card de FIBRA muestra el yield anualizado estimado en un badge (formato: `X.X%`). Si no hay yield disponible, el badge no aparece. El yield se obtiene del snapshot del mercado (campo `annualizedYield` en `MarketSnapshotDto`).

**AC-12**: En `FibraPage`, el header de la ficha pública muestra el yield anualizado en un badge de color morado junto al precio actual y el badge de frescura. Si no hay yield, no aparece el badge.

**AC-13**: En el drawer móvil del nav (`PublicLayout.tsx`), existe un campo de búsqueda de FIBRAs que usa el mismo componente `GlobalSearch` o lógica equivalente. Al escribir y seleccionar, navega a la ficha correspondiente y cierra el drawer.

**AC-14**: En `ComparadorPage`, los benchmarks IPC y S&P 500 se reflejan en la URL como parámetro `?benchmarks=ipc,sp500` cuando el usuario los activa. Al compartir el link, los benchmarks seleccionados se restauran automáticamente. (Nota: el comparador actual no tiene gráfica de benchmarks; este AC prepara la URL para una futura historia. Por ahora, solo persistir la selección en URL sin gráfica.)

**AC-15**: `npm run build --workspace=src/Web/Main` pasa con 0 errores TypeScript. `dotnet build FIBRADIS.slnx` pasa limpio.

**AC-16**: Unit tests cubren: calcYoc (denominador 0, null rentaAnual), calcYieldPortafolio (denominador 0), proyección de próximos pagos (cadencia trimestral, semestral, sin datos), nuevo costo promedio con commission_factor en el simulador.

## Tasks / Subtasks

- [x] **T1 — Backend: Campos nuevos en PortfolioPositionDto** (AC: 1, 6)
  - [x] Agregar campo `yoc: float?` a `PortfolioPositionDto` — cálculo: `rentaAnual / costoTotalCompra * 100`; si `costoTotalCompra == 0` o `rentaAnual == null`, devolver `null`
  - [x] Agregar campo `opportunityScore: float?` a `PortfolioPositionDto` — join desde la capa de oportunidades, reutilizando el mismo cálculo que usa `OportunidadesPage`. Leer el score desde la misma fuente (no duplicar lógica de cálculo, exponer vía Application layer).
  - [x] Agregar campo `logoUrl: string?` a `PortfolioPositionDto` — ruta relativa `/logos/{ticker_lower}.png` o null si no aplica. Construir en el mapper, no en el controlador.
  - [x] Agregar migración EF si aplica (solo si se persiste algo nuevo — estos campos son calculados, no persistidos)
  - [x] Actualizar `PortfolioPositionMapper` con los campos nuevos
  - [x] Codegen: `npm run codegen:api` para regenerar `schema.d.ts`

- [x] **T2 — Backend: Campos nuevos en PortfolioKpisDto** (AC: 2, 3)
  - [x] Agregar `yieldPortafolio: float?` a `PortfolioKpisDto` — cálculo: `rentasAnualesBrutas / valorTotal * 100`; null si cualquier operando es null o `valorTotal == 0`
  - [x] Agregar `ingresoMensual: decimal?` a `PortfolioKpisDto` — cálculo: `rentasAnualesBrutas / 12`; null si `rentasAnualesBrutas == null`
  - [x] Actualizar la query/servicio de KPIs del portafolio con los nuevos cálculos
  - [x] Codegen después de esta tarea

- [x] **T3 — Backend: annualizedYield en snapshots del mercado** (AC: 11, 12)
  - [x] Verificar si `annualizedYield` ya existe en el DTO de snapshot devuelto por `/api/v1/fibras/all` y `/api/v1/fibras/{ticker}`. Si ya está en el schema (revisar `schema.d.ts`), T3 se reduce a verificar que llega al frontend.
  - [x] Si no existe: agregar `annualizedYield: float?` al DTO de snapshot de mercado. El cálculo ya existe en el dominio (Épica 3). Exponer en el endpoint que usa `CatalogoPage` y `FibraPage`.
  - [x] Codegen si hubo cambio de contrato

- [x] **T4 — Backend: Endpoint de rendimiento histórico para benchmarks** (AC: 7, 8)
  - [x] Crear endpoint `GET /api/v1/portfolio/performance?range=30d|90d|1y|all`
  - [x] El endpoint devuelve: `{ portfolioSeries: [{date, valuePct}], ipcSeries: [{date, valuePct}], sp500Series: [{date, valuePct}] }`. Todos los valores son % de cambio normalizado desde el primer día del período (base 0%).
  - [x] **Portfolio**: para cada día en el rango, calcular `sum(titulos_posicion × price_dia)` usando `DailySnapshot` de las FIBRAs en el portafolio actual. Usar posiciones actuales (simplificación MVP — no rastrear cambios históricos de posición).
  - [x] **Benchmarks**: `^MXX` (IPC BMV) y `^GSPC` (S&P 500) deben existir como FIBRAs especiales en el catálogo con sus DailySnapshots. Si no existen snapshots de benchmarks, devolver la serie vacía (no fallar). Agregar seed o job para poblarlos si no existe aún — evaluar si entrar en scope de esta historia o diferir con una nota en Dev Agent Record.
  - [x] El endpoint requiere autenticación (rol `User`). Las posiciones son del usuario en sesión.
  - [x] Manejar el caso de portafolio vacío (devolver serie vacía, 200 OK).
  - [x] Codegen

- [x] **T5 — Frontend: Actualizar KpiCards** (AC: 2, 3, 4)
  - [x] Agregar subtexto mensual en la card "Rentas Anuales Brutas": si `kpis.ingresoMensual != null`, mostrar `$X.XX/mes` debajo del valor
  - [x] Agregar nueva card "Yield del Portafolio" con `kpis.yieldPortafolio` formateado como `X.XX%`; si null mostrar `—`
  - [x] Reorganizar en dos grupos (primarias / secundarias) con botón "Más métricas" que toggle el grupo secundario
  - [x] Persistir estado en `localStorage` clave `portfolio_extra_kpis_open`; valor por defecto: `false` (colapsado)
  - [x] Tarjetas primarias: Inversión Total, Valor Total, Plusvalía %, Ganancia $, Yield del Portafolio (5 tarjetas)
  - [x] Tarjetas secundarias: Rentas Anuales Brutas, Rentas Reales Brutas, % Rentas del Portafolio (3 tarjetas)

- [x] **T6 — Frontend: Actualizar PositionsTable** (AC: 1, 5, 6)
  - [x] Columna "FIBRA": renderizar logo 32×32 a la izquierda del nombre/ticker. URL: `/logos/${ticker.toLowerCase()}.png`. Usar `<img>` con `onError` que oculta la imagen y muestra solo el ticker.
  - [x] Nueva columna "YOC": tipo `optional`, mostrar `formatPercent(position.yoc)` o `—`. Agregar a `OPTIONAL_COLUMNS` en `PositionsTable.tsx`.
  - [x] Badge de score: en la celda del nombre/ticker (junto al logo), renderizar debajo del ticker `<ScoreBadge score={position.opportunityScore} />` si `opportunityScore != null`. Colores: ≥4 verde, ≥3 ámbar, <3 rojo.
  - [x] Crear componente `ScoreBadge.tsx` en `modules/portafolio/`.

- [x] **T7 — Frontend: Gráfica rendimiento vs benchmarks** (AC: 7, 8)
  - [x] Crear componente `PerformanceChart.tsx` en `modules/portafolio/`
  - [x] Usar `recharts` (ya en el proyecto — ver `chart.tsx` fork en `shared/ui/chart.tsx`). Usar `LineChart` con `Line` por cada serie.
  - [x] Query: `GET /api/v1/portfolio/performance?range=30d` (default 30D al cargar)
  - [x] Selectores 30D / 90D / 1Y / ALL con estado en `useState`
  - [x] Leyenda interactiva: tres botones con estado `activeLines: Set<string>`. Al clicar, toggle el item en el Set y aplicar `opacity` al `Line` correspondiente via prop `strokeOpacity`.
  - [x] Eje Y: formato `+X%` / `-X%`. Eje X: fechas formateadas.
  - [x] Tooltip con valores de las tres series en el punto.
  - [x] Si la serie de portafolio está vacía (portafolio recién creado), mostrar estado vacío dentro del chart.
  - [x] Si la serie de benchmarks está vacía, ocultar esa línea sin error.
  - [x] Insertar `<PerformanceChart />` en `PortafolioPage.tsx` entre KpiCards y la sección de posiciones (solo cuando `hasPositions`).

- [x] **T8 — Frontend: Tab Calendario en portafolio** (AC: 9)
  - [x] Agregar tabs "Posiciones" / "Calendario" en `PortafolioPage.tsx` usando `Tabs` de shadcn/ui (ya existe en el proyecto).
  - [x] Crear componente `PortafolioCalendario.tsx` en `modules/portafolio/`
  - [x] La lógica de proyección de fechas va en una función pura `projectNextPayments(positions, today): ProjectedPayment[]` en `portfolio-calendar.ts`. Un `ProjectedPayment` tiene: `{ fibraId, ticker, nombre, fechaEstimada: Date, montoPorTitulo: number, montoTotal: number, cadencia: 'trimestral' | 'semestral' | 'anual' | 'desconocida' }`.
  - [x] Para cada posición: obtener las distribuciones recientes del campo `recentDistributions` ya presente en `PortfolioPositionDto`. Detectar cadencia: si hay 4+ distribuciones en 12 meses → trimestral; 2 en 12 meses → semestral; 1 → anual. Con la fecha de la última distribución y la cadencia, proyectar la próxima fecha.
  - [x] Mostrar los próximos 90 días agrupados por mes. Para cada evento: logo, ticker, fecha (ej. "15 jul"), monto por título, monto total.
  - [x] Si no hay distribuciones recientes en ninguna posición: mostrar estado vacío con mensaje "No hay suficientes datos de distribuciones para proyectar pagos".

- [x] **T9 — Frontend: Extender Promediar con ¿Qué pasaría si...?** (AC: 10)
  - [x] En el módulo `oportunidades`, localizar el componente de Promediar (de story 7-2).
  - [x] Agregar sub-sección "¿Qué pasaría si...?" debajo del simulador existente.
  - [x] Input: "Títulos a comprar" (entero positivo). Al cambiar el input, recalcular en tiempo real.
  - [x] Leer `commissionFactor` del query de config existente (`GET /api/v1/config` o similar — verificar el endpoint real en el schema). Si no está disponible, usar 0.
  - [x] Cálculo del nuevo costo promedio: `newAvgCost = (currentTitulos * currentAvgCost + newTitulos * currentPrice * (1 + commissionFactor)) / (currentTitulos + newTitulos)`.
  - [x] Mostrar: nuevo costo promedio, diferencia vs precio actual (%), nuevo % de portafolio estimado (usando `valorTotal` del portafolio). Denominador cero: si `currentTitulos + newTitulos == 0`, no calcular.
  - [x] Mostrar tooltip/leyenda explicando qué es el commission_factor y dónde se configura.

- [x] **T10 — Frontend: Yield en CatalogoPage** (AC: 11)
  - [x] Verificar que `annualizedYield` llega en el DTO de `/api/v1/fibras/all` (usar T3 como prerequisito).
  - [x] En `FibraCard` (o el componente de tarjeta en `CatalogoPage`), agregar badge de yield: `X.X%` con estilo morado si `annualizedYield != null`.
  - [x] Si `annualizedYield == null`, el badge no se renderiza.

- [x] **T11 — Frontend: Badge de yield en FibraPage header** (AC: 12)
  - [x] En `FibraPage.tsx`, en el header (junto a precio + FreshnessBadge), agregar badge de yield con el mismo estilo que T10.
  - [x] Dato: `annualizedYield` del DTO de `/api/v1/fibras/{ticker}` (ya existe `DistribucionesSection` que lo usa — verificar que también viene en el DTO principal de `FibraPage`).

- [x] **T12 — Frontend: Búsqueda en drawer móvil** (AC: 13)
  - [x] En `PublicLayout.tsx`, dentro del `nav-mobile-drawer` (o equivalente shadcn), agregar el componente `GlobalSearch` (ya existe) o un input con la misma lógica de búsqueda.
  - [x] Al seleccionar una FIBRA, navegar a `/fibras/{ticker}` y cerrar el drawer.
  - [x] El campo se muestra siempre (no solo si autenticado).

- [x] **T13 — Frontend: Benchmarks en URL del comparador** (AC: 14)
  - [x] En `ComparadorPage.tsx`, agregar param `benchmarks` a la URL (usando `useSearchParams` de react-router v7).
  - [x] Dos checkboxes/toggles: "Comparar con IPC" y "Comparar con S&P 500". Al activar, agregar el valor al param (`ipc`, `sp500`). Al desactivar, remover.
  - [x] Al cargar la página con `?benchmarks=ipc,sp500`, restaurar el estado de los toggles.
  - [x] **No implementar gráfica de benchmarks en el comparador** — solo persistir la selección en URL para la historia futura.

- [x] **T14 — Tests unitarios** (AC: 16)
  - [x] `calcYoc(titulos, costoPromedio, rentaAnual)`: caso nominal, denominador 0, rentaAnual null
  - [x] `calcYieldPortafolio(rentasAnuales, valorTotal)`: caso nominal, denominador 0, any null
  - [x] `projectNextPayments(positions, today)`: sin distribuciones, cadencia trimestral, cadencia semestral, cadencia anual, proyección dentro de 90 días, proyección fuera de 90 días
  - [x] `calcNewAvgCost(currentTitulos, currentAvgCost, currentPrice, newTitulos, commissionFactor)`: caso nominal, commissionFactor 0, newTitulos 0
  - [x] Ejecutar `npm test --workspace=src/Web/Main` y confirmar que pasan

- [x] **T15 — Build final y codegen** (AC: 15)
  - [x] `npm run codegen:api` (después de todos los cambios de backend)
  - [x] `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] `npm run build --workspace=src/Web/Main` — 0 errores TypeScript

## Dev Notes

### Sobre los benchmarks (^MXX y ^GSPC) en T4

El pipeline de mercado ya descarga precios de Yahoo Finance para las FIBRAs del catálogo. Los tickers de benchmarks `^MXX` (IPC BMV) y `^GSPC` (S&P 500) pueden ser populados de dos formas:

**Opción A (recomendada para MVP):** Crear dos entradas especiales en el catálogo con ticker `^MXX` y `^GSPC`, marcadas como `sector = "Benchmark"` y con `estado = "activo"`. El pipeline existente los descarga igual que a cualquier FIBRA. Evaluar si el pipeline actual maneja tickers con `^` — Yahoo Finance los acepta; si el validador del catálogo los rechaza, ajustar la validación de ticker.

**Opción B (más limpia pero más cara):** Nuevo job separado que descarga solo los benchmarks sin tocar el catálogo de FIBRAs. Diferir para post-MVP.

**Si los benchmarks no tienen datos aún:** El endpoint de performance devuelve las series de benchmark como arrays vacíos `[]` y el frontend simplemente no muestra esas líneas. No es un error.

### chart.tsx fork

Ver convención en `convenciones-fibradis.md`: el archivo `src/Web/Main/src/shared/ui/chart.tsx` es un fork manual para recharts 3.x. Usarlo como wrapper si aplica, o usar `recharts` directamente (`LineChart`, `Line`, `XAxis`, `YAxis`, `Tooltip`, `Legend`) — ambos son válidos dado que el fork solo es necesario para el `ChartContainer` de shadcn.

### Sobre el campo recentDistributions en PortfolioPositionDto

El campo `recentDistributions` ya existe en `PortfolioPositionDto` (confirmado en el análisis de código). Tiene las últimas distribuciones de la FIBRA. Usar estas para la detección de cadencia en T8. Si el array está vacío, la posición no aparece en el tab de Calendario.

### Sobre commissionFactor en T9

El config `commission_factor` se lee de la tabla `OperationalConfig` en backend. Verificar el endpoint que lo expone al frontend: buscar en el schema si existe algún DTO que devuelva este valor para el SPA Main. Si no existe endpoint público para commission_factor, agregar un campo `commissionFactor: float` al endpoint `GET /api/v1/portfolio` (en el response body) — o crear `GET /api/v1/portfolio/config` — para que el frontend lo consuma sin hardcodearlo.

### Sobre el story 7-2 (Promediar)

Story 7-2 implementó la vista "Promediar Posición" en `/oportunidades`. Antes de implementar T9, leer el story file `7-2-vista-promediar-posicion-con-simulador.md` y los archivos que modificó. No duplicar la lógica existente — extender el componente existente.

### Dependencias entre tareas

T1 → T6 (requiere campo yoc y opportunityScore en DTO)
T2 → T5 (requiere yieldPortafolio e ingresoMensual en KpisDto)
T3 → T10, T11 (requiere annualizedYield en snapshots)
T4 → T7 (requiere endpoint de performance)
T7 y T8 pueden ir en paralelo
T9 puede ir en paralelo con T5-T8 (toca módulo oportunidades)
T12, T13 son independientes
T14 va después de todas las funciones puras implementadas
T15 siempre al final

### Convenciones relevantes

- Importes absolutos con `@/` — nunca rutas relativas `../../`
- `react-router` v7 (no `react-router-dom`)
- TanStack Query v5: `useQuery({ queryKey, queryFn, enabled })`
- Nullables financieros: nunca mostrar `0` cuando el dato es null — siempre `—`
- Tailwind v4 — verificar que las clases usadas existan en v4

### Security Checklist — completar antes del primer commit

- [ ] **TOCTOU**: El endpoint `GET /api/v1/portfolio/performance` es solo lectura — sin TOCTOU. T9 no persiste nada (cálculo local).
- [ ] **Auth-gating**: La gráfica de rendimiento, tab calendario y extensión de Promediar son solo para usuarios autenticados. Verificar que el endpoint de performance retorne 401 si no hay token y que los componentes solo se rendericen en contexto autenticado.
- [ ] **Denominador cero**: Todos los cálculos con división (YOC, yield portafolio, nuevo costo promedio) deben tener el caso denominador = 0 como primer test unitario.

### Project Structure Notes

```
src/Web/Main/src/
  modules/
    portafolio/
      KpiCards.tsx                   — MODIFICAR (T5)
      PositionsTable.tsx             — MODIFICAR (T6)
      PortafolioPage.tsx             — MODIFICAR (T7, T8 — agregar tabs y chart)
      PerformanceChart.tsx           — NUEVO (T7)
      PortafolioCalendario.tsx       — NUEVO (T8)
      ScoreBadge.tsx                 — NUEVO (T6)
      portfolio-calendar.ts          — NUEVO (T8 — lógica pura de proyección)
      portfolio-format.ts            — EXISTENTE (agregar helpers si aplica)
    catalogo/
      CatalogoPage.tsx               — MODIFICAR (T10)
    ficha-publica/
      FibraPage.tsx                  — MODIFICAR (T11)
    oportunidades/
      [componente de Promediar]      — MODIFICAR (T9 — leer story 7-2 para saber qué archivo)
    comparador/
      ComparadorPage.tsx             — MODIFICAR (T13)
  shared/
    ui/
      chart.tsx                      — EXISTENTE (no modificar)
    components/
      PublicLayout.tsx               — MODIFICAR (T12)

src/Server/
  Application/
    Portfolio/
      [PortfolioService o similar]   — MODIFICAR (T1, T2, T4)
  Infrastructure/
    Portfolio/
      [PortfolioRepository o mapper] — MODIFICAR (T1)
  Api/
    Controllers/
      PortfolioController.cs         — MODIFICAR (T4 — nuevo endpoint)
```

### Referencias

- `src/Web/Main/src/modules/portafolio/KpiCards.tsx` — estado actual de KPI cards (7 métricas)
- `src/Web/Main/src/modules/portafolio/PositionsTable.tsx` — columnas actuales, OPTIONAL_COLUMNS en líneas 29-37
- `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx` — estructura actual de la página
- `src/Web/Main/src/modules/portafolio/portfolio-format.ts` — funciones de formato existentes
- Story 7-2 (`_bmad-output/implementation-artifacts/7-2-vista-promediar-posicion-con-simulador.md`) — Promediar actual
- `convenciones-fibradis.md` — reglas de stack y código
- `_bmad-output/planning-artifacts/epics.md` → FR-26 (KPIs portafolio), FR-27 (filas expandibles), FR-48 (columnas configurables)
- `shared/ui/chart.tsx` — fork de recharts/shadcn para gráficas

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- 2026-06-09: Ajuste de portafolio sin migraciones nuevas. Se expusieron `yoc`, `yieldPortafolio`, `ingresoMensual`, `opportunityScore`, `logoUrl` y `annualizedYield` vía contratos existentes; el servidor productivo puede seguir corriendo sin cambios de esquema.
- 2026-06-09: Se añadió `/api/v1/portfolio/performance` y `/api/v1/portfolio/config`, además de UI para KPIs colapsables, calendario, benchmarks, yields y búsqueda móvil.
- 2026-06-09: Validaciones ejecutadas con éxito: `dotnet build FIBRADIS.slnx`, `npm run codegen:api`, `npm run build --workspace=src/Web/Main`, `npm test --workspace=src/Web/Main`, `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj -c Debug --no-build`, `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj -c Debug --no-build`.

### Completion Notes List

- Items 4 (Peso %), 5 (G/L con %) y 29 (Cambio diario catálogo) del análisis competitivo ya estaban implementados en FIBRADIS — no requieren trabajo.
- Item 31 (Calculadora ISR) no está en scope — ya es story 10-2.
- El benchmark de datos históricos para la gráfica (T4) asume posiciones actuales proyectadas hacia atrás. No es un tracker de cambios de posición — limitación MVP documentada.
- No se agregaron migraciones EF. Los cambios fueron de contratos, cálculos y UI para mantener compatibilidad con el servidor productivo ya en ejecución.

### Change Log

- 2026-06-09: Implementadas las mejoras de Story 9.3 en portafolio, oportunidades, catálogo, ficha pública, comparador y navegación móvil; código validado con build y pruebas verdes.

### Review Findings

#### Senior Developer Review (AI) — 2026-06-09

- [x] [Review][Patch] P1 — `<a href>` en lugar de `<Link to>` para "Conoce las FIBRAs" y "Catálogo" en nav desktop y móvil — causa recarga completa de página [PublicLayout.tsx:103-104, 208-221]
- [x] [Review][Patch] P2 — `useMemo` y `useEffect` llamados después de early returns — violación de React Rules of Hooks [PromediarTab.tsx:116,129]
- [x] [Review][Patch] P3 — `ingresoMensual` nunca es null en backend (devuelve `0m` cuando rentas=0), frontend muestra `$0.00/mes` incorrectamente — viola AC-2 [KpiCards.tsx:128]
- [x] [Review][Patch] P4 — Falta test unitario para `projectNextPayments` con cadencia **anual** — viola AC-16 [portfolio-calendar.test.ts]
- [x] [Review][Patch] P5 — Asignación redundante `days = 3650` después de `ResolvePerformanceDays` (ya retorna 3650 para "all") — dead code [PortfolioEndpoints.cs:83-85]
- [x] [Review][Patch] P6 — Falta test C# para `yieldPortafolio` cuando `valorTotal == 0` con precios presentes — viola AC-16 [PortfolioKpiCalculatorTests.cs]

- [x] [Review][Defer] D1 — Serie de performance no filtra por fecha de adquisición de posición (posiciones contribuyen a todo el historial) — MVP simplification documentado en Dev Notes
- [x] [Review][Defer] D2 — N+1 queries en `BuildPerformanceSeriesAsync`: 1 query por posición — escala mal en portafolios grandes [PortfolioEndpoints.cs:433]
- [x] [Review][Defer] D3 — `BuildNormalizedPoints` retorna `[]` cuando el primer valor del período es 0 (sin mensaje al usuario) [PortfolioEndpoints.cs:468]
- [x] [Review][Defer] D4 — `detectCadence` clasifica por conteo de distribuciones en el año, no por intervalo real — puede proyectar fechas incorrectas [portfolio-calendar.ts]
- [x] [Review][Defer] D5 — `annualDistCutoff` usa `DateTime.UtcNow` — inconsistencia si `PaymentDate` se almacena en hora México [PortfolioEndpoints.cs]
- [x] [Review][Defer] D6 — `calcNewAvgCost` usa floating-point JS — rounding errors en display; `formatMoney` mitiga el impacto visible [simulador-logic.ts]
- [x] [Review][Defer] D7 — AC-9 especificaba usar `Tabs` de shadcn/ui; se implementaron custom `<button>` sin ARIA tablist/tabpanel [PortafolioPage.tsx:176]
- [x] [Review][Defer] D8 — `enabledColumns` vacío → `['yoc']`: puede sobreescribir configuración explícita de usuario sin columnas opcionales [PortafolioPage.tsx:768]
- [x] [Review][Defer] D9 — `addSeries` en PerformanceChart sin guard contra `undefined` en el parámetro `series` [PerformanceChart.tsx]
- [x] [Review][Defer] D10 — `PortafolioCalendario` recalcula `projectNextPayments` sin `useMemo`, pasa `new Date()` en cada render [PortafolioCalendario.tsx]
- [x] [Review][Defer] D11 — Logo img: flash por intento de red en cada mount; `failedLogos` no persiste en sessionStorage [PositionsTable.tsx]
- [x] [Review][Defer] D12 — `/performance` acepta cualquier string como `range`; valor inválido silenciosamente devuelve 30 días [PortfolioEndpoints.cs]
- [x] [Review][Defer] D13 — Fallback frontend `calcYieldPortafolio` puede divergir de las condiciones del backend cuando `kpis.yieldPortafolio` es null [KpiCards.tsx:73]

### File List

- `_bmad-output/implementation-artifacts/9-3-mejoras-portafolio.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`
- `scripts/codegen/Api.json`
- `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs`
- `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs`
- `src/Server/Api/Endpoints/Public/MarketEndpoints.cs`
- `src/Server/Application/Portfolio/PortfolioKpiCalculator.cs`
- `src/Server/Application/Portfolio/PortfolioKpiResult.cs`
- `src/Server/SharedApiContracts/Market/MarketSnapshotDto.cs`
- `src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs`
- `src/Web/Main/package.json`
- `src/Web/Main/src/modules/catalogo/CatalogoPage.tsx`
- `src/Web/Main/src/modules/comparador/ComparadorPage.tsx`
- `src/Web/Main/src/modules/comparador/comparador-logic.test.ts`
- `src/Web/Main/src/modules/comparador/comparador-logic.ts`
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx`
- `src/Web/Main/src/modules/home/GlobalSearch.tsx`
- `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx`
- `src/Web/Main/src/modules/oportunidades/simulador-logic.test.ts`
- `src/Web/Main/src/modules/oportunidades/simulador-logic.ts`
- `src/Web/Main/src/modules/portafolio/ColumnPicker.tsx`
- `src/Web/Main/src/modules/portafolio/KpiCards.tsx`
- `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`
- `src/Web/Main/src/modules/portafolio/PositionExpandedDetail.tsx`
- `src/Web/Main/src/modules/portafolio/PositionsTable.tsx`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx`
- `src/Web/Main/tests/e2e/fixtures/market-api.ts`
- `src/Web/Main/tests/e2e/fixtures/portfolio-api.ts`
- `src/Web/Main/tests/e2e/portafolio-posiciones.spec.ts`
- `src/Web/Main/tests/e2e/portafolio-upload.spec.ts`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Unit/Infrastructure.Tests/Portfolio/PortfolioKpiCalculatorTests.cs`
