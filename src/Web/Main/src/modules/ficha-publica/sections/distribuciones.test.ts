import test from 'node:test'
import assert from 'node:assert/strict'
import {
  getDistributionPeriodLabel,
  inferDistributionCadence,
  groupDistributionsByPeriod,
  calcPeriodDiff,
} from './distribuciones.ts'

test('inferDistributionCadence detecta pagos mensuales ignorando duplicados cercanos', () => {
  const cadence = inferDistributionCadence([
    { date: '2025-03-31', amountPerUnit: 0.15 },
    { date: '2025-03-15', amountPerUnit: 0.05 },
    { date: '2025-02-28', amountPerUnit: 0.14 },
    { date: '2025-01-31', amountPerUnit: 0.13 },
  ])

  assert.equal(cadence, 'monthly')
})

test('inferDistributionCadence detecta pagos trimestrales', () => {
  const cadence = inferDistributionCadence([
    { date: '2025-12-15', amountPerUnit: 0.384 },
    { date: '2025-09-15', amountPerUnit: 0.378 },
    { date: '2025-06-16', amountPerUnit: 0.372 },
  ])

  assert.equal(cadence, 'quarterly')
})

// Labels reflejan periodo cubierto (= mes/trimestre/semestre anterior al pago)
test('getDistributionPeriodLabel trimestrales: pago Q2 → periodo Q1', () => {
  assert.equal(getDistributionPeriodLabel('2025-04-15', 'quarterly'), 'Q1 2025')
})

test('getDistributionPeriodLabel trimestrales: pago Q1 → periodo Q4 año anterior', () => {
  assert.equal(getDistributionPeriodLabel('2025-01-15', 'quarterly'), 'Q4 2024')
})

test('getDistributionPeriodLabel trimestrales: pago Q3 → periodo Q2', () => {
  assert.equal(getDistributionPeriodLabel('2025-07-15', 'quarterly'), 'Q2 2025')
})

test('getDistributionPeriodLabel trimestrales: pago Q4 → periodo Q3', () => {
  assert.equal(getDistributionPeriodLabel('2025-10-15', 'quarterly'), 'Q3 2025')
})

test('getDistributionPeriodLabel mensual: pago febrero → periodo enero', () => {
  assert.equal(getDistributionPeriodLabel('2025-02-28', 'monthly'), 'ene 2025')
})

test('getDistributionPeriodLabel mensual: pago enero → periodo diciembre año anterior', () => {
  assert.equal(getDistributionPeriodLabel('2025-01-31', 'monthly'), 'dic 2024')
})

test('getDistributionPeriodLabel semestral: pago S1 → periodo S2 año anterior', () => {
  assert.equal(getDistributionPeriodLabel('2025-03-15', 'semiannual'), 'S2 2024')
})

test('getDistributionPeriodLabel semestral: pago S2 → periodo S1 mismo año', () => {
  assert.equal(getDistributionPeriodLabel('2025-07-01', 'semiannual'), 'S1 2025')
})

test('getDistributionPeriodLabel anual: sin cambio', () => {
  assert.equal(getDistributionPeriodLabel('2025-12-31', 'annual'), '2025')
})

test('groupDistributionsByPeriod agrupa y suma montos del mismo periodo', () => {
  const groups = groupDistributionsByPeriod([
    { date: '2026-04-15', amountPerUnit: 0.20 },
    { date: '2026-04-30', amountPerUnit: 0.18 },
    { date: '2026-07-15', amountPerUnit: 0.22 },
  ], 'quarterly')

  assert.equal(groups.length, 2)
  assert.equal(groups[0]?.label, 'Q1 2026')
  assert.ok(Math.abs((groups[0]?.total ?? 0) - 0.38) < 1e-9)
  assert.equal(groups[0]?.items.length, 2)
  assert.equal(groups[1]?.label, 'Q2 2026')
  assert.ok(Math.abs((groups[1]?.total ?? 0) - 0.22) < 1e-9)
})

test('groupDistributionsByPeriod un solo pago por periodo no se agrupa', () => {
  const groups = groupDistributionsByPeriod([
    { date: '2026-04-15', amountPerUnit: 0.20 },
  ], 'quarterly')

  assert.equal(groups.length, 1)
  assert.equal(groups[0]?.items.length, 1)
})

test('calcPeriodDiff diferencia positiva cuando periodo actual > anterior', () => {
  const groups = [
    { label: 'Q2 2026', total: 0.38, items: [] },
    { label: 'Q1 2026', total: 0.30, items: [] },
  ]
  const diffs = calcPeriodDiff(groups)
  assert.ok(Math.abs((diffs[0] ?? 0) - 0.08) < 1e-9)
  assert.equal(diffs[1], null)
})

test('calcPeriodDiff diferencia negativa cuando periodo actual < anterior', () => {
  const groups = [
    { label: 'Q2 2026', total: 0.20, items: [] },
    { label: 'Q1 2026', total: 0.38, items: [] },
  ]
  const diffs = calcPeriodDiff(groups)
  assert.ok(Math.abs((diffs[0] ?? 0) - (-0.18)) < 1e-9)
  assert.equal(diffs[1], null)
})

test('calcPeriodDiff grupo único retorna null', () => {
  const groups = [{ label: 'Q1 2026', total: 0.30, items: [] }]
  const diffs = calcPeriodDiff(groups)
  assert.equal(diffs[0], null)
})
