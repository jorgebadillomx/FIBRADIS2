import type { components } from '@fibradis/shared-api-client'

type PortfolioPositionDto = components['schemas']['PortfolioPositionDto']
type PortfolioDistributionDto = components['schemas']['PortfolioDistributionDto']

export type PaymentCadence = 'trimestral' | 'semestral' | 'anual' | 'desconocida'

export interface ProjectedPayment {
  fibraId: string
  ticker: string
  nombre: string
  logoUrl: string | null
  fechaEstimada: Date
  montoPorTitulo: number
  montoTotal: number
  cadencia: PaymentCadence
}

const DAY_MS = 24 * 60 * 60 * 1000
const HORIZON_DAYS = 90

export function projectNextPayments(
  positions: PortfolioPositionDto[],
  today: Date = new Date(),
): ProjectedPayment[] {
  const horizon = new Date(Date.UTC(
    today.getUTCFullYear(),
    today.getUTCMonth(),
    today.getUTCDate() + HORIZON_DAYS,
  ))
  const todayUtc = toUtcDate(today)

  const payments: ProjectedPayment[] = []

  for (const position of positions) {
    const recentDistributions = (position.recentDistributions ?? [])
      .map((distribution) => ({
        distribution,
        date: parseDate(distribution.paymentDate),
      }))
      .filter((item): item is { distribution: PortfolioDistributionDto; date: Date } => item.date !== null)
      .sort((left, right) => right.date.getTime() - left.date.getTime())

    if (recentDistributions.length === 0) continue

    const cadence = detectCadence(recentDistributions.map((item) => item.date))
    if (cadence === 'desconocida') continue

    const last = recentDistributions[0]
    const montoPorTitulo = toNumberOrNull(last.distribution.amountPerUnit) ?? 0
    const titulos = toNumberOrNull(position.titulos) ?? 0
    if (montoPorTitulo <= 0 || titulos <= 0) continue

    const amount = montoPorTitulo * titulos
    let nextDate = addCadence(last.date, cadence)

    while (nextDate <= horizon) {
      if (nextDate > todayUtc) {
        payments.push({
          fibraId: position.fibraId,
          ticker: position.ticker,
          nombre: position.nombre,
          logoUrl: position.logoUrl ?? `/logos/${position.ticker.toLowerCase()}.png`,
          fechaEstimada: new Date(nextDate.getTime()),
          montoPorTitulo,
          montoTotal: amount,
          cadencia: cadence,
        })
      }

      nextDate = addCadence(nextDate, cadence)
    }
  }

  return payments.sort((left, right) => left.fechaEstimada.getTime() - right.fechaEstimada.getTime())
}

function detectCadence(distributionDates: Date[]): PaymentCadence {
  if (distributionDates.length < 2) {
    return distributionDates.length === 1 ? 'anual' : 'desconocida'
  }
  // dates are sorted descending; use the interval between the two most recent
  const intervalDays = (distributionDates[0].getTime() - distributionDates[1].getTime()) / DAY_MS
  if (intervalDays <= 105) return 'trimestral'
  if (intervalDays <= 210) return 'semestral'
  if (intervalDays <= 400) return 'anual'
  return 'desconocida'
}

function addCadence(date: Date, cadence: Exclude<PaymentCadence, 'desconocida'>): Date {
  const months = cadence === 'trimestral' ? 3 : cadence === 'semestral' ? 6 : 12
  return new Date(Date.UTC(
    date.getUTCFullYear(),
    date.getUTCMonth() + months,
    date.getUTCDate(),
  ))
}

function parseDate(value: string): Date | null {
  if (!value) return null

  const parsed = new Date(`${value}T00:00:00Z`)
  return Number.isNaN(parsed.getTime()) ? null : parsed
}

function toUtcDate(date: Date): Date {
  return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()))
}

function toNumberOrNull(value: number | string | null | undefined): number | null {
  if (value == null || value === '') return null
  const numeric = typeof value === 'string' ? Number(value) : value
  return Number.isFinite(numeric) ? numeric : null
}
