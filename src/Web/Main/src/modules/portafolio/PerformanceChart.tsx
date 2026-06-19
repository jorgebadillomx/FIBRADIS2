import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { components } from '@fibradis/shared-api-client'
import {
  CartesianGrid,
  Line,
  LineChart,
  XAxis,
  YAxis,
} from 'recharts'
import { apiClient } from '@/api/fibrasApi'
import { ChartContainer, ChartTooltip, ChartTooltipContent } from '@/shared/ui/chart'
import { cn } from '@/shared/lib/utils'

type PortfolioPerformanceResponseDto = components['schemas']['PortfolioPerformanceResponseDto']
type PortfolioPerformancePointDto = components['schemas']['PortfolioPerformancePointDto']

type PerformanceRange = '30d' | '90d' | '1y' | 'all'
type SeriesKey = 'portfolio' | 'ipc' | 'sp500' | 'inpc'

const RANGE_OPTIONS: Array<{ value: PerformanceRange; label: string }> = [
  { value: '30d', label: '30D' },
  { value: '90d', label: '90D' },
  { value: '1y', label: '1Y' },
  { value: 'all', label: 'ALL' },
]

const chartConfig = {
  portfolio: { label: 'Mi Portafolio', color: 'var(--primary)' },
  ipc: { label: 'IPC BMV', color: '#0284c7' },
  sp500: { label: 'S&P 500', color: '#8b5cf6' },
  inpc: { label: 'Inflación (INPC)', color: '#f97316' },
} as const

const dateShortFmt = new Intl.DateTimeFormat('es-MX', {
  day: '2-digit',
  month: 'short',
})
const dateLongFmt = new Intl.DateTimeFormat('es-MX', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
})

export function PerformanceChart() {
  const [range, setRange] = useState<PerformanceRange>('30d')
  const [activeLines, setActiveLines] = useState<Set<SeriesKey>>(
    () => new Set<SeriesKey>(['portfolio', 'ipc', 'sp500', 'inpc'])
  )

  const performanceQuery = useQuery<PortfolioPerformanceResponseDto>({
    queryKey: ['portfolio', 'performance', range],
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/v1/portfolio/performance', {
        params: { query: { range } },
      })
      if (error || !data) throw new Error('No se pudo cargar el rendimiento del portafolio.')
      return data
    },
    staleTime: 60_000,
  })

  const mergedData = useMemo(
    () => mergeSeries(performanceQuery.data),
    [performanceQuery.data],
  )

  const hasData = mergedData.length > 0
  const activeDataPoints = mergedData.filter((point) =>
    Array.from(activeLines).some((key) => point[key] != null),
  )
  const yDomain = useMemo(() => computeDomain(activeDataPoints, activeLines), [activeDataPoints, activeLines])

  function toggleLine(key: SeriesKey) {
    setActiveLines((current) => {
      const next = new Set(current)
      if (next.has(key)) {
        next.delete(key)
      } else {
        next.add(key)
      }
      return next
    })
  }

  if (performanceQuery.isError) {
    return (
      <div className="rounded-2xl border border-destructive/20 bg-destructive/5 p-6">
        <p className="text-sm text-destructive">No se pudo cargar la gráfica de rendimiento.</p>
      </div>
    )
  }

  return (
    <section className="space-y-4 rounded-2xl border border-border bg-card p-4 shadow-sm">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-primary">
            Rendimiento vs benchmarks
          </p>
          <h2 className="mt-1 text-lg font-semibold text-foreground">
            Mi Portafolio, IPC BMV y S&P 500
          </h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Cambio porcentual desde el inicio del período seleccionado.
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          {RANGE_OPTIONS.map((option) => (
            <button
              key={option.value}
              type="button"
              onClick={() => setRange(option.value)}
              className={cn(
                'rounded-full border px-3 py-1.5 text-xs font-semibold uppercase tracking-[0.18em] transition-colors',
                range === option.value
                  ? 'border-primary bg-primary/10 text-primary'
                  : 'border-border bg-background text-muted-foreground hover:text-foreground',
              )}
            >
              {option.label}
            </button>
          ))}
        </div>
      </div>

      <div className="flex flex-wrap gap-2">
        {(['portfolio', 'ipc', 'sp500', 'inpc'] as const).map((key) => {
          const active = activeLines.has(key)
          const disabled = key === 'inpc' && !performanceQuery.data?.inpcSeries?.length
          return (
            <button
              key={key}
              type="button"
              onClick={() => toggleLine(key)}
              disabled={disabled}
              className={cn(
                'inline-flex items-center gap-2 rounded-full border px-3 py-1.5 text-xs font-medium transition-colors',
                disabled
                  ? 'cursor-not-allowed border-border bg-muted/30 text-muted-foreground'
                  : active
                  ? 'border-border bg-background text-foreground'
                  : 'border-border bg-muted/40 text-muted-foreground',
              )}
            >
              <span
                className="h-2.5 w-2.5 rounded-full"
                style={{ backgroundColor: chartConfig[key].color }}
              />
              {chartConfig[key].label}
            </button>
          )
        })}
      </div>

      {!hasData ? (
        <div className="flex h-72 items-center justify-center rounded-xl border border-dashed border-border bg-muted/10">
          <p className="text-sm text-muted-foreground">
            No hay datos suficientes para construir el historial.
          </p>
        </div>
      ) : (
        <div className="rounded-[1.25rem] border border-border/70 bg-linear-to-b from-background via-background to-muted/30 p-3 sm:p-4">
          <ChartContainer config={chartConfig} className="h-80 w-full">
            <LineChart data={mergedData} margin={{ top: 12, right: 12, bottom: 8, left: 8 }}>
              <CartesianGrid vertical={false} strokeDasharray="3 6" className="stroke-border/70" />
              <XAxis
                dataKey="date"
                tickLine={false}
                axisLine={false}
                tickMargin={10}
                minTickGap={24}
                tickFormatter={(value: string) => formatDateTick(value)}
              />
              <YAxis
                width={68}
                tickLine={false}
                axisLine={false}
                tickMargin={10}
                domain={yDomain}
                tickFormatter={(value: number) => formatPercentTick(value)}
              />
              <ChartTooltip
                cursor={{ stroke: 'var(--primary)', strokeOpacity: 0.18, strokeWidth: 1.5 }}
                content={
                  <ChartTooltipContent
                    indicator="line"
                    labelFormatter={(label) => formatTooltipLabel(label)}
                    formatter={(value, name) => {
                      if (typeof value !== 'number') return null
                      return (
                        <div className="flex min-w-[10rem] items-center justify-between gap-4">
                          <span className="text-muted-foreground">{String(name)}</span>
                          <span className="font-mono font-semibold tabular-nums text-foreground">
                            {formatPercentTick(value)}
                          </span>
                        </div>
                      )
                    }}
                  />
                }
              />

              <Line
                type="monotone"
                dataKey="portfolio"
                name={chartConfig.portfolio.label}
                stroke="var(--color-portfolio)"
                strokeWidth={2.5}
                dot={false}
                activeDot={{ r: 4.5, fill: 'var(--color-portfolio)', stroke: 'var(--color-background)', strokeWidth: 2 }}
                connectNulls={false}
                isAnimationActive={false}
                strokeOpacity={activeLines.has('portfolio') ? 1 : 0.15}
                hide={!mergedData.some((point) => point.portfolio != null)}
              />
              <Line
                type="monotone"
                dataKey="ipc"
                name={chartConfig.ipc.label}
                stroke="var(--color-ipc)"
                strokeWidth={2.2}
                dot={false}
                activeDot={{ r: 4.5, fill: 'var(--color-ipc)', stroke: 'var(--color-background)', strokeWidth: 2 }}
                connectNulls={false}
                isAnimationActive={false}
                strokeOpacity={activeLines.has('ipc') ? 1 : 0.15}
                hide={!mergedData.some((point) => point.ipc != null)}
              />
              <Line
                type="monotone"
                dataKey="sp500"
                name={chartConfig.sp500.label}
                stroke="var(--color-sp500)"
                strokeWidth={2.2}
                dot={false}
                activeDot={{ r: 4.5, fill: 'var(--color-sp500)', stroke: 'var(--color-background)', strokeWidth: 2 }}
                connectNulls={false}
                isAnimationActive={false}
                strokeOpacity={activeLines.has('sp500') ? 1 : 0.15}
                hide={!mergedData.some((point) => point.sp500 != null)}
              />
              <Line
                type="monotone"
                dataKey="inpc"
                name={chartConfig.inpc.label}
                stroke="var(--color-inpc)"
                strokeWidth={2.2}
                dot={false}
                activeDot={{ r: 4.5, fill: 'var(--color-inpc)', stroke: 'var(--color-background)', strokeWidth: 2 }}
                connectNulls={false}
                isAnimationActive={false}
                strokeOpacity={activeLines.has('inpc') ? 1 : 0.15}
                hide={!mergedData.some((point) => point.inpc != null)}
              />
            </LineChart>
          </ChartContainer>
        </div>
      )}
    </section>
  )
}

interface MergedPerformancePoint {
  date: string
  portfolio: number | null
  ipc: number | null
  sp500: number | null
  inpc: number | null
}

function mergeSeries(data: PortfolioPerformanceResponseDto | undefined): MergedPerformancePoint[] {
  if (!data) return []

  const byDate = new Map<string, MergedPerformancePoint>()

  addSeries(byDate, data.portfolioSeries, 'portfolio')
  addSeries(byDate, data.ipcSeries, 'ipc')
  addSeries(byDate, data.sp500Series, 'sp500')
  addSeries(byDate, data.inpcSeries, 'inpc')

  return Array.from(byDate.values()).sort((left, right) => left.date.localeCompare(right.date))
}

function addSeries(
  map: Map<string, MergedPerformancePoint>,
  series: PortfolioPerformancePointDto[] | null | undefined,
  key: SeriesKey,
) {
  if (!series) return
  for (const point of series) {
    const existing = map.get(point.date) ?? {
      date: point.date,
      portfolio: null,
      ipc: null,
      sp500: null,
      inpc: null,
    }

    existing[key] = toNumber(point.valuePct)
    map.set(point.date, existing)
  }
}

function computeDomain(data: MergedPerformancePoint[], activeLines: Set<SeriesKey>): [number, number] | ['auto', 'auto'] {
  const values = data.flatMap((point) =>
    (['portfolio', 'ipc', 'sp500', 'inpc'] as const)
      .filter((key) => activeLines.has(key))
      .map((key) => point[key])
      .filter((value): value is number => value != null),
  )

  if (values.length === 0) return ['auto', 'auto']

  const min = Math.min(0, ...values)
  const max = Math.max(0, ...values)
  const spread = max - min
  const padding = spread === 0 ? Math.max(Math.abs(max) * 0.1, 1) : spread * 0.15
  return [min - padding, max + padding]
}

function formatDateTick(value: string): string {
  const date = new Date(`${value}T00:00:00Z`)
  return dateShortFmt.format(date).replace('.', '')
}

function formatTooltipLabel(label: unknown): string {
  if (typeof label !== 'string') return '—'
  const date = new Date(`${label}T00:00:00Z`)
  return dateLongFmt.format(date).replace('.', '')
}

function formatPercentTick(value: number): string {
  const sign = value > 0 ? '+' : ''
  return `${sign}${value.toFixed(1)}%`
}

function toNumber(value: number | string | null | undefined): number | null {
  if (value == null) return null
  const numeric = typeof value === 'string' ? Number(value) : value
  return Number.isFinite(numeric) ? numeric : null
}
