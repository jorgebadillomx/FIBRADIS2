import { useQuery } from '@tanstack/react-query'
import { fetchMarketSnapshots } from '@/api/fibrasApi'
import { toNum } from '@/shared/lib/format-time'
import { splitGainersLosers } from './movers-logic'

export function GainersLosers() {
  const { data: snapshots = [], isLoading } = useQuery({
    queryKey: ['market-snapshots'],
    queryFn: fetchMarketSnapshots,
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  })

  const { gainers, losers } = splitGainersLosers(snapshots, 5)
  const isEmpty = !isLoading && gainers.length === 0 && losers.length === 0

  return (
    <div aria-label="Ganadores y perdedores del día" className="rounded-xl border border-border bg-surface-elevated overflow-hidden">
      <div className="px-4 pt-4 pb-2 flex items-center gap-3">
        <div>
          <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Ganadores / Perdedores del día</h3>
          <p className="text-xs text-muted-foreground/60 mt-0.5">Top 5 por variación % hoy</p>
        </div>
        <div className="flex-1 h-px bg-border" />
      </div>

      {isLoading ? (
        <div className="grid grid-cols-2 divide-x divide-border animate-pulse">
          {[0, 1].map(col => (
            <div key={col} className="px-4 py-3 space-y-3">
              <div className="h-3 w-16 bg-muted rounded" />
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="flex justify-between">
                  <div className="h-3 w-14 bg-muted rounded" />
                  <div className="h-3 w-12 bg-muted rounded" />
                </div>
              ))}
            </div>
          ))}
        </div>
      ) : isEmpty ? (
        <p className="px-4 py-8 text-center text-sm text-muted-foreground">Sin datos suficientes hoy</p>
      ) : (
        <div className="grid grid-cols-2 divide-x divide-border">
          {/* Ganadores */}
          <div>
            <div className="px-4 py-2 border-b border-border">
              <span className="text-xs font-semibold text-positive uppercase tracking-wider">Ganadores</span>
            </div>
            <div className="divide-y divide-border">
              {gainers.length === 0 ? (
                <p className="px-4 py-3 text-xs text-muted-foreground">Sin datos</p>
              ) : (
                gainers.map(snap => {
                  const changePct = toNum(snap.dailyChangePct)
                  return (
                    <a
                      key={snap.ticker}
                      href={`/fibras/${snap.ticker}`}
                      className="px-4 py-2.5 flex items-center justify-between gap-3 hover:bg-muted/30 transition-colors"
                    >
                      <span className="text-sm font-semibold">{snap.ticker}</span>
                      <span className="text-sm tabular-nums font-medium text-positive">
                        {changePct != null ? `+${changePct.toFixed(2)}%` : '—'}
                      </span>
                    </a>
                  )
                })
              )}
            </div>
          </div>

          {/* Perdedores */}
          <div>
            <div className="px-4 py-2 border-b border-border">
              <span className="text-xs font-semibold text-negative uppercase tracking-wider">Perdedores</span>
            </div>
            <div className="divide-y divide-border">
              {losers.length === 0 ? (
                <p className="px-4 py-3 text-xs text-muted-foreground">Sin datos</p>
              ) : (
                losers.map(snap => {
                  const changePct = toNum(snap.dailyChangePct)
                  return (
                    <a
                      key={snap.ticker}
                      href={`/fibras/${snap.ticker}`}
                      className="px-4 py-2.5 flex items-center justify-between gap-3 hover:bg-muted/30 transition-colors"
                    >
                      <span className="text-sm font-semibold">{snap.ticker}</span>
                      <span className="text-sm tabular-nums font-medium text-negative">
                        {changePct != null ? `${changePct.toFixed(2)}%` : '—'}
                      </span>
                    </a>
                  )
                })
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
