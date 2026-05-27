import test from 'node:test'
import assert from 'node:assert/strict'
import { getDistributionPeriodLabel, inferDistributionCadence } from './distribuciones.ts'

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

test('getDistributionPeriodLabel genera etiquetas trimestrales, mensuales y anuales', () => {
  assert.equal(getDistributionPeriodLabel('2025-03-15', 'quarterly'), 'Q1 2025')
  assert.equal(getDistributionPeriodLabel('2025-10-31', 'monthly'), 'oct 2025')
  assert.equal(getDistributionPeriodLabel('2025-07-01', 'semiannual'), 'S2 2025')
  assert.equal(getDistributionPeriodLabel('2025-12-31', 'annual'), '2025')
})

