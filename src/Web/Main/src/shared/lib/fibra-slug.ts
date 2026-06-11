// Slug canónico de ficha de FIBRA: slugify(fullName) + '-' + ticker en minúsculas.
// DEBE producir exactamente el mismo slug que FibraSlug.Build (C#,
// src/Server/Application/Catalog/FibraSlug.cs) — si divergen, el 301 del
// middleware y la canonicalización client-side entran en loop de redirecciones.
export function buildFibraSlug(fullName: string, ticker: string): string {
  const namePart = fullName
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
  const tickerPart = ticker.toLowerCase()
  return namePart ? `${namePart}-${tickerPart}` : tickerPart
}

// El ticker SIEMPRE es el último segmento del slug (los tickers no llevan guiones),
// así que un ticker pelado sin guiones también resuelve (URLs viejas /fibras/FUNO11).
export function extractTickerFromSlug(param: string): string {
  const lastSegment = param.split('-').at(-1) ?? param
  return lastSegment.toUpperCase()
}
