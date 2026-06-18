import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { useSiteContent } from '@/shared/hooks/useSiteContent'

export function AcercaPage() {
  const { data: siteContent } = useSiteContent()
  const contactEmail = siteContent?.contactEmail?.trim() || 'portafoliodefibras@gmail.com'

  usePageTitle(
    'Sobre Fibras Inmobiliarias — Metodología y Fuentes de Datos | Fibras Inmobiliarias',
    'Conoce la metodología de Fibras Inmobiliarias: fuentes de datos, cálculo de fundamentales (Cap Rate, NAV, NOI) y scores de oportunidad para FIBRAs mexicanas.',
  )

  return (
    <div className="container mx-auto max-w-3xl px-4 py-12">
      <h1 className="font-playfair text-3xl font-semibold tracking-tight">Acerca de Fibras Inmobiliarias</h1>
      <p className="mt-2 text-sm text-muted-foreground">
        Plataforma independiente de análisis de FIBRAs inmobiliarias en México
      </p>

      <div className="mt-8 space-y-6 text-sm leading-7 text-foreground/80">

        <section>
          <h2 className="font-semibold text-base text-foreground">Misión</h2>
          <p className="mt-2">
            Fibras Inmobiliarias nació en 2023 con un objetivo: centralizar en un solo lugar los datos dispersos
            de las FIBRAs mexicanas y hacerlos accesibles para el inversionista individual. Consolidamos
            precios en tiempo real, distribuciones históricas, métricas fundamentales y noticias del
            sector inmobiliario bursátil en una plataforma gratuita e independiente. No recibimos
            compensación de ningún fideicomiso, casa de bolsa ni emisora.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">Equipo editorial</h2>
          <p className="mt-2">
            Fibras Inmobiliarias fue creada en 2023 por un equipo especializado en mercados de capitales
            mexicanos con experiencia directa en el análisis de instrumentos bursátiles regulados por la
            CNBV. El equipo combina formación en finanzas, ingeniería de software y experiencia práctica
            en los mercados de la BMV y BIVA, con enfoque específico en fideicomisos de inversión en
            bienes raíces (FIBRAs).
          </p>
          <p className="mt-2">
            Toda la información publicada es revisada por el equipo editorial antes de su publicación.
            Las metodologías de cálculo de fundamentales (Cap Rate, NAV, NOI) se basan en los estándares
            utilizados por analistas de casas de bolsa mexicanas y se documentan en detalle en este mismo
            apartado. No tenemos relación comercial con ninguna emisora, fideicomiso ni casa de bolsa;
            nuestra independencia editorial es el núcleo de nuestra credibilidad.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">¿Qué es Fibras Inmobiliarias?</h2>
          <p className="mt-2">
            Fibras Inmobiliarias es una plataforma de análisis independiente dedicada a las FIBRAs (Fideicomisos de
            Inversión en Bienes Raíces) que cotizan en la Bolsa Mexicana de Valores. Consolida en un
            solo lugar precios en tiempo real, distribuciones históricas, métricas fundamentales y
            noticias del sector inmobiliario bursátil mexicano.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">Fuentes de datos</h2>
          <p className="mt-2">
            Los datos que publicamos provienen exclusivamente de fuentes primarias y públicas:
          </p>
          <ul className="mt-2 list-disc pl-5 space-y-1">
            <li>Cotizaciones de la <strong>Bolsa Mexicana de Valores (BMV)</strong> — precios en tiempo real durante el horario de mercado (8:30–15:00 h, tiempo Ciudad de México).</li>
            <li><strong>Reportes trimestrales</strong> de cada FIBRA ante la CNBV (Estados financieros, Informes de administración).</li>
            <li><strong>Comunicados de prensa</strong> y documentos de asambleas de fideicomisarios.</li>
            <li><strong>Páginas de relación con inversionistas</strong> de cada emisora.</li>
          </ul>
          <p className="mt-2">
            Los precios y distribuciones se actualizan de forma continua durante el horario de mercado.
            Los fundamentales se actualizan con cada publicación de reportes trimestrales. Los rankings
            se recalculan diariamente.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">Metodología de fundamentales</h2>
          <p className="mt-2">
            Los indicadores se calculan a partir de las cifras reportadas en los documentos públicos
            de cada FIBRA. A continuación las fórmulas:
          </p>
          <div className="mt-3 overflow-x-auto">
            <table className="w-full text-xs border-collapse">
              <thead>
                <tr className="border-b border-border">
                  <th scope="col" className="py-2 pr-4 text-left font-semibold text-foreground">Indicador</th>
                  <th scope="col" className="py-2 pr-4 text-left font-semibold text-foreground">Fórmula</th>
                  <th scope="col" className="py-2 text-left font-semibold text-foreground">Fuente</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                <tr>
                  <td className="py-2 pr-4 font-medium text-foreground">Cap Rate</td>
                  <td className="py-2 pr-4">NOI anualizado ÷ capitalización bursátil</td>
                  <td className="py-2 text-muted-foreground">Reporte trimestral + precio BMV</td>
                </tr>
                <tr>
                  <td className="py-2 pr-4 font-medium text-foreground">NAV por CBFI</td>
                  <td className="py-2 pr-4">(Activos totales − Pasivos totales) ÷ CBFIs en circulación</td>
                  <td className="py-2 text-muted-foreground">Balance trimestral</td>
                </tr>
                <tr>
                  <td className="py-2 pr-4 font-medium text-foreground">NOI Margin</td>
                  <td className="py-2 pr-4">NOI ÷ Ingresos totales</td>
                  <td className="py-2 text-muted-foreground">Estado de resultados</td>
                </tr>
                <tr>
                  <td className="py-2 pr-4 font-medium text-foreground">LTV</td>
                  <td className="py-2 pr-4">Deuda financiera total ÷ Activos totales</td>
                  <td className="py-2 text-muted-foreground">Balance trimestral</td>
                </tr>
                <tr>
                  <td className="py-2 pr-4 font-medium text-foreground">Yield TTM</td>
                  <td className="py-2 pr-4">Suma de distribuciones últimos 12 meses ÷ Precio actual</td>
                  <td className="py-2 text-muted-foreground">Historial dist. + precio BMV</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">Score de oportunidad</h2>
          <p className="mt-2">
            El score de oportunidad es un indicador compuesto que resume qué tan atractiva es una
            FIBRA en términos relativos. Combina cuatro dimensiones:
          </p>
          <ul className="mt-2 list-disc pl-5 space-y-1">
            <li><strong>Yield de distribución</strong> — comparado con el promedio histórico de la misma FIBRA.</li>
            <li><strong>Descuento o prima sobre el NAV</strong> — si el precio de mercado cotiza por debajo del valor neto de activos, la FIBRA tiene un descuento potencialmente atractivo.</li>
            <li><strong>Cap Rate relativo al sector</strong> — comparado con las FIBRAs del mismo tipo (industrial, comercial, hotelero, etc.).</li>
            <li><strong>Momentum de precio</strong> — tendencia reciente de la cotización como señal de confirmación.</li>
          </ul>
          <p className="mt-2">
            Las FIBRAs con mayor score presentan, en conjunto, indicadores de valoración más atractivos
            respecto a su propio historial y a su grupo de pares. El score no garantiza rendimientos
            futuros ni constituye una recomendación de inversión.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">Aviso</h2>
          <p className="mt-2">
            La información publicada en Fibras Inmobiliarias es de referencia y orientativa. No constituye asesoría
            de inversión, recomendación de compra o venta de valores, ni ningún servicio regulado por
            la CNBV. Consulte a un asesor financiero certificado antes de tomar decisiones de inversión.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">Contacto</h2>
          <p className="mt-2">
            Para consultas, reportes de errores o solicitudes relacionadas con datos personales:{' '}
            <a className="text-primary hover:underline" href={`mailto:${contactEmail}`}>
              {contactEmail}
            </a>
          </p>
        </section>

        <p className="mt-4 text-xs text-muted-foreground/60">Actualizado: Junio 2026</p>

      </div>
    </div>
  )
}
