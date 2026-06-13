import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { GainersLosers } from './GainersLosers'
import { FibraUniverseTable } from './FibraUniverseTable'
import { NewsSection } from './NewsSection'

export function HomePage() {
  usePageTitle(
    'FIBRAs Inmobiliarias — Análisis y Herramientas | FIBRADIS',
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

      <div className="container mx-auto px-4 py-6 space-y-6">
        {/* Ganadores/Perdedores + Noticias en fila */}
        <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_22rem] xl:items-start">
          <section aria-labelledby="heading-ranking">
            <h2 id="heading-ranking" className="sr-only">Ganadores y perdedores del día</h2>
            <GainersLosers />
          </section>

          <aside className="xl:sticky xl:top-28">
            <section aria-labelledby="heading-noticias">
              <h2 id="heading-noticias" className="sr-only">Noticias recientes</h2>
              <NewsSection />
            </section>
          </aside>
        </div>

        {/* Tabla universo a ancho completo */}
        <section aria-labelledby="heading-universo">
          <h2 id="heading-universo" className="sr-only">Universo FIBRAS</h2>
          <FibraUniverseTable />
        </section>
      </div>
    </>
  )
}
