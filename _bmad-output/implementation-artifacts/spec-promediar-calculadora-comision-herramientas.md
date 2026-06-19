---
title: 'Calculadora con comisión en Promediar + mover ¿Qué pasaría si? a Herramientas + mejoras layout'
type: 'feature'
created: '2026-06-19'
status: 'done'
baseline_commit: '222338f832a798d19668a5f75ddf7b89ea653782'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La sección "¿Qué pasaría si?" vive únicamente en Promediar Posición; la calculadora en esa misma vista no contempla comisión/IVA; el grid de Herramientas da espacio insuficiente a "FIBRAs vs CETES"; la restricción de fecha en "Retorno total" llega hasta ayer, cuando el mínimo útil debería ser 1 año atrás.

**Approach:** Extraer "¿Qué pasaría si?" como componente auto-contenido y colocarlo en Herramientas (sobre "Referencia de datos"); reemplazarlo en PromediarTab por una nueva calculadora tipo CalculadoraPage pero con CBFI/Sobra que incorporan comisión+IVA, filtrada a FIBRAs activas en últimos 4 trimestres, pre-llenada a $1 000 y ordenada por Renta Bruta Anual desc; ajustar proporciones del grid y la fecha máxima de Retorno total; revisar FAQs SEO de las páginas afectadas.

## Boundaries & Constraints

**Always:**
- Comisión: `commissionFactor` de `/api/v1/portfolio/config`; IVA: `ivaFactor` de `/api/v1/config/fiscal-rates`. Ambos ya se obtienen en PromediarTab; el nuevo componente los recibe como props.
- Fórmula con comisión: `effectivePrice = precio × (1 + commissionFactor × (1 + ivaFactor))`, `cbfis = floor(monto / effectivePrice)`, `sobra = monto − cbfis × effectivePrice`.
- Filtro "activa en últimos 4 trimestres": `ultimoPeriodo` de `/api/v1/calculadora` debe caer en los 4 trimestres previos a la fecha actual (inclusive el trimestre en curso). FIBRAs sin `ultimoPeriodo` o con período más antiguo quedan excluidas.
- Montos pre-llenados a "1000"; sort inicial `rentaBrutaAnual desc`; sin input de búsqueda en la nueva calculadora de PromediarTab.
- La sección "¿Qué pasaría si?" movida a Herramientas mantiene exactamente la misma lógica y UX; solo cambia la ubicación (encima del `<section>` "Referencia de datos").
- Grid Herramientas: `xl:grid-cols-[minmax(0,2fr)_minmax(0,1fr)]` (FIBRAs vs CETES → 2fr, Ingreso objetivo → 1fr).
- Retorno total: `maxDateStr = hoy − 1 año` (reemplaza el actual `today - 1 day`); `retornoFechaCompra` inicializado con ese mismo valor (en lugar de `''`); `minDateStr` sin cambio (2 años atrás). Resultado: el input arranca pre-llenado y el rango seleccionable queda entre 2 años atrás y 1 año atrás.

**Ask First:**
- Si la API `/api/v1/calculadora` no devuelve `ultimoPeriodo` con formato `Qx-YYYY`, preguntar qué campo usar para el filtro de actividad.

**Never:**
- No modificar la calculadora pública `/calculadora` — sigue sin comisión/IVA.
- No crear endpoints nuevos en el backend.
- No romper el flujo del PromediarTab existente (tabla de posiciones, scores, cálculos de promediar).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| FIBRA sin dist. reciente | `ultimoPeriodo` > 4 trimestres atrás o nulo | No aparece en nueva calculadora de PromediarTab | — |
| Comisión 0% | `commissionFactor = 0` | CBFI y Sobra idénticos a calculadora pública | Correcto por diseño |
| Monto < effectivePrice | `monto < precio × factor` | `cbfis = 0`, `sobra = monto` | — |
| Fecha retorno default | Estado inicial `retornoFechaCompra = maxDateStr` | Input aparece pre-llenado con hoy − 1 año; el `max` impide seleccionar fechas más recientes | — |
| configQuery falla | Sin datos de comisión | Usar `commissionFactor = 0` (fallback existente) | Ya manejado en PromediarTab |

</frozen-after-approval>

## Code Map

- `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` — eliminar bloque JSX "¿Qué pasaría si?" (líneas 455-635); retirar estado `whatIf*` y queries exclusivas de ese bloque; agregar `<PromediarCalculadora>` en su lugar pasando `commissionFactor` e `ivaFactor`
- `src/Web/Main/src/modules/oportunidades/QuePasariaSection.tsx` — **nuevo**: componente auto-contenido que replica el bloque eliminado de PromediarTab; hace sus propias queries (opportunities, portfolio, config, fiscal-rates, indicadores, history)
- `src/Web/Main/src/modules/oportunidades/PromediarCalculadora.tsx` — **nuevo**: tabla con comisión/IVA, usa `fetchCalculadoraFibras`, filtra por últimos 4 trimestres, montos pre-llenados a "1000", sort inicial rentaBrutaAnual desc, badge "Ya contempla comisión e IVA"
- `src/Web/Main/src/modules/calculadora/calculadora-logic.ts` — agregar `calcCbfisConComision` y `calcSobraConComision`; agregar `isRecentQuarter(periodo, n=4): boolean`
- `src/Web/Main/src/modules/herramientas/HerramientasPage.tsx` — (1) insertar `<QuePasariaSection>` antes de la sección "Referencia de datos"; (2) grid `[1.1fr_0.9fr]` → `[2fr_1fr]`; (3) `maxDateStr` = hoy − 1 año; (4) `retornoFechaCompra` inicializado con ese `maxDateStr`
- `src/Server/Application/Seo/FaqSeedFactory.cs` — actualizar FAQs pre-sembradas afectadas por los cambios

## Tasks & Acceptance

**Execution:**
- [x] `src/Web/Main/src/modules/calculadora/calculadora-logic.ts` — agregar `calcCbfisConComision(monto, precio, commissionFactor, ivaFactor)`: `Math.floor(monto / (precio * (1 + commissionFactor * (1 + ivaFactor))))` (devuelve 0 si precio ≤ 0); `calcSobraConComision(monto, cbfis, precio, commissionFactor, ivaFactor)`: `monto - cbfis * precio * (1 + commissionFactor * (1 + ivaFactor))`; `isRecentQuarter(periodo, n=4)`: parsea `Qx-YYYY` y compara contra los N trimestres anteriores al trimestre actual inclusive
- [x] `src/Web/Main/src/modules/oportunidades/PromediarCalculadora.tsx` — crear componente; props: `commissionFactor: number`, `ivaFactor: number`; query `fetchCalculadoraFibras` con queryKey `['calculadora']`; filtrar filas con `isRecentQuarter`; estado `montos` inicializado `{ [ticker]: '1000' }` para todas las filas; sort inicial `{ col: 'rentaBrutaAnual', dir: 'desc' }`; header con eyebrow + badge `"Ya contempla comisión e IVA"` + valores de comisión e IVA mostrados; reutilizar `SortHeader`, `TableSkeleton` del patrón de `CalculadoraPage`; CBFI/Sobra via `calcCbfisConComision`/`calcSobraConComision`
- [x] `src/Web/Main/src/modules/oportunidades/QuePasariaSection.tsx` — crear componente sin props; copiar íntegramente el bloque JSX + estado + derivaciones de PromediarTab relacionados a `whatIf*`; hacer sus propias queries con los mismos queryKeys; reutilizar `RetornoPanel`, `MetricCard`, `calcCostoPurchase`, `calcNewAvgCost`, etc.
- [x] `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` — (a) eliminar estado `whatIfFibraId`, `whatIfTitulos`, `whatIfTargetRenta`; (b) eliminar queries `indicadoresQuery` e `historyQuery` si solo las usaba el bloque eliminado; (c) eliminar todas las derivaciones exclusivas del bloque (`selectedOpportunityRow`, `selectedPosition`, `selectedTicker`, `canSimulateWhatIf`, `newAvgCost`, `deltaVsPricePct`, `newPortfolioPct`, `dividendYieldPct`, `currentRentaAnual`, `rentaAnualEstimada`, `rentaMensualEstimada`, `costoPurchaseWhatIf`, `titulosTotalesParaTarget`, `titulosAdicionalesParaTarget`, `costoInversionParaTarget`, `targetYaCumplido`, `sinDatosRenta`, `hasPanelData`, `*Panel*`); (d) insertar `<PromediarCalculadora commissionFactor={commissionFactor} ivaFactor={ivaFactor} />` donde estaba el bloque eliminado
- [x] `src/Web/Main/src/modules/herramientas/HerramientasPage.tsx` — (a) importar `QuePasariaSection`; (b) insertar `<QuePasariaSection />` justo antes del `<section>` que tiene `eyebrow="Referencia de datos"` (línea ~274); (c) cambiar className del `<div>` de `xl:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]` a `xl:grid-cols-[minmax(0,2fr)_minmax(0,1fr)]`; (d) definir `maxDateStr = new Date(today.getFullYear() - 1, today.getMonth(), today.getDate()).toISOString().slice(0, 10)` (reemplaza `today - 1 day`); (e) cambiar `useState('')` del campo `retornoFechaCompra` a `useState(maxDateStr)` — requiere extraer `maxDateStr` como constante calculada fuera del render o usar lazy initializer
- [x] `src/Server/Application/Seo/FaqSeedFactory.cs` — (a) `CreatePrivateItem("/oportunidades", 4, ...)`: actualizar la descripción de Promediar para indicar que "¿Qué pasaría si?" ahora vive en /herramientas; (b) `CreatePrivateItem("/herramientas", ...)`: revisar/actualizar los 5 ítems — especialmente el #3 (Retorno Total) para reflejar el rango de 1 año y el valor pre-llenado, y agregar un ítem nuevo que explique la nueva calculadora con comisión/IVA en Oportunidades; actualizar `SeedUpdatedAt` a `2026-06-19`

## Spec Change Log

## Design Notes

`PromediarCalculadora` sigue el patrón visual de `CalculadoraPage`: tabla sticky con columna izquierda `$ a calcular`, columnas sortables, `SortHeader`, `TableSkeleton`. No incluye input de búsqueda (la lista es corta, ya filtrada). El badge "Ya contempla comisión e IVA" va en el encabezado de la sección junto a los valores de comisión/IVA (mismo estilo que el badge existente en la sección "¿Qué pasaría si?").

`QuePasariaSection` en Herramientas se envuelve en un `<section className="mt-8 ...">` con las mismas clases de card que en PromediarTab. No requiere `hasSelection`; es visible siempre en Herramientas. El lanzamiento de queries usa React Query cache — si el usuario viene de Oportunidades, los datos ya estarán en caché.

FAQs pre-sembradas en `FaqSeedFactory.cs`: los ítems de `/oportunidades` #4 (Promediar) y `/herramientas` #1–5 requieren actualización. El ítem de Promediar debe mencionar que "¿Qué pasaría si?" migró a Herramientas. El ítem de Retorno Total debe reflejar el selector de fecha con rango 2 años ↔ 1 año y valor pre-llenado. Agregar un ítem nuevo en `/herramientas` o en `/oportunidades` que explique la nueva calculadora con comisión+IVA. La calculadora pública `/calculadora` no cambia; sus FAQs permanecen iguales. Actualizar `SeedUpdatedAt = new(2026, 6, 19, ...)`. Los cambios al seed se aplican mediante el endpoint de seed existente en Ops (`OpsSeoFaqEndpoints.cs`).

## Verification

**Commands:**
- `npm run build --prefix src/Web/Main` — expected: 0 errores TypeScript/build

**Manual checks:**
- /oportunidades → Promediar Posición: ya no muestra "¿Qué pasaría si?"; muestra nueva calculadora con badge de comisión/IVA; montos pre-llenados a 1000; ordenada por Renta Bruta Anual desc al cargar
- /herramientas: sección "¿Qué pasaría si?" visible encima de "Referencia de datos"; funciona igual que antes
- /herramientas: "FIBRAs vs CETES" ocupa visiblemente el doble que "Ingreso objetivo"
- /herramientas → Retorno total: date picker arranca pre-llenado con hoy − 1 año; no permite seleccionar fechas más recientes que ese límite
- Ejecutar seed de FAQs desde Ops y verificar que los ítems de `/oportunidades` y `/herramientas` reflejan los cambios
