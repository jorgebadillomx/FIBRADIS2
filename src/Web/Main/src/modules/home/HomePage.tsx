import { PriceCarousel } from './PriceCarousel'
import { GainersLosers } from './GainersLosers'
import { FibraUniverseTable } from './FibraUniverseTable'
import { NewsSection } from './NewsSection'

export function HomePage() {
  return (
    <>
      <title>FIBRADIS — Plataforma de análisis de FIBRAs del mercado mexicano</title>
      <meta name="description" content="Descubre y analiza FIBRAs del mercado mexicano (BMV). Precios, fundamentales, distribuciones y noticias en tiempo real." />
      <link rel="canonical" href="https://fibradis.mx/" />

      <div className="container mx-auto px-4 py-6 space-y-8">
        <h1 className="sr-only">FIBRADIS — Plataforma de análisis de FIBRAs</h1>
        <section aria-labelledby="heading-precio">
          <h2 id="heading-precio" className="sr-only">Precios de mercado</h2>
          <PriceCarousel />
        </section>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <section aria-labelledby="heading-ranking">
              <h2 id="heading-ranking" className="sr-only">Ganadores y perdedores del día</h2>
              <GainersLosers />
            </section>
          </div>
          <div>
            <section aria-labelledby="heading-noticias">
              <h2 id="heading-noticias" className="sr-only">Noticias recientes</h2>
              <NewsSection />
            </section>
          </div>
        </div>
        <section aria-labelledby="heading-universo">
          <h2 id="heading-universo" className="sr-only">Universo FIBRAS</h2>
          <FibraUniverseTable />
        </section>
      </div>
    </>
  )
}
