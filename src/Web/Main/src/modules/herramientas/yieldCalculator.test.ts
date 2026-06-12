import assert from 'node:assert/strict'
import test from 'node:test'
import { calcYield } from './yieldCalculator.ts'

test('calcYield — price=0 retorna null', () => {
  assert.equal(calcYield(1.184, 0), null)
})

test('calcYield — distribución=0 retorna null', () => {
  assert.equal(calcYield(0, 14.30), null)
})

test('calcYield — calcula yield TTM en porcentaje', () => {
  // 1.184 TTM / 14.30 = 8.2797...%
  const result = calcYield(1.184, 14.30)

  assert.ok(result != null)
  assert.ok(Math.abs(result - 8.2797) < 0.001)
})
