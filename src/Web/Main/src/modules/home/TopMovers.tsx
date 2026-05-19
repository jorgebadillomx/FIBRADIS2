export function TopMovers() {
  return (
    <div aria-label="Top movers" className="rounded-xl border border-border bg-surface-elevated overflow-hidden">
      <div className="px-4 pt-4 pb-2 flex items-center gap-3">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Movimientos del día</h3>
        <div className="flex-1 h-px bg-border" />
      </div>
      <div className="divide-y divide-border animate-pulse">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="px-4 py-3 flex items-center justify-between gap-4">
            <div className="space-y-1.5">
              <div className="h-3 w-16 bg-muted rounded" />
              <div className="h-2.5 w-24 bg-muted rounded" />
            </div>
            <div className="text-right space-y-1.5">
              <div className="h-3 w-14 bg-muted rounded" />
              <div className="h-2.5 w-10 bg-muted rounded ml-auto" />
            </div>
          </div>
        ))}
      </div>
      <p className="px-4 py-2 text-xs text-muted-foreground/60 border-t border-border">
        Top movers disponible en Épica 3
      </p>
    </div>
  )
}
