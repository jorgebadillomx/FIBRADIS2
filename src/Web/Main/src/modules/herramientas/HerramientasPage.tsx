import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router'
import { ArrowUpRight } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { fetchCalculadoraFibras, fetchIndicadores } from '@/api/fibrasApi'
import { formatMoney, formatPercent } from '@/modules/portafolio/portfolio-format'
import {
  calcFibraVsCetes,
  calcMetaRenta,
  calcRetornoTotal,
  parseNumberInput,
} from './herramientas-logic'

const HORIZON_OPTIONS = [1, 3, 5, 10] as const

export function HerramientasPage() {
  const [fibraMonto, setFibraMonto] = useState('100000')
  const [fibraYield, setFibraYield] = useState('10')
  const [fibraCetes, setFibraCetes] = useState('')
  const [fibraHorizonte, setFibraHorizonte] = useState<(typeof HORIZON_OPTIONS)[number]>(5)
  const [metaRentaMensual, setMetaRentaMensual] = useState('5000')
  const [metaYield, setMetaYield] = useState('9')
  const [retornoPrecioCompra, setRetornoPrecioCompra] = useState('20')
  const [retornoPrecioActual, setRetornoPrecioActual] = useState('22')
  const [retornoDistribuciones, setRetornoDistribuciones] = useState('2.4')
  const [retornoIsr, setRetornoIsr] = useState('0.72')
  const [cetesTouched, setCetesTouched] = useState(false)

  const indicadoresQuery = useQuery({
    queryKey: ['herramientas', 'indicadores'],
    queryFn: fetchIndicadores,
    staleTime: 5 * 60 * 1000,
    retry: false,
  })
  const cetesApiValue = indicadoresQuery.data?.cetes28d ?? null

  const precioReferenciaQuery = useQuery({
    queryKey: ['herramientas', 'precio-referencia'],
    queryFn: async () => {
      try {
        return await fetchCalculadoraFibras()
      } catch {
        return []
      }
    },
    staleTime: 60 * 60 * 1000,
    retry: false,
  })

  useEffect(() => {
    if (cetesTouched) return
    if (cetesApiValue == null) return
    setFibraCetes(cetesApiValue.toFixed(2))
  }, [cetesApiValue, cetesTouched])

  const precioRefPromedio = useMemo(() => {
    const prices = (precioReferenciaQuery.data ?? [])
      .map((fibra) => parseNumberInput(String(fibra.precioActual ?? '')))
      .filter((price): price is number => price != null && price > 0)

    if (prices.length === 0) return null
    return prices.reduce((sum, price) => sum + price, 0) / prices.length
  }, [precioReferenciaQuery.data])

  const fibraMontoValue = parseNumberInput(fibraMonto)
  const fibraYieldValue = parseNumberInput(fibraYield)
  const fibraCetesValue = parseNumberInput(fibraCetes)
  const fibraScenario = calcFibraVsCetes(
    fibraMontoValue ?? 0,
    fibraYieldValue ?? 0,
    fibraCetesValue ?? 0,
    fibraHorizonte,
  )
  const fibraInputsValid =
    fibraMontoValue != null &&
    fibraYieldValue != null &&
    fibraCetesValue != null &&
    fibraMontoValue > 0 &&
    fibraHorizonte > 0

  const metaRentaMensualValue = parseNumberInput(metaRentaMensual)
  const metaYieldValue = parseNumberInput(metaYield)
  const metaResult = calcMetaRenta(
    metaRentaMensualValue ?? 0,
    metaYieldValue ?? 0,
    precioRefPromedio ?? undefined,
  )

  const retornoPrecioCompraValue = parseNumberInput(retornoPrecioCompra)
  const retornoPrecioActualValue = parseNumberInput(retornoPrecioActual)
  const retornoDistribucionesValue = parseNumberInput(retornoDistribuciones)
  const retornoIsrValue = parseNumberInput(retornoIsr)
  const retornoInputsValid =
    retornoPrecioCompraValue != null &&
    retornoPrecioActualValue != null &&
    retornoDistribucionesValue != null &&
    retornoIsrValue != null
  const retornoResult = retornoInputsValid
    ? calcRetornoTotal(
        retornoPrecioCompraValue,
        retornoPrecioActualValue,
        retornoDistribucionesValue,
        retornoIsrValue,
      )
    : {
        plusvaliaPct: null,
        yieldNetoPct: null,
        retornoTotalPct: null,
      }

  return (
    <>
      <title>Herramientas privadas — FIBRADIS</title>
      <meta
        name="description"
        content="Hub privado de análisis con accesos rápidos a FIBRAs, CETES y retorno total para tomar decisiones de inversión con más contexto."
      />
      <meta property="og:title" content="Herramientas privadas — FIBRADIS" />
      <meta
        property="og:description"
        content="Hub privado de análisis con accesos rápidos a FIBRAs, CETES y retorno total para tomar decisiones de inversión con más contexto."
      />
      <meta property="og:type" content="website" />

      <div className="relative overflow-hidden bg-[radial-gradient(circle_at_15%_10%,rgba(194,65,12,0.14),transparent_28%),radial-gradient(circle_at_85%_20%,rgba(15,118,110,0.10),transparent_24%),linear-gradient(180deg,rgba(10,14,26,0.02),transparent_28%)]">
        <div className="container mx-auto px-4 py-8 md:py-10">
          <header className="max-w-4xl space-y-4">
            <div className="inline-flex items-center gap-2 rounded-full border border-border bg-surface-elevated px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.28em] text-primary shadow-sm">
              Hub privado
            </div>
            <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
              Herramientas para decidir con más contexto
            </h1>
            <p className="max-w-3xl text-sm leading-6 text-muted-foreground md:text-base">
              Cruza rendimiento, ingreso objetivo y retorno total sin salir de la plataforma. Los accesos
              rápidos conectan con las superficies donde ya existe contexto detallado.
            </p>
          </header>

          <section className="mt-8 rounded-3xl border border-border bg-surface-elevated/95 p-5 shadow-sm backdrop-blur">
            <div className="flex items-start justify-between gap-4">
              <div className="space-y-2">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                  Accesos rápidos
                </p>
                <h2 className="font-playfair text-2xl font-semibold text-foreground">
                  Entra a las superficies clave
                </h2>
              </div>
            </div>

            <div className="mt-5 grid gap-4 md:grid-cols-3">
              <HubLinkCard
                href="/comparar"
                title="Comparador de FIBRAs"
                description="Compara precio, yield, fundamentales y score de hasta 4 emisoras lado a lado."
              />
              <HubLinkCard
                href="/fibras"
                title="Fichas de FIBRAs"
                description="Explora el catálogo completo con precio, distribuciones, fundamentales y análisis."
              />
              <HubLinkCard
                href="/oportunidades"
                title="Promediar en Oportunidades"
                description="Simula cuántos títulos adicionales necesitas para mejorar tu costo de entrada."
              />
            </div>
          </section>

          <div className="mt-8 grid gap-6 xl:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
            <section className="rounded-3xl border border-border bg-surface-elevated p-5 shadow-sm">
              <SectionHeader
                eyebrow="Rentabilidad comparada"
                title="FIBRAs vs CETES"
                description="Compara crecimiento compuesto con tasas netas estimadas y horizonte flexible."
              />

              <div className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                <NumberField
                  label="Monto (MXN)"
                  value={fibraMonto}
                  onChange={setFibraMonto}
                  placeholder="100000"
                />
                <NumberField
                  label="Yield FIBRA (%)"
                  value={fibraYield}
                  onChange={setFibraYield}
                  placeholder="10"
                />
                <NumberField
                  label="Tasa CETES 28d (%)"
                  value={fibraCetes}
                  onChange={(value) => {
                    setCetesTouched(true)
                    setFibraCetes(value)
                  }}
                  placeholder="ej. 9.50"
                />
                <label className="space-y-1.5">
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Horizonte
                  </span>
                  <select
                    value={fibraHorizonte}
                    onChange={(event) => setFibraHorizonte(Number(event.target.value) as (typeof HORIZON_OPTIONS)[number])}
                    className="flex h-11 w-full rounded-xl border border-input bg-background px-3 text-sm text-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                  >
                    {HORIZON_OPTIONS.map((option) => (
                      <option key={option} value={option}>
                        {option} año{option === 1 ? '' : 's'}
                      </option>
                    ))}
                  </select>
                </label>
              </div>

              <div className="mt-5 overflow-hidden rounded-2xl border border-border">
                <table className="w-full text-sm">
                  <thead className="bg-muted/30 text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    <tr>
                      <th className="px-4 py-3 text-left">Métrica</th>
                      <th className="px-4 py-3 text-right">FIBRA</th>
                      <th className="px-4 py-3 text-right">CETES</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    <MetricRow
                      label="Capital final estimado"
                      fibraValue={fibraInputsValid ? formatMoney(fibraScenario.fibra.capitalFinal) : '—'}
                      cetesValue={fibraInputsValid ? formatMoney(fibraScenario.cetes.capitalFinal) : '—'}
                    />
                    <MetricRow
                      label="Renta acumulada neta de ISR"
                      fibraValue={fibraInputsValid ? formatMoney(fibraScenario.fibra.rentaAcumuladaNeta) : '—'}
                      cetesValue={fibraInputsValid ? formatMoney(fibraScenario.cetes.rentaAcumuladaNeta) : '—'}
                    />
                    <MetricRow
                      label="Rendimiento total %"
                      fibraValue={fibraInputsValid ? formatPercent(fibraScenario.fibra.rendimientoTotalPct) : '—'}
                      cetesValue={fibraInputsValid ? formatPercent(fibraScenario.cetes.rendimientoTotalPct) : '—'}
                    />
                  </tbody>
                </table>
              </div>

              <p className="mt-3 text-xs leading-5 text-muted-foreground">
                El supuesto neto aplica 70% sobre distribuciones de FIBRA y 80% sobre CETES. Si el backend ya
                trae una tasa CETES 28d, el campo se prellena automáticamente.
              </p>
            </section>

            <section className="rounded-3xl border border-border bg-surface-elevated p-5 shadow-sm">
              <SectionHeader
                eyebrow="Ingreso objetivo"
                title="Meta de renta"
                description="Calcula el capital requerido para una renta mensual y, si hay referencia, los títulos aproximados."
              />

              <div className="mt-5 grid gap-4 sm:grid-cols-2">
                <NumberField
                  label="Renta mensual objetivo (MXN)"
                  value={metaRentaMensual}
                  onChange={setMetaRentaMensual}
                  placeholder="5000"
                />
                <NumberField
                  label="Yield estimado (%)"
                  value={metaYield}
                  onChange={setMetaYield}
                  placeholder="9"
                />
              </div>

              <div className="mt-5 grid gap-4 sm:grid-cols-2">
                <OutputTile
                  label="Capital necesario"
                  value={formatMoney(metaResult.capitalNecesario)}
                  helper="Basado en la renta mensual objetivo y el yield estimado."
                />
                <OutputTile
                  label="CBFIs estimados"
                  value={metaResult.cbfisEstimados != null ? formatInteger(metaResult.cbfisEstimados) : '—'}
                  helper={
                    precioRefPromedio != null
                      ? `Usa un precio promedio de ${formatMoney(precioRefPromedio)} como referencia.`
                      : 'No hay precio de referencia disponible.'
                  }
                />
              </div>

              <div className="mt-4 rounded-2xl border border-border bg-muted/20 px-4 py-3 text-sm text-muted-foreground">
                Validación inversa:{' '}
                <span className="font-medium text-foreground">
                  {formatMoney(metaResult.rentaMensualBrutaEstimada)}
                </span>{' '}
                al mes con el capital calculado.
              </div>
            </section>
          </div>

          <section className="mt-6 rounded-3xl border border-border bg-surface-elevated p-5 shadow-sm">
            <SectionHeader
              eyebrow="Retorno real"
              title="Retorno total"
              description="Integra precio de compra, precio actual, distribuciones e ISR retenido en una sola vista."
            />

            <div className="mt-5 grid gap-4 lg:grid-cols-4">
              <NumberField
                label="Precio de compra"
                value={retornoPrecioCompra}
                onChange={setRetornoPrecioCompra}
                placeholder="20"
              />
              <NumberField
                label="Precio actual"
                value={retornoPrecioActual}
                onChange={setRetornoPrecioActual}
                placeholder="22"
              />
              <NumberField
                label="Distribuciones TTM"
                value={retornoDistribuciones}
                onChange={setRetornoDistribuciones}
                placeholder="2.4"
              />
              <NumberField
                label="ISR retenido total"
                value={retornoIsr}
                onChange={setRetornoIsr}
                placeholder="0.72"
              />
            </div>

            <div className="mt-5 grid gap-4 md:grid-cols-3">
              <OutputTile
                label="Plusvalía %"
                value={formatPercent(retornoResult.plusvaliaPct)}
                helper="Comparación pura entre precio actual y precio de compra."
              />
              <OutputTile
                label="Yield neto recibido %"
                value={formatPercent(retornoResult.yieldNetoPct)}
                helper="Distribuciones netas después de ISR retenido."
              />
              <OutputTile
                label="Retorno total %"
                value={formatPercent(retornoResult.retornoTotalPct)}
                helper="Suma de plusvalía y flujo neto recibido."
              />
            </div>
          </section>
        </div>
      </div>
    </>
  )
}

function SectionHeader({
  eyebrow,
  title,
  description,
}: {
  eyebrow: string
  title: string
  description: string
}) {
  return (
    <div className="space-y-2">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">{eyebrow}</p>
      <h2 className="font-playfair text-2xl font-semibold text-foreground">{title}</h2>
      <p className="max-w-2xl text-sm leading-6 text-muted-foreground">{description}</p>
    </div>
  )
}

function HubLinkCard({
  href,
  title,
  description,
}: {
  href: string
  title: string
  description: string
}) {
  return (
    <Link
      to={href}
      className="group rounded-2xl border border-border bg-background/80 p-4 transition-all duration-150 hover:-translate-y-0.5 hover:border-primary hover:shadow-md"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="space-y-2">
          <p className="text-base font-semibold text-foreground">{title}</p>
          <p className="text-sm leading-6 text-muted-foreground">{description}</p>
        </div>
        <ArrowUpRight className="size-4 shrink-0 text-muted-foreground transition-transform duration-150 group-hover:translate-x-0.5 group-hover:-translate-y-0.5 group-hover:text-primary" />
      </div>
    </Link>
  )
}

function NumberField({
  label,
  value,
  onChange,
  placeholder,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  placeholder: string
}) {
  return (
    <label className="space-y-1.5">
      <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">{label}</span>
      <input
        type="number"
        inputMode="decimal"
        step="any"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        className="flex h-11 w-full rounded-xl border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
      />
    </label>
  )
}

function OutputTile({
  label,
  value,
  helper,
}: {
  label: string
  value: string
  helper: string
}) {
  return (
    <article className="rounded-2xl border border-border bg-background/70 p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">{label}</p>
      <p className="mt-2 text-2xl font-semibold tabular-nums text-foreground">{value}</p>
      <p className="mt-2 text-xs leading-5 text-muted-foreground">{helper}</p>
    </article>
  )
}

function MetricRow({
  label,
  fibraValue,
  cetesValue,
}: {
  label: string
  fibraValue: string
  cetesValue: string
}) {
  return (
    <tr>
      <th className="px-4 py-3 text-left font-medium text-foreground">{label}</th>
      <td className="px-4 py-3 text-right tabular-nums text-foreground">{fibraValue}</td>
      <td className="px-4 py-3 text-right tabular-nums text-foreground">{cetesValue}</td>
    </tr>
  )
}

function formatInteger(value: number): string {
  return value.toLocaleString('es-MX', { maximumFractionDigits: 0 })
}
