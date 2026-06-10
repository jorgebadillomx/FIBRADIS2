export function calcYoc(
  rentaAnual: number | string | null | undefined,
  costoTotalCompra: number | string | null | undefined,
): number | null {
  if (rentaAnual == null || costoTotalCompra == null) return null

  const annualRent = Number(rentaAnual)
  const totalCost = Number(costoTotalCompra)
  if (!Number.isFinite(annualRent) || !Number.isFinite(totalCost) || totalCost <= 0) return null

  return (annualRent / totalCost) * 100
}

export function calcYieldPortafolio(
  rentasAnualesBrutas: number | string | null | undefined,
  valorTotal: number | string | null | undefined,
): number | null {
  if (rentasAnualesBrutas == null || valorTotal == null) return null

  const annualRent = Number(rentasAnualesBrutas)
  const totalValue = Number(valorTotal)
  if (!Number.isFinite(annualRent) || !Number.isFinite(totalValue) || totalValue <= 0) return null

  return (annualRent / totalValue) * 100
}
