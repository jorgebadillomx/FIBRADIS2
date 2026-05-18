import { useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchFibraByTicker } from '@/api/fibrasApi'
import { FibraNotFound } from './FibraNotFound'
import { PrecioSection } from './sections/PrecioSection'
import { MercadoSection } from './sections/MercadoSection'
import { FundamentalesSection } from './sections/FundamentalesSection'
import { DistribucionesSection } from './sections/DistribucionesSection'
import { NoticiasSection } from './sections/NoticiasSection'
import { ReportesSection } from './sections/ReportesSection'

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

  return (
    <>
      <title>{pageTitle}</title>
      <meta name="description" content={pageDescription} />
      <link rel="canonical" href={canonicalUrl} />

      <div>
        <h1 className="sr-only">{fibra!.fullName} ({fibra!.ticker}) | FIBRADIS</h1>
        <header className="sticky top-14 z-40 border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
          <div className="container mx-auto px-4 py-3 space-y-1">
            <div className="flex items-center justify-between gap-4">
              <div className="min-w-0">
                <span className="text-lg font-semibold">{fibra!.ticker}</span>
                <span className="ml-2 text-sm text-muted-foreground truncate">{fibra!.fullName}</span>
              </div>
              <div className="hidden sm:flex shrink-0 flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
                <span>{fibra!.sector}</span>
                <span>{fibra!.market}</span>
                <span>{fibra!.currency}</span>
                <span>{fibra!.state}</span>
              </div>
            </div>
            <div className="sm:hidden flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
              <span>{fibra!.sector}</span>
              <span>{fibra!.market}</span>
              <span>{fibra!.currency}</span>
              <span>{fibra!.state}</span>
            </div>
            <nav aria-label="Navegación de secciones de la ficha" className="flex gap-4 overflow-x-auto text-sm text-muted-foreground pb-0.5">
              {SECTION_LABELS.map((s) => (
                <a key={s.href} href={s.href} className="hover:text-foreground transition-colors shrink-0">
                  {s.label}
                </a>
              ))}
            </nav>
          </div>
        </header>

        <div className="container mx-auto px-4 py-6 space-y-8">
          <PrecioSection />

          <section id="mercado" className="space-y-2">
            <h2 className="text-base font-semibold">{SECTION_TITLES.mercado}</h2>
            <MercadoSection />
          </section>

          <section id="fundamentales" className="space-y-2">
            <h2 className="text-base font-semibold">{SECTION_TITLES.fundamentales}</h2>
            <FundamentalesSection />
          </section>

          <section id="distribuciones" className="space-y-2">
            <h2 className="text-base font-semibold">{SECTION_TITLES.distribuciones}</h2>
            <DistribucionesSection />
          </section>

          <section id="noticias" className="space-y-2">
            <h2 className="text-base font-semibold">{SECTION_TITLES.noticias}</h2>
            <NoticiasSection />
          </section>

          <section id="reportes" className="space-y-2">
            <h2 className="text-base font-semibold">{SECTION_TITLES.reportes}</h2>
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
