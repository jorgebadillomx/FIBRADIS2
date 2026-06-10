export function calcNuevoAvg(
  titulos: number,
  costoPromedio: number,
  adicionales: number,
  precioActual: number,
): number {
  const total = titulos + adicionales
  if (total === 0) return 0
  return (titulos * costoPromedio + adicionales * precioActual) / total
}

export function calcNuevaPlusvaliaPct(nuevoAvg: number, precioActual: number): number {
  if (nuevoAvg === 0) return 0
  return ((precioActual - nuevoAvg) / nuevoAvg) * 100
}

export function calcNuevoValor(titulos: number, adicionales: number, precioActual: number): number {
  return (titulos + adicionales) * precioActual
}

export function calcNewAvgCost(
  currentTitulos: number,
  currentAvgCost: number,
  currentPrice: number,
  newTitulos: number,
  commissionFactor: number,
): number {
  const total = currentTitulos + newTitulos
  if (total === 0) return 0

  const raw =
    (currentTitulos * currentAvgCost +
      newTitulos * currentPrice * (1 + commissionFactor)) /
    total
  return Math.round(raw * 10_000) / 10_000
}
