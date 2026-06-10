import { useMemo, useState } from 'react'
import type { components } from '@fibradis/shared-api-client'
import { formatMoney } from '@/modules/portafolio/portfolio-format'
import { projectNextPayments } from '@/modules/portafolio/portfolio-calendar'

type PortfolioPositionDto = components['schemas']['PortfolioPositionDto']

const monthYearFmt = new Intl.DateTimeFormat('es-MX', {
  month: 'long',
  year: 'numeric',
})
const dayMonthFmt = new Intl.DateTimeFormat('es-MX', {
  day: '2-digit',
  month: 'short',
})

interface PortafolioCalendarioProps {
  positions: PortfolioPositionDto[]
}

export function PortafolioCalendario({ positions }: PortafolioCalendarioProps) {
  const payments = useMemo(
    () => projectNextPayments(positions, new Date()),
    [positions],
  )

  if (payments.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-border bg-card px-6 py-12 text-center">
        <p className="text-sm font-medium text-foreground">
          No hay suficientes datos de distribuciones para proyectar pagos.
        </p>
        <p className="mt-1 text-sm text-muted-foreground">
          Cuando existan pagos recientes, el calendario mostrará las próximas fechas estimadas.
        </p>
      </div>
    )
  }

  const grouped = payments.reduce((acc, payment) => {
    const key = monthYearFmt.format(payment.fechaEstimada)
    const bucket = acc.get(key) ?? []
    bucket.push(payment)
    acc.set(key, bucket)
    return acc
  }, new Map<string, typeof payments>())

  return (
    <div className="space-y-5">
      {Array.from(grouped.entries()).map(([monthLabel, monthPayments]) => (
        <section key={monthLabel} className="rounded-2xl border border-border bg-card shadow-sm">
          <div className="border-b border-border px-4 py-3">
            <h3 className="text-sm font-semibold capitalize text-foreground">{monthLabel}</h3>
          </div>

          <div className="divide-y divide-border">
            {monthPayments.map((payment) => (
              <article key={`${payment.fibraId}-${payment.fechaEstimada.toISOString()}`} className="flex flex-col gap-3 px-4 py-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex items-center gap-3">
                  <PaymentLogo ticker={payment.ticker} logoUrl={payment.logoUrl} />
                  <div className="min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-mono font-semibold text-foreground">{payment.ticker}</span>
                      <span className="rounded-full border border-violet-200 bg-violet-50 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-[0.18em] text-violet-700">
                        {payment.cadencia}
                      </span>
                    </div>
                    <p className="truncate text-sm text-muted-foreground">{payment.nombre}</p>
                  </div>
                </div>

                <div className="grid gap-2 text-sm sm:grid-cols-3 sm:items-center sm:gap-4 sm:text-right">
                  <div>
                    <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                      Fecha estimada
                    </div>
                    <div className="mt-1 font-medium text-foreground tabular-nums">
                      {dayMonthFmt.format(payment.fechaEstimada)}
                    </div>
                  </div>
                  <div>
                    <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                      Monto por título
                    </div>
                    <div className="mt-1 font-medium text-foreground tabular-nums">
                      {formatMoney(payment.montoPorTitulo)}
                    </div>
                  </div>
                  <div>
                    <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                      Total estimado
                    </div>
                    <div className="mt-1 font-medium text-foreground tabular-nums">
                      {formatMoney(payment.montoTotal)}
                    </div>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}

function PaymentLogo({ ticker, logoUrl }: { ticker: string; logoUrl: string | null }) {
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
