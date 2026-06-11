/** Devuelve el rendimiento anualizado en porcentaje (0–100), e.g. 2.48 para 2.48%. */
export function calcYield(quarterlyDist: number, currentPrice: number): number | null {
  if (!Number.isFinite(currentPrice) || currentPrice <= 0) return null
  const safeDist = Number.isFinite(quarterlyDist) ? Math.max(quarterlyDist, 0) : 0
  if (safeDist <= 0) return null
  return ((safeDist * 4) / currentPrice) * 100
}
