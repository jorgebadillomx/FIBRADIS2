import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { toNum } from '@/shared/lib/format-time'
import { fetchFibraHistory } from '@/api/fibrasApi'
import { PriceChart } from '@/shared/ui/price-chart'

const SELECTORS = ['1M', '3M', '6M', '1A'] as const
type Selector = typeof SELECTORS[number]
const PERIOD_MAP: Record<Selector, '1m' | '3m' | '6m' | '1y'> = {
  '1M': '1m', '3M': '3m', '6M': '6m', '1A': '1y',
}

interface MercadoSectionProps {
  ticker: string
  week52High: number | string | null | undefined
  week52Low: number | string | null | undefined
  volume: number | string | null | undefined
}

function formatVolume(vol: number): string {
  if (vol >= 1_000_000) return `${(vol / 1_000_000).toFixed(1)}M`
  if (vol >= 1_000) return `${(vol / 1_000).toFixed(0)}K`
  return vol.toLocaleString()
}

export function MercadoSection({ ticker, week52High, week52Low, volume }: MercadoSectionProps) {
  const [active, setActive] = useState<Selector>('1M')
  const [period, setPeriod] = useState<'1m' | '3m' | '6m' | '1y'>('1m')

  const high = toNum(week52High)
  const low = toNum(week52Low)
  const vol = toNum(volume)

  const { data: history, isLoading: isLoadingHistory, isError: isHistoryError } = useQuery({
    queryKey: ['fibra-history', ticker, period],
    queryFn: () => fetchFibraHistory(ticker, period),
    staleTime: 5 * 60_000,
    enabled: !!ticker,
  })

  return (
    <div className="space-y-4">
      {/* Métricas */}
      <div className="grid grid-cols-3 gap-3">
        <div className="rounded-lg border border-border bg-surface-elevated px-4 py-3">
          <p className="text-xs text-muted-foreground">Máx. 52 sem.</p>
          <p className="text-base font-semibold tabular-nums mt-0.5">
            {high != null ? high.toFixed(2) : '—'}
          </p>
        </div>
        <div className="rounded-lg border border-border bg-surface-elevated px-4 py-3">
          <p className="text-xs text-muted-foreground">Mín. 52 sem.</p>
          <p className="text-base font-semibold tabular-nums mt-0.5">
            {low != null ? low.toFixed(2) : '—'}
          </p>
        </div>
        <div className="rounded-lg border border-border bg-surface-elevated px-4 py-3">
          <p className="text-xs text-muted-foreground">Volumen</p>
          <p className="text-base font-semibold tabular-nums mt-0.5">
            {vol != null ? formatVolume(vol) : '—'}
          </p>
        </div>
      </div>

      {/* Selector de período */}
      <div className="flex items-center gap-2">
        {SELECTORS.map((s) => (
          <button
            key={s}
            aria-pressed={active === s ? 'true' : 'false'}
            onClick={() => {
              setActive(s)
              setPeriod(PERIOD_MAP[s])
            }}
            className={`px-3 py-1 rounded text-sm font-medium transition-colors ${
              active === s
                ? 'bg-primary text-primary-foreground'
                : 'bg-muted text-muted-foreground hover:bg-muted/80'
            }`}
          >
            {s}
          </button>
        ))}
      </div>

      {isLoadingHistory ? (
        <div className="rounded-lg border border-border bg-muted/20 animate-pulse h-48" />
      ) : isHistoryError ? (
        <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-48">
          <p className="text-sm text-muted-foreground">Error al cargar historial de precios</p>
        </div>
      ) : (
        <PriceChart data={history?.priceHistory ?? []} />
      )}
    </div>
  )
}
