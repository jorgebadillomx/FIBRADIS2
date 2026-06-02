const monthFormatter = new Intl.DateTimeFormat('es-MX', {
  month: 'short',
  year: 'numeric',
  timeZone: 'UTC',
})

type DistributionPoint = {
  date: string
  amountPerUnit: number | string
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

export function getDistributionPeriodLabel(date: string, cadence: Cadence): string {
  const current = parseDate(date)
  const year = current.getUTCFullYear()
  const month = current.getUTCMonth() // 0-11

  if (cadence === 'monthly') {
    const prev = new Date(Date.UTC(year, month - 1, 1))
    return monthFormatter.format(prev).replace('.', '')
  }

  if (cadence === 'quarterly') {
    const q = Math.floor(month / 3) + 1 // 1-4
    return q === 1 ? `Q4 ${year - 1}` : `Q${q - 1} ${year}`
  }

  if (cadence === 'semiannual') {
    return month < 6 ? `S2 ${year - 1}` : `S1 ${year}`
  }

  if (cadence === 'annual') {
    return `${year - 1}`
  }

  const prev = new Date(Date.UTC(year, month - 1, 1))
  return monthFormatter.format(prev).replace('.', '')
}

