import assert from 'node:assert/strict'
import test from 'node:test'
import {
  calcNewAvgCost,
  calcNuevoAvg,
  calcNuevaPlusvaliaPct,
  calcNuevoValor,
} from './simulador-logic.ts'

test('calcNuevoAvg — happy path: 1000×$110 + 500×$100 → $106.67', () => {
  const result = calcNuevoAvg(1000, 110, 500, 100)
  assert.ok(Math.abs(result - 106.6667) < 0.001)
})

test('calcNuevoAvg — mismo precio no cambia el promedio', () => {
  const result = calcNuevoAvg(1000, 100, 500, 100)
  assert.equal(result, 100)
})

test('calcNuevaPlusvaliaPct — compra bajo avg mejora plusvalía', () => {
  // avg original $110, precio actual $100 → plusvalía negativa -9.09%
  // comprar a $100: nuevo avg $106.67 → plusvalía mejora a aprox -6.25%
  const nuevoAvg = calcNuevoAvg(1000, 110, 500, 100)
  const pct = calcNuevaPlusvaliaPct(nuevoAvg, 100)
  // Debe ser más cercano a 0 que -9.09
  assert.ok(pct > -9.09)
  assert.ok(Math.abs(pct - (-6.25)) < 0.01)
})

test('calcNuevaPlusvaliaPct — compra sobre avg empeora plusvalía', () => {
  // avg original $100, precio actual $110 → plusvalía positiva +10%
  // comprar a $110: nuevo avg sube → plusvalía no mejora
  const nuevoAvg = calcNuevoAvg(1000, 100, 500, 110)
  const pct = calcNuevaPlusvaliaPct(nuevoAvg, 110)
  // Plusvalía sigue siendo positiva pero menor
  assert.ok(pct > 0)
  const originalPct = ((110 - 100) / 100) * 100
  assert.ok(pct < originalPct)
})

test('calcNuevoValor — cálculo correcto', () => {
  const result = calcNuevoValor(1000, 500, 100)
  assert.equal(result, 150000)
})

test('calcNuevoAvg — denominador cero retorna 0 sin lanzar excepción', () => {
  const result = calcNuevoAvg(0, 110, 0, 100)
  assert.equal(result, 0)
})

test('calcNuevaPlusvaliaPct — nuevoAvg cero retorna 0 sin lanzar excepción', () => {
  const result = calcNuevaPlusvaliaPct(0, 100)
  assert.equal(result, 0)
})

test('calcNewAvgCost — aplica comisión sobre las nuevas adquisiciones', () => {
  const result = calcNewAvgCost(1000, 110, 100, 500, 0.01)
  assert.ok(Math.abs(result - 107) < 0.0001)
})

test('calcNewAvgCost — comisión cero coincide con el promedio ponderado normal', () => {
  const result = calcNewAvgCost(1000, 110, 100, 500, 0)
  assert.ok(Math.abs(result - 106.6666666667) < 0.0001)
})

test('calcNewAvgCost — títulos nuevos cero retorna el promedio actual', () => {
  const result = calcNewAvgCost(1000, 110, 100, 0, 0.01)
  assert.equal(result, 110)
})
