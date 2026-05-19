import { useQuery } from '@tanstack/react-query'
import { fetchMarketSnapshots } from '@/api/fibrasApi'
import { FreshnessBadge } from '@/shared/ui/freshness-badge'
import type { FreshnessStatus } from '@/shared/ui/freshness-badge'
import { toNum, formatRelativeTime } from '@/shared/lib/format-time'

export function TopMovers() {
  const { data: snapshots = [], isLoading } = useQuery({
    queryKey: ['market-snapshots'],
    queryFn: fetchMarketSnapshots,
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  })

  const hasAnyChangePct = snapshots.some(s => s.dailyChangePct != null)

  const topMovers = hasAnyChangePct
    ? [...snapshots]
        .sort((a, b) => {
          const absA = toNum(a.dailyChangePct) != null ? Math.abs(toNum(a.dailyChangePct)!) : -1
          const absB = toNum(b.dailyChangePct) != null ? Math.abs(toNum(b.dailyChangePct)!) : -1
          return absB - absA
        })
        .slice(0, 5)
    : [...snapshots]
        .sort((a, b) => a.ticker.localeCompare(b.ticker))
        .slice(0, 5)

  return (
    <div aria-label="Top movers" className="rounded-xl border border-border bg-surface-elevated overflow-hidden">
      <div className="px-4 pt-4 pb-2 flex items-center gap-3">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Movimientos del día</h3>
        <div className="flex-1 h-px bg-border" />
      </div>

      {isLoading ? (
        <div className="divide-y divide-border animate-pulse">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="px-4 py-3 flex items-center justify-between gap-4">
              <div className="space-y-1.5">
                <div className="h-3 w-16 bg-muted rounded" />
                <div className="h-2.5 w-24 bg-muted rounded" />
              </div>
              <div className="text-right space-y-1.5">
                <div className="h-3 w-14 bg-muted rounded" />
                <div className="h-2.5 w-10 bg-muted rounded ml-auto" />
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="divide-y divide-border">
          {topMovers.map((snap) => {
            const lastPrice = toNum(snap.lastPrice)
            const changePct = toNum(snap.dailyChangePct)
            return (
              <a
                key={snap.ticker}
                href={`/fibras/${snap.ticker}`}
                className="px-4 py-3 flex items-center justify-between gap-4 hover:bg-muted/30 transition-colors"
              >
                <div>
                  <p className="font-semibold text-sm">{snap.ticker}</p>
                  {snap.freshnessStatus != null && (
                    <FreshnessBadge
                      status={snap.freshnessStatus as FreshnessStatus}
                      lastUpdated={snap.capturedAt ? formatRelativeTime(snap.capturedAt) : undefined}
                    />
                  )}
                </div>
                <div className="text-right">
                  <p className="text-sm tabular-nums font-medium">
                    {lastPrice != null ? lastPrice.toFixed(2) : '—'}
                  </p>
                  {changePct != null ? (
                    <p className={`text-xs font-medium ${changePct >= 0 ? 'text-positive' : 'text-negative'}`}>
                      {changePct >= 0 ? '+' : ''}{changePct.toFixed(2)}%
                    </p>
                  ) : (
                    <p className="text-xs text-muted-foreground">—</p>
                  )}
                </div>
              </a>
            )
          })}
        </div>
      )}
    </div>
  )
}
