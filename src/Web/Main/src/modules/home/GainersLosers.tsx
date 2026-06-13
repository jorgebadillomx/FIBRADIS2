import { useQuery } from '@tanstack/react-query'
import { fetchMarketSnapshots, fetchAllFibras } from '@/api/fibrasApi'
import { toNum } from '@/shared/lib/format-time'
import { splitGainersLosers } from './movers-logic'
import { useFibraSlugMap } from '@/shared/hooks/useFibraSlugMap'
import { FibraLogo } from '@/shared/ui/fibra-logo'

export function GainersLosers() {
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
  const { slugFor } = useFibraSlugMap()

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
            <div key={col} className="px-3 py-2 space-y-2">
              <div className="h-3 w-16 bg-muted rounded" />
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="flex items-center gap-2">
                  <div className="h-10 w-10 bg-muted rounded-lg shrink-0" />
                  <div className="h-3 flex-1 bg-muted rounded" />
                  <div className="h-3 w-10 bg-muted rounded" />
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
                  const dailyChange = toNum(snap.dailyChange)
                  const changePct = toNum(snap.dailyChangePct)
                  return (
                    <a
                      key={snap.ticker}
                      href={`/fibras/${slugFor(snap.ticker)}`}
                      className="px-3 py-1.5 flex items-center gap-2 hover:bg-muted/30 transition-colors"
                    >
                      <FibraLogo size="sm" siteUrl={fibraByTicker[snap.ticker]?.siteUrl ?? null} ticker={snap.ticker} />
                      <span className="text-sm font-semibold flex-1 min-w-0 truncate">{snap.ticker}</span>
                      <span className="text-xs tabular-nums font-medium text-positive shrink-0">
                        {dailyChange != null ? `+${dailyChange.toFixed(2)}` : '—'}
                      </span>
                      <span className="text-xs tabular-nums font-medium text-positive shrink-0">
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
                  const dailyChange = toNum(snap.dailyChange)
                  const changePct = toNum(snap.dailyChangePct)
                  return (
                    <a
                      key={snap.ticker}
                      href={`/fibras/${slugFor(snap.ticker)}`}
                      className="px-3 py-1.5 flex items-center gap-2 hover:bg-muted/30 transition-colors"
                    >
                      <FibraLogo size="sm" siteUrl={fibraByTicker[snap.ticker]?.siteUrl ?? null} ticker={snap.ticker} />
                      <span className="text-sm font-semibold flex-1 min-w-0 truncate">{snap.ticker}</span>
                      <span className="text-xs tabular-nums font-medium text-negative shrink-0">
                        {dailyChange != null ? `${dailyChange.toFixed(2)}` : '—'}
                      </span>
                      <span className="text-xs tabular-nums font-medium text-negative shrink-0">
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
