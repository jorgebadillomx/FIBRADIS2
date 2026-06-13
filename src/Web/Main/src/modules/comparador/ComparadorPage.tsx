import { Fragment, useMemo, useState, type ReactNode } from 'react'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { LoaderCircle, Plus, RefreshCw, Search, X } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import type { components } from '@fibradis/shared-api-client'
import { useSearchParams } from 'react-router'
import { fetchAllFibras } from '@/api/fibrasApi'
import {
  MAX_COMPARE_FIBRAS,
  MIN_COMPARE_FIBRAS,
  parseCompareBenchmarks,
  compareTableMinWidth,
  formatCompareNumber,
  formatComparePercent,
  formatCompareVolume,
  parseCompareTickers,
  serializeCompareBenchmarks,
  serializeCompareTickers,
} from './comparador-logic'
import { fetchComparacion } from './comparadorApi'

type ComparadorFibraDto = components['schemas']['ComparadorFibraDto']

type CompareMetric = {
  label: string
  render: (row: ComparadorFibraDto) => ReactNode
}

type CompareSection = {
  title: string
  rows: CompareMetric[]
}

const COMPARISON_SECTIONS: CompareSection[] = [
  {
    title: 'Mercado',
    rows: [
      {
        label: 'Precio actual (MXN)',
        render: (row) => formatMoney(row.mercado.precioActual),
      },
      {
        label: 'Cambio día (%)',
        render: (row) => formatComparePercent(row.mercado.cambiaDiaPct, 2),
      },
      {
        label: 'Promedio 52S (MXN)',
        render: (row) => formatMoney(row.mercado.avg52S),
      },
      {
        label: 'Volumen',
        render: (row) => formatCompareVolume(row.mercado.volumen),
      },
    ],
  },
  {
    title: 'Fundamentales',
    rows: [
      {
        label: 'Período del reporte',
        render: (row) => row.fundamentales.periodo ?? '—',
      },
      {
        label: 'Cap Rate (%)',
        render: (row) => formatComparePercent(row.fundamentales.capRate, 1),
      },
      {
        label: 'NAV por CBFI (MXN)',
        render: (row) => formatMoney(row.fundamentales.navPerCbfi),
      },
      {
        label: 'LTV (%)',
        render: (row) => formatComparePercent(row.fundamentales.ltv, 1),
      },
      {
        label: 'Margen NOI (%)',
        render: (row) => formatComparePercent(row.fundamentales.noiMargin, 1),
      },
      {
        label: 'Margen FFO (%)',
        render: (row) => formatComparePercent(row.fundamentales.ffoMargin, 1),
      },
    ],
  },
  {
    title: 'Distribuciones',
    rows: [
      {
        label: 'Distribución trimestral (MXN)',
        render: (row) => formatMoney(row.distribuciones.distribucionTrimestral),
      },
      {
        label: 'Yield calculado anual (%)',
        render: (row) => formatComparePercent(row.distribuciones.yieldCalculadoPct, 2),
      },
      {
        label: 'Yield decretado anual (%)',
        render: (row) => formatComparePercent(row.distribuciones.yieldDecretadoPct, 2),
      },
    ],
  },
  {
    title: 'Score público',
    rows: [
      {
        label: 'Score de oportunidad',
        render: (row) => renderOverallScore(row.score),
      },
      {
        label: 'NAV Descuento',
        render: (row) => renderScoreComponent(row.score.navDescuentoScore, row.score.isExcluded),
      },
      {
        label: 'Dividend Yield',
        render: (row) => renderScoreComponent(row.score.dividendYieldScore, row.score.isExcluded),
      },
      {
        label: 'LTV',
        render: (row) => renderScoreComponent(row.score.ltvScore, row.score.isExcluded),
      },
      {
        label: 'NOI Margin',
        render: (row) => renderScoreComponent(row.score.noiMarginScore, row.score.isExcluded),
      },
      {
        label: 'Price vs 52S',
        render: (row) => renderScoreComponent(row.score.priceVs52wScore, row.score.isExcluded),
      },
    ],
  },
]

export function ComparadorPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [search, setSearch] = useState('')
  const [isSearchFocused, setIsSearchFocused] = useState(false)

  const queryTickers = searchParams.get('fibras')
  const queryBenchmarks = searchParams.get('benchmarks')
  const selectedTickers = useMemo(() => parseCompareTickers(queryTickers), [queryTickers])
  const selectedBenchmarks = useMemo(() => parseCompareBenchmarks(queryBenchmarks), [queryBenchmarks])
  const selectedSet = useMemo(() => new Set(selectedTickers), [selectedTickers])
  const benchmarkSet = useMemo(() => new Set(selectedBenchmarks), [selectedBenchmarks])
  const selectedCount = selectedTickers.length

  const { data: fibras = [], isLoading: fibrasLoading } = useQuery({
    queryKey: ['fibras', 'compare-selector'],
    queryFn: fetchAllFibras,
    staleTime: Infinity,
  })

  const { data: comparisonRows = [], isLoading: comparisonLoading, isError, refetch } = useQuery({
    queryKey: ['compare', selectedTickers.join(',')],
    queryFn: () => fetchComparacion(selectedTickers),
    enabled: selectedCount >= MIN_COMPARE_FIBRAS,
    staleTime: 60_000,
  })

  const fibrasByTicker = useMemo(() => {
    return new Map(fibras.map((fibra) => [fibra.ticker.toUpperCase(), fibra]))
  }, [fibras])

  const suggestionRows = useMemo(() => {
    const term = search.trim().toLowerCase()
    const pool = fibras.filter((fibra) => !selectedSet.has(fibra.ticker.toUpperCase()))

    if (term.length === 0) {
      return pool.slice(0, 8)
    }

    return pool
      .filter((fibra) =>
        fibra.ticker.toLowerCase().includes(term) ||
        fibra.fullName.toLowerCase().includes(term) ||
        fibra.shortName.toLowerCase().includes(term),
      )
      .slice(0, 8)
  }, [fibras, search, selectedSet])

  const comparisonMinWidth = compareTableMinWidth(Math.max(selectedCount, 2))

  function updateSelection(nextTickers: string[], nextBenchmarks = selectedBenchmarks) {
    const params = new URLSearchParams()
    const fibras = serializeCompareTickers(nextTickers)
    const benchmarks = serializeCompareBenchmarks(nextBenchmarks)

    if (fibras) params.set('fibras', fibras)
    if (benchmarks) params.set('benchmarks', benchmarks)
    setSearchParams(params, { replace: true })
  }

  function addTicker(ticker: string) {
    if (selectedCount >= MAX_COMPARE_FIBRAS) return
    const nextCount = selectedCount + 1
    updateSelection([...selectedTickers, ticker])
    setSearch('')
    setIsSearchFocused(nextCount < MAX_COMPARE_FIBRAS)
  }

  function removeTicker(ticker: string) {
    if (selectedCount <= MIN_COMPARE_FIBRAS) return
    updateSelection(selectedTickers.filter((item) => item !== ticker))
  }

  function toggleBenchmark(benchmark: 'ipc' | 'sp500') {
    const next = benchmarkSet.has(benchmark)
      ? selectedBenchmarks.filter((item) => item !== benchmark)
      : [...selectedBenchmarks, benchmark]
    updateSelection(selectedTickers, next)
  }

  const selectorDisabled = selectedCount >= MAX_COMPARE_FIBRAS
  const canRemove = selectedCount > MIN_COMPARE_FIBRAS
  const hasComparisonError = isError && selectedCount >= MIN_COMPARE_FIBRAS
  const loadingComparison = comparisonLoading && selectedCount >= MIN_COMPARE_FIBRAS
  const showSuggestions = isSearchFocused && suggestionRows.length > 0

  usePageTitle('Comparar FIBRAs Inmobiliarias — Análisis Comparativo | FIBRADIS')

  return (
    <>
      <div className="container mx-auto px-4 py-8 space-y-8">
        <header className="space-y-3">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-primary">
            Análisis comparativo
          </p>
          <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
            Comparador de FIBRAs
          </h1>
          <p className="max-w-3xl text-sm leading-6 text-muted-foreground md:text-base">
            Selecciona de 2 a 4 emisoras para compararlas en mercado, fundamentales, distribuciones y score público.
          </p>
        </header>

        <section className="rounded-2xl border border-border bg-surface-elevated p-4 shadow-sm">
          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
              <div className="relative w-full max-w-2xl">
                <label className="space-y-1.5">
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Agregar FIBRA
                  </span>
                  <div className="relative">
                    <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                    <input
                      aria-label="Buscar FIBRAs para comparar"
                      disabled={selectorDisabled}
                      onBlur={() => setIsSearchFocused(false)}
                      onChange={(event) => setSearch(event.target.value)}
                      onFocus={() => setIsSearchFocused(true)}
                      placeholder={selectorDisabled ? 'Máximo 4 FIBRAs seleccionadas' : 'Ticker o nombre...'}
                      value={search}
                      className="flex h-11 w-full rounded-xl border border-input bg-background px-10 pr-16 text-sm text-foreground placeholder:text-muted-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:bg-muted/40"
                    />
                    {selectorDisabled ? (
                      <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 rounded-full border border-border bg-muted px-2 py-0.5 text-[10px] font-semibold uppercase tracking-[0.2em] text-muted-foreground">
                        Límite 4
                      </span>
                    ) : null}
                  </div>
                </label>

                {showSuggestions ? (
                  <div className="absolute z-20 mt-2 max-h-72 w-full overflow-auto rounded-xl border border-border bg-background shadow-lg">
                    {suggestionRows.map((fibra) => (
                      <button
                        key={fibra.ticker}
                        type="button"
                        onMouseDown={(event) => event.preventDefault()}
                        onClick={() => addTicker(fibra.ticker)}
                        className="flex w-full items-center justify-between gap-3 border-b border-border px-4 py-3 text-left text-sm transition-colors last:border-b-0 hover:bg-muted/50"
                      >
                        <div className="min-w-0">
                          <div className="flex items-center gap-2">
                            <span className="font-mono font-semibold text-primary">{fibra.ticker}</span>
                            <span className="text-xs text-muted-foreground">Agregar</span>
                          </div>
                          <p className="truncate text-xs text-muted-foreground">{fibra.fullName}</p>
                        </div>
                        <Plus className="size-4 shrink-0 text-muted-foreground" />
                      </button>
                    ))}
                  </div>
                ) : null}

                <p className="mt-2 text-xs text-muted-foreground">
                  {fibrasLoading
                    ? 'Cargando universo activo...'
                    : 'Usa ticker o nombre. La selección se refleja en la URL.'}
                </p>
              </div>

              <div className="flex flex-wrap items-center gap-2">
                <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Seleccionadas
                </span>
                <span className="rounded-full border border-border bg-background px-2.5 py-1 text-xs font-semibold text-foreground">
                  {selectedCount}
                </span>
                {hasComparisonError ? (
                  <button
                    type="button"
                    onClick={() => refetch()}
                    className="inline-flex items-center gap-1.5 rounded-lg border border-border px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground hover:border-primary"
                  >
                    <RefreshCw className="size-4" />
                    Reintentar
                  </button>
                ) : null}
              </div>
            </div>

            <div className="flex flex-wrap gap-2">
              {selectedTickers.map((ticker) => {
                const fibra = fibrasByTicker.get(ticker)
                const label = fibra?.shortName ?? fibra?.fullName ?? 'Ticker no encontrado'

                return (
                  <div
                    key={ticker}
                    className="inline-flex max-w-full items-center gap-2 rounded-full border border-border bg-background px-3 py-1.5"
                  >
                    <div className="min-w-0">
                      <span className="block font-mono text-xs font-semibold text-primary">{ticker}</span>
                      <span className="block max-w-[12rem] truncate text-xs text-muted-foreground sm:max-w-[18rem]">
                        {label}
                      </span>
                    </div>
                    <button
                      type="button"
                      disabled={!canRemove}
                      title={
                        canRemove
                          ? `Quitar ${ticker}`
                          : 'Se requieren al menos 2 FIBRAs para mantener la comparación'
                      }
                      onClick={() => removeTicker(ticker)}
                      className="rounded-full p-1 text-muted-foreground transition-colors hover:bg-muted/70 hover:text-foreground disabled:cursor-not-allowed disabled:opacity-40"
                    >
                      <X className="size-3.5" />
                    </button>
                  </div>
                )
              })}
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Benchmarks
              </span>
              {([
                { key: 'ipc', label: 'IPC BMV' },
                { key: 'sp500', label: 'S&P 500' },
              ] as const).map((benchmark) => {
                const active = benchmarkSet.has(benchmark.key)
                return (
                  <button
                    key={benchmark.key}
                    type="button"
                    onClick={() => toggleBenchmark(benchmark.key)}
                    className={`rounded-full border px-3 py-1.5 text-xs font-medium transition-colors ${
                      active
                        ? 'border-primary bg-primary/10 text-primary'
                        : 'border-border bg-background text-muted-foreground hover:text-foreground'
                    }`}
                  >
                    {benchmark.label}
                  </button>
                )
              })}
            </div>
          </div>
        </section>

        {selectedCount < MIN_COMPARE_FIBRAS ? (
          <section className="rounded-2xl border border-dashed border-border bg-background px-6 py-10 text-center">
            <p className="text-sm font-medium text-foreground">
              Selecciona al menos dos FIBRAs para ver la comparación.
            </p>
            <p className="mt-1 text-sm text-muted-foreground">
              Puedes agregar emisoras desde el buscador superior.
            </p>
          </section>
        ) : hasComparisonError ? (
          <section className="rounded-2xl border border-rose-200 bg-rose-50 px-6 py-10 text-center text-sm text-rose-800">
            <p className="font-medium">No se pudo cargar la comparación.</p>
            <p className="mt-1 text-rose-700">
              Revisa la selección actual o intenta nuevamente.
            </p>
            <button
              type="button"
              onClick={() => refetch()}
              className="mt-4 inline-flex items-center gap-2 rounded-lg border border-rose-200 bg-white px-4 py-2 text-sm font-medium text-rose-700 transition-colors hover:bg-rose-100"
            >
              <RefreshCw className="size-4" />
              Reintentar
            </button>
          </section>
        ) : loadingComparison ? (
          <section className="rounded-2xl border border-border bg-surface-elevated p-4 shadow-sm">
            <div className="flex items-center gap-3 border-b border-border px-4 py-3">
              <LoaderCircle className="size-4 animate-spin text-primary" />
              <span className="text-sm text-muted-foreground">Cargando comparación...</span>
            </div>
            <div className="space-y-3 px-4 py-4">
              {Array.from({ length: 8 }).map((_, index) => (
                <div key={index} className="grid grid-cols-1 gap-3 md:grid-cols-[15rem_minmax(0,1fr)_minmax(0,1fr)]">
                  <div className="h-4 w-40 animate-pulse rounded bg-muted" />
                  <div className="h-4 animate-pulse rounded bg-muted" />
                  <div className="h-4 animate-pulse rounded bg-muted" />
                </div>
              ))}
            </div>
          </section>
        ) : (
          <section className="rounded-2xl border border-border bg-surface-elevated shadow-sm">
            <div className="overflow-x-auto">
              <table className="w-full table-fixed text-sm" style={{ minWidth: comparisonMinWidth }}>
                <colgroup>
                  <col className="w-[15rem]" />
                  {comparisonRows.map((row) => (
                    <col key={row.ticker} />
                  ))}
                </colgroup>
                <thead>
                  <tr className="border-b border-border bg-muted/30 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    <th className="px-4 py-3 text-left">Métrica</th>
                    {comparisonRows.map((row) => (
                      <th key={row.ticker} className="px-4 py-3 text-right align-top">
                        <div className="space-y-1">
                          <span className="block font-mono text-sm font-semibold text-primary">
                            {row.ticker}
                          </span>
                          <span className="block max-w-[10rem] truncate text-xs font-normal uppercase tracking-normal text-muted-foreground">
                            {row.nombre}
                          </span>
                        </div>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {COMPARISON_SECTIONS.map((section) => (
                    <Fragment key={section.title}>
                      <tr>
                        <th
                          colSpan={comparisonRows.length + 1}
                          className="border-b border-border bg-muted/20 px-4 py-2 text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground"
                        >
                          {section.title}
                        </th>
                      </tr>
                      {section.rows.map((metric) => (
                        <tr key={`${section.title}-${metric.label}`} className="border-b border-border last:border-0">
                          <th className="px-4 py-3 text-left font-medium text-foreground">
                            {metric.label}
                          </th>
                          {comparisonRows.map((row) => (
                            <td key={`${section.title}-${metric.label}-${row.ticker}`} className="px-4 py-3 text-right">
                              {metric.render(row)}
                            </td>
                          ))}
                        </tr>
                      ))}
                    </Fragment>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        )}
      </div>
    </>
  )
}

function formatMoney(value: string | number | null | undefined): string {
  return value == null ? '—' : `$${formatCompareNumber(value, 2)}`
}

function toNum(value: string | number | null | undefined): number | null {
  if (value == null) return null
  const n = Number(value)
  return Number.isFinite(n) ? n : null
}

function renderScoreComponent(value: string | number | null | undefined, isExcluded = false): ReactNode {
  const n = toNum(value)
  if (isExcluded || n == null) return <span className="text-muted-foreground">—</span>

  return (
    <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold tabular-nums ${scoreToneClass(n)}`}>
      {n.toFixed(1)}
    </span>
  )
}

function renderOverallScore(score: ComparadorFibraDto['score']): ReactNode {
  const n = toNum(score.score)
  if (score.isExcluded || n == null) {
    return <span className="text-muted-foreground">—</span>
  }

  return (
    <div className="flex items-center justify-end gap-2">
      <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold tabular-nums ${scoreToneClass(n)}`}>
        {n.toFixed(1)}
      </span>
      {score.isLimitedData ? (
        <span className="rounded-full border border-amber-200 bg-amber-50 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-[0.18em] text-amber-700">
          Datos limitados
        </span>
      ) : null}
    </div>
  )
}

function scoreToneClass(score: number): string {
  if (score >= 65) return 'bg-emerald-100 text-emerald-800'
  if (score >= 35) return 'bg-amber-100 text-amber-800'
  return 'bg-rose-100 text-rose-800'
}
