import { useQuery } from '@tanstack/react-query'
import { fetchFibraHistory } from '@/api/fibrasApi'
import { toNum } from '@/shared/lib/format-time'

interface DistribucionesSectionProps {
  ticker: string
}

export function DistribucionesSection({ ticker }: DistribucionesSectionProps) {
  const { data: history, isLoading, isError } = useQuery({
    queryKey: ['fibra-history', ticker, '1y'],
    queryFn: () => fetchFibraHistory(ticker, '1y'),
    staleTime: 60 * 60_000,
    enabled: !!ticker,
  })

  if (isLoading) {
    return (
      <div className="space-y-3 animate-pulse">
        <div className="h-16 bg-muted rounded-lg" />
        <div className="h-32 bg-muted rounded-lg" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-24">
        <p className="text-sm text-muted-foreground">Error al cargar datos de distribuciones</p>
      </div>
    )
  }

  const yieldRaw = toNum(history?.annualizedYield)
  const dists = history?.distributions ?? []

  return (
    <div className="space-y-4">
      {/* Yield anualizado */}
      <div className="rounded-lg border border-border bg-surface-elevated px-4 py-3">
        <p className="text-xs text-muted-foreground mb-0.5">Yield anualizado estimado</p>
        {yieldRaw != null ? (
          <p className="text-2xl font-semibold tabular-nums">{(yieldRaw * 100).toFixed(2)}%</p>
        ) : (
          <p className="text-base text-muted-foreground">no disponible</p>
        )}
      </div>

      {/* Tabla de distribuciones */}
      {dists.length === 0 ? (
        <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-24">
          <p className="text-sm text-muted-foreground">Sin distribuciones registradas</p>
        </div>
      ) : (
        <div className="rounded-lg border border-border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-muted/50 text-muted-foreground">
                <th className="text-left px-4 py-2 font-medium">Fecha de pago</th>
                <th className="text-right px-4 py-2 font-medium">Monto por CBFI</th>
              </tr>
            </thead>
            <tbody>
              {dists.map((d, i) => (
                <tr key={d.date} className={i % 2 === 0 ? '' : 'bg-muted/20'}>
                  <td className="px-4 py-2">{d.date}</td>
                  <td className="px-4 py-2 text-right tabular-nums">
                    ${toNum(d.amountPerUnit)?.toFixed(4) ?? '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
