/**
 * Réplica de buildFibraSlug de src/shared/lib/fibra-slug.ts (este script corre con
 * node puro y no puede importar .ts). DEBE producir el mismo slug que el TS y que
 * FibraSlug.Build en C# — si divergen, el prerender genera rutas que el middleware
 * 301 redirige y se pierde el beneficio del HTML estático.
 */
export function buildFibraSlug(fullName, ticker) {
  const namePart = fullName
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
  const tickerPart = ticker.toLowerCase()
  return namePart ? `${namePart}-${tickerPart}` : tickerPart
}

/**
 * Extrae <title>, <meta name="description">, <link rel="canonical"> del HTML
 * renderizado por React (no del template completo), evitando tocar el <head> del template.
 * Retorna los elementos extraídos y el cuerpo limpio listo para inyectar en <div id="root">.
 */
export function extractHeadElements(rendered) {
  const headElements = []
  let clean = rendered

  clean = clean.replace(/<title[^>]*>[\s\S]*?<\/title>/gi, match => {
    headElements.push(match.trim())
    return ''
  })
  clean = clean.replace(/<meta\s+name="description"[^>]*>/gi, match => {
    headElements.push(match.trim())
    return ''
  })
  clean = clean.replace(/<link\s+rel="canonical"[^>]*>/gi, match => {
    headElements.push(match.trim())
    return ''
  })

  return { headElements, cleanBody: clean }
}
