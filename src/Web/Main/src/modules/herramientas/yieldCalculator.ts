/** Devuelve el rendimiento TTM en porcentaje (0–100), e.g. 8.28 para 8.28%.
 *  annualDist: suma real de las distribuciones de los últimos 12 meses por título. */
export function calcYield(annualDist: number, currentPrice: number): number | null {
  if (!Number.isFinite(currentPrice) || currentPrice <= 0) return null
  const safeDist = Number.isFinite(annualDist) ? Math.max(annualDist, 0) : 0
  if (safeDist <= 0) return null
  return (safeDist / currentPrice) * 100
}
