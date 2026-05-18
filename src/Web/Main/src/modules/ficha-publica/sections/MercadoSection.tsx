import { useState } from 'react'

const SELECTORS = ['1M', '3M', '6M', '1A'] as const
type Selector = typeof SELECTORS[number]

export function MercadoSection() {
  const [active, setActive] = useState<Selector>('1M')

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        {SELECTORS.map((s) => (
          <button
            key={s}
            aria-pressed={active === s ? "true" : "false"}
            onClick={() => setActive(s)}
            className={`px-3 py-1 rounded text-sm font-medium transition-colors ${
              active === s
                ? 'bg-primary text-primary-foreground'
                : 'bg-muted text-muted-foreground hover:bg-muted/80'
            }`}
          >
            {s}
          </button>
        ))}
      </div>
      <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-48">
        <p className="text-sm text-muted-foreground">Historial de precios disponible en Épica 3</p>
      </div>
    </div>
  )
}
