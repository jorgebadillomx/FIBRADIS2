import { PriceCarousel } from './PriceCarousel'
import { GainersLosers } from './GainersLosers'
import { FibraUniverseTable } from './FibraUniverseTable'
import { NewsSection } from './NewsSection'

export function HomePage() {
  return (
    <>
      <title>Fibras Inmobiliarias — Plataforma de análisis de FIBRAs del mercado mexicano</title>
      <meta name="description" content="Descubre y analiza FIBRAs del mercado mexicano (BMV). Precios, fundamentales, distribuciones y noticias en tiempo real." />
      {/* canonical lo inyecta SpaMetadataMiddleware con el dominio configurado (App:BaseUrl);
          el client-side apuntaba al dominio incorrecto fibradis.mx — retirado en code review 11-2 */}

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

      <div className="container mx-auto px-4 py-6 space-y-8">
        <section aria-labelledby="heading-precio">
          <h2 id="heading-precio" className="sr-only">Precios de mercado</h2>
          <PriceCarousel />
        </section>
        <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_22rem] xl:items-start">
          <div className="min-w-0 space-y-6">
            <section aria-labelledby="heading-ranking">
              <h2 id="heading-ranking" className="sr-only">Ganadores y perdedores del día</h2>
              <GainersLosers />
            </section>

            <section aria-labelledby="heading-universo">
              <h2 id="heading-universo" className="sr-only">Universo FIBRAS</h2>
              <FibraUniverseTable />
            </section>
          </div>

          <aside className="xl:sticky xl:top-28">
            <section aria-labelledby="heading-noticias">
              <h2 id="heading-noticias" className="sr-only">Noticias recientes</h2>
              <NewsSection />
            </section>
          </aside>
        </div>
      </div>
    </>
  )
}
