import { GainersLosers } from './GainersLosers'
import { FibraUniverseTable } from './FibraUniverseTable'
import { NewsSection } from './NewsSection'

export function HomeMarketSections() {
  return (
    <div className="container mx-auto px-4 py-6">
      {/* Columna izquierda (Ganadores/Perdedores + Universo) · Noticias como barra lateral de alto completo */}
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
  )
}
