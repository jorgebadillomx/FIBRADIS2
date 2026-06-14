import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { useSiteContent } from '@/shared/hooks/useSiteContent'

export function ContactoPage() {
  const { data: siteContent } = useSiteContent()
  const contactEmail = siteContent?.contactEmail?.trim() || 'contacto@fibradis.mx'

  usePageTitle(
    'Contacto | FIBRADIS',
    'Contacta con FIBRADIS para reportar errores en datos, solicitar eliminación de cuenta o cualquier consulta sobre la plataforma.',
  )

  return (
    <div className="container mx-auto max-w-3xl px-4 py-12">
      <h1 className="font-playfair text-3xl font-semibold tracking-tight">Contacto</h1>
      <p className="mt-2 text-sm text-muted-foreground">
        FIBRADIS es una plataforma de análisis independiente de FIBRAs inmobiliarias mexicanas.
      </p>

      <div className="mt-8 space-y-6 text-sm leading-7 text-foreground/80">

        <section>
          <h2 className="font-semibold text-base text-foreground">Correo electrónico</h2>
          <p className="mt-2">
            Para reportar errores en los datos, solicitudes de acceso o eliminación de cuenta, y
            cualquier otra consulta:{' '}
            <a className="text-primary hover:underline" href={`mailto:${contactEmail}`}>
              {contactEmail}
            </a>
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">Reportar un problema</h2>
          <p className="mt-2">
            Si encuentras un dato incorrecto o un error técnico en la plataforma, puedes reportarlo
            enviando un correo descriptivo con el nombre de la FIBRA afectada y la fecha en que
            observaste el problema.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">Aviso legal</h2>
          <p className="mt-2">
            La información publicada en FIBRADIS es de referencia y no constituye asesoría de inversión.
            Consulta nuestro{' '}
            <a className="text-primary hover:underline" href="/privacidad">
              aviso de privacidad
            </a>{' '}
            para más detalles sobre el tratamiento de datos personales.
          </p>
        </section>

      </div>
    </div>
  )
}
