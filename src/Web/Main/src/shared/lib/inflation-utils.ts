import type { components } from '@fibradis/shared-api-client'

type InpcMonthlyDto = components['schemas']['InpcMonthlyDto']

function toFiniteNumber(value: number | string | null | undefined): number | null {
  if (value == null) return null
  const numeric = typeof value === 'string' ? Number(value) : value
  return Number.isFinite(numeric) ? numeric : null
}

export function calcRealReturn(nominalPct: number, inflationPct: number): number {
  if (!Number.isFinite(nominalPct) || !Number.isFinite(inflationPct)) return 0

  const denominator = 1 + inflationPct / 100
  if (Math.abs(denominator) < 0.0001) return 0

  return ((1 + nominalPct / 100) / denominator - 1) * 100
}

export function latestInpcPct(inpcHistory: InpcMonthlyDto[] | null | undefined): number | null {
  if (!Array.isArray(inpcHistory) || inpcHistory.length === 0) return null

  const latest = inpcHistory[inpcHistory.length - 1]
  if (!latest) return null

  return toFiniteNumber(latest.anualPct)
}
