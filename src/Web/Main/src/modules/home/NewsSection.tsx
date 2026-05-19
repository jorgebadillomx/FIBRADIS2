export function NewsSection() {
  return (
    <div aria-label="Noticias recientes" className="rounded-xl border border-border bg-surface-elevated overflow-hidden h-full">
      <div className="px-4 pt-4 pb-2 flex items-center gap-3">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Noticias</h3>
        <div className="flex-1 h-px bg-border" />
      </div>
      <div className="divide-y divide-border animate-pulse">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="px-4 py-3 space-y-2">
            <div className="h-3 w-full bg-muted rounded" />
            <div className="h-3 w-4/5 bg-muted rounded" />
            <div className="h-2.5 w-20 bg-muted rounded" />
          </div>
        ))}
      </div>
      <p className="px-4 py-2 text-xs text-muted-foreground/60 border-t border-border">
        Noticias disponibles en Épica 4
      </p>
    </div>
  )
}
