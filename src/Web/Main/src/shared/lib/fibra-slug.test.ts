import assert from 'node:assert/strict'
import test from 'node:test'
import { buildFibraSlug, extractTickerFromSlug } from './fibra-slug.ts'

test('buildFibraSlug genera slug básico con ticker al final', () => {
  assert.equal(buildFibraSlug('Fibra Uno', 'FUNO11'), 'fibra-uno-funo11')
})

// Tabla de la historia 11.3 — debe coincidir 1:1 con FibraSlugTests.cs (paridad C# ↔ TS)
test('buildFibraSlug coincide con los ejemplos del catálogo', () => {
  assert.equal(buildFibraSlug('Fibra Uno', 'FUNO11'), 'fibra-uno-funo11')
  assert.equal(buildFibraSlug('Fibra Macquarie', 'FIBRAMQ12'), 'fibra-macquarie-fibramq12')
  assert.equal(buildFibraSlug('Fibra Hotel City Express', 'HCITY17'), 'fibra-hotel-city-express-hcity17')
  assert.equal(buildFibraSlug('CFE Fibra E', 'FCFE18'), 'cfe-fibra-e-fcfe18')
})

test('buildFibraSlug normaliza acentos y eñes', () => {
  assert.equal(buildFibraSlug('Fibra Próximamente', 'NU11'), 'fibra-proximamente-nu11')
  assert.equal(buildFibraSlug('Fibra Montaña', 'TEST1'), 'fibra-montana-test1')
})

test('buildFibraSlug colapsa puntuación a un guión (paridad con C#)', () => {
  assert.equal(buildFibraSlug('Fibra Plus, S.A.', 'FPLUS16'), 'fibra-plus-s-a-fplus16')
  assert.equal(buildFibraSlug('  Fibra -- Test!  ', 'X99'), 'fibra-test-x99')
})

test('buildFibraSlug con nombre vacío devuelve solo el ticker', () => {
  assert.equal(buildFibraSlug('', 'FUNO11'), 'funo11')
  assert.equal(buildFibraSlug('   ', 'FUNO11'), 'funo11')
})

test('extractTickerFromSlug extrae el último segmento en mayúsculas', () => {
  assert.equal(extractTickerFromSlug('fibra-uno-funo11'), 'FUNO11')
  assert.equal(extractTickerFromSlug('fibra-hotel-city-express-hcity17'), 'HCITY17')
})

test('extractTickerFromSlug resuelve ticker pelado sin guiones (URL vieja)', () => {
  assert.equal(extractTickerFromSlug('FUNO11'), 'FUNO11')
  assert.equal(extractTickerFromSlug('funo11'), 'FUNO11')
})

test('extractTickerFromSlug con param vacío devuelve string vacío', () => {
  assert.equal(extractTickerFromSlug(''), '')
})

test('round-trip: extract(build(name, ticker)) recupera el ticker', () => {
  for (const [name, ticker] of [
    ['Fibra Uno', 'FUNO11'],
    ['CFE Fibra E', 'FCFE18'],
    ['', 'SOMA21'],
  ] as const) {
    assert.equal(extractTickerFromSlug(buildFibraSlug(name, ticker)), ticker)
  }
})
