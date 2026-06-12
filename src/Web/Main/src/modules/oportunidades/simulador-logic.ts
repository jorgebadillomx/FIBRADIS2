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

export function calcRentaProyectadaAnual(
  currentRentaAnual: number,
  additionalTitulos: number,
  precioActual: number,
  dividendYieldPct: number | null | undefined,
  currentTitulos: number,
): number {
  if (additionalTitulos <= 0) return currentRentaAnual
  if (dividendYieldPct != null) {
    return currentRentaAnual + additionalTitulos * precioActual * (dividendYieldPct / 100)
  }
  if (currentTitulos > 0 && currentRentaAnual > 0) {
    const rentaPerTitle = currentRentaAnual / currentTitulos
    return currentRentaAnual + additionalTitulos * rentaPerTitle
  }
  return currentRentaAnual
}

export function calcTitulosParaRentaTarget(
  targetMensual: number,
  precioActual: number,
  dividendYieldPct: number | null | undefined,
  currentTitulos: number,
  currentRentaAnual: number,
): number | null {
  if (targetMensual <= 0) return null
  const targetAnual = targetMensual * 12
  if (dividendYieldPct != null && dividendYieldPct > 0 && precioActual > 0) {
    return Math.ceil(targetAnual / (precioActual * (dividendYieldPct / 100)))
  }
  if (currentTitulos > 0 && currentRentaAnual > 0) {
    const rentaPerTitle = currentRentaAnual / currentTitulos
    return Math.ceil(targetAnual / rentaPerTitle)
  }
  return null
}
