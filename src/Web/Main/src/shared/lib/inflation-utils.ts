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

/**
 * Compone dos tasas anuales consecutivas para obtener la inflación acumulada a 2 años.
 * Requiere al menos 13 entradas (la más reciente cubre los últimos 12 meses, y la
 * que está 12 posiciones atrás cubre los 12 meses anteriores).
 */
export function cumulative2yInflation(inpcHistory: InpcMonthlyDto[] | null | undefined): number | null {
  if (!Array.isArray(inpcHistory) || inpcHistory.length < 13) return null

  const sorted = [...inpcHistory].sort((a, b) => a.periodo.localeCompare(b.periodo))
  const recent = sorted[sorted.length - 1]
  const prior = sorted[sorted.length - 13]

  const r1 = toFiniteNumber(recent.anualPct)
  const r2 = toFiniteNumber(prior.anualPct)

  if (r1 == null || r2 == null) return null

  return ((1 + r1 / 100) * (1 + r2 / 100) - 1) * 100
}
