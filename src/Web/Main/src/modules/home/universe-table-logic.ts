export type SortKey = 'lastPrice' | 'dailyChange' | 'dailyChangePct' | 'volume' | 'week52High' | 'week52Low'

export interface SnapshotRow {
  ticker: string
  lastPrice: null | number | string
  dailyChange: null | number | string
  dailyChangePct: null | number | string
  volume: null | number | string
  week52High: null | number | string
  week52Low: null | number | string
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
