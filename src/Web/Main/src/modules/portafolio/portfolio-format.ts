function toNumericValue(value: number | string | null | undefined): number | null {
  if (value == null) return null
  if (value === '') return null

  const numericValue = typeof value === 'string' ? Number(value) : value
  return Number.isFinite(numericValue) ? numericValue : null
}

export function formatMoney(value: number | string | null | undefined): string {
  const numericValue = toNumericValue(value)
  if (numericValue == null) return '—'

  return numericValue.toLocaleString('es-MX', {
    style: 'currency',
    currency: 'MXN',
  })
}

export function formatPercent(value: number | string | null | undefined): string {
  const numericValue = toNumericValue(value)
  if (numericValue == null) return '—'

  return `${numericValue.toLocaleString('es-MX', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}%`
}

export function formatVolume(value: number | string | null | undefined): string {
  const numericValue = toNumericValue(value)
  if (numericValue == null) return '—'

  return numericValue.toLocaleString('es-MX')
}

export function toNumberOrNull(value: number | string | null | undefined): number | null {
  return toNumericValue(value)
}
