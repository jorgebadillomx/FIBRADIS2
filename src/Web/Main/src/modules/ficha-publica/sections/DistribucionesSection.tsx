import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchFibraHistory } from '@/api/fibrasApi'
import { toNum } from '@/shared/lib/format-time'
import {
  groupDistributionsByPeriod,
  calcPeriodDiff,
  inferDistributionCadence,
  type PeriodGroup,
} from './distribuciones'

const INITIAL_GROUPS = 8

interface DistribucionesSectionProps {
  ticker: string
}

function DiffCell({ diff }: { diff: number | null }) {
  if (diff == null) return <span className="text-muted-foreground">—</span>
  const sign = diff > 0 ? '+' : ''
  const cls = diff > 0 ? 'text-positive' : diff < 0 ? 'text-negative' : 'text-muted-foreground'
  return (
    <span className={`font-mono tabular-nums ${cls}`}>
      {sign}${Math.abs(diff).toFixed(4)}
    </span>
  )
}

function GroupRow({
  group,
  diff,
  expanded,
  onToggle,
  isAlt,
}: {
  group: PeriodGroup
  diff: number | null
  expanded: boolean
  onToggle: () => void
  isAlt: boolean
}) {
  return (
    <>
      <tr
        className={`cursor-pointer hover:bg-muted/30 transition-colors ${isAlt ? 'bg-muted/20' : ''}`}
        onClick={onToggle}
        aria-expanded={expanded ? 'true' : 'false'}
      >
        <td className="px-4 py-2 font-medium text-foreground/85 whitespace-nowrap">
          <span className="inline-flex items-center gap-1.5">
            <span className="text-[10px] text-muted-foreground select-none">{expanded ? '▼' : '▶'}</span>
            {group.label}
            {group.items.length > 1 && (
              <span className="text-[10px] text-muted-foreground">({group.items.length} pagos)</span>
            )}
          </span>
        </td>
        <td className="px-4 py-2 text-right tabular-nums font-medium">
          ${group.total.toFixed(4)}
        </td>
        <td className="px-4 py-2 text-right">
          <DiffCell diff={diff} />
        </td>
      </tr>
      {expanded && group.items.map((item) => (
        <tr key={item.date} className="bg-brand-muted/20 border-l-2 border-brand/20">
          <td className="pl-8 pr-4 py-1.5 text-xs text-muted-foreground whitespace-nowrap">
            {item.date}
          </td>
          <td className="px-4 py-1.5 text-right text-xs tabular-nums text-muted-foreground">
            ${toNum(item.amountPerUnit)?.toFixed(4) ?? '—'}
          </td>
          <td />
        </tr>
      ))}
    </>
  )
}

export function DistribucionesSection({ ticker }: DistribucionesSectionProps) {
  const [showAll, setShowAll] = useState(false)
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set())

  const { data: history, isLoading, isError } = useQuery({
    queryKey: ['fibra-history', ticker, '1y'],
    queryFn: () => fetchFibraHistory(ticker, '1y'),
    staleTime: 60 * 60_000,
    enabled: !!ticker,
  })

  const toggleGroup = (label: string) => {
    setExpandedGroups(prev => {
      const next = new Set(prev)
      if (next.has(label)) next.delete(label)
      else next.add(label)
      return next
    })
  }

  if (isLoading) {
    return (
      <div className="space-y-3 animate-pulse">
        <div className="h-16 bg-muted rounded-lg" />
        <div className="h-32 bg-muted rounded-lg" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-24">
        <p className="text-sm text-muted-foreground">Error al cargar datos de distribuciones</p>
      </div>
    )
  }

  const yieldRaw = toNum(history?.annualizedYield)
  const dists = history?.distributions ?? []
  const cadence = inferDistributionCadence(dists)
  const allGroups = groupDistributionsByPeriod(dists, cadence)
  const diffs = calcPeriodDiff(allGroups)
  const displayGroups = showAll ? allGroups : allGroups.slice(0, INITIAL_GROUPS)

  return (
    <div className="space-y-4">
      {/* Yield anualizado */}
      <div className="rounded-lg border border-border bg-surface-elevated px-4 py-3">
        <p className="text-xs text-muted-foreground mb-0.5">Yield anualizado estimado</p>
        {yieldRaw != null ? (
          <p className="text-2xl font-semibold tabular-nums">{(yieldRaw * 100).toFixed(2)}%</p>
        ) : (
          <p className="text-base text-muted-foreground">no disponible</p>
        )}
      </div>

      {/* Tabla de distribuciones */}
      {dists.length === 0 ? (
        <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-24">
          <p className="text-sm text-muted-foreground">Sin distribuciones registradas</p>
        </div>
      ) : (
        <div className="rounded-xl border border-border bg-surface-elevated overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/50 text-muted-foreground">
                <th className="text-left px-4 py-2 font-medium">Periodo</th>
                <th className="text-right px-4 py-2 font-medium">Monto por CBFI</th>
                <th className="text-right px-4 py-2 font-medium">Diferencia</th>
              </tr>
            </thead>
            <tbody>
              {displayGroups.map((group, i) => (
                <GroupRow
                  key={group.label}
                  group={group}
                  diff={diffs[i] ?? null}
                  expanded={expandedGroups.has(group.label)}
                  onToggle={() => toggleGroup(group.label)}
                  isAlt={i % 2 !== 0}
                />
              ))}
            </tbody>
          </table>
          {allGroups.length > INITIAL_GROUPS && (
            <div className="px-4 py-2 border-t border-border">
              <button
                type="button"
                aria-expanded={showAll ? 'true' : 'false'}
                onClick={() => setShowAll(prev => !prev)}
                className="text-xs text-muted-foreground hover:text-foreground underline underline-offset-2 transition-colors"
              >
                {showAll ? 'Ver menos' : `Ver historial completo (${allGroups.length} periodos)`}
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
