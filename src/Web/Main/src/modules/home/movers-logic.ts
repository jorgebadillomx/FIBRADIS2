export interface Snapshot {
  ticker: string
  dailyChangePct: null | number | string
  lastPrice: null | number | string
  volume: null | number | string
}

function numOf(val: null | number | string | undefined): number | null {
  if (val == null) return null
  const n = typeof val === 'string' ? parseFloat(val) : val
  return isNaN(n) ? null : n
}

export function formatVolume(vol: number): string {
  if (vol >= 1_000_000) return `${(vol / 1_000_000).toFixed(1)}M`
  if (vol >= 1_000) return `${(vol / 1_000).toFixed(0)}K`
  return vol.toLocaleString()
}

export function getTopMovers<T extends Snapshot>(snapshots: T[], n: number): T[] {
  const hasAny = snapshots.some(s => numOf(s.dailyChangePct) != null)
  if (hasAny) {
    return [...snapshots]
      .sort((a, b) => {
        const va = numOf(a.dailyChangePct)
        const vb = numOf(b.dailyChangePct)
        const absA = va != null ? Math.abs(va) : -1
        const absB = vb != null ? Math.abs(vb) : -1
        const diff = absB - absA
        return diff !== 0 ? diff : a.ticker.localeCompare(b.ticker)
      })
      .slice(0, n)
  }
  return [...snapshots].sort((a, b) => a.ticker.localeCompare(b.ticker)).slice(0, n)
}

export function splitGainersLosers<T extends Snapshot>(
  snapshots: T[],
  n: number,
): { gainers: T[]; losers: T[] } {
  const gainers = snapshots
    .filter(s => (numOf(s.dailyChangePct) ?? 0) > 0)
    .sort((a, b) => {
      const diff = (numOf(b.dailyChangePct) ?? 0) - (numOf(a.dailyChangePct) ?? 0)
      return diff !== 0 ? diff : a.ticker.localeCompare(b.ticker)
    })
    .slice(0, n)

  const losers = snapshots
    .filter(s => (numOf(s.dailyChangePct) ?? 0) < 0)
    .sort((a, b) => {
      const diff = (numOf(a.dailyChangePct) ?? 0) - (numOf(b.dailyChangePct) ?? 0)
      return diff !== 0 ? diff : a.ticker.localeCompare(b.ticker)
    })
    .slice(0, n)

  return { gainers, losers }
}
