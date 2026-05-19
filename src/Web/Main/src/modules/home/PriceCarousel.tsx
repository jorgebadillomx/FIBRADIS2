import { useQuery } from '@tanstack/react-query'
import { fetchMarketSnapshots, fetchAllFibras } from '@/api/fibrasApi'
import { FreshnessBadge } from '@/shared/ui/freshness-badge'
import type { FreshnessStatus } from '@/shared/ui/freshness-badge'
import { toNum, formatRelativeTime } from '@/shared/lib/format-time'

export function PriceCarousel() {
  const { data: snapshots = [], isLoading } = useQuery({
    queryKey: ['market-snapshots'],
    queryFn: fetchMarketSnapshots,
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  })

  const { data: fibras = [] } = useQuery({
    queryKey: ['fibras'],
    queryFn: fetchAllFibras,
    staleTime: 5 * 60_000,
  })

  const fibraByTicker = Object.fromEntries(fibras.map(f => [f.ticker, f]))

  if (isLoading) {
    return (
      <div aria-label="Carrusel de precios" className="relative">
        <div className="flex gap-3 overflow-x-auto pb-1 scrollbar-none">
          {Array.from({ length: 9 }).map((_, i) => (
            <div
              key={i}
              className="shrink-0 rounded-xl border border-border bg-surface-elevated p-3 w-36 space-y-2 animate-pulse"
            >
              <div className="h-3 w-14 bg-muted rounded" />
              <div className="h-6 w-20 bg-muted rounded" />
              <div className="h-2.5 w-10 bg-muted rounded" />
            </div>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div aria-label="Carrusel de precios" className="relative">
      <div className="flex gap-3 overflow-x-auto pb-1 scrollbar-none">
        {snapshots.map((snap) => {
          const fibra = fibraByTicker[snap.ticker]
          const lastPrice = toNum(snap.lastPrice)
          const changePct = toNum(snap.dailyChangePct)
          const hasPrice = lastPrice != null && snap.freshnessStatus != null
          return (
            <a
              key={snap.ticker}
              href={`/fibras/${snap.ticker}`}
              className="shrink-0 rounded-xl border border-border bg-surface-elevated p-3 w-36 space-y-1.5 hover:border-brand/50 transition-colors"
            >
              <p className="text-xs font-semibold">{snap.ticker}</p>
              {fibra?.shortName && (
                <p className="text-xs text-muted-foreground truncate">{fibra.shortName}</p>
              )}
              {hasPrice ? (
                <>
                  <p className="text-lg font-bold tabular-nums">
                    {lastPrice!.toFixed(2)}
                  </p>
                  <div className="flex items-center gap-1 flex-wrap">
                    {changePct != null && (
                      <span className={`text-xs font-medium ${changePct >= 0 ? 'text-positive' : 'text-negative'}`}>
                        {changePct >= 0 ? '+' : ''}{changePct.toFixed(2)}%
                      </span>
                    )}
                    <FreshnessBadge
                      status={snap.freshnessStatus as FreshnessStatus}
                      lastUpdated={snap.capturedAt ? formatRelativeTime(snap.capturedAt) : undefined}
                    />
                  </div>
                </>
              ) : (
                <p className="text-lg font-bold tabular-nums text-muted-foreground">—</p>
              )}
            </a>
          )
        })}
      </div>
    </div>
  )
}
