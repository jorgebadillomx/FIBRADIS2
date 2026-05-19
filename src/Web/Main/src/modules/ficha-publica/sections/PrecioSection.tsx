import { FreshnessBadge } from '@/shared/ui/freshness-badge'

export function PrecioSection() {
  return (
    <div className="rounded-xl border border-border bg-surface-elevated px-5 py-4">
      <div className="flex items-end gap-3">
        <span className="text-4xl font-bold tabular-nums tracking-tight text-muted-foreground">—</span>
        <div className="pb-1 space-y-1">
          <FreshnessBadge status="off-hours" />
          <p className="text-xs text-muted-foreground/60">Precio en tiempo real disponible en Épica 3</p>
        </div>
      </div>
    </div>
  )
}
