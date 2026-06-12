import assert from 'node:assert/strict'
import test from 'node:test'
import { calcFibraVsCetes, calcMetaRenta, calcRetornoTotal } from './herramientas-logic.ts'

test('calcFibraVsCetes — monto=0 devuelve capital y renta en cero', () => {
  const result = calcFibraVsCetes(0, 10, 9.5, 5)

  assert.equal(result.fibra.capitalFinal, 0)
  assert.equal(result.cetes.capitalFinal, 0)
  assert.equal(result.fibra.rentaAcumuladaNeta, 0)
  assert.equal(result.cetes.rentaAcumuladaNeta, 0)
})

test('calcFibraVsCetes — yield FIBRA negativo se clampea a cero (capitalFinal = monto)', () => {
  const result = calcFibraVsCetes(100000, -5, 9.5, 5)

  assert.equal(result.fibra.capitalFinal, 100000)
  assert.equal(result.fibra.rentaAcumuladaNeta, 0)
  assert.equal(result.fibra.rendimientoTotalPct, 0)
})

test('calcFibraVsCetes — horizonte=0 conserva el capital inicial', () => {
  const result = calcFibraVsCetes(100000, 10, 9.5, 0)

  assert.equal(result.fibra.capitalFinal, 100000)
  assert.equal(result.cetes.capitalFinal, 100000)
  assert.equal(result.fibra.rentaAcumuladaNeta, 0)
  assert.equal(result.cetes.rentaAcumuladaNeta, 0)
})

test('calcFibraVsCetes — calcula capital compuesto con tasas netas', () => {
  const result = calcFibraVsCetes(100000, 10, 9.5, 5)

  assert.ok(Math.abs(result.fibra.capitalFinal - 140255.17307) < 0.01)
  assert.ok(Math.abs(result.cetes.capitalFinal - 144231.91064) < 0.01)
  assert.ok(Math.abs(result.fibra.rendimientoTotalPct - 40.25517307) < 0.001)
  assert.ok(Math.abs(result.cetes.rendimientoTotalPct - 44.23191064) < 0.001)
})

test('calcMetaRenta — yieldPct=0 retorna nulls', () => {
  const result = calcMetaRenta(5000, 0)

  assert.equal(result.capitalNecesario, null)
  assert.equal(result.rentaMensualBrutaEstimada, null)
  assert.equal(result.cbfisEstimados, null)
})

test('calcMetaRenta — yieldPct negativo retorna nulls', () => {
  const result = calcMetaRenta(5000, -1)

  assert.equal(result.capitalNecesario, null)
  assert.equal(result.rentaMensualBrutaEstimada, null)
  assert.equal(result.cbfisEstimados, null)
})

test('calcMetaRenta — capital necesario con renta=5000 y yield=9', () => {
  const result = calcMetaRenta(5000, 9)

  assert.ok(result.capitalNecesario != null)
  assert.ok(Math.abs(result.capitalNecesario - 666666.6667) < 0.01)
  assert.ok(Math.abs(result.rentaMensualBrutaEstimada! - 5000) < 0.001)
})

test('calcMetaRenta — CBFIs estimados usa precio de referencia', () => {
  const result = calcMetaRenta(5000, 9, 25)

  assert.equal(result.cbfisEstimados, 26667)
})

test('calcRetornoTotal — precioCompra=0 retorna nulls', () => {
  const result = calcRetornoTotal(0, 22, 2.4, 0.72)

  assert.equal(result.plusvaliaPct, null)
  assert.equal(result.yieldNetoPct, null)
  assert.equal(result.retornoTotalPct, null)
})

test('calcRetornoTotal — calcula plusvalía, yield neto y retorno total', () => {
  const result = calcRetornoTotal(20, 22, 2.4, 0.72)

  assert.ok(Math.abs(result.plusvaliaPct! - 10) < 0.001)
  assert.ok(Math.abs(result.yieldNetoPct! - 8.4) < 0.001)
  assert.ok(Math.abs(result.retornoTotalPct! - 18.4) < 0.001)
})

test('calcRetornoTotal — maneja plusvalía negativa correctamente', () => {
  const result = calcRetornoTotal(25, 20, 2.4, 0.72)

  assert.ok(Math.abs(result.plusvaliaPct! - (-20)) < 0.001)
  assert.ok(Math.abs(result.retornoTotalPct! - (-13.28)) < 0.001)
})
