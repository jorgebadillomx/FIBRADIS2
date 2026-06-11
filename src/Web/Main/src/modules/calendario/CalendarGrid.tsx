import type { MarketCalendarEvent } from '@/api/calendarApi'
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/ui/popover'
import { EventChip } from './EventChip'
import { formatDateOnly, endOfMonth } from './calendarUtils'

const WEEKDAY_LABELS = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom']

type CalendarCellData = {
  date: Date | null
  dateKey: string | null
  day: number | null
  events: MarketCalendarEvent[]
  isToday: boolean
}

export function CalendarGrid({
  monthAnchor,
  groupedEvents,
}: {
  monthAnchor: Date
  groupedEvents: Map<string, MarketCalendarEvent[]>
}) {
  const cells = buildCalendarCells(monthAnchor, groupedEvents)

  return (
    <div className="mt-4 grid grid-cols-7 gap-px overflow-hidden rounded-2xl border border-border bg-border">
      {WEEKDAY_LABELS.map((label) => (
        <div
          className="bg-muted/70 px-3 py-3 text-center text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground"
          key={label}
        >
          {label}
        </div>
      ))}

      {cells.map((cell, index) => (
        <CalendarCell key={`${cell.dateKey ?? 'pad'}-${index}`} cell={cell} />
      ))}
    </div>
  )
}

function CalendarCell({ cell }: { cell: CalendarCellData }) {
  if (!cell.date) {
    return <div className="min-h-28 bg-background/90" />
  }

  const isToday = cell.isToday

  return (
    <div className="min-h-28 bg-background/95 p-2.5 transition hover:bg-muted/20">
      <div className="flex items-start justify-between gap-2">
        <div
          className={[
            'inline-flex h-7 min-w-7 items-center justify-center rounded-full text-sm font-semibold tabular-nums',
            isToday ? 'bg-teal-600 text-white shadow-sm' : 'text-foreground',
          ].join(' ')}
        >
          {cell.day}
        </div>
        {cell.events.length > 0 ? (
          <span className="rounded-full border border-border bg-muted px-2 py-0.5 text-[10px] font-semibold uppercase tracking-[0.16em] text-muted-foreground">
            {cell.events.length}
          </span>
        ) : null}
      </div>

      <div className="mt-2 space-y-1.5">
        {cell.events.slice(0, 3).map((event) => (
          <Popover key={`${cell.dateKey}-${event.eventType}-${event.ticker}`}>
            <PopoverTrigger asChild>
              <button
                type="button"
                className="flex w-full items-center gap-1.5 rounded-lg border border-border/70 bg-white px-2 py-1 text-[11px] leading-4 shadow-[0_1px_0_rgba(15,23,42,0.02)] transition hover:border-primary/40 hover:shadow-sm"
              >
                <EventChip eventType={event.eventType} compact />
                <span className="min-w-0 flex-1 truncate text-left font-medium text-foreground">
                  {event.ticker}
                </span>
              </button>
            </PopoverTrigger>
            <PopoverContent align="start" className="w-64">
              <EventPopoverBody event={event} />
            </PopoverContent>
          </Popover>
        ))}

        {cell.events.length > 3 ? (
          <p className="px-1 text-[11px] font-medium text-muted-foreground">
            +{cell.events.length - 3} eventos más
          </p>
        ) : null}
      </div>
    </div>
  )
}

function EventPopoverBody({ event }: { event: MarketCalendarEvent }) {
  const taxable = normalizeNumber(event.taxableAmount)
  const capital = normalizeNumber(event.capitalReturnAmount)
  const breakdownParts: string[] = []
  if (taxable != null) breakdownParts.push(`Fiscal $${taxable.toFixed(4)}`)
  if (capital != null) breakdownParts.push(`Capital $${capital.toFixed(4)}`)

  return (
    <div className="space-y-2 text-xs">
      <div className="flex items-center gap-2">
        <EventChip eventType={event.eventType} />
        <span className="font-semibold text-foreground">{event.ticker}</span>
      </div>
      <p className="text-muted-foreground">{event.empresa}</p>
      <dl className="grid grid-cols-2 gap-1.5">
        <div>
          <dt className="font-semibold uppercase tracking-wide text-muted-foreground">Fecha</dt>
          <dd className="mt-0.5 font-medium text-foreground">{event.date}</dd>
        </div>
        <div>
          <dt className="font-semibold uppercase tracking-wide text-muted-foreground">Monto</dt>
          <dd className="mt-0.5 font-medium text-foreground">
            {normalizeNumber(event.amountPerUnit)?.toLocaleString('es-MX', {
              style: 'currency',
              currency: 'MXN',
            }) ?? '—'}
          </dd>
        </div>
      </dl>
      {breakdownParts.length > 0 ? (
        <p className="text-muted-foreground">{breakdownParts.join(' · ')}</p>
      ) : (
        <p className="text-muted-foreground">Clasificación fiscal pendiente</p>
      )}
      {event.avisoUrl ? (
        <a
          href={event.avisoUrl}
          target="_blank"
          rel="noreferrer"
          className="inline-flex font-semibold text-teal-700 underline underline-offset-2 hover:text-teal-800"
        >
          Aviso oficial
        </a>
      ) : null}
    </div>
  )
}

function buildCalendarCells(
  monthAnchor: Date,
  groupedEvents: Map<string, MarketCalendarEvent[]>,
): CalendarCellData[] {
  const cells: CalendarCellData[] = []
  const start = new Date(monthAnchor.getFullYear(), monthAnchor.getMonth(), 1)
  const end = endOfMonth(monthAnchor)
  const padCount = (start.getDay() + 6) % 7
  const totalDays = end.getDate()
  const todayKey = formatDateOnly(new Date())

  for (let i = 0; i < padCount; i++) {
    cells.push({ date: null, dateKey: null, day: null, events: [], isToday: false })
  }

  for (let day = 1; day <= totalDays; day++) {
    const date = new Date(monthAnchor.getFullYear(), monthAnchor.getMonth(), day)
    const key = formatDateOnly(date)
    cells.push({ date, dateKey: key, day, events: groupedEvents.get(key) ?? [], isToday: key === todayKey })
  }

  while (cells.length % 7 !== 0 || cells.length < 35) {
    cells.push({ date: null, dateKey: null, day: null, events: [], isToday: false })
    if (cells.length >= 42) break
  }

  return cells
}

function normalizeNumber(value: number | string | null | undefined) {
  if (value == null) return null
  const n = typeof value === 'number' ? value : Number(value)
  return Number.isFinite(n) ? n : null
}
