import { useQuery } from '@tanstack/react-query'
import { fetchMarketSnapshots } from '@/api/fibrasApi'
import { toNum } from '@/shared/lib/format-time'
import { getTopMovers, formatVolume } from './movers-logic'

export function TopMovers() {
  const { data: snapshots = [], isLoading } = useQuery({
    queryKey: ['market-snapshots'],
    queryFn: fetchMarketSnapshots,
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  })

  const hasAnyChangePct = snapshots.some(s => toNum(s.dailyChangePct) != null)
  const topMovers = getTopMovers(snapshots, 5)

  return (
    <div aria-label="Top movers" className="rounded-xl border border-border bg-surface-elevated overflow-hidden">
      <div className="px-4 pt-4 pb-2 flex items-center gap-3">
        <div>
          <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Movimientos del día</h3>
          <p className="text-xs text-muted-foreground/60 mt-0.5">Top 5 con mayor variación % hoy (sube o baja)</p>
        </div>
        <div className="flex-1 h-px bg-border" />
      </div>

      <div className="px-4 py-2 border-b border-border grid grid-cols-[2rem_1fr_auto_auto_auto] gap-3 text-xs font-semibold text-muted-foreground">
        <span>#</span>
        <span>Ticker</span>
        <span className="text-right">Cambio</span>
        <span className="text-right">Precio</span>
        <span className="text-right">Volumen</span>
      </div>

      {isLoading ? (
        <div className="divide-y divide-border animate-pulse">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="px-4 py-3 grid grid-cols-[2rem_1fr_auto_auto_auto] gap-3 items-center">
              <div className="h-3 w-5 bg-muted rounded" />
              <div className="h-3 w-14 bg-muted rounded" />
              <div className="h-3 w-12 bg-muted rounded" />
              <div className="h-3 w-16 bg-muted rounded" />
              <div className="h-3 w-14 bg-muted rounded" />
            </div>
          ))}
        </div>
      ) : (
        <div className="divide-y divide-border">
          {topMovers.map((snap, idx) => {
            const lastPrice = toNum(snap.lastPrice)
            const changePct = toNum(snap.dailyChangePct)
            const vol = toNum(snap.volume)
            return (
              <a
                key={snap.ticker}
                href={`/fibras/${snap.ticker}`}
                className="px-4 py-3 grid grid-cols-[2rem_1fr_auto_auto_auto] gap-3 items-center hover:bg-muted/30 transition-colors"
              >
                <span className="text-xs tabular-nums text-muted-foreground/60 font-medium">{idx + 1}</span>
                <span className="text-sm font-semibold">{snap.ticker}</span>
                <span className={`text-sm tabular-nums text-right font-medium ${
                  changePct == null ? 'text-muted-foreground' : changePct >= 0 ? 'text-positive' : 'text-negative'
                }`}>
                  {changePct != null ? `${changePct >= 0 ? '+' : ''}${changePct.toFixed(2)}%` : '—'}
                </span>
                <span className="text-sm tabular-nums text-right">
                  {hasAnyChangePct && lastPrice != null ? lastPrice.toFixed(2) : '—'}
                </span>
                <span className="text-sm tabular-nums text-right text-muted-foreground">
                  {hasAnyChangePct && vol != null ? formatVolume(vol) : '—'}
                </span>
              </a>
            )
          })}
        </div>
      )}
    </div>
  )
}
