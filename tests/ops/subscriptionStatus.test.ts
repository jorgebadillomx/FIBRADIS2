import assert from 'node:assert/strict'
import test from 'node:test'
import type { UserSummaryDto } from '../../src/Web/Ops/src/api/usersApi.ts'
import {
  formatSubscriptionDate,
  getSubscriptionStatus,
  toDateInput,
} from '../../src/Web/Ops/src/utils/subscriptionStatus.ts'

function makeUser(overrides: Partial<UserSummaryDto>): UserSummaryDto {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    email: 'usuario@ejemplo.com',
    role: 'User',
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    pago: null,
    fechaPago: null,
    subscriptionType: null,
    subscriptionStartedAt: null,
    subscriptionEndsAt: null,
    trialEndsAt: null,
    emailConfirmedAt: null,
    ...overrides,
  }
}

test('formatSubscriptionDate preserves the expected DD/MM/AAAA output', () => {
  assert.equal(formatSubscriptionDate('2026-06-10T00:00:00Z'), '10/06/2026')
})

test('toDateInput preserves the expected YYYY-MM-DD output', () => {
  assert.equal(toDateInput('2026-06-10T00:00:00Z'), '2026-06-10')
})

test('getSubscriptionStatus returns Lifetime as a green status without date', () => {
  const status = getSubscriptionStatus(makeUser({ subscriptionType: 'Lifetime' }))

  assert.deepEqual(status, { label: 'Lifetime', color: 'green' })
})

test('getSubscriptionStatus returns an active Monthly plan with a green due date badge', () => {
  const status = getSubscriptionStatus(makeUser({
    subscriptionType: 'Monthly',
    subscriptionEndsAt: '2099-06-10T00:00:00Z',
  }))

  assert.deepEqual(status, { label: 'Monthly · vence 10/06/2099', color: 'green' })
})

test('getSubscriptionStatus returns an active trial badge when the user has no plan', () => {
  const status = getSubscriptionStatus(makeUser({
    trialEndsAt: '2099-06-10T00:00:00Z',
  }))

  assert.deepEqual(status, { label: 'Trial · vence 10/06/2099', color: 'amber' })
})

test('getSubscriptionStatus returns Sin acceso for expired plans even when trial data exists', () => {
  const status = getSubscriptionStatus(makeUser({
    subscriptionType: 'Annual',
    subscriptionEndsAt: '2000-01-01T00:00:00Z',
    trialEndsAt: '2099-06-10T00:00:00Z',
  }))

  assert.deepEqual(status, { label: 'Sin acceso', color: 'gray' })
})

test('getSubscriptionStatus returns an active Annual plan with a green due date badge', () => {
  const status = getSubscriptionStatus(makeUser({
    subscriptionType: 'Annual',
    subscriptionEndsAt: '2099-06-10T00:00:00Z',
  }))

  assert.deepEqual(status, { label: 'Annual · vence 10/06/2099', color: 'green' })
})

test('getSubscriptionStatus returns Sin acceso when subscriptionType is set but subscriptionEndsAt is null', () => {
  const status = getSubscriptionStatus(makeUser({
    subscriptionType: 'Monthly',
    subscriptionEndsAt: null,
  }))

  assert.deepEqual(status, { label: 'Sin acceso', color: 'gray' })
})
