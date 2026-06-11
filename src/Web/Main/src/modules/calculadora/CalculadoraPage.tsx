const RETENTION_ROWS = [
  {
    investor: 'Persona física residente en México',
    rate: '30% provisional',
    note: 'Retención sobre el resultado fiscal distribuido; acreditable en la declaración anual.',
  },
  {
    investor: 'Persona moral residente en México',
    rate: '30% provisional',
    note: 'Acumula la distribución a sus demás ingresos y acredita la retención.',
  },
  {
    investor: 'Residente en el extranjero',
    rate: 'Según tratado',
    note: 'Aplica la tasa del tratado para evitar la doble tributación; sin tratado, retención del 30%.',
  },
]

export function CalculadoraPage() {
  return (
    <>
      {/* title/meta/canonical/og los inyecta SpaMetadataMiddleware (SpaMetadataProvider, ruta /calculadora) —
          emitirlos también aquí duplica tags tras la hidratación de React 19 */}
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8 space-y-3">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-primary">Herramientas fiscales</p>
          <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
            Calculadora ISR para FIBRAs Inmobiliarias
          </h1>
          <p className="max-w-3xl text-sm leading-6 text-muted-foreground md:text-base">
            Cuando una FIBRA reparte una distribución, el fiduciario retiene el Impuesto Sobre la Renta antes de
            depositarla en tu cuenta. Esa retención no es opcional ni uniforme: depende de cuánto de la distribución
            corresponde a resultado fiscal, del número de CBFIs que posees y de tu régimen como inversionista. Esta
            página explica qué es el ISR en las distribuciones de FIBRAs, cómo se calcula paso a paso con la fórmula
            que usan las casas de bolsa y qué tasa de retención aplica en cada caso, para que puedas anticipar el
            flujo neto que realmente recibirás.
          </p>
        </div>

        <div
          className="mb-8 rounded-xl border border-amber-200 bg-amber-50/80 px-4 py-3 text-sm font-medium leading-6 text-amber-900 dark:border-amber-900/50 dark:bg-amber-950/25 dark:text-amber-100"
          role="status"
        >
          Calculadora interactiva próximamente
        </div>

        <div className="space-y-6">
          <section className="rounded-3xl border border-border bg-surface-elevated p-6 shadow-sm">
            <h2 className="font-playfair text-2xl font-semibold text-foreground">
              ¿Qué es el ISR en las distribuciones de FIBRAs?
            </h2>
            <div className="mt-4 space-y-3 text-sm leading-6 text-muted-foreground">
              <p>
                Las FIBRAs están obligadas a distribuir al menos el 95% de su resultado fiscal una vez al año. Ese
                resultado fiscal —la utilidad gravable del fideicomiso— es la base sobre la que se paga el Impuesto
                Sobre la Renta. A diferencia de las acciones, donde el dividendo puede llegar libre de retención, en
                las FIBRAs la institución fiduciaria retiene el ISR en el momento del pago: el monto que ves
                depositado ya es neto de impuesto.
              </p>
              <p>
                No toda la distribución paga ISR. Una parte puede ser reembolso de capital, que no es ingreso gravable
                sino devolución de tu propia inversión y reduce el costo fiscal de tus CBFIs. El desglose entre
                resultado fiscal y reembolso de capital lo publica cada FIBRA en el aviso de distribución y lo refleja
                tu casa de bolsa en la constancia anual.
              </p>
            </div>
          </section>

          <section className="rounded-3xl border border-border bg-surface-elevated p-6 shadow-sm">
            <h2 className="font-playfair text-2xl font-semibold text-foreground">
              ¿Cómo se calcula el ISR de distribuciones?
            </h2>
            <div className="mt-4 space-y-3 text-sm leading-6 text-muted-foreground">
              <p>
                El cálculo parte de la distribución por CBFI que anuncia la FIBRA y del número de certificados que
                posees. La fórmula básica de la retención es:
              </p>
              <p className="rounded-xl border border-border bg-muted/20 px-4 py-3 font-mono text-[13px] text-foreground">
                ISR retenido = Resultado fiscal por CBFI × Número de CBFIs × 30%
              </p>
              <p>
                El flujo neto que recibes es la distribución total menos esa retención, más la porción de reembolso de
                capital (que no se grava). Si la FIBRA no publica el desglose, el cálculo conservador asume que el
                100% de la distribución es resultado fiscal, lo que da la retención máxima posible. La tasa efectiva
                final puede ser menor: la retención es un pago provisional acreditable cuando presentas tu declaración
                anual.
              </p>
            </div>
          </section>

          <section className="rounded-3xl border border-border bg-surface-elevated p-6 shadow-sm">
            <h2 className="font-playfair text-2xl font-semibold text-foreground">Tasa de retención aplicable</h2>
            <p className="mt-4 text-sm leading-6 text-muted-foreground">
              La tasa depende del tipo de inversionista que recibe la distribución:
            </p>
            <div className="mt-4 overflow-hidden rounded-xl border border-border">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-muted/20 text-left">
                    <th scope="col" className="px-4 py-2.5 font-semibold text-foreground">
                      Tipo de inversionista
                    </th>
                    <th scope="col" className="px-4 py-2.5 font-semibold text-foreground">
                      Retención
                    </th>
                    <th scope="col" className="hidden px-4 py-2.5 font-semibold text-foreground md:table-cell">
                      Detalle
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {RETENTION_ROWS.map((row) => (
                    <tr key={row.investor}>
                      <td className="px-4 py-2.5 text-foreground">{row.investor}</td>
                      <td className="px-4 py-2.5 tabular-nums text-foreground">{row.rate}</td>
                      <td className="hidden px-4 py-2.5 text-muted-foreground md:table-cell">{row.note}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <p className="mt-3 text-xs leading-5 text-muted-foreground">
              Información general con base en la Ley del ISR vigente; no constituye asesoría fiscal. Consulta a tu
              contador para tu situación particular.
            </p>
          </section>
        </div>
      </div>
    </>
  )
}
