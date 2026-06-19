import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchPortfolioCalendar } from '@/api/portfolioCalendarApi'
import type { PortfolioCalendarEvent } from '@/api/portfolioCalendarApi'
import { fetchFiscalRates } from '@/api/fiscalRatesApi'
import { DEFAULT_ISR_RATE } from '@/modules/herramientas/isrCalculator'
import { formatMoney } from '@/modules/portafolio/portfolio-format'

const monthYearFmt = new Intl.DateTimeFormat('es-MX', {
  month: 'long',
  year: 'numeric',
  timeZone: 'UTC',
})
const dayMonthFmt = new Intl.DateTimeFormat('es-MX', {
  day: '2-digit',
  month: 'short',
  timeZone: 'UTC',
})

function parseDate(value: string): Date {
  return new Date(`${value}T00:00:00Z`)
}

function toNumber(value: number | string | null | undefined): number | null {
  if (value == null || value === '') return null
  const n = typeof value === 'string' ? Number(value) : value
  return Number.isFinite(n) ? n : null
}

export function PortafolioCalendario() {
  const { data: events = [], isLoading, isError } = useQuery({
    queryKey: ['portfolio', 'calendar'],
    queryFn: () => fetchPortfolioCalendar(),
    staleTime: 5 * 60_000,
  })
  const { data: fiscalRates } = useQuery({
    queryKey: ['fiscal-rates'],
    queryFn: fetchFiscalRates,
    staleTime: 10 * 60_000,
  })
  const isrRate = fiscalRates?.isrRetentionRate ?? DEFAULT_ISR_RATE

  if (isLoading) {
    return (
      <div className="rounded-2xl border border-border bg-card px-6 py-10 text-center">
        <p className="text-sm text-muted-foreground">Cargando distribuciones…</p>
      </div>
    )
  }

  if (isError) {
    return (
      <div className="rounded-2xl border border-border bg-card px-6 py-10 text-center">
        <p className="text-sm text-muted-foreground">No se pudieron cargar las distribuciones.</p>
      </div>
    )
  }

  if (events.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-border bg-card px-6 py-12 text-center">
        <p className="text-sm font-medium text-foreground">
          No hay distribuciones confirmadas en este período.
        </p>
        <p className="mt-1 text-sm text-muted-foreground">
          Los datos provienen del registro oficial de la BMV. Pueden tardar en reflejarse.
        </p>
      </div>
    )
  }

  const grouped = events.reduce((acc, evt) => {
    const key = monthYearFmt.format(parseDate(evt.paymentDate))
    const bucket = acc.get(key) ?? []
    bucket.push(evt)
    acc.set(key, bucket)
    return acc
  }, new Map<string, PortfolioCalendarEvent[]>())

  return (
    <div className="space-y-5">
      {Array.from(grouped.entries()).map(([monthLabel, monthEvents]) => (
        <section key={monthLabel} className="rounded-2xl border border-border bg-card shadow-sm">
          <div className="border-b border-border px-4 py-3">
            <h3 className="text-sm font-semibold capitalize text-foreground">{monthLabel}</h3>
          </div>

          <div className="divide-y divide-border">
            {monthEvents.map((evt) => (
              <CalendarEventRow key={`${evt.ticker}-${evt.paymentDate}`} evt={evt} isrRate={isrRate} />
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}

function CalendarEventRow({ evt, isrRate }: { evt: PortfolioCalendarEvent; isrRate: number }) {
  const titulos = toNumber(evt.titulos) ?? 0
  const totalAmount = toNumber(evt.totalAmount) ?? 0
  const totalTaxable = toNumber(evt.totalTaxable)
  const totalCapital = toNumber(evt.totalCapital)
  const hasBreakdown = totalTaxable !== null || totalCapital !== null
  const isr = totalTaxable !== null ? totalTaxable * isrRate : null
  const netEstimado = isr !== null
    ? (totalCapital ?? 0) + (totalTaxable ?? 0) - isr
    : null

  return (
    <article className="flex flex-col gap-4 px-4 py-4 sm:flex-row sm:items-start sm:justify-between">
      <div className="flex items-center gap-3">
        <EventLogo ticker={evt.ticker} logoUrl={evt.logoUrl} />
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <span className="font-mono font-semibold text-foreground">{evt.ticker}</span>
            <span className="text-xs text-muted-foreground">
              {titulos.toLocaleString('es-MX')} CBFIs
            </span>
          </div>
          <p className="truncate text-sm text-muted-foreground">{evt.nombre}</p>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {dayMonthFmt.format(parseDate(evt.paymentDate))}
          </p>
        </div>
      </div>

      <div className="min-w-[220px] space-y-1 text-sm sm:text-right">
        {hasBreakdown ? (
          <>
            <div className="flex justify-between gap-4 sm:justify-end">
              <span className="text-muted-foreground">Bruto</span>
              <span className="font-medium tabular-nums text-foreground">{formatMoney(totalAmount)}</span>
            </div>
            {totalTaxable !== null && (
              <>
                <div className="flex justify-between gap-4 sm:justify-end">
                  <span className="text-muted-foreground">Componente CUFIN</span>
                  <span className="tabular-nums text-foreground">{formatMoney(totalTaxable)}</span>
                </div>
                <div className="flex justify-between gap-4 sm:justify-end">
                  <span className="text-muted-foreground">ISR {(isrRate * 100).toFixed(0)}%</span>
                  <span className="tabular-nums text-red-600">-{formatMoney(isr!)}</span>
                </div>
              </>
            )}
            {totalCapital !== null && (
              <div className="flex justify-between gap-4 sm:justify-end">
                <span className="text-muted-foreground">Retorno capital</span>
                <span className="tabular-nums text-foreground">{formatMoney(totalCapital)}</span>
              </div>
            )}
            {netEstimado !== null && (
              <div className="flex justify-between gap-4 border-t border-border pt-1 sm:justify-end">
                <span className="font-semibold text-foreground">Neto estimado</span>
                <span className="font-semibold tabular-nums text-foreground">{formatMoney(netEstimado)}</span>
              </div>
            )}
          </>
        ) : (
          <>
            <div className="flex justify-between gap-4 sm:justify-end">
              <span className="text-muted-foreground">Total bruto</span>
              <span className="font-medium tabular-nums text-foreground">{formatMoney(totalAmount)}</span>
            </div>
            <div className="flex justify-end">
              <span className="rounded-full border border-amber-200 bg-amber-50 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-[0.15em] text-amber-800">
                clasificación fiscal pendiente
              </span>
            </div>
          </>
        )}
      </div>
    </article>
  )
}

function EventLogo({ ticker, logoUrl }: { ticker: string; logoUrl: string | null }) {
  const [failed, setFailed] = useState(false)

  if (logoUrl && !failed) {
    return (
      <img
        alt={ticker}
        className="h-10 w-10 shrink-0 rounded-lg border border-border bg-background object-contain p-1"
        onError={() => setFailed(true)}
        src={logoUrl}
      />
    )
  }

  return (
    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg border border-border bg-muted/60 text-[10px] font-bold text-muted-foreground">
      {ticker.slice(0, 5)}
    </div>
  )
}
