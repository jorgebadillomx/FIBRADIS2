import test from 'node:test'
import assert from 'node:assert/strict'
import {
  formatFundamentalValue,
  getLatestCapturedAt,
  hasFundamentalesItems,
  shouldShowFundamentalesWarning,
  type FundamentalesPublicData,
} from './fundamentales.ts'

test('shouldShowFundamentalesWarning only shows the stale-data alert at three periods or more', () => {
  assert.equal(shouldShowFundamentalesWarning(undefined), false)
  assert.equal(shouldShowFundamentalesWarning({ periodsAgo: 2 }), false)
  assert.equal(shouldShowFundamentalesWarning({ periodsAgo: 3 }), true)
})

test('formatFundamentalValue preserves zeroes and replaces nullables with em dash', () => {
  assert.equal(formatFundamentalValue(0), 0)
  assert.equal(formatFundamentalValue(12.5), 12.5)
  assert.equal(formatFundamentalValue(null), '—')
  assert.equal(formatFundamentalValue(undefined), '—')
})

test('hasFundamentalesItems detects when the section should render data rows', () => {
  const data: FundamentalesPublicData = {
    items: [{ label: 'Cap Rate', period: 'Q3 2024', value: 8.4 }],
  }

  assert.equal(hasFundamentalesItems(undefined), false)
  assert.equal(hasFundamentalesItems({ items: [] }), false)
  assert.equal(hasFundamentalesItems(data), true)
})

test('getLatestCapturedAt returns the newest timestamp or null for empty rows', () => {
  assert.equal(getLatestCapturedAt([]), null)

  const latest = getLatestCapturedAt([
    { capturedAt: '2026-06-10T14:00:00.000Z' },
    { capturedAt: '2026-06-13T08:45:00.000Z' },
  ])

  assert.ok(latest)
  assert.equal(latest.toISOString(), '2026-06-13T08:45:00.000Z')
})
