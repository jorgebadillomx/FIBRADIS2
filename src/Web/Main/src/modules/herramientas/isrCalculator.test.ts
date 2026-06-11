import assert from 'node:assert/strict'
import test from 'node:test'
import { calcIsr } from './isrCalculator.ts'

test('calcIsr — con desglose calcula bruto, ISR, reembolso y neto', () => {
  const result = calcIsr(0.62, 500, 0.40)

  assert.equal(result.isEstimate, false)
  assert.equal(result.unitsUsed, 500)
  assert.ok(Math.abs(result.taxableGross - 200) < 0.0001)
  assert.ok(Math.abs(result.isr - 60) < 0.0001)
  assert.ok(Math.abs(result.capitalReturn - 110) < 0.0001)
  assert.ok(Math.abs(result.net - 250) < 0.0001)
})

test('calcIsr — sin desglose usa modo estimado conservador', () => {
  const result = calcIsr(0.62, 500)

  assert.equal(result.isEstimate, true)
  assert.equal(result.unitsUsed, 500)
  assert.ok(Math.abs(result.taxableGross - 310) < 0.0001)
  assert.ok(Math.abs(result.isr - 93) < 0.0001)
  assert.equal(result.capitalReturn, 0)
  assert.ok(Math.abs(result.net - 217) < 0.0001)
})

test('calcIsr — units=0 usa 1 unidad para mostrar valores por unidad', () => {
  const result = calcIsr(0.62, 0)

  assert.equal(result.unitsUsed, 1)
  assert.ok(Math.abs(result.taxableGross - 0.62) < 0.0001)
})

test('calcIsr — taxablePerUnit mayor que distPerUnit no genera capitalReturn negativo', () => {
  const result = calcIsr(0.40, 500, 0.62)

  assert.equal(result.capitalReturn, 0)
  assert.ok(Math.abs(result.taxableGross - 200) < 0.0001)
  assert.ok(Math.abs(result.net - 140) < 0.0001)
})

test('calcIsr — dist negativa se normaliza a cero', () => {
  const result = calcIsr(-1, 500, 0.40)

  assert.equal(result.taxableGross, 0)
  assert.equal(result.capitalReturn, 0)
  assert.equal(result.isr, 0)
  assert.equal(result.net, 0)
})

test('calcIsr — units negativas usan safeUnits=1', () => {
  const result = calcIsr(0.62, -10)

  assert.equal(result.unitsUsed, 1)
  assert.ok(Math.abs(result.taxableGross - 0.62) < 0.0001)
})
