import { useCallback, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchAllFibras } from '@/api/fibrasApi'
import { buildFibraSlug } from '@/shared/lib/fibra-slug'

// Resuelve ticker → slug canónico para componentes cuyos DTOs solo traen ticker
// (snapshots, calendario, fundamentales). Reusa el MISMO queryKey ['fibras', 'all']
// que CatalogoPage/GlobalSearch — la query se dedupe y no hay fetch extra en páginas
// que ya cargan el catálogo. El fallback ticker.toLowerCase() produce un link viejo
// que el middleware 301 / FibraPage canonicalizan; solo ocurre mientras carga.
export function useFibraSlugMap() {
  const { data } = useQuery({
    queryKey: ['fibras', 'all'],
    queryFn: fetchAllFibras,
    staleTime: 5 * 60_000,
  })

  const map = useMemo(
    () => new Map((data ?? []).map((f) => [f.ticker, buildFibraSlug(f.fullName, f.ticker)])),
    [data],
  )

  const slugFor = useCallback(
    (ticker: string) => map.get(ticker) ?? ticker.toLowerCase(),
    [map],
  )

  return { slugFor }
}
