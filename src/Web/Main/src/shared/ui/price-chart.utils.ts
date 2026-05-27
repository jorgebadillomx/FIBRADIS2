import { toNum } from '../lib/format-time.ts'

export interface PriceChartInputPoint {
  date: string
  close: number | string | null | undefined
}

export interface PriceChartPoint {
  close: number | null
  date: string
  fullLabel: string
  shortLabel: string
}

export interface PriceChartSummary {
  first: number | null
  last: number | null
  min: number | null
  max: number | null
  change: number | null
  changePct: number | null
  visibleDot: boolean
}

const shortDateFormatter = new Intl.DateTimeFormat('es-MX', {
  day: '2-digit',
  month: 'short',
})

const fullDateFormatter = new Intl.DateTimeFormat('es-MX', {
  day: '2-digit',
  month: 'long',
  year: 'numeric',
})

export function buildPriceChartPoints(raw: PriceChartInputPoint[]): PriceChartPoint[] {
  return raw.map((entry) => {
    const date = new Date(`${entry.date}T00:00:00`)

    return {
      date: entry.date,
      close: toNum(entry.close),
      shortLabel: shortDateFormatter.format(date),
      fullLabel: fullDateFormatter.format(date),
    }
  })
}

export function summarizePriceChart(points: PriceChartPoint[]): PriceChartSummary {
  const values = points
    .map(point => point.close)
    .filter((value): value is number => value != null)

  if (values.length === 0) {
    return {
      first: null,
      last: null,
      min: null,
      max: null,
      change: null,
      changePct: null,
      visibleDot: false,
    }
  }

  const first = values[0]
  const last = values[values.length - 1]
  const min = Math.min(...values)
  const max = Math.max(...values)
  const change = last - first
  const changePct = first === 0 ? null : (change / first) * 100

  return {
    first,
    last,
    min,
    max,
    change,
    changePct,
    visibleDot: points.length <= 45,
  }
}
