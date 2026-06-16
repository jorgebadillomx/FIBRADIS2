import assert from 'node:assert/strict'
import test from 'node:test'
import {
  buildFibraSuggestions,
  buildFundamentalPeriodOptions,
  getDefaultFundamentalPeriod,
  sortFundamentalPeriods,
} from './reportes-logic.ts'

test('sortFundamentalPeriods orders quarter strings from newest to oldest and removes duplicates', () => {
  assert.deepEqual(
    sortFundamentalPeriods(['Q1-2024', 'Q4-2025', 'Q2-2025', 'Q4-2025', 'Q1-2024']),
    ['Q4-2025', 'Q2-2025', 'Q1-2024'],
  )
})

test('buildFundamentalPeriodOptions reuses the same newest-first ordering for the combo', () => {
  assert.deepEqual(
    buildFundamentalPeriodOptions(['Q1-2024', 'Q3-2025', 'Q2-2025']),
    ['Q3-2025', 'Q2-2025', 'Q1-2024'],
  )
})

test('getDefaultFundamentalPeriod returns the most recent period and null when empty', () => {
  assert.equal(getDefaultFundamentalPeriod(['Q1-2024', 'Q4-2025', 'Q2-2025']), 'Q4-2025')
  assert.equal(getDefaultFundamentalPeriod([]), null)
})

test('buildFibraSuggestions excludes the selected ticker and caps the list at eight rows', () => {
  const result = buildFibraSuggestions(
    [
      { ticker: 'FUNO11', empresa: 'Fibra Uno' },
      { ticker: 'FMTY14', empresa: 'Fibra MTY' },
      { ticker: 'TERRA13', empresa: 'Fibra Terrafina' },
      { ticker: 'VESTA', empresa: 'Vesta' },
      { ticker: 'FIBRAMQ', empresa: 'Fibra Macquarie' },
      { ticker: 'NEXO', empresa: 'Nexus' },
      { ticker: 'TEST1', empresa: 'Test Uno' },
      { ticker: 'TEST2', empresa: 'Test Dos' },
      { ticker: 'TEST3', empresa: 'Test Tres' },
    ],
    '',
    'FMTY14',
  )

  assert.equal(result.length, 8)
  assert.equal(result.some((item) => item.ticker === 'FMTY14'), false)
  assert.deepEqual(
    result.map((item) => item.ticker),
    ['FIBRAMQ', 'FUNO11', 'NEXO', 'TERRA13', 'TEST1', 'TEST2', 'TEST3', 'VESTA'],
  )
})

test('buildFibraSuggestions filters by ticker or company name', () => {
  const result = buildFibraSuggestions(
    [
      { ticker: 'ZZZ1', empresa: 'Zed' },
      { ticker: 'AAA1', empresa: 'Alpha Fibra' },
      { ticker: 'MMM1', empresa: 'Middle' },
    ],
    'fibra',
  )

  assert.deepEqual(result.map((item) => item.ticker), ['AAA1'])
})
