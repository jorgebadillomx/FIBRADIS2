export function PrivacidadPage() {
  return (
    <div className="container mx-auto max-w-3xl px-4 py-12">
      <h1 className="font-playfair text-3xl font-semibold tracking-tight">Aviso de privacidad</h1>
      <p className="mt-2 text-sm text-muted-foreground">Última actualización: junio 2026</p>

      <div className="mt-8 space-y-6 text-sm leading-7 text-foreground/80">

        <section>
          <h2 className="font-semibold text-base text-foreground">1. Información de referencia, no asesoría de inversión</h2>
          <p className="mt-2">
            La información publicada en Fibras Inmobiliarias — precios, rendimientos, fundamentales, noticias y
            cualquier otro dato — tiene carácter exclusivamente informativo y orientativo. No constituye,
            ni debe interpretarse como, asesoría financiera, recomendación de inversión, oferta de compra
            o venta de valores, ni ningún otro tipo de servicio regulado. Las decisiones de inversión que
            tome el usuario son de su exclusiva responsabilidad.
          </p>
          <p className="mt-2">
            Fibras Inmobiliarias no garantiza la exactitud, integridad o actualidad de los datos mostrados.
            Los mercados financieros implican riesgos, incluida la posible pérdida total del capital
            invertido. Fibras Inmobiliarias no será responsable, bajo ninguna circunstancia, de pérdidas o daños
            derivados del uso de la información contenida en este sitio.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">2. Datos personales que recopilamos</h2>
          <p className="mt-2">
            Únicamente recopilamos la dirección de correo electrónico que el usuario proporciona al
            registrarse. No solicitamos ni almacenamos nombre, número telefónico, domicilio, ni ningún
            otro dato de identificación personal más allá del correo.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">3. Uso de los datos</h2>
          <p className="mt-2">
            El correo electrónico se utiliza exclusivamente para autenticar al usuario dentro de la
            plataforma. No se usa para envío de correos comerciales, boletines ni comunicaciones de
            marketing sin consentimiento explícito.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">4. Protección y cifrado</h2>
          <p className="mt-2">
            Los datos personales se almacenan cifrados en la base de datos mediante AES-256. Las
            contraseñas nunca se guardan en texto claro; se aplica un hash unidireccional (bcrypt) con
            factor de trabajo elevado. Las comunicaciones entre el navegador y el servidor se realizan
            exclusivamente a través de HTTPS.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">5. No compartimos sus datos</h2>
          <p className="mt-2">
            Los datos personales no se venden, ceden ni comparten con terceros, anunciantes ni
            plataformas de análisis de comportamiento. No se integran servicios de rastreo de publicidad
            (Google Ads, Meta Pixel, etc.).
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">6. Derechos del usuario</h2>
          <p className="mt-2">
            El usuario puede solicitar la corrección o eliminación de su cuenta y datos asociados en
            cualquier momento escribiendo a{' '}
            <a className="text-primary hover:underline" href="mailto:contacto@fibradis.mx">
              contacto@fibradis.mx
            </a>
            . La solicitud será atendida en un plazo máximo de 5 días hábiles.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">7. Cookies</h2>
          <p className="mt-2">
            Fibras Inmobiliarias utiliza únicamente cookies de sesión estrictamente necesarias para el
            funcionamiento de la autenticación. No se utilizan cookies de rastreo ni de terceros.
          </p>
        </section>

        <section>
          <h2 className="font-semibold text-base text-foreground">8. Cambios a este aviso</h2>
          <p className="mt-2">
            Cualquier modificación se publicará en esta página con la fecha de actualización. El uso
            continuado del sitio implica la aceptación de los términos vigentes.
          </p>
        </section>

      </div>
    </div>
  )
}
