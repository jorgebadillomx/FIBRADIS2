import assert from 'node:assert/strict'
import test from 'node:test'
import { calcYield } from './yieldCalculator.ts'

test('calcYield — price=0 retorna null', () => {
  assert.equal(calcYield(0.62, 0), null)
})

test('calcYield — distribución=0 retorna null', () => {
  assert.equal(calcYield(0, 100), null)
})

test('calcYield — calcula yield anualizado en porcentaje', () => {
  const result = calcYield(0.62, 100)

  assert.ok(result != null)
  assert.ok(Math.abs(result - 2.48) < 0.0001)
})
