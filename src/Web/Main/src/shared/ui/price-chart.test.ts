import test from 'node:test'
import assert from 'node:assert/strict'
import { buildPriceChartPoints, summarizePriceChart } from './price-chart.utils.ts'

test('buildPriceChartPoints formatea fechas y normaliza valores nulos', () => {
  const points = buildPriceChartPoints([
    { date: '2026-05-01', open: '24.10', close: '24.55' },
    { date: '2026-05-02', close: null },
  ])

  assert.equal(points[0]?.open, 24.10)
  assert.equal(points[0]?.close, 24.55)
  assert.equal(points[0]?.date, '2026-05-01')
  assert.match(points[0]?.shortLabel ?? '', /01/)
  assert.match(points[0]?.fullLabel ?? '', /2026/)
  assert.equal(points[1]?.close, null)
  assert.equal(points[1]?.open, null)
})

test('buildPriceChartPoints asigna open null cuando no se provee', () => {
  const points = buildPriceChartPoints([
    { date: '2026-05-01', close: '24.55' },
  ])
  assert.equal(points[0]?.open, null)
})

test('summarizePriceChart calcula rango y variación', () => {
  const summary = summarizePriceChart(
    buildPriceChartPoints([
      { date: '2026-05-01', close: 20 },
      { date: '2026-05-02', close: 22 },
      { date: '2026-05-03', close: 21 },
    ]),
  )

  assert.equal(summary.first, 20)
  assert.equal(summary.last, 21)
  assert.equal(summary.min, 20)
  assert.equal(summary.max, 22)
  assert.equal(summary.change, 1)
  assert.equal(summary.changePct, 5)
  assert.equal(summary.visibleDot, true)
})

test('summarizePriceChart desactiva dots cuando la serie es larga', () => {
  const summary = summarizePriceChart(
    buildPriceChartPoints(
      Array.from({ length: 60 }, (_, index) => ({
        date: `2026-03-${String((index % 28) + 1).padStart(2, '0')}`,
        close: 20 + index * 0.1,
      })),
    ),
  )

  assert.equal(summary.visibleDot, false)
})

