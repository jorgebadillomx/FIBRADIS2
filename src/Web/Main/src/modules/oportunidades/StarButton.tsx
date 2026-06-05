import { Star } from 'lucide-react'

interface StarButtonProps {
  fibraId: string
  isFavorite: boolean
  onToggle: (fibraId: string) => void
  size?: number
}

export function StarButton({ fibraId, isFavorite, onToggle, size = 18 }: StarButtonProps) {
  return (
    <button
      type="button"
      aria-pressed={isFavorite}
      aria-label={isFavorite ? 'Quitar de favoritas' : 'Marcar como favorita'}
      onClick={(event) => {
        event.stopPropagation()
        onToggle(fibraId)
      }}
      className="rounded p-1 transition-colors hover:bg-muted/60"
    >
      <Star
        size={size}
        className={isFavorite ? 'fill-yellow-400 text-yellow-400' : 'text-muted-foreground'}
      />
    </button>
  )
}
