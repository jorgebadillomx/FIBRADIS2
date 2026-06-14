import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchRelatedFibras } from '@/api/fibrasApi'
import { buildFibraSlug } from '@/shared/lib/fibra-slug'
import { shouldShowRelacionadas } from './fibras-relacionadas'

interface FibrasRelacionadasSectionProps {
  ticker: string
}

export function FibrasRelacionadasSection({ ticker }: FibrasRelacionadasSectionProps) {
  const { data: related } = useQuery({
    queryKey: ['fibras-relacionadas', ticker],
    queryFn: () => fetchRelatedFibras(ticker),
    staleTime: 60 * 60_000,
    enabled: !!ticker,
  })

  // Sin pares del sector → la sección no se renderiza (AC-2: sin estado vacío feo).
  if (!shouldShowRelacionadas(related)) return null

  // Reusa buildFibraSlug (paridad con el resto del sitio, gate A2 anti triple-slugify).
  const links = (related ?? []).map((f) => ({
    ...f,
    to: `/fibras/${buildFibraSlug(f.fullName, f.ticker)}`,
  }))

  return (
    <section id="relacionadas" className="scroll-mt-32 space-y-4">
      <div className="flex items-center gap-3">
        <h2 className="whitespace-nowrap text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          FIBRAs relacionadas
        </h2>
        <div className="h-px flex-1 bg-border" />
      </div>

      <nav aria-label="FIBRAs del mismo sector" className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {links.map((f) => (
          <Link
            key={f.ticker}
            to={f.to}
            className="group rounded-xl border border-border bg-surface-elevated px-4 py-3 transition-colors hover:border-brand/40 hover:bg-muted/40"
          >
            <span className="block truncate text-sm font-medium text-foreground group-hover:text-brand">
              {f.fullName}
            </span>
            <span className="mt-0.5 block text-xs text-muted-foreground">
              {f.ticker} · {f.sector}
            </span>
          </Link>
        ))}
      </nav>
    </section>
  )
}
