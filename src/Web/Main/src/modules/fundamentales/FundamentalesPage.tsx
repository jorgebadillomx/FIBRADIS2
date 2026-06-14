import { useMemo, useState } from 'react'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchFundamentalesSummary, fetchAllFundamentalesPeriods } from '@/api/fundamentalesApi'
import { fetchFaqItems } from '@/api/faqApi'
import { formatFundamentalValue } from '@/modules/ficha-publica/sections/fundamentales'
import { KPI_DEFINITIONS } from '@/shared/lib/kpi-definitions'
import { useFibraSlugMap } from '@/shared/hooks/useFibraSlugMap'
import { FaqAccordion } from '@/shared/ui/FaqAccordion'
import type { FundamentalesSummaryItemDto } from '@/api/fundamentalesApi'

export function FundamentalesPage() {
  const [selectedPeriod, setSelectedPeriod] = useState('')
  const [fibraFilter, setFibraFilter] = useState('')
  // una sola suscripción para toda la tabla — llamarlo por fila crea N observers y N Maps
  const { slugFor } = useFibraSlugMap()

  const isAllPeriods = selectedPeriod === 'all'

  const { data: summaryData, isLoading: isSummaryLoading } = useQuery({
    queryKey: isAllPeriods
      ? ['fundamentales', 'summary', { recent: 12 }]
      : ['fundamentales', 'summary', { period: selectedPeriod || undefined }],
    queryFn: () =>
      isAllPeriods
        ? fetchFundamentalesSummary({ recent: 12 })
        : fetchFundamentalesSummary({ period: selectedPeriod || undefined }),
    staleTime: 5 * 60_000,
  })

  const { data: periods = [] } = useQuery({
    queryKey: ['fundamentales', 'periods'],
    queryFn: fetchAllFundamentalesPeriods,
    staleTime: 10 * 60_000,
  })

  const faqQuery = useQuery({
    queryKey: ['faq', 'StaticPage', '/fundamentales'],
    queryFn: () => fetchFaqItems('StaticPage', '/fundamentales'),
    staleTime: 60 * 60_000,
  })

  const filteredRows = useMemo(
    () =>
      (summaryData ?? []).filter(
        (row) =>
          fibraFilter === '' ||
          row.ticker.toLowerCase().includes(fibraFilter.toLowerCase()) ||
          row.name.toLowerCase().includes(fibraFilter.toLowerCase()),
      ),
    [summaryData, fibraFilter],
  )

  usePageTitle(
    'Fundamentales FIBRAs — Cap Rate, NAV, NOI | FIBRADIS',
    'Métricas fundamentales comparativas de FIBRAs: Cap Rate, NAV por CBFI, LTV, NOI Margin y más. Análisis cross-FIBRA actualizado.',
  )

  return (
    <>
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8 space-y-2">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-primary">Análisis comparativo</p>
          <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
            Fundamentales Fibras Inmobiliarias
          </h1>
          <p className="max-w-2xl text-sm leading-6 text-muted-foreground md:text-base">
            Compara los indicadores financieros de todo el universo de FIBRAs en un solo vistazo.
          </p>
        </div>

        <section className="mb-6 rounded-2xl border border-border bg-surface-elevated p-4 shadow-sm">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
            <label className="flex-1 space-y-1.5">
              <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Período</span>
              <select
                value={selectedPeriod}
                onChange={(e) => setSelectedPeriod(e.target.value)}
                className="flex h-10 w-full rounded-lg border border-input bg-background px-3 text-sm text-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                aria-label="Seleccionar período de fundamentales"
              >
                <option value="">Último disponible</option>
                <option value="all">Todas las disponibles</option>
                {periods.map((p) => (
                  <option key={p} value={p}>{p}</option>
                ))}
              </select>
            </label>

            <label className="flex-1 space-y-1.5">
              <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Buscar FIBRA</span>
              <input
                type="search"
                value={fibraFilter}
                onChange={(e) => setFibraFilter(e.target.value)}
                placeholder="Ticker o nombre..."
                className="flex h-10 w-full rounded-lg border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                aria-label="Filtrar FIBRAs por ticker o nombre"
              />
            </label>
          </div>
        </section>

        <section className="overflow-x-auto rounded-2xl border border-border bg-surface-elevated shadow-sm">
          <table className="w-full min-w-[640px] text-sm">
            <thead>
              <tr className="border-b border-border text-xs font-medium text-muted-foreground/70 uppercase tracking-wide bg-muted/30">
                <th className="px-4 py-3 text-left font-medium">FIBRA</th>
                <th className="px-4 py-3 text-left font-medium">Período</th>
                <th className="px-4 py-3 text-right font-medium" title={KPI_DEFINITIONS.capRate.description}>{KPI_DEFINITIONS.capRate.label}</th>
                <th className="px-4 py-3 text-right font-medium" title={KPI_DEFINITIONS.navPerCbfi.description}>{KPI_DEFINITIONS.navPerCbfi.label}</th>
                <th className="px-4 py-3 text-right font-medium" title={KPI_DEFINITIONS.ltv.description}>{KPI_DEFINITIONS.ltv.label}</th>
                <th className="px-4 py-3 text-right font-medium" title={KPI_DEFINITIONS.noiMargin.description}>{KPI_DEFINITIONS.noiMargin.label}</th>
                <th className="px-4 py-3 text-right font-medium" title={KPI_DEFINITIONS.ffoMargin.description}>{KPI_DEFINITIONS.ffoMargin.label}</th>
                <th className="px-4 py-3 text-right font-medium" title={KPI_DEFINITIONS.quarterlyDistribution.description}>{KPI_DEFINITIONS.quarterlyDistribution.label}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {isSummaryLoading ? (
                <FundamentalesSkeleton />
              ) : filteredRows.length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-4 py-12 text-center">
                    <p className="text-base font-medium text-muted-foreground">
                      {summaryData?.length === 0
                        ? isAllPeriods
                          ? 'Sin datos disponibles.'
                          : selectedPeriod
                            ? `Sin datos para el período «${selectedPeriod}».`
                            : 'No hay fundamentales procesados en el sistema.'
                        : `Sin resultados para «${fibraFilter}» en el período seleccionado.`}
                    </p>
                    {(summaryData?.length ?? 0) > 0 && fibraFilter && (
                      <p className="mt-1 text-sm text-muted-foreground/70">
                        Ajusta el texto del filtro para encontrar la FIBRA.
                      </p>
                    )}
                  </td>
                </tr>
              ) : (
                filteredRows.map((row) => (
                  <FundamentalesRow key={`${row.ticker}-${row.period}`} row={row} slug={slugFor(row.ticker)} />
                ))
              )}
            </tbody>
          </table>
        </section>

        {faqQuery.isLoading ? <FaqSkeleton /> : null}

        {faqQuery.isSuccess && faqQuery.data.length > 0 ? (
          <div className="mt-8">
            <FaqAccordion
              items={faqQuery.data}
              kicker="FAQ de fundamentales"
              title="Respuestas sobre métricas y rendimiento"
              description="Las mismas definiciones que ves aquí se materializan como FAQPage JSON-LD en el servidor."
            />
          </div>
        ) : null}

        {faqQuery.isError ? (
          <div className="mt-8 rounded-[1.5rem] border border-rose-200 bg-white px-5 py-4 text-sm text-rose-700 shadow-sm">
            {faqQuery.error.message}
          </div>
        ) : null}
      </div>
    </>
  )
}

function FundamentalesRow({ row, slug }: { row: FundamentalesSummaryItemDto; slug: string }) {
  return (
    <tr className="hover:bg-muted/40 transition-colors">
      <td className="px-4 py-3">
        <Link
          to={`/fibras/${slug}`}
          className="group flex flex-col gap-0.5"
        >
          <span className="font-mono font-semibold text-primary group-hover:underline">{row.ticker}</span>
          <span className="text-xs text-muted-foreground truncate max-w-[12rem]">{row.name}</span>
        </Link>
      </td>
      <td className="px-4 py-3">
        <span className="rounded-md bg-muted px-2 py-0.5 text-xs font-semibold tracking-wider text-muted-foreground uppercase">
          {row.period}
        </span>
      </td>
      <td className="px-4 py-3 text-right font-mono tabular-nums">
        {formatFundamentalValue(toNullableNumber(row.capRate))}
      </td>
      <td className="px-4 py-3 text-right font-mono tabular-nums">
        {formatFundamentalValue(toNullableNumber(row.navPerCbfi))}
      </td>
      <td className="px-4 py-3 text-right font-mono tabular-nums">
        {formatFundamentalValue(toNullableNumber(row.ltv))}
      </td>
      <td className="px-4 py-3 text-right font-mono tabular-nums">
        {formatFundamentalValue(toNullableNumber(row.noiMargin))}
      </td>
      <td className="px-4 py-3 text-right font-mono tabular-nums">
        {formatFundamentalValue(toNullableNumber(row.ffoMargin))}
      </td>
      <td className="px-4 py-3 text-right font-mono tabular-nums">
        {formatFundamentalValue(toNullableNumber(row.quarterlyDistribution))}
      </td>
    </tr>
  )
}

function FundamentalesSkeleton() {
  return (
    <>
      {Array.from({ length: 6 }).map((_, index) => (
        <tr key={index} className="border-b border-border last:border-0">
          <td className="px-4 py-3">
            <div className="space-y-1">
              <div className="h-4 w-16 animate-pulse rounded bg-muted" />
              <div className="h-3 w-24 animate-pulse rounded bg-muted" />
            </div>
          </td>
          <td className="px-4 py-3"><div className="h-4 w-16 animate-pulse rounded bg-muted" /></td>
          <td className="px-4 py-3 text-right"><div className="ml-auto h-4 w-12 animate-pulse rounded bg-muted" /></td>
          <td className="px-4 py-3 text-right"><div className="ml-auto h-4 w-14 animate-pulse rounded bg-muted" /></td>
          <td className="px-4 py-3 text-right"><div className="ml-auto h-4 w-10 animate-pulse rounded bg-muted" /></td>
          <td className="px-4 py-3 text-right"><div className="ml-auto h-4 w-12 animate-pulse rounded bg-muted" /></td>
          <td className="px-4 py-3 text-right"><div className="ml-auto h-4 w-12 animate-pulse rounded bg-muted" /></td>
          <td className="px-4 py-3 text-right"><div className="ml-auto h-4 w-10 animate-pulse rounded bg-muted" /></td>
        </tr>
      ))}
    </>
  )
}

function toNullableNumber(value: number | string | null | undefined): number | null {
  if (value === null || value === undefined) return null
  const n = typeof value === 'string' ? Number(value) : value
  return Number.isNaN(n) ? null : n
}

function FaqSkeleton() {
  return (
    <div className="mt-8 rounded-[2rem] border border-slate-200 bg-[linear-gradient(180deg,rgba(255,255,255,0.94),rgba(247,250,255,0.92))] px-5 py-6 shadow-[0_20px_50px_rgba(15,23,42,0.08)] md:px-8">
      <div className="h-4 w-24 animate-pulse rounded bg-slate-200" />
      <div className="mt-3 h-8 w-80 max-w-full animate-pulse rounded bg-slate-200" />
      <div className="mt-3 h-4 w-2/3 animate-pulse rounded bg-slate-200" />
      <div className="mt-6 space-y-3">
        {Array.from({ length: 3 }).map((_, index) => (
          <div key={index} className="h-20 animate-pulse rounded-[1.5rem] bg-slate-100" />
        ))}
      </div>
    </div>
  )
}
