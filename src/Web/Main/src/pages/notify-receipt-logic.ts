import { MAX_RECEIPT_BYTES } from './payment-plans.ts'

export type ReceiptValidationResult =
  | { valid: true }
  | { valid: false; error: 'invalid_type' | 'too_large' }

export function validateReceiptFile(file: { type: string; size: number }): ReceiptValidationResult {
  if (!file.type) return { valid: false, error: 'invalid_type' }
  const typeOk = file.type.startsWith('image/') || file.type === 'application/pdf'
  if (!typeOk) return { valid: false, error: 'invalid_type' }
  if (file.size > MAX_RECEIPT_BYTES) return { valid: false, error: 'too_large' }
  return { valid: true }
}
