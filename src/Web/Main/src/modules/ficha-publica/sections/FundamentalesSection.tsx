import {
  formatFundamentalValue,
  hasFundamentalesItems,
  shouldShowFundamentalesWarning,
  type FundamentalesData,
} from './fundamentales'

interface Props {
  data?: FundamentalesData
}

export function FundamentalesSection({ data }: Props) {
  const showWarning = shouldShowFundamentalesWarning(data)
  const items = data?.items ?? []

  return (
    <div className="space-y-3">
      {showWarning && (
        <div className="rounded-lg border border-yellow-400 bg-yellow-50 dark:bg-yellow-950/20 px-4 py-3 text-sm text-yellow-800 dark:text-yellow-200">
          Último reporte disponible: hace {data!.periodsAgo} periodos — datos podrían estar desactualizados.
        </div>
      )}
      {hasFundamentalesItems(data) ? (
        <table className="w-full text-sm">
          <tbody className="divide-y divide-border">
            {items.map((item) => (
              <tr key={`${item.label}-${item.period}`} className="hover:bg-muted/40 transition-colors">
                <td className="py-2.5 pr-4">
                  <span className="text-foreground">{item.label}</span>
                  <span className="ml-2 text-xs text-muted-foreground/70">{item.period}</span>
                </td>
                <td className="py-2.5 text-right font-mono font-medium tabular-nums">
                  {formatFundamentalValue(item.value)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <div className="rounded-lg border border-border bg-surface-elevated px-4 py-8 flex flex-col items-center justify-center gap-2">
          <p className="text-sm font-medium text-muted-foreground">Sin fundamentales disponibles</p>
          <p className="text-xs text-muted-foreground/60">Los datos fundamentales estarán disponibles en Épica 5</p>
        </div>
      )}
    </div>
  )
}
