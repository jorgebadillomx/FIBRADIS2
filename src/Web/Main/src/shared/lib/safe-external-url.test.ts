import assert from 'node:assert/strict'
import test from 'node:test'
import { getSafeExternalUrl } from './safe-external-url.ts'

test('getSafeExternalUrl preserves http and https URLs', () => {
  assert.equal(getSafeExternalUrl('https://example.com/news'), 'https://example.com/news')
  assert.equal(getSafeExternalUrl(' http://example.com/item '), 'http://example.com/item')
})

test('getSafeExternalUrl rejects non-http schemes and malformed URLs', () => {
  assert.equal(getSafeExternalUrl('javascript:alert(1)'), null)
  assert.equal(getSafeExternalUrl('data:text/html;base64,abcd'), null)
  assert.equal(getSafeExternalUrl('/relative/path'), null)
  assert.equal(getSafeExternalUrl('not a url'), null)
})
