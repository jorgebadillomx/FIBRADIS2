import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchAllFibras, fetchMarketSnapshots } from '@/api/fibrasApi'
import { toNum } from '@/shared/lib/format-time'
import { Input } from '@/shared/ui/input'
import { FibraLogo } from '@/shared/ui/fibra-logo'

export function CatalogoPage() {
  const { data: fibras = [], isLoading } = useQuery({
    queryKey: ['fibras', 'all'],
    queryFn: fetchAllFibras,
    staleTime: 5 * 60_000,
  })

  const { data: snapshots = [] } = useQuery({
    queryKey: ['market-snapshots'],
    queryFn: fetchMarketSnapshots,
    staleTime: 60_000,
  })

  const snapshotMap = useMemo(
    () => new Map(snapshots.map((s) => [s.ticker, s])),
    [snapshots],
  )

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
            title="Filtrar por sector"
            value={sector}
          >
            <option value="">Todos los sectores</option>
            {sectors.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
          <select
            className="rounded-lg border border-border bg-background px-3 py-2 text-sm text-foreground outline-none transition focus:border-ring cursor-pointer"
            onChange={(e) => setMarket(e.target.value)}
            title="Filtrar por mercado"
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
              <div key={i} className="h-52 animate-pulse rounded-2xl bg-muted" />
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
              <FibraCard
                key={fibra.id}
                fibra={fibra}
                snapshot={snapshotMap.get(fibra.ticker) ?? null}
              />
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

// ─── Componentes ──────────────────────────────────────────────────────────

interface FibraCardProps {
  fibra: {
    id: string
    ticker: string
    fullName: string
    sector: string
    market: string
    currency: string
    siteUrl: string | null
  }
  snapshot: {
    lastPrice: number | string | null
    dailyChangePct: number | string | null
    dailyChange: number | string | null
    week52High: number | string | null
    week52Low: number | string | null
  } | null
}

function FibraCard({ fibra, snapshot }: FibraCardProps) {
  const price = toNum(snapshot?.lastPrice)
  const changePct = toNum(snapshot?.dailyChangePct)
  const change = toNum(snapshot?.dailyChange)
  const high52 = toNum(snapshot?.week52High)
  const low52 = toNum(snapshot?.week52Low)
  const isPositive = changePct != null && changePct >= 0

  return (
    <Link
      className="group flex flex-col rounded-2xl border border-border bg-card shadow-sm transition hover:-translate-y-0.5 hover:shadow-md cursor-pointer overflow-hidden"
      to={`/fibras/${fibra.ticker}`}
    >
      {/* Cuerpo principal */}
      <div className="flex flex-col gap-3 p-5">
        {/* Identidad: logo + nombre + ticker */}
        <div className="flex items-start gap-3">
          <FibraLogo siteUrl={fibra.siteUrl} ticker={fibra.ticker} />
          <div className="min-w-0 flex-1 pt-0.5">
            <p className="line-clamp-2 text-lg font-semibold leading-snug text-foreground">
              {fibra.fullName}
            </p>
            <p className="mt-1 font-playfair text-sm font-bold text-primary">
              {fibra.ticker}
            </p>
          </div>
        </div>

        {/* Métricas de mercado */}
        <div className="grid grid-cols-2 gap-px overflow-hidden rounded-xl border border-border bg-border">
          <StatCell label="Precio">
            {price != null
              ? <span className="font-semibold text-foreground">${price.toFixed(2)}</span>
              : <span className="text-muted-foreground">—</span>}
          </StatCell>
          <StatCell label="Var. día">
            {changePct != null
              ? (
                <span className={`font-semibold ${isPositive ? 'text-positive' : 'text-negative'}`}>
                  {isPositive ? '+' : ''}{changePct.toFixed(2)}%
                  {change != null && (
                    <span className="ml-1 text-[10px] font-normal text-muted-foreground">
                      ({isPositive ? '+' : ''}{change.toFixed(2)})
                    </span>
                  )}
                </span>
              )
              : <span className="text-muted-foreground">—</span>}
          </StatCell>
          <StatCell label="Máx 52S">
            {high52 != null
              ? <span className="text-muted-foreground">${high52.toFixed(2)}</span>
              : <span className="text-muted-foreground">—</span>}
          </StatCell>
          <StatCell label="Mín 52S">
            {low52 != null
              ? <span className="text-muted-foreground">${low52.toFixed(2)}</span>
              : <span className="text-muted-foreground">—</span>}
          </StatCell>
        </div>
      </div>

      {/* Pie: chips + CTA */}
      <div className="flex items-center justify-between gap-2 border-t border-border bg-muted/30 px-5 py-3">
        <div className="flex flex-wrap gap-1.5">
          <Chip>{fibra.sector}</Chip>
          <Chip>{fibra.market}</Chip>
          <Chip>{fibra.currency}</Chip>
        </div>
        <span className="inline-flex shrink-0 items-center gap-1 text-sm font-semibold text-primary transition group-hover:text-primary/80">
          Ver ficha
          <svg aria-hidden="true" className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path d="M9 18l6-6-6-6" strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} />
          </svg>
        </span>
      </div>
    </Link>
  )
}

function StatCell({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 bg-card px-3 py-2.5">
      <span className="text-[10px] font-medium uppercase tracking-wider text-muted-foreground">{label}</span>
      <span className="text-sm tabular-nums">{children}</span>
    </div>
  )
}

function Chip({ children }: { children: React.ReactNode }) {
  return (
    <span className="rounded-full border border-border bg-background px-2 py-0.5 text-xs text-muted-foreground">
      {children}
    </span>
  )
}
