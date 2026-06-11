import { useState, type ReactNode } from 'react'
import { ISR_RATE, calcIsr, parseInput } from './isrCalculator'
import { calcYield } from './yieldCalculator'
import { formatMoney, formatPercent } from '@/modules/portafolio/portfolio-format'

const ISR_RATE_LABEL = `${(ISR_RATE * 100).toFixed(0)}%`

function Card({
  title,
  eyebrow,
  description,
  children,
}: {
  title: string
  eyebrow: string
  description: string
  children: ReactNode
}) {
  return (
    <section className="rounded-3xl border border-border bg-surface-elevated p-5 shadow-sm">
      <div className="space-y-1.5">
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">{eyebrow}</p>
        <h2 className="font-playfair text-2xl font-semibold text-foreground">{title}</h2>
        <p className="max-w-xl text-sm leading-6 text-muted-foreground">{description}</p>
      </div>
      <div className="mt-5">{children}</div>
    </section>
  )
}

export function HerramientasPage() {
  const [yieldPrice, setYieldPrice] = useState('')
  const [yieldDistribution, setYieldDistribution] = useState('')
  const [isrDistribution, setIsrDistribution] = useState('')
  const [isrUnits, setIsrUnits] = useState('')

  const yieldResult = calcYield(parseInput(yieldDistribution), parseInput(yieldPrice))
  const isrResult = calcIsr(parseInput(isrDistribution), parseInput(isrUnits))

  return (
    <>
      {/* title/meta/canonical/og los inyecta SpaMetadataMiddleware (SpaMetadataProvider, ruta /herramientas) —
          el og:url con dominio fibradis.mx (deuda 10-2) se resolvió moviendo la metadata al servidor */}
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8 space-y-3">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-primary">Herramientas públicas</p>
          <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
            Calculadoras para FIBRAs
          </h1>
          <p className="max-w-3xl text-sm leading-6 text-muted-foreground md:text-base">
            Estima el rendimiento anualizado de una distribución y el ISR retenido sobre el flujo recibido sin salir del sitio.
          </p>
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          <Card
            eyebrow="Rendimiento"
            title="Calculadora Yield"
            description="Ingresa el precio actual y la distribución trimestral para obtener el yield anualizado en porcentaje."
          >
            <div className="grid gap-4">
              <div className="grid gap-4 sm:grid-cols-2">
                <label className="space-y-1.5">
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Precio actual (MXN)
                  </span>
                  <input
                    type="number"
                    inputMode="decimal"
                    step="0.0001"
                    value={yieldPrice}
                    onChange={(event) => setYieldPrice(event.target.value)}
                    className="flex h-10 w-full rounded-lg border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                    placeholder="100"
                  />
                </label>
                <label className="space-y-1.5">
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Distribución trimestral (MXN)
                  </span>
                  <input
                    type="number"
                    inputMode="decimal"
                    step="0.0001"
                    value={yieldDistribution}
                    onChange={(event) => setYieldDistribution(event.target.value)}
                    className="flex h-10 w-full rounded-lg border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                    placeholder="0.62"
                  />
                </label>
              </div>

              <div className="rounded-2xl border border-border bg-muted/20 px-4 py-4" aria-live="polite">
                <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Yield anualizado</p>
                <p className="mt-1 text-3xl font-semibold tabular-nums text-foreground">
                  {yieldResult != null ? formatPercent(yieldResult) : '—'}
                </p>
                <p className="mt-1 text-xs leading-5 text-muted-foreground">
                  Cálculo anualizado a partir de cuatro distribuciones por año.
                </p>
              </div>
            </div>
          </Card>

          <Card
            eyebrow="Impuestos"
            title="Calculadora ISR"
            description="La versión pública parte de la distribución completa como base conservadora y muestra el ISR estimado."
          >
            <div className="space-y-4">
              <div className="rounded-xl border border-amber-200 bg-amber-50/80 px-4 py-3 text-sm leading-6 text-amber-900 dark:border-amber-900/50 dark:bg-amber-950/25 dark:text-amber-100">
                La calculadora asume que el 100% es resultado fiscal. Para un cálculo exacto, ingresa el desglose fiscal de tu brokerage.
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <label className="space-y-1.5">
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Distribución por CBFI
                  </span>
                  <input
                    type="number"
                    inputMode="decimal"
                    step="0.0001"
                    value={isrDistribution}
                    onChange={(event) => setIsrDistribution(event.target.value)}
                    className="flex h-10 w-full rounded-lg border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                    placeholder="0.62"
                  />
                </label>
                <label className="space-y-1.5">
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Número de CBFIs
                  </span>
                  <input
                    type="number"
                    inputMode="numeric"
                    step="1"
                    value={isrUnits}
                    onChange={(event) => setIsrUnits(event.target.value)}
                    className="flex h-10 w-full rounded-lg border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                    placeholder="500"
                  />
                </label>
              </div>

              <div className="overflow-hidden rounded-xl border border-border" aria-live="polite">
                <table className="w-full text-sm">
                  <tbody className="divide-y divide-border">
                    <MetricRow label="Distribución bruta (est.)" value={formatMoney(isrResult.taxableGross)} />
                    <MetricRow label={`ISR estimado (${ISR_RATE_LABEL})`} value={formatMoney(isrResult.isr)} />
                    <MetricRow label="Neto estimado" value={formatMoney(isrResult.net)} emphasized />
                  </tbody>
                </table>
              </div>

              <p className="text-xs leading-5 text-muted-foreground">
                El cálculo usa la misma tasa provisional de retención del {ISR_RATE_LABEL} que la ficha pública.
              </p>
            </div>
          </Card>
        </div>
      </div>
    </>
  )
}

function MetricRow({ label, value, emphasized = false }: { label: string; value: string; emphasized?: boolean }) {
  return (
    <tr className={emphasized ? 'bg-muted/20' : undefined}>
      <td className="px-4 py-2.5 text-muted-foreground">{label}</td>
      <td className={`px-4 py-2.5 text-right tabular-nums ${emphasized ? 'font-semibold text-foreground' : 'text-foreground'}`}>
        {value}
      </td>
    </tr>
  )
}
