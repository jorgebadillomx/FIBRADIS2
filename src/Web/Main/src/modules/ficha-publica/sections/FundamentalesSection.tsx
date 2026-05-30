import {
  formatFundamentalValue,
  hasFundamentalesItems,
  shouldShowFundamentalesWarning,
  type FundamentalesData,
} from './fundamentales'
import { KpiLabel } from '@/shared/ui/KpiLabel'

interface Props {
  data?: FundamentalesData
}

export function FundamentalesSection({ data }: Props) {
  const showWarning = shouldShowFundamentalesWarning(data)
  const items = data?.items ?? []

  return (
    <div className="space-y-4">
      {showWarning && (
        <div className="rounded-lg border border-yellow-400 bg-yellow-50 dark:bg-yellow-950/20 px-4 py-3 text-sm text-yellow-800 dark:text-yellow-200">
          Último reporte disponible: hace {data!.periodsAgo} periodos — datos podrían estar desactualizados.
        </div>
      )}

      {hasFundamentalesItems(data) ? (
        <div className="space-y-4">
          {/* Period header */}
          {data?.period && (
            <div className="flex items-center gap-2">
              <span className="rounded-md bg-muted px-2.5 py-1 text-xs font-semibold tracking-widest text-muted-foreground uppercase">
                {data.period}
              </span>
            </div>
          )}

          {/* KPI table: Nombre | Valor | Nota */}
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-xs font-medium text-muted-foreground/60 uppercase tracking-wide">
                <th className="pb-2 pr-4 text-left font-medium">Indicador</th>
                <th className="pb-2 pr-4 text-right font-medium">Valor</th>
                <th className="pb-2 text-left font-medium">Nota</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {items.map((item) => (
                <tr key={item.kpiKey} className="hover:bg-muted/40 transition-colors">
                  <td className="py-2.5 pr-4">
                    <KpiLabel kpiKey={item.kpiKey} label={item.label} />
                  </td>
                  <td className="py-2.5 pr-4 text-right">
                    <span className="font-mono font-semibold tabular-nums">
                      {formatFundamentalValue(item.value)}
                    </span>
                  </td>
                  <td className="py-2.5">
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

          {/* Summary */}
          {data?.summary && (
            <div className="rounded-lg border border-border bg-muted/20 px-4 py-4 space-y-1.5">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground/70">
                Resumen del período
              </p>
              <p className="text-sm text-foreground/90 leading-relaxed">
                {data.summary}
              </p>
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
