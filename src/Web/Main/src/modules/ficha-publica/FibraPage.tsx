import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { FibraLogo } from '@/shared/ui/fibra-logo'
import ReactMarkdown from 'react-markdown'
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
import { FundamentalesSection } from './sections/FundamentalesSection'
import { DistribucionesSection } from './sections/DistribucionesSection'
import { NoticiasSection } from './sections/NoticiasSection'
import { ReportesSection } from './sections/ReportesSection'
import { IsrCalculatorWidget } from './IsrCalculatorWidget'
import { FreshnessBadge } from '@/shared/ui/freshness-badge'
import type { FreshnessStatus } from '@/shared/ui/freshness-badge'
import { toNum, formatRelativeTime } from '@/shared/lib/format-time'
import { buildFibraSlug, extractTickerFromSlug } from '@/shared/lib/fibra-slug'
import { KPI_DEFINITIONS, type KpiKey } from '@/shared/lib/kpi-definitions'

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
    <div className="animate-pulse space-y-4 container mx-auto px-4 py-6">
      <div className="h-12 bg-muted rounded w-1/3" />
      <div className="h-4 bg-muted rounded w-1/2" />
      <div className="h-4 bg-muted rounded w-1/4" />
      <div className="h-48 bg-muted rounded" />
      <div className="h-32 bg-muted rounded" />
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
  const [selectedPeriod, setSelectedPeriod] = useState<string | undefined>(undefined)
  const { isAuthenticated } = useAuth()
  const { favoriteIds, toggle } = useFavorites()

  const { data: fibra, isLoading, isError } = useQuery({
    queryKey: ['fibra', ticker],
    queryFn: () => fetchFibraByTicker(ticker!),
    enabled: !!ticker,
  })

  const { data: snapshots = [] } = useQuery({
    queryKey: ['market-snapshots'],
    queryFn: fetchMarketSnapshots,
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  })

  const { data: history } = useQuery({
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

  const { data: fundamentalesDto } = useQuery({
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

  // Canonicalización client-side (CA-10): un link viejo /fibras/FUNO11 o un slug
  // obsoleto navegado en la SPA se reemplaza por la URL slug canónica sin recargar
  const slugCanonico = fibra ? buildFibraSlug(fibra.fullName, fibra.ticker) : undefined
  useEffect(() => {
    if (slugCanonico && slug !== slugCanonico) {
      navigate(`/fibras/${slugCanonico}`, { replace: true })
    }
  }, [slugCanonico, slug, navigate])

  const pageTitle = fibra
    ? `${fibra.ticker} — ${fibra.fullName} | Fibras Inmobiliarias`
    : `${ticker?.toUpperCase() ?? 'FIBRA'} | Fibras Inmobiliarias`

  const pageDescription = fibra
    ? `Análisis de ${fibra.fullName} (${fibra.ticker}): precio de mercado, fundamentales, distribuciones y noticias. ${fibra.sector} — ${fibra.market}.`
    : `Perfil de FIBRA ${ticker} en Fibras Inmobiliarias.`

  // dominio canónico per historias 11-1/11-2 + URL slug per 11-3
  const canonicalUrl = `https://fibrasinmobiliarias.com/fibras/${slugCanonico ?? slug}`

  if (isLoading) return <FibraPageSkeleton />
  if (isError) return <FibraErrorState />
  if (fibra === null) return <FibraNotFound ticker={ticker!} />

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
      <title>{pageTitle}</title>
      <meta name="description" content={pageDescription} />
      <link rel="canonical" href={canonicalUrl} />

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
              <div className="flex items-center gap-2 shrink-0 pt-1">
                {hasMarketPrice ? (
                  <>
                    <span className="text-2xl font-semibold tabular-nums">
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
                ) : (
                  <span className="text-2xl font-semibold tabular-nums text-muted-foreground">—</span>
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
              to="/catalogo"
            >
              <svg aria-hidden="true" className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path d="M15 18l-6-6 6-6" strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} />
              </svg>
              Catálogo
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
                <select
                  value={activePeriod}
                  onChange={(e) => setSelectedPeriod(e.target.value)}
                  className="shrink-0 rounded-lg border border-border bg-background px-2 py-1 text-xs text-muted-foreground focus:outline-none focus:ring-1 focus:ring-brand"
                  aria-label="Seleccionar período de fundamentales"
                >
                  {availablePeriods.map((p) => (
                    <option key={p} value={p}>{p}</option>
                  ))}
                </select>
              )}
            </div>
            <FundamentalesSection data={fundamentalesData} />
          </section>

          <section id="distribuciones" className="scroll-mt-32 space-y-4">
            <SectionHeader title={SECTION_TITLES.distribuciones} />
            <DistribucionesSection ticker={fibra!.ticker} />
            {history ? (
              <IsrCalculatorWidget
                lastDistribution={toNum(history.distributions[0]?.amountPerUnit)}
                taxableAmountPerUnit={toNum(history.distributions[0]?.taxableAmountPerUnit)}
                capitalReturnAmountPerUnit={toNum(history.distributions[0]?.capitalReturnAmountPerUnit)}
              />
            ) : null}
          </section>

          <section id="noticias" className="scroll-mt-32 space-y-4">
            <SectionHeader title={SECTION_TITLES.noticias} />
            <NoticiasSection fibraId={fibra!.id} fibra={fibra} />
          </section>

          {fibra!.description ? (
            <section className="scroll-mt-32 space-y-4" id="descripcion">
              <SectionHeader title={SECTION_TITLES.descripcion} />
              <div className="rounded-2xl border border-border bg-card p-6">
                <div className="prose prose-slate max-w-none text-sm leading-7">
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>
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
