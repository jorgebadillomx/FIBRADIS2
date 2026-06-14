import { useEffect, useState } from 'react'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import { FibraLogo } from '@/shared/ui/fibra-logo'
import ReactMarkdown, { type Components } from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { useQuery } from '@tanstack/react-query'
import { fetchFibraByTicker, fetchFibraHistory, fetchMarketSnapshots } from '@/api/fibrasApi'
import { fetchFundamentalesPublic, fetchFundamentalesAvailablePeriods } from '@/api/fundamentalesApi'
import { useAuth } from '@/modules/auth/AuthContext'
import { StarButton } from '@/modules/oportunidades/StarButton'
import { useFavorites } from '@/modules/oportunidades/useFavorites'
import type { FundamentalesData } from './sections/fundamentales'
import { FibraNotFound } from './FibraNotFound'
import { PrecioSection } from './sections/PrecioSection'
import { MercadoSection } from './sections/MercadoSection'
import { FundamentalesSection, FundamentalesSectionSkeleton } from './sections/FundamentalesSection'
import { DistribucionesSection } from './sections/DistribucionesSection'
import { NoticiasSection } from './sections/NoticiasSection'
import { ReportesSection } from './sections/ReportesSection'
import { IsrCalculatorWidget } from './IsrCalculatorWidget'
import { FreshnessBadge } from '@/shared/ui/freshness-badge'
import type { FreshnessStatus } from '@/shared/ui/freshness-badge'
import { toNum, formatRelativeTime } from '@/shared/lib/format-time'
import { buildFibraSlug, extractTickerFromSlug } from '@/shared/lib/fibra-slug'
import { KPI_DEFINITIONS, type KpiKey } from '@/shared/lib/kpi-definitions'
import { PriceChartSkeleton } from '@/shared/ui/price-chart'
import { FIBRA_PAGE_LOADING_COUNTS, FIBRA_PAGE_LOADING_TABS, FIBRA_HEADER_LOADING_SHELL } from './cwv-layout'

const FIBRA_BRAND_SUFFIX =
  ' en FIBRADIS — precio en tiempo real, distribuciones, fundamentales y score de inversión.'
const FIBRA_MAX_DESC = 160

// Desplaza los headings del markdown de "Descripción" +1 (h1→h2, h2→h3, …) para que el <h1>
// del título de la ficha sea el único de la página (jerarquía correcta — fix H2 auditoría SEO 2026-06-13).
const DESCRIPTION_MARKDOWN_COMPONENTS: Components = {
  h1: ({ ...props }) => <h2 {...props} />,
  h2: ({ ...props }) => <h3 {...props} />,
  h3: ({ ...props }) => <h4 {...props} />,
  h4: ({ ...props }) => <h5 {...props} />,
  h5: ({ ...props }) => <h6 {...props} />,
  h6: ({ ...props }) => <h6 {...props} />,
}

function buildFibraDescription(fibra: { fullName: string; ticker: string; sector?: string | null }): string {
  const sector = fibra.sector ? ` · ${fibra.sector}` : ''
  const text = `${fibra.fullName} (${fibra.ticker})${sector}${FIBRA_BRAND_SUFFIX}`
  if (text.length <= FIBRA_MAX_DESC) return text
  const cut = text.charCodeAt(FIBRA_MAX_DESC - 4) >= 0xd800 ? FIBRA_MAX_DESC - 4 : FIBRA_MAX_DESC - 3
  return text.slice(0, cut) + '...'
}

function SectionHeader({ title }: { title: string }) {
  return (
    <div className="flex items-center gap-3">
      <h2 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground whitespace-nowrap">
        {title}
      </h2>
      <div className="flex-1 h-px bg-border" />
    </div>
  )
}

function FibraPageSkeleton() {
  return (
    // El skeleton es puramente decorativo: se marca aria-busy en el contenedor y el contenido se
    // oculta del árbol de accesibilidad (aria-hidden) para no anunciar landmarks/tabs vacíos.
    <div aria-busy="true" role="status" className="min-h-[3800px]">
      <span className="sr-only">Cargando ficha de la FIBRA…</span>
      <div aria-hidden="true" className="space-y-6">
      <header className="sticky top-14 z-40 border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="container mx-auto px-4 pt-3 pb-0">
          <div className="flex items-start justify-between gap-4">
            <div className="flex min-w-0 items-start gap-3">
              <div className="h-12 w-12 shrink-0 animate-pulse rounded-xl bg-muted/70" />
              <div className="min-w-0 space-y-2 pt-1">
                <div className="h-6 w-64 animate-pulse rounded bg-muted/70" />
                <div className="h-3 w-48 animate-pulse rounded bg-muted/70" />
              </div>
            </div>
            <div className={`flex items-center justify-end gap-2 pt-1 ${FIBRA_HEADER_LOADING_SHELL.containerWidthClass}`}>
              <div className={`h-8 animate-pulse rounded bg-muted/70 ${FIBRA_HEADER_LOADING_SHELL.priceSkeletonWidthClass}`} />
              <div className={`h-6 animate-pulse rounded-full bg-muted/70 ${FIBRA_HEADER_LOADING_SHELL.yieldBadgeWidthClass}`} />
              <div className={`h-6 animate-pulse rounded-full bg-muted/70 ${FIBRA_HEADER_LOADING_SHELL.freshnessBadgeWidthClass}`} />
            </div>
          </div>

          <nav aria-label="Navegación de secciones de la ficha" className="mt-3 flex gap-1 overflow-x-auto -mx-1">
            {FIBRA_PAGE_LOADING_TABS.map((tab) => (
              <span
                key={tab}
                className="shrink-0 rounded-t-lg px-3 pb-2.5 pt-1 text-sm text-transparent"
              >
                <span className="block h-4 w-24 animate-pulse rounded bg-muted/70" />
              </span>
            ))}
          </nav>
        </div>
      </header>

      <div className="container mx-auto space-y-10 px-4 py-6">
        <nav aria-label="breadcrumb" className="flex items-center gap-1.5 text-sm text-muted-foreground">
          <span className="h-4 w-16 animate-pulse rounded bg-muted/70" />
          <span aria-hidden="true">/</span>
          <span className="h-4 w-20 animate-pulse rounded bg-muted/70" />
        </nav>

        <div className="rounded-xl border border-border bg-surface-elevated px-5 py-4">
          <div className="flex min-h-14 items-end gap-3">
            <div className="h-10 w-32 animate-pulse rounded bg-muted/70" />
            <div className="min-w-[11rem] pb-1 space-y-1">
              <div className="h-6 w-24 animate-pulse rounded-full bg-muted/70" />
              <div className="h-4 w-32 animate-pulse rounded bg-muted/70" />
            </div>
          </div>
        </div>

        <section id="mercado" className="scroll-mt-32 space-y-4">
          <SectionHeader title={SECTION_TITLES.mercado} />
          <div className="grid grid-cols-3 gap-3">
            {Array.from({ length: FIBRA_PAGE_LOADING_COUNTS.marketMetricCards }).map((_, index) => (
              <div
                key={index}
                className="rounded-lg border border-border bg-surface-elevated px-4 py-3"
              >
                <div className="h-3 w-20 animate-pulse rounded bg-muted/70" />
                <div className="mt-2 h-5 w-16 animate-pulse rounded bg-muted/70" />
              </div>
            ))}
          </div>
          <div className="rounded-2xl border border-border bg-surface-elevated shadow-sm">
            <div className="flex flex-col gap-4 border-b border-border px-4 py-4 md:flex-row md:items-end md:justify-between">
              <div className="space-y-2">
                <div className="h-3 w-40 animate-pulse rounded bg-muted/70" />
                <div className="h-6 w-64 animate-pulse rounded bg-muted/70" />
                <div className="h-3 w-96 max-w-full animate-pulse rounded bg-muted/70" />
              </div>
              <div className="flex gap-2">
                {Array.from({ length: FIBRA_PAGE_LOADING_COUNTS.marketRangeButtons }).map((_, index) => (
                  <div key={index} className="h-8 w-12 animate-pulse rounded-full bg-muted/70" />
                ))}
              </div>
            </div>
            <div className="p-4">
              <PriceChartSkeleton />
            </div>
          </div>
        </section>

        <section id="fundamentales" className="scroll-mt-32 space-y-4">
          <div className="flex items-center justify-between gap-4">
            <SectionHeader title={SECTION_TITLES.fundamentales} />
            <div className="h-7 w-24 shrink-0 animate-pulse rounded-lg bg-muted/70" />
          </div>
          {/* Reusa el skeleton de la sección para que la geometría coincida exactamente
              entre el page-skeleton y el section-skeleton (evita shift al transicionar). */}
          <FundamentalesSectionSkeleton />
        </section>

        <section id="distribuciones" className="scroll-mt-32 space-y-4">
          <SectionHeader title={SECTION_TITLES.distribuciones} />
          <div className="space-y-4">
            <div className="rounded-lg border border-border bg-surface-elevated px-4 py-3">
              {Array.from({ length: FIBRA_PAGE_LOADING_COUNTS.distributionSummaryLines }).map((_, index) => (
                <div
                  key={index}
                  className={`h-3 animate-pulse rounded bg-muted/70 ${index === 0 ? 'w-40' : 'w-24'}`}
                />
              ))}
              <div className="mt-2 h-8 w-24 animate-pulse rounded bg-muted/70" />
            </div>
            <div className="rounded-xl border border-border bg-surface-elevated px-4 py-4">
              <div className="h-56 animate-pulse rounded-xl bg-muted/20" />
            </div>
          </div>
        </section>

        <section id="noticias" className="scroll-mt-32 space-y-4">
          <SectionHeader title={SECTION_TITLES.noticias} />
          <div className="rounded-lg border border-border bg-surface-elevated px-4 py-4">
            <div className="space-y-3">
              {Array.from({ length: FIBRA_PAGE_LOADING_COUNTS.newsLines }).map((_, index) => (
                <div
                  key={index}
                  className={`h-4 animate-pulse rounded bg-muted/70 ${
                    index === 0 ? 'w-3/4' : index === 1 ? 'w-2/3' : 'w-1/2'
                  }`}
                />
              ))}
            </div>
          </div>
        </section>

        <section id="descripcion" className="scroll-mt-32 space-y-4">
          <SectionHeader title={SECTION_TITLES.descripcion} />
          <div className="rounded-2xl border border-border bg-card p-6">
            <div className="space-y-3">
              {Array.from({ length: FIBRA_PAGE_LOADING_COUNTS.descriptionLines }).map((_, index) => (
                <div
                  key={index}
                  className={`h-4 animate-pulse rounded bg-muted/70 ${
                    index === 0 ? 'w-3/4' : index === 1 ? 'w-full' : 'w-5/6'
                  }`}
                />
              ))}
            </div>
          </div>
        </section>

        <section id="enlaces" className="scroll-mt-32 space-y-4">
          <SectionHeader title={SECTION_TITLES.enlaces} />
          <div className="space-y-2">
            {Array.from({ length: FIBRA_PAGE_LOADING_COUNTS.reportLines }).map((_, index) => (
              <div key={index} className="h-4 w-48 animate-pulse rounded bg-muted/70" />
            ))}
          </div>
        </section>
      </div>
      </div>
    </div>
  )
}

function FibraErrorState() {
  return (
    <div className="container mx-auto px-4 py-16 text-center">
      <p className="text-muted-foreground">Ocurrió un error al cargar la ficha. Intenta de nuevo más tarde.</p>
    </div>
  )
}

const SECTION_TITLES: Record<string, string> = {
  mercado: 'Mercado',
  fundamentales: 'Fundamentales',
  distribuciones: 'Distribuciones',
  noticias: 'Noticias',
  descripcion: 'Descripción',
  enlaces: 'Enlaces',
}

export function FibraPage() {
  const { slug } = useParams<{ slug: string }>()
  // el ticker (mayúsculas) es el último segmento del slug — los queryKeys no cambian,
  // lo que mantiene compatible el initialData del prerender
  const ticker = slug ? extractTickerFromSlug(slug) : undefined
  const navigate = useNavigate()
  const location = useLocation()
  const [selectedPeriod, setSelectedPeriod] = useState<string | undefined>(undefined)
  const { isAuthenticated } = useAuth()
  const { favoriteIds, toggle } = useFavorites()

  const { data: fibra, isLoading, isError } = useQuery({
    queryKey: ['fibra', ticker],
    queryFn: () => fetchFibraByTicker(ticker!),
    enabled: !!ticker,
  })

  const { data: snapshots = [], isLoading: isSnapshotsLoading, isError: isSnapshotsError } = useQuery({
    queryKey: ['market-snapshots'],
    queryFn: fetchMarketSnapshots,
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  })

  const { data: history, isLoading: isHistoryLoading } = useQuery({
    queryKey: ['fibra-history', ticker, '1y'],
    queryFn: () => fetchFibraHistory(ticker!, '1y'),
    staleTime: 60 * 60_000,
    enabled: !!ticker,
  })

  const { data: availablePeriods = [], isFetched: periodsFetched } = useQuery({
    queryKey: ['fundamentales-periods', ticker],
    queryFn: () => fetchFundamentalesAvailablePeriods(ticker!),
    enabled: !!ticker,
    staleTime: 5 * 60_000,
  })

  const activePeriod = selectedPeriod ?? availablePeriods[0]

  const { data: fundamentalesDto, isLoading: isFundamentalesLoading } = useQuery({
    queryKey: ['fundamentales', ticker, activePeriod],
    queryFn: () => fetchFundamentalesPublic(ticker!, activePeriod),
    enabled: !!ticker && periodsFetched,
    staleTime: 5 * 60_000,
  })

  const toFundamentalNum = (v: null | number | string | undefined): number | null =>
    v == null ? null : Number(v)

  const fundamentalesData: FundamentalesData | undefined = fundamentalesDto
    ? {
        period: fundamentalesDto.period,
        periodsAgo: typeof fundamentalesDto.periodsAgo === 'number' ? fundamentalesDto.periodsAgo : undefined,
        capturedAt: fundamentalesDto.capturedAt,
        summary: fundamentalesDto.summary ?? null,
        summaryMarkdown: fundamentalesDto.summaryMarkdown ?? null,
        investorTakeaway: fundamentalesDto.investorTakeaway ?? null,
        operationalSignals: fundamentalesDto.operationalSignals ?? [],
        financialSignals: fundamentalesDto.financialSignals ?? [],
        riskFlags: fundamentalesDto.riskFlags ?? [],
        items: ([
          'capRate',
          'navPerCbfi',
          'ltv',
          'noiMargin',
          'ffoMargin',
          'quarterlyDistribution',
        ] as const).map((key) => ({
          label: KPI_DEFINITIONS[key].label,
          kpiKey: key as KpiKey,
          period: fundamentalesDto.period,
          value: toFundamentalNum(fundamentalesDto[key]),
          note: fundamentalesDto.fieldNotes?.[key] ?? undefined,
        })),
      }
    : undefined

  const marketData = snapshots.find(s => s.ticker === fibra?.ticker) ?? null
  const isMarketDataLoading = isSnapshotsLoading
  const isFundamentalesSectionLoading = !periodsFetched || isFundamentalesLoading

  // Canonicalización client-side (CA-10): un link viejo /fibras/FUNO11 o un slug
  // obsoleto navegado en la SPA se reemplaza por la URL slug canónica sin recargar
  const slugCanonico = fibra ? buildFibraSlug(fibra.fullName, fibra.ticker) : undefined
  useEffect(() => {
    if (slugCanonico && slug !== slugCanonico) {
      // conservar query string y hash (ej. #noticias desde NoticiaPage, UTM)
      navigate(`/fibras/${slugCanonico}${location.search}${location.hash}`, { replace: true })
    }
  }, [slugCanonico, slug, navigate, location.search, location.hash])

  const pageTitle = fibra
    ? `${fibra.ticker} — ${fibra.fullName} | Fibras Inmobiliarias`
    : `${ticker?.toUpperCase() ?? 'FIBRA'} | Fibras Inmobiliarias`

  usePageTitle(pageTitle, fibra ? buildFibraDescription(fibra) : undefined)

  // slug sin ticker extraíble (ej. /fibras/fibra-uno- o /fibras/-): la query queda
  // deshabilitada (isLoading=false, fibra=undefined) y sin este guard el render
  // llegaría a fibra!.* con undefined
  if (!ticker) return <FibraNotFound ticker={slug ?? ''} />
  if (isLoading) return <FibraPageSkeleton />
  if (isError) return <FibraErrorState />
  if (fibra === null) return <FibraNotFound ticker={ticker} />

  const marketPrice = toNum(marketData?.lastPrice)
  const annualizedYield = toNum(marketData?.annualizedYield)
  const hasMarketPrice = marketPrice != null && marketData?.freshnessStatus != null

  const sectionLabels = [
    { href: '#mercado', label: 'Mercado' },
    { href: '#fundamentales', label: 'Fundamentales' },
    { href: '#distribuciones', label: 'Distribuciones' },
    { href: '#noticias', label: 'Noticias' },
    ...(fibra!.description ? [{ href: '#descripcion', label: 'Descripción' }] : []),
    { href: '#enlaces', label: 'Enlaces' },
  ]

  return (
    <>
      <div>
        <h1 className="sr-only">{fibra!.fullName} ({fibra!.ticker}) | Fibras Inmobiliarias</h1>
        <header className="sticky top-14 z-40 border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
          <div className="container mx-auto px-4 pt-3 pb-0">
            <div className="flex items-start justify-between gap-4">
              {/* Identidad de la FIBRA */}
              <div className="flex min-w-0 items-start gap-3">
                <FibraLogo siteUrl={fibra!.siteUrl ?? null} ticker={fibra!.ticker} size="md" />
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="text-xl font-semibold leading-tight text-foreground truncate">{fibra!.fullName}</span>
                    {isAuthenticated && fibra != null && (
                      <StarButton
                        fibraId={fibra.id}
                        isFavorite={favoriteIds.has(fibra.id)}
                        onToggle={toggle}
                        size={22}
                      />
                    )}
                  </div>
                  <div className="flex flex-wrap items-center gap-x-3 gap-y-0.5 mt-0.5 text-xs text-muted-foreground">
                    <span className="font-playfair font-bold text-primary">{fibra!.ticker}</span>
                    <span className="text-border">·</span>
                    <span>{fibra!.sector}</span>
                    <span className="text-border">·</span>
                    <span>{fibra!.market}</span>
                    <span className="text-border">·</span>
                    <span>{fibra!.currency}</span>
                  </div>
                </div>
              </div>
              {/* Precio real con badge de frescura */}
              <div className={`flex items-center justify-end gap-2 shrink-0 pt-1 ${FIBRA_HEADER_LOADING_SHELL.containerWidthClass}`}>
                {isMarketDataLoading ? (
                  <div aria-busy="true" className="flex items-center gap-2">
                    <div className={`h-8 animate-pulse rounded bg-muted/70 ${FIBRA_HEADER_LOADING_SHELL.priceSkeletonWidthClass}`} />
                    <div className={`h-6 animate-pulse rounded-full bg-muted/70 ${FIBRA_HEADER_LOADING_SHELL.yieldBadgeWidthClass}`} />
                    <div className={`h-6 animate-pulse rounded-full bg-muted/70 ${FIBRA_HEADER_LOADING_SHELL.freshnessBadgeWidthClass}`} />
                  </div>
                ) : hasMarketPrice ? (
                  <>
                    <span className={`text-2xl font-semibold tabular-nums ${FIBRA_HEADER_LOADING_SHELL.priceReserveClass}`}>
                      {marketPrice!.toFixed(2)}
                    </span>
                    {annualizedYield != null ? (
                      <span className="inline-flex items-center rounded-full border border-violet-200 bg-violet-50 px-2.5 py-1 text-[11px] font-semibold tabular-nums text-violet-700">
                        {annualizedYield.toFixed(1)}%
                      </span>
                    ) : null}
                    <FreshnessBadge
                      status={marketData!.freshnessStatus as FreshnessStatus}
                      lastUpdated={marketData!.capturedAt ? formatRelativeTime(marketData!.capturedAt) : undefined}
                    />
                  </>
                ) : isSnapshotsError ? (
                  <span
                    title="No se pudo cargar el precio"
                    className={`text-2xl font-semibold tabular-nums text-muted-foreground ${FIBRA_HEADER_LOADING_SHELL.priceReserveClass}`}
                  >
                    —
                  </span>
                ) : (
                  <span className={`text-2xl font-semibold tabular-nums text-muted-foreground ${FIBRA_HEADER_LOADING_SHELL.priceReserveClass}`}>—</span>
                )}
              </div>
            </div>
            {/* Anclas de sección */}
            <nav
              aria-label="Navegación de secciones de la ficha"
              className="flex gap-1 overflow-x-auto mt-3 -mx-1"
            >
              {sectionLabels.map((s) => (
                <a
                  key={s.href}
                  href={s.href}
                  className="shrink-0 px-3 pb-2.5 pt-1 text-sm text-muted-foreground hover:text-foreground hover:border-b-2 hover:border-brand transition-colors"
                >
                  {s.label}
                </a>
              ))}
            </nav>
          </div>
        </header>

        <div className="container mx-auto px-4 py-6 space-y-10">
          <nav aria-label="breadcrumb" className="flex items-center gap-1.5 text-sm text-muted-foreground">
            <Link
              className="flex items-center gap-1 transition hover:text-foreground cursor-pointer"
              to="/fibras"
            >
              <svg aria-hidden="true" className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path d="M15 18l-6-6 6-6" strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} />
              </svg>
              Fibras
            </Link>
            <span aria-hidden="true">/</span>
            <span className="font-medium text-foreground">{ticker}</span>
          </nav>

          <PrecioSection
            lastPrice={marketData?.lastPrice}
            dailyChange={marketData?.dailyChange}
            dailyChangePct={marketData?.dailyChangePct}
            capturedAt={marketData?.capturedAt}
            freshnessStatus={marketData?.freshnessStatus}
            isLoading={isSnapshotsLoading}
            isError={isSnapshotsError}
          />

          <section id="mercado" className="scroll-mt-32 space-y-4">
            <SectionHeader title={SECTION_TITLES.mercado} />
            <MercadoSection
              ticker={fibra!.ticker}
              week52High={marketData?.week52High}
              week52Low={marketData?.week52Low}
              volume={marketData?.volume}
            />
          </section>

          <section id="fundamentales" className="scroll-mt-32 space-y-4">
            <div className="flex items-center justify-between gap-4">
              <SectionHeader title={SECTION_TITLES.fundamentales} />
              {availablePeriods.length > 1 && (
                <div className="min-w-[6.5rem] shrink-0">
                  <select
                    value={activePeriod}
                    onChange={(e) => setSelectedPeriod(e.target.value)}
                    className="w-full rounded-lg border border-border bg-background px-2 py-1 text-xs text-muted-foreground focus:outline-none focus:ring-1 focus:ring-brand"
                    aria-label="Seleccionar período de fundamentales"
                  >
                    {availablePeriods.map((p) => (
                      <option key={p} value={p}>{p}</option>
                    ))}
                  </select>
                </div>
              )}
              {availablePeriods.length <= 1 && (
                <div aria-hidden="true" className="h-7 min-w-[6.5rem] shrink-0 rounded-lg border border-transparent" />
              )}
            </div>
            <FundamentalesSection data={fundamentalesData} isLoading={isFundamentalesSectionLoading} />
          </section>

          <section id="distribuciones" className="scroll-mt-32 space-y-4">
            <SectionHeader title={SECTION_TITLES.distribuciones} />
            <DistribucionesSection ticker={fibra!.ticker} />
            {isHistoryLoading ? (
              // Reserva la altura de la calculadora ISR mientras carga `history` para que no
              // aparezca de golpe empujando el footer (CLS, story 12-7).
              <div aria-busy="true" className="min-h-[46rem] rounded-2xl border border-border bg-surface-elevated" />
            ) : history ? (
              <IsrCalculatorWidget
                lastDistribution={toNum(history.distributions[0]?.amountPerUnit)}
                taxableAmountPerUnit={toNum(history.distributions[0]?.taxableAmountPerUnit)}
                capitalReturnAmountPerUnit={toNum(history.distributions[0]?.capitalReturnAmountPerUnit)}
              />
            ) : null}
          </section>

          <section id="noticias" className="scroll-mt-32 space-y-4">
            <SectionHeader title={SECTION_TITLES.noticias} />
            <NoticiasSection fibraId={fibra!.id} />
          </section>

          {fibra!.description ? (
            <section className="scroll-mt-32 space-y-4" id="descripcion">
              <SectionHeader title={SECTION_TITLES.descripcion} />
              <div className="rounded-2xl border border-border bg-card p-6">
                <div className="prose prose-slate max-w-none text-sm leading-7">
                  <ReactMarkdown remarkPlugins={[remarkGfm]} components={DESCRIPTION_MARKDOWN_COMPONENTS}>
                    {fibra!.description}
                  </ReactMarkdown>
                </div>
              </div>
            </section>
          ) : null}

          <section id="enlaces" className="scroll-mt-32 space-y-4">
            <SectionHeader title={SECTION_TITLES.enlaces} />
            <ReportesSection
              siteUrl={fibra!.siteUrl ?? null}
              investorUrl={fibra!.investorUrl ?? null}
              reportsUrl={fibra!.reportsUrl ?? null}
            />
          </section>
        </div>
      </div>
    </>
  )
}
