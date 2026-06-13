import { useMemo, useState } from 'react'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Link } from 'react-router'
import type { MarketCalendarEvent } from '@/api/calendarApi'
import { useCalendarEvents } from './useCalendarEvents'
import { CalendarGrid } from './CalendarGrid'
import { EventChip } from './EventChip'
import { useFibraSlugMap } from '@/shared/hooks/useFibraSlugMap'

export function CalendarioPage() {
  const [monthOffset, setMonthOffset] = useState(0)
  const { slugFor } = useFibraSlugMap()

  const monthAnchor = useMemo(() => {
    const now = new Date()
    return new Date(now.getFullYear(), now.getMonth() + monthOffset, 1)
  }, [monthOffset])

  const year = monthAnchor.getFullYear()
  const month = monthAnchor.getMonth() + 1

  const eventsQuery = useCalendarEvents(year, month)

  const events = eventsQuery.data ?? []
  const groupedEvents = useMemo(() => groupEventsByDate(events), [events])
  const monthLabel = monthAnchor.toLocaleDateString('es-MX', { month: 'long', year: 'numeric' })
  const summary = useMemo(() => buildSummary(events), [events])
  const upcomingEvents = useMemo(
    () =>
      [...events]
        .sort((a, b) => a.date.localeCompare(b.date) || a.ticker.localeCompare(b.ticker))
        .slice(0, 10),
    [events],
  )

  usePageTitle('Calendario de Eventos Corporativos FIBRAs | FIBRADIS')

  return (
    <>
      <div className="container mx-auto px-4 py-8">
        <div className="overflow-hidden rounded-[2rem] border border-border bg-[radial-gradient(circle_at_top_left,_rgba(15,118,110,0.16),_transparent_35%),linear-gradient(135deg,_rgba(255,255,255,0.98),_rgba(236,253,245,0.92))] shadow-sm">
          <div className="grid gap-6 px-6 py-8 lg:grid-cols-[minmax(0,1.2fr)_18rem] lg:items-start lg:px-8">
            <div className="space-y-4">
              <p className="text-xs font-semibold uppercase tracking-[0.26em] text-teal-700">
                Centro público
              </p>
              <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
                Calendario de eventos
                <span className="block text-primary">corporativos.</span>
              </h1>
              <p className="max-w-2xl text-sm leading-7 text-muted-foreground md:text-base">
                Sigue pagos, fechas ex derecho y avisos oficiales de FIBRAs mexicanas en una vista mensual pensada para lectura rápida.
              </p>
              <div className="flex flex-wrap gap-2">
                <span className="rounded-full border border-green-200 bg-green-100 px-3 py-1 text-xs font-semibold text-green-800">
                  Pagos
                </span>
                <span className="rounded-full border border-blue-200 bg-blue-100 px-3 py-1 text-xs font-semibold text-blue-800">
                  Ex derechos
                </span>
                <span className="rounded-full border border-slate-200 bg-white px-3 py-1 text-xs font-semibold text-slate-600">
                  Avisos BMV
                </span>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <SummaryCard label="Eventos" value={summary.total} />
              <SummaryCard label="FIBRAs" value={summary.tickers} />
              <SummaryCard label="Pagos" value={summary.payments} />
              <SummaryCard label="Ex derechos" value={summary.exRights} />
            </div>
          </div>
        </div>

        <div className="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1.45fr)_22rem]">
          <section className="rounded-[2rem] border border-border bg-background/90 p-4 shadow-sm md:p-5">
            <div className="flex flex-col gap-4 border-b border-border pb-4 md:flex-row md:items-center md:justify-between">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                  Vista mensual
                </p>
                <h2 className="mt-1 font-playfair text-3xl font-bold text-foreground">
                  {capitalizeFirst(monthLabel)}
                </h2>
              </div>

              <div className="flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  className="rounded-full border border-border bg-background px-3 py-2 text-sm font-medium text-foreground transition hover:border-primary hover:text-primary"
                  onClick={() => setMonthOffset((v) => v - 1)}
                >
                  Mes anterior
                </button>
                <button
                  type="button"
                  className="rounded-full border border-border bg-background px-3 py-2 text-sm font-medium text-foreground transition hover:border-primary hover:text-primary"
                  onClick={() => setMonthOffset(0)}
                >
                  Hoy
                </button>
                <button
                  type="button"
                  className="rounded-full border border-border bg-background px-3 py-2 text-sm font-medium text-foreground transition hover:border-primary hover:text-primary"
                  onClick={() => setMonthOffset((v) => v + 1)}
                >
                  Mes siguiente
                </button>
              </div>
            </div>

            {eventsQuery.isLoading ? (
              <div className="mt-4 grid grid-cols-7 gap-px overflow-hidden rounded-2xl border border-border bg-border">
                {Array.from({ length: 35 }).map((_, i) => (
                  <div key={i} className="min-h-28 animate-pulse bg-muted/70" />
                ))}
              </div>
            ) : eventsQuery.isError ? (
              <div className="mt-4 rounded-2xl border border-destructive/30 bg-destructive/5 px-4 py-5 text-sm text-destructive">
                {eventsQuery.error.message}
              </div>
            ) : (
              <>
                <CalendarGrid monthAnchor={monthAnchor} groupedEvents={groupedEvents} />

                <div className="mt-4 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                  <LegendChip tone="green">Pago</LegendChip>
                  <LegendChip tone="blue">Ex derecho</LegendChip>
                  <span>Los eventos se agrupan por fecha. Haz clic en un evento para ver el desglose completo.</span>
                </div>
              </>
            )}
          </section>

          <aside className="space-y-4">
            <section className="rounded-[2rem] border border-border bg-background/90 p-5 shadow-sm">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                    Próximos eventos
                  </p>
                  <h3 className="mt-1 text-xl font-semibold text-foreground">Agenda de mes</h3>
                </div>
                <span className="rounded-full border border-border px-3 py-1 text-xs font-semibold text-muted-foreground">
                  {events.length} registros
                </span>
              </div>

              <div className="mt-4 space-y-3">
                {upcomingEvents.length === 0 ? (
                  <p className="rounded-2xl border border-dashed border-border px-4 py-8 text-center text-sm text-muted-foreground">
                    Sin distribuciones registradas para este mes.
                  </p>
                ) : (
                  upcomingEvents.map((event) => (
                    <article
                      className="rounded-2xl border border-border bg-muted/25 p-4 transition hover:border-primary/35"
                      key={`${event.eventType}-${event.ticker}-${event.date}`}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <div className="flex flex-wrap items-center gap-2">
                            <EventChip eventType={event.eventType} />
                            <span className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
                              {formatDateLabel(event.date)}
                            </span>
                          </div>
                          <h4 className="mt-2 text-sm font-semibold text-foreground">{event.empresa}</h4>
                          <p className="text-xs font-medium text-primary">{event.ticker}</p>
                        </div>
                        <Link
                          className="rounded-full border border-border px-3 py-1.5 text-xs font-semibold text-foreground transition hover:border-primary hover:text-primary"
                          to={`/fibras/${slugFor(event.ticker)}`}
                        >
                          Ver ficha
                        </Link>
                      </div>

                      <dl className="mt-4 grid grid-cols-2 gap-2 text-xs">
                        <InfoTile label="Monto" value={formatCurrency(event.amountPerUnit)} />
                        <InfoTile
                          label="Desglose"
                          value={formatBreakdown(event.taxableAmount, event.capitalReturnAmount)}
                        />
                      </dl>

                      {event.avisoUrl ? (
                        <a
                          className="mt-3 inline-flex text-xs font-semibold text-teal-700 underline underline-offset-2 transition hover:text-teal-800"
                          href={event.avisoUrl}
                          rel="noreferrer"
                          target="_blank"
                        >
                          Aviso oficial
                        </a>
                      ) : null}
                    </article>
                  ))
                )}
              </div>
            </section>

            <section className="rounded-[2rem] border border-border bg-[linear-gradient(180deg,_rgba(15,118,110,0.10),_rgba(255,255,255,0.94))] p-5 shadow-sm">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-teal-700">
                Cómo leerlo
              </p>
              <ul className="mt-3 space-y-3 text-sm leading-6 text-slate-600">
                <li>• Un mismo día puede incluir el pago y la fecha ex derecho de la misma distribución.</li>
                <li>• Si hay aviso BMV disponible, se enlaza desde la tarjeta del evento.</li>
                <li>• La vista se construye directamente desde el calendario público de distribuciones.</li>
              </ul>
            </section>
          </aside>
        </div>
      </div>
    </>
  )
}

function LegendChip({ tone, children }: { tone: 'green' | 'blue'; children: string }) {
  const className =
    tone === 'green'
      ? 'border-green-200 bg-green-100 text-green-800'
      : 'border-blue-200 bg-blue-100 text-blue-800'

  return (
    <span className={`inline-flex items-center rounded-full border px-2.5 py-1 font-semibold ${className}`}>
      {children}
    </span>
  )
}

function SummaryCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-2xl border border-border bg-white/80 px-4 py-3 shadow-sm">
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{label}</p>
      <p className="mt-2 text-2xl font-semibold tracking-tight text-foreground">{value}</p>
    </div>
  )
}

function InfoTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-border bg-background px-3 py-2">
      <dt className="text-[10px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{label}</dt>
      <dd className="mt-1 text-sm font-semibold text-foreground">{value}</dd>
    </div>
  )
}

function groupEventsByDate(events: MarketCalendarEvent[]) {
  const map = new Map<string, MarketCalendarEvent[]>()

  for (const event of events) {
    const bucket = map.get(event.date)
    if (bucket) bucket.push(event)
    else map.set(event.date, [event])
  }

  for (const bucket of map.values()) {
    bucket.sort((a, b) => a.eventType.localeCompare(b.eventType) || a.ticker.localeCompare(b.ticker))
  }

  return map
}

function buildSummary(events: MarketCalendarEvent[]) {
  return {
    total: events.length,
    tickers: new Set(events.map((e) => e.ticker)).size,
    payments: events.filter((e) => e.eventType === 'Pago').length,
    exRights: events.filter((e) => e.eventType === 'ExDerecho').length,
  }
}

function formatBreakdown(taxableAmount: number | string | null, capitalReturnAmount: number | string | null) {
  const taxable = normalizeNumber(taxableAmount)
  const capital = normalizeNumber(capitalReturnAmount)
  const parts: string[] = []

  if (taxable != null) parts.push(`Fiscal $${taxable.toFixed(4)}`)
  if (capital != null) parts.push(`Capital $${capital.toFixed(4)}`)

  return parts.length > 0 ? parts.join(' · ') : 'Clasificación fiscal pendiente'
}

function formatCurrency(value: number | string) {
  const n = normalizeNumber(value)
  return n == null ? '—' : n.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' })
}

function formatDateLabel(value: string) {
  return new Date(`${value}T12:00:00`).toLocaleDateString('es-MX', { day: '2-digit', month: 'short' })
}

function capitalizeFirst(value: string) {
  return value.charAt(0).toUpperCase() + value.slice(1)
}

function normalizeNumber(value: number | string | null | undefined) {
  if (value == null) return null
  const n = typeof value === 'number' ? value : Number(value)
  return Number.isFinite(n) ? n : null
}
