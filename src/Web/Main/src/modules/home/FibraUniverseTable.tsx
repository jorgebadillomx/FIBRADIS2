import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronDown } from 'lucide-react'
import { fetchMarketSnapshots, fetchAllFibras } from '@/api/fibrasApi'
import { fetchFundamentalesSummary } from '@/api/fundamentalesApi'
import { toNum } from '@/shared/lib/format-time'
import { FreshnessBadge, type FreshnessStatus } from '@/shared/ui/freshness-badge'
import { FibraLogo } from '@/shared/ui/fibra-logo'
import { formatVolume } from './movers-logic'
import { useFibraSlugMap } from '@/shared/hooks/useFibraSlugMap'
import {
  filterSnapshots,
  sortSnapshots,
  calcRange52Pct,
  buildUniverseMobileCardData,
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

const MOBILE_SORT_COLUMNS: { key: SortKey | 'annualizedYield'; label: string }[] = [
  { key: 'lastPrice', label: 'Precio' },
  { key: 'dailyChange', label: 'Var $' },
  { key: 'dailyChangePct', label: 'Var %' },
  { key: 'volume', label: 'Volumen' },
  { key: 'week52High', label: 'Máx 52S' },
  { key: 'week52Low', label: 'Mín 52S' },
  { key: 'annualizedYield', label: 'Yield' },
]

function SortIcon({ active, dir }: { active: boolean; dir: 'asc' | 'desc' }) {
  if (!active) return <span className="text-xs opacity-40">⇅</span>
  return <span className="text-xs opacity-60">{dir === 'asc' ? '▲' : '▼'}</span>
}

function MetricTile({
  label,
  value,
  valueClassName = 'text-foreground',
}: {
  label: string
  value: string
  valueClassName?: string
}) {
  return (
    <div className="rounded-xl border border-border/60 bg-background/80 px-3 py-2">
      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
        {label}
      </div>
      <div className={`mt-1 text-sm font-medium tabular-nums ${valueClassName}`}>{value}</div>
    </div>
  )
}

export function FibraUniverseTable() {
  const { data: snapshots = [], isLoading, isError } = useQuery({
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
  const { data: fundamentalsSummary = [] } = useQuery({
    queryKey: ['fundamentals-summary'],
    queryFn: () => fetchFundamentalesSummary(),
    staleTime: 10 * 60_000,
  })

  const fibraByTicker = Object.fromEntries(fibras.map(f => [f.ticker, f]))
  const latestPeriodByTicker = Object.fromEntries(
    fundamentalsSummary.map(f => [f.ticker, f.period])
  )
  const { slugFor } = useFibraSlugMap()

  const [sortKey, setSortKey] = useState<SortKey | null>(null)
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc')
  const [filterText, setFilterText] = useState('')
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set())

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

  function toggleRow(ticker: string) {
    setExpandedRows(current => {
      const next = new Set(current)
      if (next.has(ticker)) {
        next.delete(ticker)
      } else {
        next.add(ticker)
      }
      return next
    })
  }

  return (
    <div
      aria-label="Universo FIBRAS"
      className="rounded-xl border border-border bg-surface-elevated overflow-hidden"
    >
      <div className="px-4 pt-4 pb-3 flex flex-col gap-3 md:flex-row md:items-center">
        <div className="min-w-0">
          <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Universo FIBRAS
          </h3>
          <p className="text-xs text-muted-foreground/60 mt-0.5">Todos los FIBRAs del mercado</p>
        </div>
        <div className="hidden h-px flex-1 bg-border md:block" />
        <input
          id="universe-filter"
          name="universeFilter"
          type="text"
          value={filterText}
          onChange={e => setFilterText(e.target.value)}
          placeholder="Filtrar por ticker..."
          aria-label="Filtrar FIBRAs del universo por ticker"
          className="w-full rounded-md border border-border bg-background px-3 py-2 text-xs text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:ring-1 focus:ring-ring md:w-36 md:px-2 md:py-1"
        />
      </div>

      <div className="flex flex-wrap gap-2 px-4 pb-3 md:hidden">
        {MOBILE_SORT_COLUMNS.map(col => (
          <button
            key={col.key}
            onClick={() => handleSort(col.key)}
            type="button"
            className={`inline-flex min-h-11 items-center gap-1 rounded-full border px-3 text-xs font-medium transition-colors cursor-pointer ${
              sortKey === col.key
                ? 'border-primary bg-primary/10 text-primary'
                : 'border-border bg-background text-muted-foreground hover:text-foreground'
            }`}
          >
            {col.label}
            <SortIcon active={sortKey === col.key} dir={sortDir} />
          </button>
        ))}
      </div>

      <div className="md:hidden">
        {isError ? (
          <p className="px-4 py-8 text-center text-sm text-muted-foreground">
            No se pudo cargar el universo FIBRAS. Intenta de nuevo más tarde.
          </p>
        ) : isLoading ? (
          <div className="divide-y divide-border">
            {Array.from({ length: 6 }).map((_, index) => (
              <div key={index} className="px-4 py-4">
                <div className="flex gap-3">
                  <div className="h-10 w-10 shrink-0 rounded-lg bg-muted" />
                  <div className="min-w-0 flex-1 space-y-3">
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0 space-y-2">
                        <div className="h-4 w-16 rounded bg-muted" />
                        <div className="h-5 w-28 rounded-full bg-muted" />
                      </div>
                      <div className="space-y-2 text-right">
                        <div className="ml-auto h-5 w-16 rounded bg-muted" />
                        <div className="ml-auto h-4 w-20 rounded bg-muted" />
                      </div>
                    </div>
                    <div className="flex items-center justify-between gap-2">
                      <div className="h-3 w-24 rounded bg-muted" />
                      <div className="h-11 w-11 rounded-full bg-muted" />
                    </div>
                    <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                      <div className="h-16 rounded-xl bg-muted" />
                      <div className="h-16 rounded-xl bg-muted" />
                    </div>
                  </div>
                </div>
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
              const yield_ = toNum(snap.annualizedYield)
              const rangePct = calcRange52Pct(lastPrice, high, low)
              const latestPeriod = latestPeriodByTicker[snap.ticker] ?? null
              const cardData = buildUniverseMobileCardData({
                ticker: snap.ticker,
                lastPrice,
                dailyChange,
                dailyChangePct,
                freshnessStatus: snap.freshnessStatus as FreshnessStatus | null,
                latestPeriod,
                rangePct,
                volume: vol,
                week52High: high,
                week52Low: low,
                annualizedYield: yield_,
              })

              const changeColor =
                cardData.summary.changeTone === 'positive'
                  ? 'text-positive'
                  : cardData.summary.changeTone === 'negative'
                    ? 'text-negative'
                    : 'text-muted-foreground'
              const isExpanded = expandedRows.has(snap.ticker)

              return (
                <article
                  key={snap.ticker}
                  data-testid="universe-mobile-card"
                  className="relative px-4 py-4 transition-colors hover:bg-muted/25"
                >
                  <div className="flex gap-3">
                    <FibraLogo
                      size="sm"
                      siteUrl={fibraByTicker[snap.ticker]?.siteUrl ?? null}
                      ticker={snap.ticker}
                    />

                    <div className="min-w-0 flex-1">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <a
                            href={`/fibras/${slugFor(snap.ticker)}`}
                            aria-label={`Abrir ficha de ${snap.ticker}`}
                            className="block truncate text-sm font-semibold outline-none after:absolute after:inset-0 after:rounded-xl after:content-[''] focus-visible:after:ring-2 focus-visible:after:ring-ring/40"
                          >
                            {cardData.summary.ticker}
                          </a>
                          <div className="mt-1 flex flex-wrap items-center gap-2">
                            {cardData.summary.freshnessStatus ? (
                              <FreshnessBadge
                                status={cardData.summary.freshnessStatus}
                              />
                            ) : (
                              <span className="text-xs text-muted-foreground">—</span>
                            )}
                            <span className="text-xs text-muted-foreground">
                              Último rep. {cardData.summary.latestPeriod}
                            </span>
                          </div>
                        </div>

                        <div className="shrink-0 text-right">
                          <div className="text-base font-semibold tabular-nums">
                            {cardData.summary.price}
                          </div>
                          <div className={`text-xs font-medium tabular-nums ${changeColor}`}>
                            {cardData.summary.change} · {cardData.summary.changePct}
                          </div>
                        </div>
                      </div>

                      <div className="mt-3 flex items-center justify-between gap-2">
                        <span className="text-xs text-muted-foreground">Toca para abrir la ficha</span>
                        <button
                          type="button"
                          aria-label={`${isExpanded ? 'Colapsar' : 'Expandir'} detalles de ${snap.ticker}`}
                          aria-expanded={isExpanded}
                          aria-controls={`universe-mobile-${snap.ticker}`}
                          onClick={() => toggleRow(snap.ticker)}
                          className="relative z-10 inline-flex h-11 w-11 items-center justify-center rounded-full border border-border bg-background text-muted-foreground transition-colors duration-200 hover:border-primary hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50 motion-reduce:transition-none cursor-pointer"
                        >
                          <ChevronDown
                            className={`size-4 transition-transform duration-200 motion-reduce:transition-none ${isExpanded ? 'rotate-180' : ''}`}
                          />
                        </button>
                      </div>

                      {isExpanded ? (
                        <div id={`universe-mobile-${snap.ticker}`} className="mt-4 space-y-3">
                          <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                            <MetricTile label="Volumen" value={cardData.details.volume} />
                            <MetricTile label="Yield" value={cardData.details.annualizedYield} />
                            <MetricTile label="Máx. 52S" value={cardData.details.week52High} />
                            <MetricTile label="Mín. 52S" value={cardData.details.week52Low} />
                          </div>

                          <div className="rounded-xl border border-border/60 bg-background/80 px-3 py-3">
                            <div className="mb-2 flex items-center justify-between text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                              <span>Rango 52S</span>
                              <span>
                                {cardData.details.week52Low} - {cardData.details.week52High}
                              </span>
                            </div>
                            {cardData.details.rangePct != null ? (
                              <div className="h-2 overflow-hidden rounded-full bg-muted">
                                <div
                                  className="h-full rounded-full bg-muted-foreground/60"
                                  style={{ width: `${(cardData.details.rangePct * 100).toFixed(1)}%` }}
                                />
                              </div>
                            ) : (
                              <span className="text-sm text-muted-foreground">—</span>
                            )}
                          </div>
                        </div>
                      ) : null}
                    </div>
                  </div>
                </article>
              )
            })}
          </div>
        )}
      </div>

      <div className="hidden md:block">
        <div className="overflow-x-auto">
          <div className="min-w-[60rem]">
            <div className="grid grid-cols-[2.5rem_minmax(5rem,1fr)_auto_auto_auto_auto_6rem_auto_auto_auto_auto_auto] gap-3 border-y border-border px-4 py-2 text-xs font-semibold text-muted-foreground">
              <span />
              <span>Emisora</span>
              {SORT_COLUMNS.slice(0, 4).map(col => (
                <button
                  key={col.key}
                  onClick={() => handleSort(col.key)}
                  type="button"
                  className="flex items-center gap-1 text-right tabular-nums transition-colors hover:text-foreground"
                >
                  {col.label}
                  <SortIcon active={sortKey === col.key} dir={sortDir} />
                </button>
              ))}
              <span className="text-right">Rango 52S</span>
              {SORT_COLUMNS.slice(4, 6).map(col => (
                <button
                  key={col.key}
                  onClick={() => handleSort(col.key)}
                  type="button"
                  className="flex items-center gap-1 text-right tabular-nums transition-colors hover:text-foreground"
                >
                  {col.label}
                  <SortIcon active={sortKey === col.key} dir={sortDir} />
                </button>
              ))}
              <button
                onClick={() => handleSort('annualizedYield')}
                type="button"
                className="flex items-center gap-1 text-right tabular-nums transition-colors hover:text-foreground"
              >
                Yield
                <SortIcon active={sortKey === 'annualizedYield'} dir={sortDir} />
              </button>
              <span className="text-right">Último Rep.</span>
              <span>Estado</span>
            </div>

            {isError ? (
              <p className="px-4 py-8 text-center text-sm text-muted-foreground">
                No se pudo cargar el universo FIBRAS. Intenta de nuevo más tarde.
              </p>
            ) : isLoading ? (
              <div className="divide-y divide-border animate-pulse">
                {Array.from({ length: 8 }).map((_, i) => (
                  <div
                    key={i}
                    className="grid grid-cols-[2.5rem_minmax(5rem,1fr)_auto_auto_auto_auto_6rem_auto_auto_auto_auto_auto] items-center gap-3 px-4 py-3"
                  >
                    <div className="h-10 w-10 shrink-0 rounded-lg bg-muted" />
                    <div className="h-3 w-16 rounded bg-muted" />
                    <div className="h-3 w-12 rounded bg-muted" />
                    <div className="h-3 w-10 rounded bg-muted" />
                    <div className="h-3 w-10 rounded bg-muted" />
                    <div className="h-3 w-12 rounded bg-muted" />
                    <div className="h-2 w-full rounded-full bg-muted" />
                    <div className="h-3 w-12 rounded bg-muted" />
                    <div className="h-3 w-12 rounded bg-muted" />
                    <div className="h-3 w-10 rounded bg-muted" />
                    <div className="h-3 w-12 rounded bg-muted" />
                    <div className="h-5 w-16 rounded-full bg-muted" />
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
                  const yield_ = toNum(snap.annualizedYield)
                  const rangePct = calcRange52Pct(lastPrice, high, low)
                  const latestPeriod = latestPeriodByTicker[snap.ticker] ?? null

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
                      href={`/fibras/${slugFor(snap.ticker)}`}
                      className="grid grid-cols-[2.5rem_minmax(5rem,1fr)_auto_auto_auto_auto_6rem_auto_auto_auto_auto_auto] items-center gap-3 px-4 py-3 transition-colors hover:bg-muted/30"
                    >
                      <FibraLogo
                        size="sm"
                        siteUrl={fibraByTicker[snap.ticker]?.siteUrl ?? null}
                        ticker={snap.ticker}
                      />

                      <span className="min-w-0">
                        <span className="block truncate text-sm font-semibold">{snap.ticker}</span>
                      </span>

                      <span className="text-right text-sm tabular-nums">
                        {lastPrice != null ? lastPrice.toFixed(2) : '—'}
                      </span>

                      <span className={`text-right text-sm font-medium tabular-nums ${changeColor}`}>
                        {dailyChange != null
                          ? `${dailyChange >= 0 ? '+' : ''}${dailyChange.toFixed(2)}`
                          : '—'}
                      </span>

                      <span className={`text-right text-sm font-medium tabular-nums ${changeColor}`}>
                        {dailyChangePct != null
                          ? `${dailyChangePct >= 0 ? '+' : ''}${dailyChangePct.toFixed(2)}%`
                          : '—'}
                      </span>

                      <span className="text-right text-sm tabular-nums text-muted-foreground">
                        {vol != null ? formatVolume(vol) : '—'}
                      </span>

                      <span className="flex items-center">
                        {rangePct != null ? (
                          <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                            <div
                              className="h-full rounded-full bg-muted-foreground/60"
                              style={{ width: `${(rangePct * 100).toFixed(1)}%` }}
                            />
                          </div>
                        ) : (
                          <span className="text-sm text-muted-foreground">—</span>
                        )}
                      </span>

                      <span className="text-right text-sm tabular-nums text-muted-foreground">
                        {high != null ? high.toFixed(2) : '—'}
                      </span>

                      <span className="text-right text-sm tabular-nums text-muted-foreground">
                        {low != null ? low.toFixed(2) : '—'}
                      </span>

                      <span className="text-right text-sm tabular-nums text-muted-foreground">
                        {yield_ != null ? `${yield_.toFixed(2)}%` : '—'}
                      </span>

                      <span className="text-right text-xs tabular-nums text-muted-foreground">
                        {latestPeriod ?? '—'}
                      </span>

                      <span>
                        {snap.freshnessStatus ? (
                          <FreshnessBadge status={snap.freshnessStatus as FreshnessStatus} />
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
        </div>
      </div>
    </div>
  )
}
