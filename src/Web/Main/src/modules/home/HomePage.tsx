import { lazy, Suspense, useEffect } from 'react'
import { usePageTitle } from '@/shared/hooks/usePageTitle'

const HomeMarketSections = lazy(() =>
  import('./HomeMarketSections').then(m => ({ default: m.HomeMarketSections })),
)

function HomeMarketSectionsPlaceholder() {
  return (
    <div className="container mx-auto px-4 py-6">
      <div className="min-h-screen" />
    </div>
  )
}

export function HomePage() {
  useEffect(() => {
    document.getElementById('initial-home-shell')?.remove()
  }, [])

  usePageTitle(
    'FIBRAs Inmobiliarias — Análisis y Herramientas | Fibras Inmobiliarias',
    'Plataforma de análisis de FIBRAs inmobiliarias mexicanas. Precios en tiempo real, distribuciones, fundamentales y ranking de oportunidades.',
  )

  return (
    <>
      {/* Hero editorial */}
      <section className="border-b border-border py-10">
        <div className="container mx-auto px-4">
          <h1 className="font-playfair text-4xl md:text-5xl font-bold text-foreground leading-tight">
            El universo de FIBRAs<br />
            <em className="not-italic text-primary">del mercado mexicano.</em>
          </h1>
          <p className="mt-3 text-muted-foreground text-base max-w-lg leading-relaxed">
            Precios, fundamentales, distribuciones y noticias consolidadas en un solo lugar.
          </p>
        </div>
      </section>

      <Suspense fallback={<HomeMarketSectionsPlaceholder />}>
        <HomeMarketSections />
      </Suspense>
    </>
  )
}
