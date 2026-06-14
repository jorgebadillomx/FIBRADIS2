import ReactMarkdown from 'react-markdown'
import { KpiLabel } from '@/shared/ui/KpiLabel'
import { FIBRA_PAGE_LOADING_COUNTS, FUNDAMENTALES_SECTION_LOADING_SHELL } from '../cwv-layout'
import {
  formatFundamentalValue,
  hasFundamentalesItems,
  shouldShowFundamentalesWarning,
  type FundamentalesData,
} from './fundamentales'

interface Props {
  data?: FundamentalesData
  isLoading?: boolean
}

export function FundamentalesSectionSkeleton() {
  return (
    <div aria-busy="true" className="space-y-4">
      <div className="rounded-xl border border-border bg-surface-elevated overflow-hidden">
        <div className="flex items-center justify-between gap-2 border-b border-border px-4 py-3">
          <div className={`h-5 animate-pulse rounded bg-muted/70 ${FUNDAMENTALES_SECTION_LOADING_SHELL.headerTitleWidthClass}`} />
          <div className={`h-4 animate-pulse rounded bg-muted/70 ${FUNDAMENTALES_SECTION_LOADING_SHELL.headerMetaWidthClass}`} />
        </div>

        <div className="divide-y divide-border">
          {Array.from({ length: FIBRA_PAGE_LOADING_COUNTS.fundamentalsRows }).map((_, index) => (
            <div
              key={index}
              className="grid gap-3 px-4 py-2.5 sm:grid-cols-[minmax(0,1.2fr)_7rem_minmax(0,1fr)] sm:items-center"
            >
              <div className={`h-5 animate-pulse rounded bg-muted/70 ${FUNDAMENTALES_SECTION_LOADING_SHELL.rowLabelWidthClass}`} />
              <div className={`ml-auto h-5 animate-pulse rounded bg-muted/70 ${FUNDAMENTALES_SECTION_LOADING_SHELL.rowValueWidthClass}`} />
              <div className={`h-4 animate-pulse rounded bg-muted/70 ${FUNDAMENTALES_SECTION_LOADING_SHELL.rowNoteWidthClass}`} />
            </div>
          ))}
        </div>

        <div className="space-y-4 border-t border-border px-4 py-4">
          <div className="space-y-2">
            <div className="h-3 w-32 animate-pulse rounded bg-muted/70" />
            <div className="h-16 rounded-lg bg-muted/40 animate-pulse" />
          </div>

          <div className="rounded-lg border border-border bg-muted/20 px-3 py-3">
            <div className="h-3 w-36 animate-pulse rounded bg-muted/70" />
            <div className="mt-2 h-4 w-full animate-pulse rounded bg-muted/70" />
            <div className="mt-2 h-4 w-5/6 animate-pulse rounded bg-muted/70" />
            <div className="mt-2 h-4 w-2/3 animate-pulse rounded bg-muted/70" />
          </div>
        </div>
      </div>
    </div>
  )
}

export function FundamentalesSection({ data, isLoading = false }: Props) {
  if (isLoading) {
    return <FundamentalesSectionSkeleton />
  }

  const showWarning = shouldShowFundamentalesWarning(data)
  const items = data?.items ?? []
  const operationalSignals = data?.operationalSignals ?? []
  const financialSignals = data?.financialSignals ?? []
  const riskFlags = data?.riskFlags ?? []
  const summaryContent = data?.summaryMarkdown ?? data?.summary
  const hasOperationalSignals = operationalSignals.length > 0
  const hasFinancialSignals = financialSignals.length > 0
  const hasRiskFlags = riskFlags.length > 0
  const hasInvestorTakeaway = !!data?.investorTakeaway?.trim()

  return (
    <div className="space-y-4">
      {showWarning && (
        <div className="rounded-lg border border-yellow-400 bg-yellow-50 px-4 py-3 text-sm text-yellow-800 dark:bg-yellow-950/20 dark:text-yellow-200">
          Último reporte disponible: hace {data!.periodsAgo} periodos — datos podrían estar desactualizados.
        </div>
      )}

      {hasFundamentalesItems(data) ? (
        <div className="overflow-hidden rounded-xl border border-border bg-surface-elevated">
          {/* Period header */}
          {data?.period && (
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border px-4 py-3">
              <span className="rounded-md bg-muted px-2.5 py-1 text-xs font-semibold uppercase tracking-widest text-muted-foreground">
                {data.period}
              </span>
              {(() => {
                if (!data.capturedAt) return null
                const captured = new Date(data.capturedAt)
                if (Number.isNaN(captured.getTime())) return null
                return (
                  <p className="text-xs text-muted-foreground">
                    Datos al {captured.toLocaleDateString('es-MX', { dateStyle: 'long' })}
                  </p>
                )
              })()}
            </div>
          )}

          {/* KPI table: Nombre | Valor | Nota */}
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-xs font-medium uppercase tracking-wide text-muted-foreground/60">
                <th className="px-4 pb-2 pt-3 text-left font-medium">Indicador</th>
                <th className="px-4 pb-2 pt-3 text-right font-medium">Valor</th>
                <th className="px-4 pb-2 pt-3 text-left font-medium">Nota</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {items.map((item) => (
                <tr key={item.kpiKey} className="transition-colors hover:bg-muted/40">
                  <td className="px-4 py-2.5">
                    <KpiLabel kpiKey={item.kpiKey} label={item.label} />
                  </td>
                  <td className="px-4 py-2.5 text-right">
                    <span className="font-mono font-semibold tabular-nums">
                      {formatFundamentalValue(item.value)}
                    </span>
                  </td>
                  <td className="px-4 py-2.5">
                    {item.note ? (
                      <span className="text-xs italic leading-relaxed text-muted-foreground">
                        {item.note}
                      </span>
                    ) : (
                      <span className="text-xs text-muted-foreground/30">—</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {(summaryContent || hasOperationalSignals || hasFinancialSignals || hasRiskFlags || hasInvestorTakeaway) && (
            <div className="space-y-4 border-t border-border px-4 py-4">
              {summaryContent && (
                <div className="space-y-2">
                  <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground/70">
                    Resumen analítico
                  </p>
                  <div className="prose prose-sm max-w-none text-sm leading-relaxed text-foreground/90">
                    <ReactMarkdown
                      urlTransform={(url) =>
                        url.startsWith('https://') || url.startsWith('http://') || url.startsWith('#')
                          ? url
                          : ''
                      }
                    >
                      {summaryContent}
                    </ReactMarkdown>
                  </div>
                </div>
              )}

              {hasOperationalSignals && (
                <SignalsBlock
                  title="Señales operativas"
                  items={operationalSignals}
                />
              )}

              {hasFinancialSignals && (
                <SignalsBlock
                  title="Señales financieras"
                  items={financialSignals}
                />
              )}

              {hasRiskFlags && (
                <SignalsBlock
                  title="Alertas de riesgo"
                  items={riskFlags}
                  className="rounded-lg border border-amber-200 bg-amber-50/70 px-3 py-3"
                  listClassName="text-amber-900"
                  bulletClassName="text-amber-600"
                />
              )}

              {hasInvestorTakeaway && (
                <div className="rounded-lg border-l-4 border-brand bg-muted/50 px-4 py-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-brand">
                    Perspectiva del analista
                  </p>
                  <p className="mt-1 text-sm leading-relaxed text-foreground/90">
                    {data!.investorTakeaway}
                  </p>
                </div>
              )}
            </div>
          )}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-border bg-surface-elevated px-4 py-8">
          <p className="text-sm font-medium text-muted-foreground">Sin fundamentales disponibles</p>
          <p className="text-xs text-muted-foreground/60">No hay datos de fundamentales disponibles para esta FIBRA.</p>
        </div>
      )}
    </div>
  )
}

interface SignalsBlockProps {
  title: string
  items: string[]
  className?: string
  listClassName?: string
  bulletClassName?: string
}

function SignalsBlock({ title, items, className, listClassName, bulletClassName }: SignalsBlockProps) {
  return (
    <div className={className}>
      <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground/70">
        {title}
      </p>
      <ul className={`space-y-1.5 text-sm leading-relaxed text-muted-foreground ${listClassName ?? ''}`}>
        {items.map((item, index) => (
          <li key={`${index}-${item}`} className="flex gap-2">
            <span className={`mt-1 h-1.5 w-1.5 shrink-0 rounded-full bg-current ${bulletClassName ?? ''}`} />
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}
