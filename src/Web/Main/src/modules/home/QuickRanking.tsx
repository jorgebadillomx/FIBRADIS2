export function QuickRanking() {
  return (
    <div aria-label="Ranking rápido del universo" className="rounded-xl border border-border bg-surface-elevated overflow-hidden">
      <div className="px-4 pt-4 pb-2 flex items-center gap-3">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Ranking del universo</h3>
        <div className="flex-1 h-px bg-border" />
      </div>
      {/* Header de tabla simulado */}
      <div className="px-4 py-2 border-b border-border grid grid-cols-4 gap-4 animate-pulse">
        {['Ticker', 'Precio', 'Cambio', 'Volumen'].map((col) => (
          <div key={col} className="h-2.5 w-12 bg-muted rounded" />
        ))}
      </div>
      <div className="divide-y divide-border animate-pulse">
        {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="px-4 py-3 grid grid-cols-4 gap-4 items-center">
            <div className="space-y-1">
              <div className="h-3 w-14 bg-muted rounded" />
              <div className="h-2 w-20 bg-muted rounded" />
            </div>
            <div className="h-3 w-16 bg-muted rounded" />
            <div className="h-3 w-12 bg-muted rounded" />
            <div className="h-3 w-14 bg-muted rounded" />
          </div>
        ))}
      </div>
      <p className="px-4 py-2 text-xs text-muted-foreground/60 border-t border-border">
        Ranking completo disponible en Épica 3
      </p>
    </div>
  )
}
