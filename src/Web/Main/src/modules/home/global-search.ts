export interface SearchableFibra {
  ticker: string
  fullName?: string | null
}

export function filterFibrasByQuery(
  fibras: readonly SearchableFibra[],
  query: string,
): SearchableFibra[] {
  const normalizedQuery = query.trim().toLowerCase()

  if (normalizedQuery.length === 0) {
    return []
  }

  return fibras
    .filter((fibra) =>
      (fibra.ticker ?? '').toLowerCase().includes(normalizedQuery)
      || (fibra.fullName ?? '').toLowerCase().includes(normalizedQuery),
    )
    .slice(0, 8)
}
