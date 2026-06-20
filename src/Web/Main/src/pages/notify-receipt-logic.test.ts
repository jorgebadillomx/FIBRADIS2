import test from 'node:test'
import assert from 'node:assert/strict'
import { validateReceiptFile } from './notify-receipt-logic.ts'
import { MAX_RECEIPT_BYTES } from './payment-plans.ts'

test('validateReceiptFile rechaza tipo no-imagen no-pdf', () => {
  assert.deepEqual(
    validateReceiptFile({ type: 'application/zip', size: 100 }),
    { valid: false, error: 'invalid_type' },
  )
})

test('validateReceiptFile rechaza archivo que supera 5 MB', () => {
  assert.deepEqual(
    validateReceiptFile({ type: 'image/png', size: MAX_RECEIPT_BYTES + 1 }),
    { valid: false, error: 'too_large' },
  )
})

test('validateReceiptFile acepta image/png dentro del límite', () => {
  assert.deepEqual(
    validateReceiptFile({ type: 'image/png', size: 1000 }),
    { valid: true },
  )
})

test('validateReceiptFile acepta application/pdf', () => {
  assert.deepEqual(
    validateReceiptFile({ type: 'application/pdf', size: MAX_RECEIPT_BYTES }),
    { valid: true },
  )
})

test('validateReceiptFile acepta exactamente en el límite', () => {
  assert.deepEqual(
    validateReceiptFile({ type: 'image/jpeg', size: MAX_RECEIPT_BYTES }),
    { valid: true },
  )
})

test('validateReceiptFile rechaza text/plain', () => {
  assert.deepEqual(
    validateReceiptFile({ type: 'text/plain', size: 50 }),
    { valid: false, error: 'invalid_type' },
  )
})

test('validateReceiptFile rechaza tipo vacío (MIME no detectado por el browser)', () => {
  assert.deepEqual(
    validateReceiptFile({ type: '', size: 1000 }),
    { valid: false, error: 'invalid_type' },
  )
})
