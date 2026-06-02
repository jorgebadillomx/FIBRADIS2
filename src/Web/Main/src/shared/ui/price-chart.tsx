import {
  Area,
  AreaChart,
  CartesianGrid,
  Line,
  ReferenceLine,
  XAxis,
  YAxis,
} from 'recharts'
import { ChartContainer, ChartTooltip, ChartTooltipContent } from '@/shared/ui/chart'
import { buildPriceChartPoints, summarizePriceChart, type PriceChartInputPoint } from '@/shared/ui/price-chart.utils'

const dayMonthFmt = new Intl.DateTimeFormat('es-MX', { day: '2-digit', month: 'short' })
const monthYearFmt = new Intl.DateTimeFormat('es-MX', { month: 'short', year: '2-digit' })

interface PriceChartProps {
  data: PriceChartInputPoint[]
  periodLabel: string
}

const chartConfig = {
  close: { label: 'Precio' },
}

function formatCurrency(value: number | null) {
  return value == null ? '—' : `$${value.toFixed(2)}`
}

function formatChange(value: number | null, pct: number | null) {
  if (value == null || pct == null) return '—'
  const sign = value > 0 ? '+' : ''
  return `${sign}$${value.toFixed(2)} · ${sign}${pct.toFixed(2)}%`
}

function getYAxisDomain(min: number | null, max: number | null): [number, number] | ['auto', 'auto'] {
  if (min == null || max == null) return ['auto', 'auto']

  const spread = max - min
  const padding = spread === 0 ? Math.max(min * 0.02, 0.5) : spread * 0.18

  return [Math.max(0, min - padding), max + padding]
}

export function PriceChart({ data, periodLabel }: PriceChartProps) {
  const points = buildPriceChartPoints(data)
  const summary = summarizePriceChart(points)
  const multiYear = (points.at(0)?.date.slice(0, 4) ?? '') !== (points.at(-1)?.date.slice(0, 4) ?? '')

  if (summary.last == null) {
    return (
      <div className="flex h-72 items-center justify-center rounded-xl border border-border bg-muted/20">
        <p className="text-sm text-muted-foreground">Sin datos hist&oacute;ricos disponibles</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-3">
        <div className="rounded-xl border border-border/80 bg-background px-4 py-3">
          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
            &Uacute;ltimo cierre
          </p>
          <p className="mt-1 text-3xl font-semibold tabular-nums text-foreground">
            {formatCurrency(summary.last)}
          </p>
          <p className="mt-1 text-xs text-muted-foreground">Serie diaria {periodLabel}</p>
        </div>
        <div className="rounded-xl border border-border/80 bg-background px-4 py-3">
          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
            Variaci&oacute;n del periodo
          </p>
          <p className="mt-1 text-lg font-semibold tabular-nums text-foreground">
            {formatChange(summary.change, summary.changePct)}
          </p>
          <p className="mt-1 text-xs text-muted-foreground">Inicio vs. cierre del rango seleccionado</p>
        </div>
        <div className="rounded-xl border border-border/80 bg-background px-4 py-3">
          <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
            Rango observado
          </p>
          <p className="mt-1 text-lg font-semibold tabular-nums text-foreground">
            {formatCurrency(summary.min)} <span className="text-muted-foreground">a</span> {formatCurrency(summary.max)}
          </p>
          <p className="mt-1 text-xs text-muted-foreground">M&iacute;nimo y m&aacute;ximo del periodo</p>
        </div>
      </div>

      <div className="rounded-[1.25rem] border border-border/70 bg-linear-to-b from-background via-background to-muted/30 p-3 sm:p-4">
        <ChartContainer config={chartConfig} className="h-72 w-full">
          <AreaChart data={points} margin={{ top: 12, right: 12, bottom: 8, left: 8 }}>
            <defs>
              <linearGradient id="price-fill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="var(--primary)" stopOpacity={0.24} />
                <stop offset="70%" stopColor="var(--primary)" stopOpacity={0.08} />
                <stop offset="100%" stopColor="var(--primary)" stopOpacity={0.02} />
              </linearGradient>
            </defs>

            <CartesianGrid vertical={false} strokeDasharray="3 6" className="stroke-border/70" />

            <XAxis
              dataKey="date"
              tickLine={false}
              axisLine={false}
              tickMargin={10}
              minTickGap={24}
              interval="preserveStartEnd"
              tickFormatter={(value: string) => {
                const date = new Date(`${value}T00:00:00`)
                return multiYear
                  ? monthYearFmt.format(date).replace('.', '')
                  : dayMonthFmt.format(date)
              }}
            />

            <YAxis
              width={68}
              tickLine={false}
              axisLine={false}
              tickMargin={10}
              domain={getYAxisDomain(summary.min, summary.max)}
              tickFormatter={(value: number) => `$${value.toFixed(2)}`}
            />

            <ChartTooltip
              cursor={{ stroke: 'var(--primary)', strokeOpacity: 0.18, strokeWidth: 1.5 }}
              content={
                <ChartTooltipContent
                  indicator="line"
                  labelFormatter={(_, payload) => {
                    const item = payload[0]?.payload as { fullLabel?: string } | undefined
                    return item?.fullLabel ?? '—'
                  }}
                  formatter={(value) => (
                    <div className="flex min-w-[10rem] items-center justify-between gap-4">
                      <span className="text-muted-foreground">Cierre</span>
                      <span className="font-mono font-semibold tabular-nums text-foreground">
                        {typeof value === 'number' ? `$${value.toFixed(2)}` : '—'}
                      </span>
                    </div>
                  )}
                />
              }
            />

            <ReferenceLine
              y={summary.last}
              stroke="var(--primary)"
              strokeDasharray="4 4"
              strokeOpacity={0.2}
            />

            <Area
              type="monotone"
              dataKey="close"
              stroke="none"
              fill="url(#price-fill)"
              connectNulls={false}
              isAnimationActive={false}
            />

            <Line
              type="monotone"
              dataKey="close"
              stroke="var(--primary)"
              strokeWidth={2.5}
              dot={summary.visibleDot ? { r: 2.25, fill: 'var(--color-background)', stroke: 'var(--primary)', strokeWidth: 1.5 } : false}
              activeDot={{ r: 4.5, fill: 'var(--primary)', stroke: 'var(--color-background)', strokeWidth: 2 }}
              connectNulls={false}
              isAnimationActive={false}
            />
          </AreaChart>
        </ChartContainer>
      </div>
    </div>
  )
}
