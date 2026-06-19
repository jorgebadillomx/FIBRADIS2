export interface ReportFibraCandidate {
  ticker: string
  empresa: string
}

export interface ReportFibraSuggestion {
  ticker: string
  empresa: string
}

const FUNDAMENTAL_PERIOD_RE = /^Q([1-4])-(\d{4})$/

export function buildFundamentalPeriodOptions(periods: string[]): string[] {
  return sortFundamentalPeriods(periods)
}

export function getDefaultFundamentalPeriod(periods: string[]): string | null {
  return sortFundamentalPeriods(periods)[0] ?? null
}

export function buildFibraSuggestions(
  fibras: ReportFibraCandidate[],
  search: string,
  selectedTicker?: string | null,
): ReportFibraSuggestion[] {
  const term = search.trim().toLowerCase()
  const pool = fibras.filter((fibra) => fibra.ticker.toUpperCase() !== selectedTicker?.toUpperCase())

  const filtered = term
    ? pool.filter(
        (fibra) =>
          fibra.ticker.toLowerCase().includes(term) || fibra.empresa.toLowerCase().includes(term),
      )
    : pool

  return [...filtered]
    .sort((a, b) => a.ticker.localeCompare(b.ticker))
}

export function sortFundamentalPeriods(periods: string[]): string[] {
  return [...new Set(periods.map((period) => period.trim()).filter(Boolean))].sort(compareFundamentalPeriods)
}

function compareFundamentalPeriods(a: string, b: string): number {
  const parsedA = parseFundamentalPeriod(a)
  const parsedB = parseFundamentalPeriod(b)

  if (parsedA && parsedB) {
    if (parsedA.year !== parsedB.year) return parsedB.year - parsedA.year
    if (parsedA.quarter !== parsedB.quarter) return parsedB.quarter - parsedA.quarter
    return 0
  }

  if (parsedA) return -1
  if (parsedB) return 1
  return b.localeCompare(a)
}

function parseFundamentalPeriod(period: string): { quarter: number; year: number } | null {
  const match = FUNDAMENTAL_PERIOD_RE.exec(period.toUpperCase())
  if (!match) return null

  return {
    quarter: Number(match[1]),
    year: Number(match[2]),
  }
}
