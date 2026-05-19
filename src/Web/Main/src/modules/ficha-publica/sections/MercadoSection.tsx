import { useState } from 'react'
import { toNum } from '@/shared/lib/format-time'

const SELECTORS = ['1M', '3M', '6M', '1A'] as const
type Selector = typeof SELECTORS[number]

interface MercadoSectionProps {
  week52High: number | string | null | undefined
  week52Low: number | string | null | undefined
  volume: number | string | null | undefined
}

function formatVolume(vol: number): string {
  if (vol >= 1_000_000) return `${(vol / 1_000_000).toFixed(1)}M`
  if (vol >= 1_000) return `${(vol / 1_000).toFixed(0)}K`
  return vol.toLocaleString()
}

export function MercadoSection({ week52High, week52Low, volume }: MercadoSectionProps) {
  const [active, setActive] = useState<Selector>('1M')

  const high = toNum(week52High)
  const low = toNum(week52Low)
  const vol = toNum(volume)

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
            onClick={() => setActive(s)}
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

      {/* Placeholder gráfica — llega en Story 3.3 */}
      <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-48">
        <p className="text-sm text-muted-foreground">Historial de precios disponible en Historia 3.3</p>
      </div>
    </div>
  )
}
