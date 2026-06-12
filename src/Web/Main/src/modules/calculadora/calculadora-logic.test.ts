import assert from 'node:assert/strict'
import test from 'node:test'
import { calcCbfis, calcRentaBruta, calcRentaBrutaAnual, calcSobra } from './calculadora-logic.ts'

test('calcCbfis — monto=1000 y precio=22.34 devuelve 44', () => {
  assert.equal(calcCbfis(1000, 22.34), 44)
})

test('calcCbfis — monto=0 devuelve 0', () => {
  assert.equal(calcCbfis(0, 22.34), 0)
})

test('calcCbfis — precio=0 devuelve 0', () => {
  assert.equal(calcCbfis(1000, 0), 0)
})

test('calcCbfis — precio negativo devuelve 0', () => {
  assert.equal(calcCbfis(1000, -1), 0)
})

test('calcCbfis — monto negativo devuelve 0', () => {
  assert.equal(calcCbfis(-1000, 22.34), 0)
})

test('calcSobra — monto=1000, cbfis=44 y precio=22.34 devuelve 17.04', () => {
  const result = calcSobra(1000, 44, 22.34)

  assert.ok(Math.abs(result - 17.04) < 0.0001)
})

test('calcRentaBruta — cbfis=44 y distCbfi=0.60 devuelve 26.40', () => {
  const result = calcRentaBruta(44, 0.60)

  assert.ok(result != null)
  assert.ok(Math.abs(result - 26.4) < 0.0001)
})

test('calcRentaBruta — distCbfi=null devuelve null', () => {
  assert.equal(calcRentaBruta(44, null), null)
})

test('calcRentaBrutaAnual — cbfis=44 y distCbfiAnual=2.40 devuelve 105.60', () => {
  const result = calcRentaBrutaAnual(44, 2.40)

  assert.ok(result != null)
  assert.ok(Math.abs(result - 105.6) < 0.0001)
})

test('calcRentaBrutaAnual — distCbfiAnual=null devuelve null', () => {
  assert.equal(calcRentaBrutaAnual(44, null), null)
})
