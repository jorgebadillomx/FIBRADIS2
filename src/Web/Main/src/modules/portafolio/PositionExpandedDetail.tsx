import type { ReactNode } from 'react'
import type { components } from '@fibradis/shared-api-client'
import { formatMoney, formatPercent, formatVolume, toNumberOrNull } from './portfolio-format'

type PortfolioPositionDto = components['schemas']['PortfolioPositionDto']

interface PositionExpandedDetailProps {
  position: PortfolioPositionDto
}

function DetailStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-border/60 bg-background/80 px-3 py-2">
      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
        {label}
      </div>
      <div className="mt-1 text-sm font-medium text-foreground tabular-nums">{value}</div>
    </div>
  )
}

function DetailSection({
  title,
  subtitle,
  children,
}: {
  title: string
  subtitle?: string | null
  children: ReactNode
}) {
  return (
    <section className="rounded-2xl border border-border/70 bg-card p-4 shadow-sm">
      <div className="mb-3">
        <h3 className="text-sm font-semibold text-foreground">{title}</h3>
        {subtitle && <p className="mt-0.5 text-xs text-muted-foreground">{subtitle}</p>}
      </div>
      {children}
    </section>
  )
}

export function PositionExpandedDetail({ position }: PositionExpandedDetailProps) {
  const precioActual = toNumberOrNull(position.precioActual)
  const titulos = toNumberOrNull(position.titulos)
  const rentaAnual = toNumberOrNull(position.rentaAnual)
  const recentDistributions = position.recentDistributions ?? []
  const latestDistribution = recentDistributions[0]
  const latestDistributionAmount = toNumberOrNull(latestDistribution?.amountPerUnit)

  const quarterlyRent = rentaAnual == null ? null : rentaAnual / 4
  const annualYield =
    rentaAnual != null && titulos != null && precioActual != null && titulos > 0 && precioActual > 0
      ? (rentaAnual / (titulos * precioActual)) * 100
      : null
  const declaredYield =
    latestDistributionAmount != null && precioActual != null && precioActual > 0
      ? (latestDistributionAmount * 4 / precioActual) * 100
      : null

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
      <DetailSection title="Mi posición">
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          <DetailStat label="Títulos" value={formatVolume(position.titulos)} />
          <DetailStat label="Costo Promedio" value={formatMoney(position.costoPromedio)} />
          <DetailStat label="Costo Total Compra" value={formatMoney(position.costoTotalCompra)} />
          <DetailStat label="Valor Mercado" value={formatMoney(position.valorMercado)} />
          <DetailStat label="Plusvalía %" value={formatPercent(position.plusvaliaFilaPct)} />
          <DetailStat label="Plusvalía $" value={formatMoney(position.plusvaliaFilaMxn)} />
          <DetailStat label="YOC" value={formatPercent(position.yoc)} />
          <DetailStat label="% Portafolio" value={formatPercent(position.pctPortafolio)} />
        </div>
      </DetailSection>

      <DetailSection title="Mercado">
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          <DetailStat label="Precio Actual" value={formatMoney(position.precioActual)} />
          <DetailStat label="Cambio %" value={formatPercent(position.dailyChangePct)} />
          <DetailStat label="Volumen" value={formatVolume(position.volume)} />
          <DetailStat label="AVG 52S" value={formatMoney(position.week52Avg)} />
          <DetailStat label="Máx. 52S" value={formatMoney(position.week52High)} />
          <DetailStat label="Mín. 52S" value={formatMoney(position.week52Low)} />
        </div>
      </DetailSection>

      <DetailSection
        title="Fundamentales"
        subtitle={position.fundamentalsPeriod ?? null}
      >
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          <DetailStat label="Cap Rate" value={formatPercent(position.capRate)} />
          <DetailStat label="NAV/CBFI" value={formatMoney(position.navPerCbfi)} />
          <DetailStat label="LTV" value={formatPercent(position.ltv)} />
          <DetailStat label="Margen NOI" value={formatPercent(position.noiMargin)} />
          <DetailStat label="Margen FFO" value={formatPercent(position.ffoMargin)} />
        </div>
      </DetailSection>

      <DetailSection title="Distribuciones">
        <div className="space-y-4">
          <div>
            <div className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
              Últimas 4 distribuciones
            </div>
            {recentDistributions.length > 0 ? (
              <ul className="space-y-2">
                {recentDistributions.map((distribution, index) => (
                  <li
                    key={`${distribution.paymentDate}-${distribution.amountPerUnit}-${index}`}
                    className="flex items-center justify-between gap-3 rounded-xl border border-border/60 bg-background/80 px-3 py-2"
                  >
                    <span className="text-sm text-muted-foreground tabular-nums">
                      {distribution.paymentDate}
                    </span>
                    <span className="text-sm font-medium text-foreground tabular-nums">
                      {formatMoney(distribution.amountPerUnit)}
                    </span>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-sm text-muted-foreground">—</p>
            )}
          </div>

          <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
            <DetailStat label="Renta Trimestral Estimada" value={formatMoney(quarterlyRent)} />
            <DetailStat label="Renta Anual (TTM)" value={formatMoney(rentaAnual)} />
            <DetailStat label="Yield Estimado" value={formatPercent(annualYield)} />
            <DetailStat label="Yield Decretado" value={formatPercent(declaredYield)} />
          </div>
        </div>
      </DetailSection>
    </div>
  )
}
