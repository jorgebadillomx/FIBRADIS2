export const ISR_RATE = 0.30

export interface IsrResult {
  taxableGross: number
  capitalReturn: number
  isr: number
  net: number
  unitsUsed: number
  isEstimate: boolean
}

export function parseInput(value: string): number {
  const trimmed = value.trim()
  if (trimmed === '') return 0
  const parsed = Number(trimmed)
  return Number.isFinite(parsed) ? parsed : 0
}

function normalizeNumber(value: number | null | undefined): number {
  if (value == null || !Number.isFinite(value)) return 0
  return value
}

function normalizeUnits(value: number | null | undefined): number {
  if (value == null || !Number.isFinite(value)) return 1
  return Math.max(Math.floor(value), 1)
}

export function calcIsr(
  distPerUnit: number,
  units = 1,
  taxablePerUnit?: number | null,
): IsrResult {
  const safeDist = Math.max(normalizeNumber(distPerUnit), 0)
  const safeUnits = normalizeUnits(units)
  const hasBreakdown = taxablePerUnit != null && Number.isFinite(taxablePerUnit)
  const isEstimate = !hasBreakdown
  const taxableValue = taxablePerUnit ?? 0
  const taxableBase = isEstimate
    ? safeDist
    // Cap: ISR withholding cannot exceed the cash distributed
    : Math.min(safeDist, Math.max(0, taxableValue))
  const capitalBase = isEstimate ? 0 : Math.max(0, safeDist - taxableBase)
  const taxableGross = taxableBase * safeUnits
  const capitalReturn = capitalBase * safeUnits
  const isr = taxableGross * ISR_RATE

  return {
    taxableGross,
    capitalReturn,
    isr,
    net: taxableGross - isr + capitalReturn,
    unitsUsed: safeUnits,
    isEstimate,
  }
}
