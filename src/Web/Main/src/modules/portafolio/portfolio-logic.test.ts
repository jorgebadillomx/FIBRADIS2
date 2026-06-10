import assert from 'node:assert/strict'
import test from 'node:test'
import { calcYieldPortafolio, calcYoc } from './portfolio-logic.ts'

test('calcYoc — caso nominal', () => {
  assert.equal(calcYoc(2000, 40000), 5)
})

test('calcYoc — denominador cero retorna null', () => {
  assert.equal(calcYoc(2000, 0), null)
})

test('calcYoc — renta anual nula retorna null', () => {
  assert.equal(calcYoc(null, 40000), null)
})

test('calcYieldPortafolio — caso nominal', () => {
  assert.equal(calcYieldPortafolio(2000, 50000), 4)
})

test('calcYieldPortafolio — denominador cero retorna null', () => {
  assert.equal(calcYieldPortafolio(2000, 0), null)
})

test('calcYieldPortafolio — valores nulos retornan null', () => {
  assert.equal(calcYieldPortafolio(null, 50000), null)
})
