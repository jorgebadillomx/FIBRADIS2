import { useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchFibraByTicker, fetchMarketSnapshots } from '@/api/fibrasApi'
import { fetchFundamentalesPublic } from '@/api/fundamentalesApi'
import type { FundamentalesData } from './sections/fundamentales'
import { FibraNotFound } from './FibraNotFound'
import { PrecioSection } from './sections/PrecioSection'
import { MercadoSection } from './sections/MercadoSection'
import { FundamentalesSection } from './sections/FundamentalesSection'
import { DistribucionesSection } from './sections/DistribucionesSection'
import { NoticiasSection } from './sections/NoticiasSection'
import { ReportesSection } from './sections/ReportesSection'
import { FreshnessBadge } from '@/shared/ui/freshness-badge'
import type { FreshnessStatus } from '@/shared/ui/freshness-badge'
import { toNum, formatRelativeTime } from '@/shared/lib/format-time'

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

const SECTION_LABELS = [
  { href: '#mercado', label: 'Mercado' },
  { href: '#fundamentales', label: 'Fundamentales' },
  { href: '#distribuciones', label: 'Distribuciones' },
  { href: '#noticias', label: 'Noticias' },
  { href: '#reportes', label: 'Reportes' },
] as const

const SECTION_TITLES: Record<string, string> = {
  mercado: 'Mercado',
  fundamentales: 'Fundamentales',
  distribuciones: 'Distribuciones',
  noticias: 'Noticias',
  reportes: 'Reportes',
}

export function FibraPage() {
  const { ticker } = useParams<{ ticker: string }>()

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

  const { data: fundamentalesDto } = useQuery({
    queryKey: ['fundamentales', ticker],
    queryFn: () => fetchFundamentalesPublic(ticker!),
    enabled: !!ticker,
    staleTime: 5 * 60_000,
  })

  const toFundamentalNum = (v: null | number | string | undefined): number | null =>
    v == null ? null : Number(v)

  const fundamentalesData: FundamentalesData | undefined = fundamentalesDto
    ? {
        periodsAgo: typeof fundamentalesDto.periodsAgo === 'number' ? fundamentalesDto.periodsAgo : undefined,
        items: [
          { label: 'Cap Rate', period: fundamentalesDto.period, value: toFundamentalNum(fundamentalesDto.capRate) },
          { label: 'NAV por CBFI', period: fundamentalesDto.period, value: toFundamentalNum(fundamentalesDto.navPerCbfi) },
          { label: 'LTV', period: fundamentalesDto.period, value: toFundamentalNum(fundamentalesDto.ltv) },
          { label: 'Margen NOI', period: fundamentalesDto.period, value: toFundamentalNum(fundamentalesDto.noiMargin) },
          { label: 'Margen FFO', period: fundamentalesDto.period, value: toFundamentalNum(fundamentalesDto.ffoMargin) },
          { label: 'Dist. Trimestral', period: fundamentalesDto.period, value: toFundamentalNum(fundamentalesDto.quarterlyDistribution) },
        ],
      }
    : undefined

  const marketData = snapshots.find(s => s.ticker === fibra?.ticker) ?? null

  const pageTitle = fibra
    ? `${fibra.ticker} — ${fibra.fullName} | FIBRADIS`
    : `${ticker?.toUpperCase() ?? 'FIBRA'} | FIBRADIS`

  const pageDescription = fibra
    ? `Análisis de ${fibra.fullName} (${fibra.ticker}): precio de mercado, fundamentales, distribuciones y noticias. ${fibra.sector} — ${fibra.market}.`
    : `Perfil de FIBRA ${ticker} en FIBRADIS.`

  const canonicalUrl = `https://fibradis.mx/fibras/${fibra?.ticker ?? ticker}`

  if (isLoading) return <FibraPageSkeleton />
  if (isError) return <FibraErrorState />
  if (fibra === null) return <FibraNotFound ticker={ticker!} />

  const marketPrice = toNum(marketData?.lastPrice)
  const hasMarketPrice = marketPrice != null && marketData?.freshnessStatus != null

  return (
    <>
      <title>{pageTitle}</title>
      <meta name="description" content={pageDescription} />
      <link rel="canonical" href={canonicalUrl} />

      <div>
        <h1 className="sr-only">{fibra!.fullName} ({fibra!.ticker}) | FIBRADIS</h1>
        <header className="sticky top-14 z-40 border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
          <div className="container mx-auto px-4 pt-3 pb-0">
            <div className="flex items-start justify-between gap-4">
              {/* Identidad de la FIBRA */}
              <div className="min-w-0">
                <div className="flex items-baseline gap-2">
                  <span className="text-2xl font-bold tracking-tight">{fibra!.ticker}</span>
                  <span className="text-sm text-muted-foreground truncate hidden sm:block">{fibra!.fullName}</span>
                </div>
                <div className="flex flex-wrap gap-x-3 gap-y-0.5 mt-0.5 text-xs text-muted-foreground">
                  <span>{fibra!.sector}</span>
                  <span className="text-border">·</span>
                  <span>{fibra!.market}</span>
                  <span className="text-border">·</span>
                  <span>{fibra!.currency}</span>
                </div>
              </div>
              {/* Precio real con badge de frescura */}
              <div className="flex items-center gap-2 shrink-0 pt-1">
                {hasMarketPrice ? (
                  <>
                    <span className="text-2xl font-semibold tabular-nums">
                      {marketPrice!.toFixed(2)}
                    </span>
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
              {SECTION_LABELS.map((s) => (
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
            <SectionHeader title={SECTION_TITLES.fundamentales} />
            <FundamentalesSection data={fundamentalesData} />
          </section>

          <section id="distribuciones" className="scroll-mt-32 space-y-4">
            <SectionHeader title={SECTION_TITLES.distribuciones} />
            <DistribucionesSection ticker={fibra!.ticker} />
          </section>

          <section id="noticias" className="scroll-mt-32 space-y-4">
            <SectionHeader title={SECTION_TITLES.noticias} />
            <NoticiasSection fibraId={fibra!.id} fibra={fibra} />
          </section>

          <section id="reportes" className="scroll-mt-32 space-y-4">
            <SectionHeader title={SECTION_TITLES.reportes} />
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
