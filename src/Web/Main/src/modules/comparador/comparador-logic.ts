export const MIN_COMPARE_FIBRAS = 2
export const MAX_COMPARE_FIBRAS = 4
export const BENCHMARK_OPTIONS = ['ipc', 'sp500'] as const
export type CompareBenchmark = (typeof BENCHMARK_OPTIONS)[number]

export function parseCompareTickers(value: string | null | undefined): string[] {
  return normalizeCompareTickers(value?.split(',') ?? [], MAX_COMPARE_FIBRAS)
}

export function normalizeCompareTickers(values: string[], max = Number.POSITIVE_INFINITY): string[] {
  const seen = new Set<string>()
  const normalized: string[] = []

  for (const raw of values) {
    const ticker = raw.trim().toUpperCase()
    if (!ticker || seen.has(ticker)) continue
    seen.add(ticker)
    normalized.push(ticker)
    if (normalized.length >= max) break
  }

  return normalized
}

export function serializeCompareTickers(values: string[]): string {
  return normalizeCompareTickers(values, MAX_COMPARE_FIBRAS).join(',')
}

export function formatCompareNumber(value: string | number | null | undefined, digits = 2): string {
  if (value == null) return '—'
  const n = Number(value)
  return Number.isFinite(n) ? n.toFixed(digits) : '—'
}

export function formatComparePercent(value: string | number | null | undefined, digits = 1): string {
  if (value == null) return '—'
  const n = Number(value)
  return Number.isFinite(n) ? `${n.toFixed(digits)}%` : '—'
}

export function formatCompareVolume(value: string | number | null | undefined): string {
  if (value == null) return '—'
  const n = Number(value)
  return Number.isFinite(n) ? new Intl.NumberFormat('es-MX').format(n) : '—'
}

export function compareTableMinWidth(selectedCount: number): string {
  if (selectedCount <= 2) return '36rem'
  if (selectedCount === 3) return '44rem'
  return '56rem'
}

export function parseCompareBenchmarks(value: string | null | undefined): CompareBenchmark[] {
  return normalizeCompareBenchmarks(value?.split(',') ?? [])
}

export function normalizeCompareBenchmarks(values: string[]): CompareBenchmark[] {
  const seen = new Set<CompareBenchmark>()
  const normalized: CompareBenchmark[] = []

  for (const raw of values) {
    const benchmark = raw.trim().toLowerCase()
    if (!benchmark || !BENCHMARK_OPTIONS.includes(benchmark as CompareBenchmark)) continue
    if (seen.has(benchmark as CompareBenchmark)) continue
    seen.add(benchmark as CompareBenchmark)
    normalized.push(benchmark as CompareBenchmark)
  }

  return normalized
}

export function serializeCompareBenchmarks(values: string[]): string {
  return normalizeCompareBenchmarks(values).join(',')
}
