export function calcCbfis(monto: number, precio: number): number {
  if (precio <= 0 || monto <= 0) return 0
  return Math.floor(monto / precio)
}

export function calcSobra(monto: number, cbfis: number, precio: number): number {
  return monto - cbfis * precio
}

export function calcRentaBruta(cbfis: number, distCbfi: number | null | undefined): number | null {
  if (distCbfi == null) return null
  return cbfis * distCbfi
}

export function calcRentaBrutaAnual(cbfis: number, distCbfiAnual: number | null | undefined): number | null {
  if (distCbfiAnual == null) return null
  return cbfis * distCbfiAnual
}
