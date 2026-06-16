import type { FreshnessStatus } from '@/shared/ui/freshness-badge'
import { formatVolume } from './movers-logic.ts'

export type SortKey = 'lastPrice' | 'dailyChange' | 'dailyChangePct' | 'volume' | 'week52High' | 'week52Low' | 'annualizedYield'

export type UniverseChangeTone = 'negative' | 'neutral' | 'positive'

export interface SnapshotRow {
  ticker: string
  lastPrice: null | number | string
  dailyChange: null | number | string
  dailyChangePct: null | number | string
  volume: null | number | string
  week52High: null | number | string
  week52Low: null | number | string
  annualizedYield: null | number | string
}

export interface UniverseMobileCardData {
  summary: {
    ticker: string
    price: string
    change: string
    changePct: string
    changeTone: UniverseChangeTone
    freshnessStatus: FreshnessStatus | null
    latestPeriod: string
  }
  details: {
    volume: string
    rangePct: number | null
    week52High: string
    week52Low: string
    annualizedYield: string
  }
}

function numOf(val: null | number | string | undefined): number | null {
  if (val == null) return null
  const n = typeof val === 'string' ? parseFloat(val) : val
  return isNaN(n) ? null : n
}

export function filterSnapshots<T extends { ticker: string }>(snapshots: T[], text: string): T[] {
  const q = text.trim().toLowerCase()
  if (!q) return snapshots
  return snapshots.filter(s => s.ticker.toLowerCase().includes(q))
}

export function sortSnapshots<T extends SnapshotRow>(
  snapshots: T[],
  key: SortKey | null,
  dir: 'asc' | 'desc',
): T[] {
  if (!key) return [...snapshots]
  return [...snapshots].sort((a, b) => {
    const va = numOf(a[key] as null | number | string)
    const vb = numOf(b[key] as null | number | string)
    if (va == null && vb == null) return 0
    if (va == null) return 1
    if (vb == null) return -1
    const diff = dir === 'asc' ? va - vb : vb - va
    return diff !== 0 ? diff : a.ticker.localeCompare(b.ticker)
  })
}

export function calcRange52Pct(
  lastPrice: number | null,
  high: number | null,
  low: number | null,
): number | null {
  if (lastPrice == null || high == null || low == null) return null
  if (high === low) return null
  return Math.min(1, Math.max(0, (lastPrice - low) / (high - low)))
}

function formatNumber(value: number | null, digits = 2, suffix = ''): string {
  if (value == null) return '—'
  return `${value.toFixed(digits)}${suffix}`
}

function formatSignedNumber(value: number | null, digits = 2, suffix = ''): string {
  if (value == null) return '—'
  return `${value >= 0 ? '+' : ''}${value.toFixed(digits)}${suffix}`
}

export function buildUniverseMobileCardData(params: {
  dailyChange: number | null
  dailyChangePct: number | null
  freshnessStatus: FreshnessStatus | null
  lastPrice: number | null
  latestPeriod: string | null
  rangePct: number | null
  volume: number | null
  week52High: number | null
  week52Low: number | null
  annualizedYield: number | null
  ticker: string
}): UniverseMobileCardData {
  return {
    summary: {
      ticker: params.ticker,
      price: formatNumber(params.lastPrice),
      change: formatSignedNumber(params.dailyChange),
      changePct: formatSignedNumber(params.dailyChangePct, 2, '%'),
      changeTone:
        params.dailyChangePct == null
          ? 'neutral'
          : params.dailyChangePct > 0
            ? 'positive'
            : params.dailyChangePct < 0
              ? 'negative'
              : 'neutral',
      freshnessStatus: params.freshnessStatus,
      latestPeriod: params.latestPeriod ?? '—',
    },
    details: {
      volume: params.volume == null ? '—' : formatVolume(params.volume),
      rangePct: params.rangePct,
      week52High: formatNumber(params.week52High),
      week52Low: formatNumber(params.week52Low),
      annualizedYield: formatNumber(params.annualizedYield, 2, '%'),
    },
  }
}
