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
