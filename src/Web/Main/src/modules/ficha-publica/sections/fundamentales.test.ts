import test from 'node:test'
import assert from 'node:assert/strict'
import {
  formatFundamentalValue,
  hasFundamentalesItems,
  shouldShowFundamentalesWarning,
  type FundamentalesData,
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
  const data: FundamentalesData = {
    items: [{ label: 'Cap Rate', period: 'Q3 2024', value: 8.4 }],
  }

  assert.equal(hasFundamentalesItems(undefined), false)
  assert.equal(hasFundamentalesItems({ items: [] }), false)
  assert.equal(hasFundamentalesItems(data), true)
})
