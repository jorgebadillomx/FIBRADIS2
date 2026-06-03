interface RowError {
  rowNumber: number
  ticker: string
  message: string
}

interface ErrorTableProps {
  errors: RowError[]
}

export function ErrorTable({ errors }: ErrorTableProps) {
  return (
    <div className="rounded-lg border border-destructive/30 overflow-hidden">
      <table className="w-full text-sm">
        <thead className="bg-destructive/10">
          <tr>
            <th className="text-left px-4 py-2 font-medium text-destructive w-16">Fila</th>
            <th className="text-left px-4 py-2 font-medium text-destructive w-24">Ticker</th>
            <th className="text-left px-4 py-2 font-medium text-destructive">Error</th>
          </tr>
        </thead>
        <tbody>
          {errors.map((e, i) => (
            <tr key={`${e.rowNumber}-${e.ticker}-${i}`} className="border-t border-destructive/10">
              <td className="px-4 py-2 text-muted-foreground">{e.rowNumber || '—'}</td>
              <td className="px-4 py-2 font-mono">{e.ticker || '—'}</td>
              <td className="px-4 py-2 text-destructive">{e.message}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
