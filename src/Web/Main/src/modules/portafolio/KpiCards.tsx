import { useEffect, useState } from 'react'
import type { components } from '@fibradis/shared-api-client'
import { formatMoney, formatPercent } from '@/modules/portafolio/portfolio-format'
import { cn } from '@/shared/lib/utils'
import { ChevronDown } from 'lucide-react'

type PortfolioKpisDto = components['schemas']['PortfolioKpisDto']

const EXTRA_KPIS_KEY = 'portfolio_extra_kpis_open'

interface KpiCardsProps {
  kpis: PortfolioKpisDto | null | undefined
}

function getToneClass(value: number | string | null | undefined): string {
  if (value == null) return 'text-foreground'
  const numericValue = typeof value === 'string' ? Number(value) : value
  if (numericValue > 0) return 'text-green-600'
  if (numericValue < 0) return 'text-red-600'
  return 'text-foreground'
}

function KpiCard({
  title,
  value,
  description,
  toneClassName = 'text-foreground',
  partial = false,
}: {
  title: string
  value: string
  description?: string | null
  toneClassName?: string
  partial?: boolean
}) {
  return (
    <article className="rounded-2xl border border-border bg-card/80 p-4 shadow-sm backdrop-blur-sm">
      <div className="mb-2 flex items-center gap-2 text-xs font-medium uppercase tracking-[0.18em] text-muted-foreground">
        <span>{title}</span>
        {partial && (
          <span className="rounded-full border border-amber-500/20 bg-amber-500/10 px-2 py-0.5 text-[10px] font-semibold tracking-[0.18em] text-amber-700">
            parcial
          </span>
        )}
      </div>
      <div className={`text-2xl font-semibold tracking-tight tabular-nums ${toneClassName}`}>
        {value}
      </div>
      {description ? (
        <div className="mt-1 text-xs text-muted-foreground">
          {description}
        </div>
      ) : null}
    </article>
  )
}

export function KpiCards({ kpis }: KpiCardsProps) {
  const [extraOpen, setExtraOpen] = useState(false)

  useEffect(() => {
    const stored = window.localStorage.getItem(EXTRA_KPIS_KEY)
    setExtraOpen(stored === '1')
  }, [])

  useEffect(() => {
    window.localStorage.setItem(EXTRA_KPIS_KEY, extraOpen ? '1' : '0')
  }, [extraOpen])

  if (kpis == null) return null

  const yieldPortafolio = kpis.yieldPortafolio ?? null
  const ingresoMensual = kpis.ingresoMensual ?? null

  return (
    <section className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
        <KpiCard title="Inversión Total" value={formatMoney(kpis.inversionTotal)} />
        <KpiCard
          title="Valor Total"
          value={formatMoney(kpis.valorTotal)}
          partial={kpis.isPartial}
        />
        <KpiCard
          title="Plusvalía %"
          value={formatPercent(kpis.plusvaliaTotal_Pct)}
          toneClassName={getToneClass(kpis.plusvaliaTotal_Pct)}
          partial={kpis.isPartial}
        />
        <KpiCard
          title="Ganancia $"
          value={formatMoney(kpis.plusvaliaTotal_Mxn)}
          toneClassName={getToneClass(kpis.plusvaliaTotal_Mxn)}
          partial={kpis.isPartial}
        />
        <KpiCard
          title="Yield del Portafolio"
          value={formatPercent(yieldPortafolio)}
          toneClassName={getToneClass(yieldPortafolio)}
          partial={kpis.isPartial}
        />
      </div>

      <div className="rounded-2xl border border-border bg-card/70 p-4 shadow-sm backdrop-blur-sm">
        <button
          type="button"
          onClick={() => setExtraOpen((current) => !current)}
          className="flex w-full items-center justify-between gap-3 text-left"
        >
          <div>
            <div className="text-xs font-medium uppercase tracking-[0.18em] text-muted-foreground">
              Más métricas
            </div>
            <div className="mt-1 text-sm text-muted-foreground">
              {extraOpen ? 'Ocultar rentas y desglose' : 'Ver rentas y desglose'}
            </div>
          </div>
          <ChevronDown className={cn('size-4 text-muted-foreground transition-transform', extraOpen && 'rotate-180')} />
        </button>

        {extraOpen ? (
          <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            <KpiCard
              title="Rentas Anuales Brutas"
              value={formatMoney(kpis.rentasAnualesBrutas)}
              description={ingresoMensual != null && Number(ingresoMensual) > 0 ? `${formatMoney(ingresoMensual)}/mes` : null}
            />
            <KpiCard
              title="Rentas Reales Brutas"
              value={formatMoney(kpis.rentasRealesBrutas)}
            />
            <KpiCard
              title="% Rentas del Portafolio"
              value={formatPercent(kpis.pctRentasPortafolio)}
            />
          </div>
        ) : null}
      </div>
    </section>
  )
}
