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
