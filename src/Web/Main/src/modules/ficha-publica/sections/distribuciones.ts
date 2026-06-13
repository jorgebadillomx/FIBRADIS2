import { toNum } from '../../../shared/lib/format-time.ts'

const monthFormatter = new Intl.DateTimeFormat('es-MX', {
  month: 'short',
  year: 'numeric',
  timeZone: 'UTC',
})

export type DistributionPoint = {
  date: string
  amountPerUnit: number | string
}

export type PeriodGroup = {
  label: string
  total: number
  items: DistributionPoint[]
}

type Cadence = 'monthly' | 'quarterly' | 'semiannual' | 'annual' | 'irregular'

function parseDate(value: string) {
  return new Date(`${value}T00:00:00Z`)
}

function diffDays(left: Date, right: Date) {
  return Math.round(Math.abs(left.getTime() - right.getTime()) / 86_400_000)
}

function median(values: number[]) {
  if (values.length === 0) return null

  const sorted = [...values].sort((a, b) => a - b)
  const middle = Math.floor(sorted.length / 2)

  return sorted.length % 2 === 0
    ? (sorted[middle - 1]! + sorted[middle]!) / 2
    : sorted[middle]!
}

export function inferDistributionCadence(distributions: DistributionPoint[]): Cadence {
  if (distributions.length <= 1) return 'quarterly'

  const dates = distributions
    .map(item => parseDate(item.date))
    .sort((a, b) => b.getTime() - a.getTime())

  const gaps = dates
    .slice(0, -1)
    .map((date, index) => diffDays(date, dates[index + 1]!))
    .filter(gap => gap >= 20)

  const typicalGap = median(gaps)

  if (typicalGap == null) return 'quarterly'
  if (typicalGap <= 45) return 'monthly'
  if (typicalGap <= 135) return 'quarterly'
  if (typicalGap <= 260) return 'semiannual'

  return 'annual'
}

// La fecha de pago corresponde al periodo anterior: una distribución pagada en Q2
// cubre el periodo Q1, en S2 cubre S1, en mes M cubre M-1, etc.
export function getDistributionPeriodLabel(date: string, cadence: Cadence): string {
  const current = parseDate(date)
  const year = current.getUTCFullYear()
  const month = current.getUTCMonth() // 0-11

  if (cadence === 'monthly') {
    // shift −1 mes
    const periodDate = new Date(Date.UTC(year, month - 1, 1))
    return monthFormatter.format(periodDate).replace('.', '')
  }

  if (cadence === 'quarterly') {
    const paymentQ = Math.floor(month / 3) + 1 // 1-4
    const periodQ = paymentQ === 1 ? 4 : paymentQ - 1
    const periodYear = paymentQ === 1 ? year - 1 : year
    return `Q${periodQ} ${periodYear}`
  }

  if (cadence === 'semiannual') {
    // S1 (Jan-Jun) → pago por S2 anterior; S2 (Jul-Dec) → pago por S1 mismo año
    const isFirstHalf = month < 6
    return isFirstHalf ? `S2 ${year - 1}` : `S1 ${year}`
  }

  if (cadence === 'annual') {
    return `${year}`
  }

  return monthFormatter.format(current).replace('.', '')
}

export function groupDistributionsByPeriod(
  distributions: DistributionPoint[],
  cadence: Cadence,
): PeriodGroup[] {
  const map = new Map<string, PeriodGroup>()
  // preserve insertion order (distributions are most-recent-first)
  for (const dist of distributions) {
    const label = getDistributionPeriodLabel(dist.date, cadence)
    const amount = toNum(dist.amountPerUnit)
    if (amount == null) continue
    const existing = map.get(label)
    if (existing) {
      existing.total += amount
      existing.items.push(dist)
    } else {
      map.set(label, { label, total: amount, items: [dist] })
    }
  }
  return Array.from(map.values())
}

// Returns an array of diffs aligned with groups: diff[i] = groups[i].total - groups[i+1].total
// (positive = current period paid more than previous period). Last group = null.
export function calcPeriodDiff(groups: PeriodGroup[]): (number | null)[] {
  return groups.map((group, i) => {
    const next = groups[i + 1]
    return next != null ? group.total - next.total : null
  })
}
