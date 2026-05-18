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
          <tbody>
            {items.map((item) => (
              <tr key={`${item.label}-${item.period}`} className="border-b border-border last:border-0">
                <td className="py-2 text-muted-foreground">{item.label} — {item.period}</td>
                <td className="py-2 text-right font-mono">
                  {formatFundamentalValue(item.value)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-32">
          <p className="text-sm text-muted-foreground">Fundamentales disponibles en Épica 5</p>
        </div>
      )}
    </div>
  )
}
