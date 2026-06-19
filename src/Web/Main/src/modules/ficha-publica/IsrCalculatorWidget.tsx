import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { DEFAULT_ISR_RATE, calcIsr, parseInput } from '@/modules/herramientas/isrCalculator'
import { fetchFiscalRates } from '@/api/fiscalRatesApi'
import { formatMoney } from '@/modules/portafolio/portfolio-format'

interface IsrCalculatorWidgetProps {
  lastDistribution?: number | null
  taxableAmountPerUnit?: number | null
  capitalReturnAmountPerUnit?: number | null
}

const UNIT_FORMAT = new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
  minimumFractionDigits: 4,
  maximumFractionDigits: 4,
})

function formatPerUnit(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) return '—'
  return UNIT_FORMAT.format(value)
}

export function IsrCalculatorWidget({
  lastDistribution,
  taxableAmountPerUnit,
  capitalReturnAmountPerUnit,
}: IsrCalculatorWidgetProps) {
  const fiscalRatesQuery = useQuery({
    queryKey: ['fiscal-rates'],
    queryFn: fetchFiscalRates,
    staleTime: 10 * 60_000,
  })
  const isrRate = fiscalRatesQuery.data?.isrRetentionRate ?? DEFAULT_ISR_RATE
  const isrRateLabel = `${(isrRate * 100).toFixed(0)}%`

  const [distPerUnit, setDistPerUnit] = useState(
    lastDistribution != null ? String(lastDistribution) : '',
  )
  const [units, setUnits] = useState('')

  const result = useMemo(
    () => calcIsr(parseInput(distPerUnit), parseInput(units), taxableAmountPerUnit, isrRate),
    [distPerUnit, taxableAmountPerUnit, units, isrRate],
  )

  if (lastDistribution == null) {
    return (
      <div className="rounded-2xl border border-dashed border-border bg-muted/20 px-6 py-8 text-sm text-muted-foreground">
        Sin datos de distribución disponibles.
      </div>
    )
  }

  return (
    <div className="rounded-2xl border border-border bg-surface-elevated p-5 space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="space-y-1">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
            Distribuciones
          </p>
          <h3 className="font-playfair text-xl font-semibold text-foreground">
            Calculadora ISR
          </h3>
          <p className="max-w-2xl text-sm leading-6 text-muted-foreground">
            Ajusta la distribución por CBFI o el número de CBFIs para ver el ISR retenido y el neto estimado.
          </p>
        </div>

        <span className="rounded-full border border-border bg-muted px-2.5 py-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          ISR {isrRateLabel}
        </span>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <label className="space-y-1.5">
          <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Distribución por CBFI
          </span>
          <input
            id="isr-distribution-per-unit"
            name="distributionPerUnit"
            type="number"
            inputMode="decimal"
            step="0.0001"
            value={distPerUnit}
            onChange={(event) => setDistPerUnit(event.target.value)}
            className="flex h-10 w-full rounded-lg border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
          />
        </label>

        <label className="space-y-1.5">
          <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Tus CBFIs
          </span>
          <input
            id="isr-units"
            name="units"
            type="number"
            inputMode="numeric"
            step="1"
            value={units}
            onChange={(event) => setUnits(event.target.value)}
            className="flex h-10 w-full rounded-lg border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
            placeholder="1"
          />
        </label>
      </div>

      {result.isEstimate ? (
        <div className="rounded-xl border border-amber-200 bg-amber-50/80 px-4 py-3 text-sm leading-6 text-amber-900 dark:border-amber-900/50 dark:bg-amber-950/25 dark:text-amber-100">
          ⚠️ Desglose fiscal no disponible — ISR calculado sobre monto total (estimado conservador)
        </div>
      ) : null}

      <div className="overflow-hidden rounded-xl border border-border" aria-live="polite">
        <table className="w-full text-sm">
          <tbody className="divide-y divide-border">
            {result.isEstimate ? (
              <>
                <IsrRow label="Distribución bruta (est.)" value={formatMoney(result.taxableGross)} />
                <IsrRow label={`ISR estimado (${isrRateLabel})`} value={formatMoney(result.isr)} />
                <IsrRow label="Neto estimado" value={formatMoney(result.net)} emphasized />
              </>
            ) : (
              <>
                <IsrRow label="Resultado Fiscal (bruto)" value={formatMoney(result.taxableGross)} />
                <IsrRow label={`ISR retenido (${isrRateLabel})`} value={formatMoney(result.isr)} />
                <IsrRow label="Reembolso de Capital *" value={formatMoney(result.capitalReturn)} />
                <IsrRow label="Distribución neta" value={formatMoney(result.net)} emphasized />
              </>
            )}
          </tbody>
        </table>
      </div>

      {parseInput(units) === 0 ? (
        <p className="text-xs leading-5 text-muted-foreground">
          Valores por CBFI — ingresa tus CBFIs para ver el total.
        </p>
      ) : null}

      {!result.isEstimate ? (
        <p className="text-xs leading-5 text-muted-foreground">
          Desglose reportado por la API: resultado fiscal {formatPerUnit(taxableAmountPerUnit)} / CBFI ·
          reembolso de capital {formatPerUnit(capitalReturnAmountPerUnit)} / CBFI.
        </p>
      ) : null}

      <p className="text-xs leading-5 text-muted-foreground">
        * Reembolso de capital no sujeto a retención. Reduce tu costo base fiscal para la venta futura de CBFIs.
      </p>
      <p className="text-xs leading-5 text-muted-foreground">
        Tasa de retención provisional del {isrRateLabel} sobre resultado fiscal para personas físicas residentes en México
        (LISR Art. 188). No considera deducciones adicionales ni regímenes especiales.
      </p>
    </div>
  )
}

function IsrRow({ label, value, emphasized = false }: { label: string; value: string; emphasized?: boolean }) {
  return (
    <tr className={emphasized ? 'bg-muted/20' : undefined}>
      <td className="px-4 py-2.5 text-muted-foreground">{label}</td>
      <td className={`px-4 py-2.5 text-right tabular-nums ${emphasized ? 'font-semibold text-foreground' : 'text-foreground'}`}>
        {value}
      </td>
    </tr>
  )
}
