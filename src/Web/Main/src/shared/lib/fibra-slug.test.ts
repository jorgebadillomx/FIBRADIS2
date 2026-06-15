import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'
import { buildFibraSlug, extractTickerFromSlug } from './fibra-slug.ts'

// Corpus de paridad COMPARTIDO con FibraSlugTests.cs (C#). Fuente única: slug-parity.fixture.json.
// Si buildFibraSlug (TS) y FibraSlug.Build (C#) divergen sobre este corpus, el 301 del middleware
// y la canonicalización client-side entran en loop de redirecciones.
const parityFixture = JSON.parse(
  readFileSync(new URL('./slug-parity.fixture.json', import.meta.url), 'utf-8'),
) as { cases: { fullName: string; ticker: string; expected: string }[] }

test('buildFibraSlug genera slug básico con ticker al final', () => {
  assert.equal(buildFibraSlug('Fibra Uno', 'FUNO11'), 'fibra-uno-funo11')
})

test('buildFibraSlug coincide 1:1 con el corpus de paridad compartido (C# ↔ TS)', () => {
  for (const { fullName, ticker, expected } of parityFixture.cases) {
    assert.equal(
      buildFibraSlug(fullName, ticker),
      expected,
      `slug de "${fullName}" + "${ticker}" debe ser "${expected}"`,
    )
  }
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
