import test from 'node:test'
import assert from 'node:assert/strict'
import { areAllReportLinksMissing, getReportLinkItems } from './reportes.ts'

test('areAllReportLinksMissing returns true only when all report URLs are null', () => {
  assert.equal(
    areAllReportLinksMissing({ siteUrl: null, investorUrl: null, reportsUrl: null }),
    true,
  )
  assert.equal(
    areAllReportLinksMissing({ siteUrl: 'https://fibra.uno', investorUrl: null, reportsUrl: null }),
    false,
  )
})

test('getReportLinkItems preserves labels and nullable URLs for rendering', () => {
  assert.deepEqual(
    getReportLinkItems({
      siteUrl: 'https://fibra.uno',
      investorUrl: null,
      reportsUrl: 'https://fibra.uno/reportes',
    }),
    [
      { key: 'siteUrl', label: 'Sitio web', url: 'https://fibra.uno' },
      { key: 'investorUrl', label: 'Relación con inversionistas', url: null },
      { key: 'reportsUrl', label: 'Reportes oficiales', url: 'https://fibra.uno/reportes' },
    ],
  )
})
