import assert from 'node:assert/strict'
import test from 'node:test'
import { calcRealReturn, latestInpcPct } from './inflation-utils.ts'

test('calcRealReturn — fisher con inflación cero coincide con nominal', () => {
  assert.ok(Math.abs(calcRealReturn(12, 0) - 12) < 0.0001)
})

test('calcRealReturn — fisher con nominal e inflación iguales se acerca a cero', () => {
  assert.ok(Math.abs(calcRealReturn(10, 10)) < 0.0001)
})

test('calcRealReturn — inflación de -100% no divide por cero', () => {
  assert.equal(calcRealReturn(8, -100), 0)
})

test('latestInpcPct — null y vacío retornan null', () => {
  assert.equal(latestInpcPct(null), null)
  assert.equal(latestInpcPct([]), null)
})

test('latestInpcPct — devuelve el último elemento disponible', () => {
  const result = latestInpcPct([
    { periodo: '2025-05-01', anualPct: 4.8 },
    { periodo: '2025-06-01', anualPct: '5.2' },
  ])

  assert.equal(result, 5.2)
})
