import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchAllFibras } from '@/api/fibrasApi'
import { Input } from '@/shared/ui/input'

export function CatalogoPage() {
  const { data: fibras = [], isLoading } = useQuery({
    queryKey: ['fibras', 'all'],
    queryFn: fetchAllFibras,
    staleTime: 5 * 60_000,
  })

  const [search, setSearch] = useState('')
  const [sector, setSector] = useState('')
  const [market, setMarket] = useState('')

  const sectors = useMemo(
    () => Array.from(new Set(fibras.map((f) => f.sector))).sort(),
    [fibras],
  )
  const markets = useMemo(
    () => Array.from(new Set(fibras.map((f) => f.market))).sort(),
    [fibras],
  )

  const filtered = useMemo(() => {
    const q = search.toLowerCase().trim()
    return fibras.filter((f) => {
      const matchesSearch =
        !q || f.ticker.toLowerCase().includes(q) || f.fullName.toLowerCase().includes(q)
      const matchesSector = !sector || f.sector === sector
      const matchesMarket = !market || f.market === market
      return matchesSearch && matchesSector && matchesMarket
    })
  }, [fibras, search, sector, market])

  const hasFilters = search || sector || market

  return (
    <>
      <title>Catálogo de FIBRAs — FIBRADIS</title>
      <meta
        name="description"
        content="Explora el universo completo de FIBRAs inmobiliarias mexicanas con datos clave, sector, mercado y descripción editorial."
      />

      <div className="container mx-auto px-4 py-8">
        <div className="mb-8 space-y-2">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-primary">
            Universo FIBRAS
          </p>
          <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
            Catálogo de FIBRAs
          </h1>
          <p className="max-w-2xl text-sm leading-6 text-muted-foreground md:text-base">
            {isLoading
              ? 'Cargando...'
              : `${fibras.length} emisoras activas en el universo FIBRADIS.`}
          </p>
        </div>

        <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center">
          <Input
            className="sm:max-w-xs"
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar por ticker o nombre..."
            value={search}
          />
          <select
            className="rounded-lg border border-border bg-background px-3 py-2 text-sm text-foreground outline-none transition focus:border-ring cursor-pointer"
            onChange={(e) => setSector(e.target.value)}
            value={sector}
          >
            <option value="">Todos los sectores</option>
            {sectors.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
          <select
            className="rounded-lg border border-border bg-background px-3 py-2 text-sm text-foreground outline-none transition focus:border-ring cursor-pointer"
            onChange={(e) => setMarket(e.target.value)}
            value={market}
          >
            <option value="">Todos los mercados</option>
            {markets.map((m) => <option key={m} value={m}>{m}</option>)}
          </select>
          {hasFilters ? (
            <button
              className="text-sm text-muted-foreground underline underline-offset-2 transition hover:text-foreground cursor-pointer"
              onClick={() => { setSearch(''); setSector(''); setMarket('') }}
              type="button"
            >
              Limpiar filtros
            </button>
          ) : null}
        </div>

        {isLoading ? (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 9 }).map((_, i) => (
              <div key={i} className="h-48 animate-pulse rounded-2xl bg-muted" />
            ))}
          </div>
        ) : filtered.length === 0 ? (
          <div className="py-16 text-center">
            <p className="text-muted-foreground">
              No se encontraron FIBRAs con esos filtros.
            </p>
          </div>
        ) : (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {filtered.map((fibra) => (
              <FibraCard key={fibra.id} fibra={fibra} />
            ))}
          </div>
        )}

        {!isLoading && filtered.length > 0 && hasFilters ? (
          <p className="mt-6 text-center text-xs text-muted-foreground">
            Mostrando {filtered.length} de {fibras.length} emisoras
          </p>
        ) : null}
      </div>
    </>
  )
}

interface FibraCardProps {
  fibra: {
    id: string
    ticker: string
    fullName: string
    sector: string
    market: string
    currency: string
    hasDescription: boolean
  }
}

function FibraCard({ fibra }: FibraCardProps) {
  return (
    <Link
      to={`/fibras/${fibra.ticker}`}
      className="group flex flex-col justify-between rounded-2xl border border-border bg-card p-5 shadow-sm transition hover:shadow-md hover:-translate-y-0.5 cursor-pointer"
    >
      <div className="space-y-2">
        <div className="flex items-start justify-between gap-2">
          <span className="font-playfair text-2xl font-bold text-primary leading-none">
            {fibra.ticker}
          </span>
          <DescriptionBadge has={fibra.hasDescription} />
        </div>
        <p className="text-sm font-medium text-foreground leading-snug line-clamp-2">
          {fibra.fullName}
        </p>
        <div className="flex flex-wrap gap-1.5 pt-1">
          <Chip>{fibra.sector}</Chip>
          <Chip>{fibra.market}</Chip>
          <Chip>{fibra.currency}</Chip>
        </div>
      </div>

      <div className="mt-5">
        <span className="inline-flex items-center gap-1 text-sm font-semibold text-primary transition group-hover:text-primary/80">
          Ver ficha
          <svg aria-hidden="true" className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path d="M9 18l6-6-6-6" strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} />
          </svg>
        </span>
      </div>
    </Link>
  )
}

function DescriptionBadge({ has }: { has: boolean }) {
  if (has) {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-700 whitespace-nowrap border border-emerald-200">
        <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" />
        Con descripción
      </span>
    )
  }
  return (
    <span className="inline-flex rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground whitespace-nowrap">
      Sin descripción
    </span>
  )
}

function Chip({ children }: { children: React.ReactNode }) {
  return (
    <span className="rounded-full border border-border bg-background px-2 py-0.5 text-xs text-muted-foreground">
      {children}
    </span>
  )
}
