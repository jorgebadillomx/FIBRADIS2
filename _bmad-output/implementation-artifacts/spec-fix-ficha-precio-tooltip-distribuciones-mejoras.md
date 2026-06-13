---
title: 'Fix ficha: tooltip open/close + distribuciones agrupadas con diferencia'
type: 'bugfix'
created: '2026-06-12'
status: 'done'
baseline_commit: 'd90c0be25fcb04c0a9dfdc4dd2c92fd7b2f1c5b9'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Dos bugs visuales en la ficha pública de la FIBRA: (1) el tooltip del gráfico de precios muestra "Cierre/Cierre" duplicado porque `Area` y `Line` comparten `dataKey="close"` y el API no expone `open`, cuando debería mostrar "Apertura" y "Cierre"; (2) la tabla de distribuciones muestra filas planas sin agrupar—pagos del mismo trimestre aparecen separados, el label del periodo tiene un offset de +1 trimestre respecto al periodo real, y no hay columna de comparación con el periodo anterior.

**Approach:** Añadir `Open` a `DailyPricePointDto` (campo ya existe en `DailySnapshot.Open`), regenerar el API client, y usar un tooltip custom en el chart. Para distribuciones: corregir el label del periodo (−1 periodo), agregar función de agrupación por periodo, filas expandibles al hacer click, y columna "Diferencia" vs periodo anterior en verde/rojo.

## Boundaries & Constraints

**Always:**
- El shift de periodo aplica a cadencia quarterly (−1 trimestre), monthly (−1 mes) y semiannual (−1 semestre). Annual no cambia.
- Q1 del año N → shift → Q4 del año N−1 (cambio de año).
- Diferencia = monto absoluto (no porcentaje).
- El grupo más antiguo muestra "—" en Diferencia (sin periodo previo).
- Collapse/expand es estado local (no persistido).
- La clave de agrupación es el string exacto que devuelve `getDistributionPeriodLabel` tras la corrección.

**Ask First:**
- Si `open` es `null` para puntos históricos (pre-migración), la fila "Apertura" se omite del tooltip — confirmar antes de implementar si debe mostrarse "—" en su lugar.

**Never:**
- No cambiar el gráfico a candlestick/OHLC.
- No exponer `high`/`low` en el tooltip ni en el API.
- No paginar ni ordenar la tabla de distribuciones.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Pago en abril (Q2) | date=2026-04-15, cadence=quarterly | Label = "Q1 2026" |
| Pago en enero (Q1) | date=2026-01-15, cadence=quarterly | Label = "Q4 2025" |
| Dos pagos mismo periodo | Q1 2026 × 2, montos 0.18 + 0.20 | Fila agrupada: $0.3800, expandible |
| Periodo más antiguo | sin periodo previo | Diferencia = "—" |
| Open = null | open=null, close=24.55 | Tooltip muestra solo "Cierre: $24.55" |
| Open disponible | open=24.10, close=24.55 | Tooltip: "Apertura: $24.10 / Cierre: $24.55" |

</frozen-after-approval>

## Code Map

- `src/Server/SharedApiContracts/Market/FibraHistoryDto.cs` — record `DailyPricePointDto`; agregar `Open`
- `src/Server/Api/Endpoints/Public/MarketEndpoints.cs` — mapping `s.Open` al crear el DTO
- `src/Web/SharedApiClient/schema.d.ts` — regenerado por codegen; no editar a mano
- `src/Web/Main/src/shared/ui/price-chart.utils.ts` — tipos `PriceChartInputPoint` / `PriceChartPoint` y `buildPriceChartPoints`
- `src/Web/Main/src/shared/ui/price-chart.tsx` — chart + tooltip
- `src/Web/Main/src/shared/ui/price-chart.test.ts` — unit tests de utils
- `src/Web/Main/src/modules/ficha-publica/sections/distribuciones.ts` — lógica de label, agrupación y diferencia
- `src/Web/Main/src/modules/ficha-publica/sections/distribuciones.test.ts` — tests unitarios
- `src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx` — UI de la tabla

## Tasks & Acceptance

**Execution:**
- [x] `src/Server/SharedApiContracts/Market/FibraHistoryDto.cs` — añadir `decimal? Open` como primer parámetro posicional del record `DailyPricePointDto`
- [x] `src/Server/Api/Endpoints/Public/MarketEndpoints.cs` — actualizar `new DailyPricePointDto(s.Date.ToString("yyyy-MM-dd"), s.Close)` → `new DailyPricePointDto(s.Date.ToString("yyyy-MM-dd"), s.Open, s.Close)`
- [x] Ejecutar `npm run codegen:api` — regenerar `src/Web/SharedApiClient/schema.d.ts` con el nuevo campo `open`
- [x] `price-chart.utils.ts` — añadir `open?: number | string | null` a `PriceChartInputPoint` y `open: number | null` a `PriceChartPoint`; mapear `toNum(entry.open) ?? null` en `buildPriceChartPoints`
- [x] `price-chart.tsx` — reemplazar `ChartTooltipContent` con tooltip custom inline que lee `open` y `close` de `payload[0]?.payload as PriceChartPoint`; omitir fila Apertura si `open == null`
- [x] `price-chart.test.ts` — actualizar fixtures para pasar `open` en inputs; añadir caso `open` presente/ausente
- [x] `distribuciones.ts` — corregir `getDistributionPeriodLabel` aplicando shift −1 periodo por cadencia; exportar `groupDistributionsByPeriod(dists, cadence): PeriodGroup[]` donde `PeriodGroup = { label, total, items: DistributionPoint[] }`; exportar `calcPeriodDiff(groups): (number | null)[]` con diferencia `total[i] − total[i+1]` (null para el último)
- [x] `distribuciones.test.ts` — actualizar tests existentes para nuevos labels; añadir tests de agrupación (periodo repetido suma montos) y diferencia (positiva/negativa/null en último)
- [x] `DistribucionesSection.tsx` — usar `groupDistributionsByPeriod` + `calcPeriodDiff`; tabla con columnas: Periodo | Monto por CBFI | Diferencia; fila de grupo con `▶/▼` icono; al expandir mostrar sub-filas: Fecha de pago | Monto por CBFI; Diferencia en `text-positive` (verde) si >0, `text-negative` (rojo) si <0, muted si null

**Acceptance Criteria:**
- Given fecha de pago en enero/Q1, when rendered, then el label muestra Q4 del año anterior
- Given fecha de pago en abril/Q2, when rendered, then el label muestra Q1 del mismo año
- Given dos distribuciones en el mismo periodo, when rendered, then aparece una fila agrupada con el monto sumado
- Given una fila agrupada, when el usuario hace click, then se expanden las filas de detalle con fechas individuales
- Given chart data con `open` disponible, when hover sobre un punto, then el tooltip muestra "Apertura: $XX.XX" y "Cierre: $XX.XX"
- Given chart data con `open = null`, when hover, then el tooltip muestra solo "Cierre: $XX.XX" sin fila Apertura
- Given el periodo más reciente vs el anterior, when rendered, then Diferencia positiva aparece en verde y negativa en rojo
- Given el grupo más antiguo, when rendered, then Diferencia = "—"

## Design Notes

El tooltip custom debe acceder a los datos crudos del punto vía `payload[0]?.payload` (tipado como `PriceChartPoint`) en lugar de depender del render automático de `ChartTooltipContent`, ya que hay dos series (`Area` + `Line`) con el mismo `dataKey="close"` y eso produce doble entrada en el payload.

Para el shift de periodo en quarterly: `paymentQ = Math.floor(month / 3) + 1`; `periodQ = paymentQ === 1 ? 4 : paymentQ - 1`; `periodYear = paymentQ === 1 ? year - 1 : year`.

## Suggested Review Order

**Contrato API (backend → schema → frontend)**

- `DailyPricePointDto` gana `Open`; orden posicional del record C# es el contrato de serialización
  [`FibraHistoryDto.cs:10`](../../src/Server/SharedApiContracts/Market/FibraHistoryDto.cs#L10)

- Único caller actualizado; mapea `s.Open` antes de `s.Close`
  [`MarketEndpoints.cs:94`](../../src/Server/Api/Endpoints/Public/MarketEndpoints.cs#L94)

- Schema generado; confirmar que `open: null | number | string` aparece correctamente
  [`schema.d.ts:5770`](../../src/Web/SharedApiClient/schema.d.ts#L5770)

**Tooltip del gráfico**

- `PriceTooltip` lee directamente de `payload[0].payload` para evitar doble-render de dos series con el mismo `dataKey`
  [`price-chart.tsx:28`](../../src/Web/Main/src/shared/ui/price-chart.tsx#L28)

- Tipos `PriceChartInputPoint` y `PriceChartPoint` agregan `open` opcional/nullable
  [`price-chart.utils.ts:3`](../../src/Web/Main/src/shared/ui/price-chart.utils.ts#L3)

**Lógica de periodo y agrupación de distribuciones**

- Shift −1 por cadencia: quarterly (paymentQ→periodQ), monthly (month-1), semiannual (S→S-1)
  [`distribuciones.ts:65`](../../src/Web/Main/src/modules/ficha-publica/sections/distribuciones.ts#L65)

- `groupDistributionsByPeriod`: Map preserva orden; `toNum` guard contra NaN
  [`distribuciones.ts:96`](../../src/Web/Main/src/modules/ficha-publica/sections/distribuciones.ts#L96)

- `calcPeriodDiff`: `diff[i] = groups[i].total − groups[i+1].total`; null en el grupo más antiguo
  [`distribuciones.ts:119`](../../src/Web/Main/src/modules/ficha-publica/sections/distribuciones.ts#L119)

**UI de la tabla agrupada**

- `GroupRow`: aria-expanded en tr, chevron toggle, sub-filas de detalle condicionales
  [`DistribucionesSection.tsx:30`](../../src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx#L30)

- `DiffCell`: text-positive/negative según signo; Math.abs para el display
  [`DistribucionesSection.tsx:14`](../../src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx#L14)

**Tests**

- Labels post-shift: Q1→Q4 prev year, Q2→Q1, monthly y semiannual
  [`distribuciones.test.ts:28`](../../src/Web/Main/src/modules/ficha-publica/sections/distribuciones.test.ts#L28)

- Agrupación + diff: sum, positivo, negativo, grupo único
  [`distribuciones.test.ts:73`](../../src/Web/Main/src/modules/ficha-publica/sections/distribuciones.test.ts#L73)

- Chart utils: open presente y ausente
  [`price-chart.test.ts:5`](../../src/Web/Main/src/shared/ui/price-chart.test.ts#L5)

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` — expected: Build succeeded, 0 errors
- `npm run codegen:api` — expected: exits 0, `src/Web/SharedApiClient/schema.d.ts` contiene `open` en `DailyPricePointDto`
- `node --test src/Web/Main/src/shared/ui/price-chart.test.ts` — expected: all tests pass
- `node --test src/Web/Main/src/modules/ficha-publica/sections/distribuciones.test.ts` — expected: all tests pass
