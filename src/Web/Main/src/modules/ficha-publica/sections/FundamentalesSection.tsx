import {
  formatFundamentalValue,
  hasFundamentalesItems,
  shouldShowFundamentalesWarning,
  type FundamentalesData,
} from './fundamentales'
import ReactMarkdown from 'react-markdown'
import { KpiLabel } from '@/shared/ui/KpiLabel'

interface Props {
  data?: FundamentalesData
}

export function FundamentalesSection({ data }: Props) {
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
        <div className="rounded-lg border border-yellow-400 bg-yellow-50 dark:bg-yellow-950/20 px-4 py-3 text-sm text-yellow-800 dark:text-yellow-200">
          Último reporte disponible: hace {data!.periodsAgo} periodos — datos podrían estar desactualizados.
        </div>
      )}

      {hasFundamentalesItems(data) ? (
        <div className="rounded-xl border border-border bg-surface-elevated overflow-hidden">
          {/* Period header */}
          {data?.period && (
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border px-4 py-3">
              <span className="rounded-md bg-muted px-2.5 py-1 text-xs font-semibold tracking-widest text-muted-foreground uppercase">
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
              <tr className="border-b border-border text-xs font-medium text-muted-foreground/60 uppercase tracking-wide">
                <th className="px-4 pb-2 pt-3 text-left font-medium">Indicador</th>
                <th className="px-4 pb-2 pt-3 text-right font-medium">Valor</th>
                <th className="px-4 pb-2 pt-3 text-left font-medium">Nota</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {items.map((item) => (
                <tr key={item.kpiKey} className="hover:bg-muted/40 transition-colors">
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
                      <span className="text-xs italic text-muted-foreground leading-relaxed">
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
            <div className="border-t border-border px-4 py-4 space-y-4">
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
        <div className="rounded-lg border border-border bg-surface-elevated px-4 py-8 flex flex-col items-center justify-center gap-2">
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
