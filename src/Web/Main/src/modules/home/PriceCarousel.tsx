export function PriceCarousel() {
  return (
    <div aria-label="Carrusel de precios" className="relative">
      <div className="flex gap-3 overflow-x-auto pb-1 scrollbar-none">
        {Array.from({ length: 9 }).map((_, i) => (
          <div
            key={i}
            className="shrink-0 rounded-xl border border-border bg-surface-elevated p-3 w-36 space-y-2 animate-pulse"
          >
            <div className="h-3 w-14 bg-muted rounded" />
            <div className="h-6 w-20 bg-muted rounded" />
            <div className="h-2.5 w-10 bg-muted rounded" />
          </div>
        ))}
      </div>
      <p className="mt-2 text-xs text-muted-foreground/60 text-center">
        Precios en tiempo real — disponible en Épica 3
      </p>
    </div>
  )
}
