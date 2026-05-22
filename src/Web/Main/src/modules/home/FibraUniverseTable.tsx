import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchMarketSnapshots } from '@/api/fibrasApi'
import { toNum } from '@/shared/lib/format-time'
import { FreshnessBadge, type FreshnessStatus } from '@/shared/ui/freshness-badge'
import { formatVolume } from './movers-logic'
import {
  filterSnapshots,
  sortSnapshots,
  calcRange52Pct,
  type SortKey,
} from './universe-table-logic'

const SORT_COLUMNS: { key: SortKey; label: string }[] = [
  { key: 'lastPrice', label: 'Precio' },
  { key: 'dailyChange', label: 'Var $' },
  { key: 'dailyChangePct', label: 'Var %' },
  { key: 'volume', label: 'Volumen' },
  { key: 'week52High', label: 'Máx 52S' },
  { key: 'week52Low', label: 'Mín 52S' },
]

function SortIcon({ active, dir }: { active: boolean; dir: 'asc' | 'desc' }) {
  if (!active) return <span className="text-xs opacity-40">⇅</span>
  return <span className="text-xs opacity-60">{dir === 'asc' ? '▲' : '▼'}</span>
}

export function FibraUniverseTable() {
  const { data: snapshots = [], isLoading, isError } = useQuery({
    queryKey: ['market-snapshots'],
    queryFn: fetchMarketSnapshots,
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  })

  const [sortKey, setSortKey] = useState<SortKey | null>(null)
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc')
  const [filterText, setFilterText] = useState('')

  function handleSort(key: SortKey) {
    if (sortKey === key) {
      setSortDir(d => (d === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortKey(key)
      setSortDir('asc')
    }
  }

  const filtered = filterSnapshots(snapshots, filterText)
  const sorted = sortSnapshots(filtered, sortKey, sortDir)

  return (
    <div
      aria-label="Universo FIBRAS"
      className="rounded-xl border border-border bg-surface-elevated overflow-hidden"
    >
      {/* Header */}
      <div className="px-4 pt-4 pb-3 flex items-center gap-3">
        <div>
          <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Universo FIBRAS
          </h3>
          <p className="text-xs text-muted-foreground/60 mt-0.5">Todos los FIBRAs del mercado</p>
        </div>
        <div className="flex-1 h-px bg-border" />
        <input
          type="text"
          value={filterText}
          onChange={e => setFilterText(e.target.value)}
          placeholder="Filtrar por ticker..."
          className="text-xs px-2 py-1 rounded-md border border-border bg-background text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:ring-1 focus:ring-ring w-36"
        />
      </div>

      {/* Column headers — order must match data row order */}
      <div className="px-4 py-2 border-y border-border grid grid-cols-[1fr_auto_auto_auto_auto_6rem_auto_auto_auto] gap-3 text-xs font-semibold text-muted-foreground">
        <span>Emisora</span>
        {SORT_COLUMNS.slice(0, 4).map(col => (
          <button
            key={col.key}
            onClick={() => handleSort(col.key)}
            type="button"
            className="flex items-center gap-1 text-right hover:text-foreground transition-colors tabular-nums"
          >
            {col.label}
            <SortIcon active={sortKey === col.key} dir={sortDir} />
          </button>
        ))}
        <span className="text-right">Rango 52S</span>
        {SORT_COLUMNS.slice(4).map(col => (
          <button
            key={col.key}
            onClick={() => handleSort(col.key)}
            type="button"
            className="flex items-center gap-1 text-right hover:text-foreground transition-colors tabular-nums"
          >
            {col.label}
            <SortIcon active={sortKey === col.key} dir={sortDir} />
          </button>
        ))}
        <span>Estado</span>
      </div>

      {/* Body */}
      {isError ? (
        <p className="px-4 py-8 text-center text-sm text-muted-foreground">
          No se pudo cargar el universo FIBRAS. Intenta de nuevo más tarde.
        </p>
      ) : isLoading ? (
        <div className="divide-y divide-border animate-pulse">
          {Array.from({ length: 8 }).map((_, i) => (
            <div
              key={i}
              className="px-4 py-3 grid grid-cols-[1fr_auto_auto_auto_auto_6rem_auto_auto_auto] gap-3 items-center"
            >
              <div className="h-3 w-16 bg-muted rounded" />
              <div className="h-3 w-12 bg-muted rounded" />
              <div className="h-3 w-10 bg-muted rounded" />
              <div className="h-3 w-10 bg-muted rounded" />
              <div className="h-3 w-12 bg-muted rounded" />
              <div className="h-2 w-full bg-muted rounded-full" />
              <div className="h-3 w-12 bg-muted rounded" />
              <div className="h-3 w-12 bg-muted rounded" />
              <div className="h-5 w-16 bg-muted rounded-full" />
            </div>
          ))}
        </div>
      ) : sorted.length === 0 ? (
        <p className="px-4 py-8 text-center text-sm text-muted-foreground">
          {filterText.trim()
            ? `Sin resultados para "${filterText.trim()}"`
            : 'Sin datos de mercado disponibles'}
        </p>
      ) : (
        <div className="divide-y divide-border">
          {sorted.map(snap => {
            const lastPrice = toNum(snap.lastPrice)
            const dailyChange = toNum(snap.dailyChange)
            const dailyChangePct = toNum(snap.dailyChangePct)
            const vol = toNum(snap.volume)
            const high = toNum(snap.week52High)
            const low = toNum(snap.week52Low)
            const rangePct = calcRange52Pct(lastPrice, high, low)

            const changeColor =
              dailyChangePct == null
                ? 'text-muted-foreground'
                : dailyChangePct > 0
                  ? 'text-positive'
                  : dailyChangePct < 0
                    ? 'text-negative'
                    : 'text-muted-foreground'

            return (
              <a
                key={snap.ticker}
                href={`/fibras/${snap.ticker}`}
                className="px-4 py-3 grid grid-cols-[1fr_auto_auto_auto_auto_6rem_auto_auto_auto] gap-3 items-center hover:bg-muted/30 transition-colors"
              >
                {/* Emisora */}
                <span className="text-sm font-semibold">{snap.ticker}</span>

                {/* Precio */}
                <span className="text-sm tabular-nums text-right">
                  {lastPrice != null ? lastPrice.toFixed(2) : '—'}
                </span>

                {/* Var $ */}
                <span className={`text-sm tabular-nums text-right font-medium ${changeColor}`}>
                  {dailyChange != null
                    ? `${dailyChange >= 0 ? '+' : ''}${dailyChange.toFixed(2)}`
                    : '—'}
                </span>

                {/* Var % */}
                <span className={`text-sm tabular-nums text-right font-medium ${changeColor}`}>
                  {dailyChangePct != null
                    ? `${dailyChangePct >= 0 ? '+' : ''}${dailyChangePct.toFixed(2)}%`
                    : '—'}
                </span>

                {/* Volumen */}
                <span className="text-sm tabular-nums text-right text-muted-foreground">
                  {vol != null ? formatVolume(vol) : '—'}
                </span>

                {/* Rango 52S */}
                <span className="flex items-center">
                  {rangePct != null ? (
                    <div className="w-full h-2 rounded-full bg-muted overflow-hidden">
                      <div
                        className="h-full bg-muted-foreground/60 rounded-full"
                        style={{ width: `${(rangePct * 100).toFixed(1)}%` }}
                      />
                    </div>
                  ) : (
                    <span className="text-sm text-muted-foreground">—</span>
                  )}
                </span>

                {/* Máx 52S */}
                <span className="text-sm tabular-nums text-right text-muted-foreground">
                  {high != null ? high.toFixed(2) : '—'}
                </span>

                {/* Mín 52S */}
                <span className="text-sm tabular-nums text-right text-muted-foreground">
                  {low != null ? low.toFixed(2) : '—'}
                </span>

                {/* Estado */}
                <span>
                  {snap.freshnessStatus ? (
                    <FreshnessBadge
                      status={snap.freshnessStatus as FreshnessStatus}
                    />
                  ) : (
                    <span className="text-sm text-muted-foreground">—</span>
                  )}
                </span>
              </a>
            )
          })}
        </div>
      )}
    </div>
  )
}
