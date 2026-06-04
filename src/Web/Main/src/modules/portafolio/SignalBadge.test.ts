import assert from 'node:assert/strict'
import test from 'node:test'
import { calcSignal } from './signal-badge.ts'

test('calcSignal returns green when price trades more than 10% below NAV', () => {
  const result = calcSignal(120, 100)

  assert.equal(result.status, 'verde')
  assert.equal(result.tooltip, 'Cotiza con descuento de 16.7% respecto al NAV')
})

test('calcSignal returns yellow when price is within +/-10% of NAV', () => {
  const result = calcSignal(100, 105)

  assert.equal(result.status, 'amarillo')
  assert.equal(result.tooltip, 'Cotiza dentro de ±10% del NAV (+5.0%)')
})

test('calcSignal returns red when price trades more than 10% above NAV', () => {
  const result = calcSignal(100, 115)

  assert.equal(result.status, 'rojo')
  assert.equal(result.tooltip, 'Cotiza con prima de 15.0% respecto al NAV')
})

test('calcSignal returns gray when NAV is missing or invalid', () => {
  assert.deepEqual(calcSignal(null, 100), {
    status: 'gris',
    tooltip: 'Sin datos de NAV',
  })
  assert.deepEqual(calcSignal(0, 100), {
    status: 'gris',
    tooltip: 'Sin datos de NAV',
  })
  assert.deepEqual(calcSignal(100, null), {
    status: 'gris',
    tooltip: 'Sin datos de NAV',
  })
})
