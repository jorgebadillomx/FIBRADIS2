import { useEffect, useMemo, useState } from 'react'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchAllFibras } from '@/api/fibrasApi'
import { fetchNewsPaged } from '@/api/newsApi'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue'
import { formatRelativeTime } from '@/shared/lib/format-time'
import { getArticleImageUrl } from '@/shared/lib/news-image-fallback'

const PAGE_SIZE = 20

function toCount(value: number | string | undefined) {
  const n = typeof value === 'string' ? Number.parseInt(value, 10) : (value ?? 0)
  return Number.isNaN(n) ? 0 : n
}

export function NoticiasListPage() {
  const [page, setPage] = useState(1)
  const [query, setQuery] = useState('')
  const [fibraId, setFibraId] = useState<string | undefined>()
  const debouncedQuery = useDebouncedValue(query.trim(), 300)

  const { data: fibras = [], isError: isFibrasError } = useQuery({
    queryKey: ['fibras', 'all'],
    queryFn: fetchAllFibras,
    staleTime: 5 * 60_000,
  })

  const {
    data,
    isLoading,
    isFetching,
    isError,
  } = useQuery({
    queryKey: ['news', 'paged', { page, pageSize: PAGE_SIZE, q: debouncedQuery, fibraId: fibraId ?? null }],
    queryFn: () => fetchNewsPaged(page, PAGE_SIZE, debouncedQuery || undefined, fibraId),
    staleTime: 2 * 60_000,
  })

  useEffect(() => {
    window.scrollTo({ top: 0, behavior: 'auto' })
  }, [page])

  const totalResults = toCount(data?.total)
  const totalPages = Math.max(1, Math.ceil(totalResults / PAGE_SIZE))
  const items = data?.items ?? []
  const isFiltered = query.trim().length > 0 || !!fibraId

  const activeFibraName = useMemo(() => {
    if (!fibraId) return null
    return fibras.find((fibra) => fibra.id === fibraId)?.ticker ?? null
  }, [fibraId, fibras])

  function clearFilters() {
    setQuery('')
    setFibraId(undefined)
    setPage(1)
  }

  usePageTitle('Noticias FIBRAs Inmobiliarias | FIBRADIS')

  return (
    <>
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div className="space-y-2">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-primary">Cobertura pública</p>
            <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
              Noticias
            </h1>
            <p className="max-w-2xl text-sm leading-6 text-muted-foreground md:text-base">
              Recorre el flujo procesado de noticias, filtra por título o por FIBRA y entra al detalle cuando una nota amerite contexto adicional.
            </p>
          </div>

          <div className="rounded-xl border border-border bg-surface-elevated px-4 py-3 text-sm text-muted-foreground">
            {data ? `${totalResults} noticia${totalResults === 1 ? '' : 's'} en resultados` : 'Cargando cobertura'}
          </div>
        </div>

        <section className="mb-8 rounded-2xl border border-border bg-surface-elevated p-4 shadow-sm">
          <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem_auto] lg:items-end">
            <label className="space-y-2">
              <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Buscar por título</span>
              <Input
                type="search"
                value={query}
                onChange={(event) => {
                  setQuery(event.target.value)
                  setPage(1)
                }}
                placeholder="Ej. FUNO11, ocupación, resultados trimestrales"
                className="h-10"
              />
            </label>

            <label className="space-y-2">
              <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Filtrar por FIBRA</span>
              <select
                value={fibraId ?? ''}
                onChange={(event) => {
                  const nextFibraId = event.target.value || undefined
                  setFibraId(nextFibraId)
                  setPage(1)
                }}
                disabled={isFibrasError}
                className="flex h-10 w-full rounded-lg border border-input bg-background px-3 text-sm text-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50"
                aria-label="Filtrar noticias por FIBRA"
              >
                <option value="">Todas las FIBRAs</option>
                {fibras.map((fibra) => (
                  <option key={fibra.id} value={fibra.id}>
                    {fibra.ticker} · {fibra.shortName}
                  </option>
                ))}
              </select>
            </label>

            <div className="flex items-center justify-end">
              <Button type="button" variant="outline" onClick={clearFilters} disabled={!isFiltered}>
                Limpiar filtros
              </Button>
            </div>
          </div>

          {isFibrasError ? (
            <p className="mt-3 text-sm text-muted-foreground">
              No se pudo cargar el catálogo para el filtro por FIBRA.
            </p>
          ) : null}
        </section>

        {isLoading ? (
          <NoticiasListSkeleton />
        ) : isError ? (
          <section className="rounded-2xl border border-border bg-surface-elevated px-6 py-12 text-center">
            <p className="text-base font-medium text-foreground">No se pudieron cargar las noticias.</p>
            <p className="mt-2 text-sm text-muted-foreground">
              Intenta de nuevo en unos momentos.
            </p>
          </section>
        ) : items.length === 0 ? (
          <section className="rounded-2xl border border-border bg-surface-elevated px-6 py-12 text-center">
            <p className="text-base font-medium text-foreground">
              {buildEmptyMessage(query.trim(), activeFibraName)}
            </p>
            <p className="mt-2 text-sm text-muted-foreground">
              Ajusta la búsqueda o limpia los filtros para volver al listado completo.
            </p>
            <div className="mt-5">
              <Button type="button" variant="outline" onClick={clearFilters}>
                Limpiar filtros
              </Button>
            </div>
          </section>
        ) : (
          <div className={isFetching ? 'opacity-60 transition-opacity duration-150' : undefined}>
            <section className="grid gap-5 md:grid-cols-2">
              {items.map((article) => {
                const imageUrl = getArticleImageUrl(article)
                const preview = article.snippet?.trim() || article.aiAnalysis?.headline || null
                const linkedFibras = article.linkedFibras ?? []
                const visibleFibras = linkedFibras.slice(0, 3)
                const hiddenFibrasCount = Math.max(0, linkedFibras.length - visibleFibras.length)

                return (
                  <article key={article.id} className="group overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm transition-colors hover:border-primary/35">
                    <Link to={`/noticias/${article.slug ?? article.id}`} className="block h-full">
                      {false && imageUrl ? (
                        <div className="aspect-video overflow-hidden bg-muted">
                          <img
                            src={imageUrl ?? undefined}
                            alt={article.title}
                            className="h-full w-full object-cover"
                            loading="lazy"
                            onError={(e) => {
                              e.currentTarget.onerror = null
                              const parent = e.currentTarget.parentElement
                              if (parent) parent.style.display = 'none'
                            }}
                          />
                        </div>
                      ) : null}
                      <div className="flex h-full flex-col gap-4 p-5">
                        <div className="space-y-2">
                          <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                            {article.source} · {formatRelativeTime(article.publishedAt)}
                          </p>
                          <h2 className="font-playfair text-2xl font-semibold leading-tight text-foreground transition-colors group-hover:text-primary">
                            {article.title}
                          </h2>
                          {preview ? (
                            <p className="text-sm leading-6 text-muted-foreground line-clamp-3">
                              {preview}
                            </p>
                          ) : null}
                        </div>

                        {linkedFibras.length > 0 ? (
                          <div className="mt-auto flex flex-wrap gap-2">
                            {visibleFibras.map((fibra) => (
                              <span
                                key={`${article.id}-${fibra.ticker}`}
                                className="rounded-full border border-primary/25 bg-primary/5 px-2.5 py-1 text-xs font-semibold text-primary"
                              >
                                {fibra.ticker}
                              </span>
                            ))}
                            {hiddenFibrasCount > 0 ? (
                              <span className="rounded-full border border-border bg-background px-2.5 py-1 text-xs font-semibold text-muted-foreground">
                                +{hiddenFibrasCount} más
                              </span>
                            ) : null}
                          </div>
                        ) : null}
                      </div>
                    </Link>
                  </article>
                )
              })}
            </section>

            <nav className="mt-8 flex flex-col gap-4 rounded-2xl border border-border bg-surface-elevated px-4 py-4 sm:flex-row sm:items-center sm:justify-between" aria-label="Paginación de noticias">
              <Button
                type="button"
                variant="outline"
                onClick={() => setPage((current) => Math.max(1, current - 1))}
                disabled={page <= 1}
              >
                Página anterior
              </Button>

              <p className="text-center text-sm text-muted-foreground">
                Página <span className="font-semibold text-foreground">{page}</span> de{' '}
                <span className="font-semibold text-foreground">{totalPages}</span>
              </p>

              <Button
                type="button"
                variant="outline"
                onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
                disabled={page >= totalPages}
              >
                Página siguiente
              </Button>
            </nav>
          </div>
        )}
      </div>
    </>
  )
}

function NoticiasListSkeleton() {
  return (
    <section className="grid gap-5 md:grid-cols-2">
      {Array.from({ length: 6 }).map((_, index) => (
        <div
          key={index}
          className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm"
        >
          {false && <div className="aspect-video animate-pulse bg-muted" />}
          <div className="space-y-3 p-5">
            <div className="h-3 w-40 animate-pulse rounded bg-muted" />
            <div className="h-8 w-11/12 animate-pulse rounded bg-muted" />
            <div className="h-4 w-full animate-pulse rounded bg-muted" />
            <div className="h-4 w-4/5 animate-pulse rounded bg-muted" />
            <div className="flex gap-2 pt-2">
              <div className="h-6 w-16 animate-pulse rounded-full bg-muted" />
              <div className="h-6 w-20 animate-pulse rounded-full bg-muted" />
            </div>
          </div>
        </div>
      ))}
    </section>
  )
}

function buildEmptyMessage(query: string, fibraTicker: string | null) {
  if (query && fibraTicker) {
    return `Sin resultados para «${query}» en ${fibraTicker}`
  }

  if (query) {
    return `Sin resultados para «${query}»`
  }

  if (fibraTicker) {
    return `Sin resultados para ${fibraTicker}`
  }

  return 'Sin resultados'
}
