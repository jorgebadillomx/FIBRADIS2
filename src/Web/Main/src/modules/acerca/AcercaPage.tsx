export function AcercaPage() {
  return (
    <div className="container mx-auto max-w-3xl px-4 py-12">
      <h1 className="font-playfair text-3xl font-semibold tracking-tight">Acerca de FIBRADIS</h1>
      <p className="mt-2 text-sm text-muted-foreground">
        Plataforma independiente de análisis de FIBRAs inmobiliarias en México
      </p>

      <div className="mt-8 space-y-6 text-sm leading-7 text-foreground/80">

        <section>
          <h2 className="font-semibold text-base text-foreground">¿Qué es FIBRADIS?</h2>
          <p className="mt-2">
            FIBRADIS es una plataforma de análisis independiente dedicada a las FIBRAs (Fideicomisos de
            Inversión en Bienes Raíces) que cotizan en la Bolsa Mexicana de Valores. Consolida en un
            solo lugar precios en tiempo real, distribuciones históricas, métricas fundamentales y
            noticias del sector inmobiliario bursátil mexicano.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">Metodología</h2>
          <p className="mt-2">
            Los datos de precios y distribuciones se obtienen de fuentes públicas: cotizaciones de la
            BMV, reportes trimestrales publicados por cada fideicomiso ante la CNBV y comunicados de
            prensa oficiales. Los fundamentales — Cap Rate, NAV, NOI Margin, LTV — se calculan a partir
            de las cifras reportadas en los documentos públicos de cada FIBRA.
          </p>
          <p className="mt-2">
            La información publicada es de referencia y orientativa. No constituye asesoría de inversión,
            recomendación de compra o venta de valores, ni ningún servicio regulado por la CNBV.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">Actualización de datos</h2>
          <p className="mt-2">
            Los precios y distribuciones se actualizan de forma continua durante el horario de mercado
            de la BMV (8:30–15:00 h, tiempo Ciudad de México). Los fundamentales se actualizan con
            cada publicación de reportes trimestrales.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">Contacto</h2>
          <p className="mt-2">
            Para consultas, reportes de errores o solicitudes relacionadas con datos personales:{' '}
            <a className="text-primary hover:underline" href="mailto:contacto@fibradis.mx">
              contacto@fibradis.mx
            </a>
          </p>
        </section>

      </div>
    </div>
  )
}
