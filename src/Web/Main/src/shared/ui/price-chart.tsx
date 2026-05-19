import { LineChart, Line, XAxis, YAxis, CartesianGrid } from 'recharts'
import { ChartContainer, ChartTooltip, ChartTooltipContent } from '@/shared/ui/chart'
import { toNum } from '@/shared/lib/format-time'

interface PriceChartProps {
  data: Array<{ date: string; close: number | string | null | undefined }>
}

const chartConfig = {
  close: { label: 'Precio', color: 'hsl(var(--primary))' },
}

// Inserts a null entry between non-consecutive dates so connectNulls={false} renders real gaps.
function withGaps(raw: PriceChartProps['data']): Array<{ date: string; close: number | null }> {
  const out: Array<{ date: string; close: number | null }> = []
  for (let i = 0; i < raw.length; i++) {
    out.push({ date: raw[i].date.slice(5), close: toNum(raw[i].close) })
    if (i < raw.length - 1) {
      const diff = Math.round(
        (new Date(raw[i + 1].date).getTime() - new Date(raw[i].date).getTime()) / 86_400_000
      )
      if (diff > 1) out.push({ date: raw[i + 1].date.slice(5), close: null })
    }
  }
  return out
}

export function PriceChart({ data }: PriceChartProps) {
  const points = withGaps(data)
  const hasData = points.some(p => p.close != null)

  if (!hasData) {
    return (
      <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-48">
        <p className="text-sm text-muted-foreground">Sin datos históricos disponibles</p>
      </div>
    )
  }

  return (
    <ChartContainer config={chartConfig} className="h-48 w-full">
      <LineChart data={points} margin={{ top: 4, right: 8, bottom: 4, left: 0 }}>
        <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
        <XAxis
          dataKey="date"
          tick={{ fontSize: 10 }}
          tickLine={false}
          axisLine={false}
          interval="preserveStartEnd"
        />
        <YAxis
          tick={{ fontSize: 10 }}
          tickLine={false}
          axisLine={false}
          width={48}
          tickFormatter={(v: number) => v.toFixed(2)}
          domain={['auto', 'auto']}
        />
        <ChartTooltip
          content={<ChartTooltipContent formatter={(v) => typeof v === 'number' ? `$${v.toFixed(2)}` : '—'} />}
        />
        <Line
          type="monotone"
          dataKey="close"
          stroke="var(--color-close)"
          strokeWidth={1.5}
          dot={false}
          connectNulls={false}
        />
      </LineChart>
    </ChartContainer>
  )
}
