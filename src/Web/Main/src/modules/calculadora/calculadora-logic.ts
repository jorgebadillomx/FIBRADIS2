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

export function calcCbfisConComision(
  monto: number,
  precio: number,
  commissionFactor: number,
  ivaFactor: number,
): number {
  if (precio <= 0 || monto <= 0) return 0
  const effectivePrice = precio * (1 + commissionFactor * (1 + ivaFactor))
  return Math.floor(monto / effectivePrice)
}

export function calcSobraConComision(
  monto: number,
  cbfis: number,
  precio: number,
  commissionFactor: number,
  ivaFactor: number,
): number {
  const effectivePrice = precio * (1 + commissionFactor * (1 + ivaFactor))
  return monto - cbfis * effectivePrice
}

export function isRecentQuarter(periodo: string | null, n = 4): boolean {
  if (!periodo) return false
  const match = /^Q([1-4])-(\d{4})$/.exec(periodo)
  if (!match) return false
  const pq = Number(match[1])
  const py = Number(match[2])
  const now = new Date()
  const currentQ = Math.ceil((now.getMonth() + 1) / 3)
  const currentY = now.getFullYear()
  const periodKey = py * 4 + pq
  const currentKey = currentY * 4 + currentQ
  return periodKey >= currentKey - n + 1 && periodKey <= currentKey
}
