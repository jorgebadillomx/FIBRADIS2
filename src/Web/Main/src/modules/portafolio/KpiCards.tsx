import type { components } from '@fibradis/shared-api-client'
import { formatMoney, formatPercent } from '@/modules/portafolio/portfolio-format'

type PortfolioKpisDto = components['schemas']['PortfolioKpisDto']

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
  toneClassName = 'text-foreground',
  partial = false,
}: {
  title: string
  value: string
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
    </article>
  )
}

export function KpiCards({ kpis }: KpiCardsProps) {
  if (kpis == null) return null

  return (
    <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
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
      <KpiCard title="Rentas Anuales Brutas" value={formatMoney(kpis.rentasAnualesBrutas)} />
      <KpiCard title="Rentas Reales Brutas" value={formatMoney(kpis.rentasRealesBrutas)} />
      <KpiCard title="% Rentas del Portafolio" value={formatPercent(kpis.pctRentasPortafolio)} />
    </section>
  )
}
