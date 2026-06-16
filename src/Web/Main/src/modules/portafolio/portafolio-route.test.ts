import assert from 'node:assert/strict'
import test from 'node:test'
import { resolvePortafolioRouteView } from './portafolio-route.ts'

test('resolvePortafolioRouteView returns loading while auth status is checking', () => {
  assert.equal(resolvePortafolioRouteView('checking'), 'loading')
})

test('resolvePortafolioRouteView returns landing for anonymous users', () => {
  assert.equal(resolvePortafolioRouteView('anonymous'), 'landing')
})

test('resolvePortafolioRouteView returns dashboard for authenticated users', () => {
  assert.equal(resolvePortafolioRouteView('authenticated'), 'dashboard')
})
