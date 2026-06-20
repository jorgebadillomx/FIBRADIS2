import assert from 'node:assert/strict'
import test from 'node:test'
import { resolveSubscriptionState } from './suscripcion-logic.ts'

const FUTURE = new Date(Date.now() + 7 * 86400000).toISOString()
const PAST = new Date(Date.now() - 7 * 86400000).toISOString()

test('resolveSubscriptionState: Lifetime activo → kind=lifetime', () => {
  const state = resolveSubscriptionState(true, 'Lifetime', null, null)
  assert.equal(state.kind, 'lifetime')
})

test('resolveSubscriptionState: Monthly activo → kind=active con subscriptionType=Monthly', () => {
  const state = resolveSubscriptionState(true, 'Monthly', null, FUTURE)
  assert.equal(state.kind, 'active')
  if (state.kind === 'active') {
    assert.equal(state.subscriptionType, 'Monthly')
    assert.equal(state.subscriptionEndsAt, FUTURE)
  }
})

test('resolveSubscriptionState: Annual activo → kind=active con subscriptionType=Annual', () => {
  const state = resolveSubscriptionState(true, 'Annual', null, FUTURE)
  assert.equal(state.kind, 'active')
  if (state.kind === 'active') {
    assert.equal(state.subscriptionType, 'Annual')
  }
})

test('resolveSubscriptionState: trial activo (subscriptionType=null, trialEndsAt futuro) → kind=trial con daysRemaining>0', () => {
  const state = resolveSubscriptionState(true, null, FUTURE, null)
  assert.equal(state.kind, 'trial')
  if (state.kind === 'trial') {
    assert.ok(state.daysRemaining > 0)
    assert.equal(state.trialEndsAt, FUTURE)
  }
})

test('resolveSubscriptionState: isActive=false con hadTrial → kind=expired con hadTrial=true', () => {
  const state = resolveSubscriptionState(false, null, PAST, null)
  assert.equal(state.kind, 'expired')
  if (state.kind === 'expired') {
    assert.equal(state.hadTrial, true)
  }
})

test('resolveSubscriptionState: isActive=false sin trial → kind=expired con hadTrial=false', () => {
  const state = resolveSubscriptionState(false, null, null, null)
  assert.equal(state.kind, 'expired')
  if (state.kind === 'expired') {
    assert.equal(state.hadTrial, false)
  }
})

test('resolveSubscriptionState: Lifetime NO muestra sección de pago (kind=lifetime)', () => {
  const state = resolveSubscriptionState(true, 'Lifetime', null, null)
  // La sección de pago se oculta cuando state.kind === 'lifetime'
  assert.equal(state.kind !== 'lifetime', false)
})

test('resolveSubscriptionState: trial activo SÍ requiere sección de pago (kind≠lifetime)', () => {
  const state = resolveSubscriptionState(true, null, FUTURE, null)
  assert.notEqual(state.kind, 'lifetime')
})

test('resolveSubscriptionState: Lifetime desactivado (isActive=false) → kind=lifetime (P4)', () => {
  const state = resolveSubscriptionState(false, 'Lifetime', null, null)
  assert.equal(state.kind, 'lifetime')
})

test('resolveSubscriptionState: modo degradado (isActive=true, sin datos) → kind=trial, daysRemaining=0 (P3)', () => {
  const state = resolveSubscriptionState(true, null, null, null)
  assert.equal(state.kind, 'trial')
  if (state.kind === 'trial') {
    assert.equal(state.daysRemaining, 0)
  }
})

test('resolveSubscriptionState: trial con trialEndsAt pasado pero isActive=true → daysRemaining=0, no negativo (P2)', () => {
  const state = resolveSubscriptionState(true, null, PAST, null)
  assert.equal(state.kind, 'trial')
  if (state.kind === 'trial') {
    assert.ok(state.daysRemaining >= 0, 'daysRemaining no debe ser negativo')
  }
})
