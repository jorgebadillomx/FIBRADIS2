import { calcRealReturn } from '../../shared/lib/inflation-utils.ts'

type FibraVsCetesScenario = {
  netRatePct: number
  capitalFinal: number
  rentaAcumuladaNeta: number
  rendimientoTotalPct: number
}

export type FibraVsCetesResult = {
  fibra: FibraVsCetesScenario
  cetes: FibraVsCetesScenario
}

export type MetaRentaResult = {
  capitalNecesario: number | null
  rentaMensualBrutaEstimada: number | null
  cbfisEstimados: number | null
}

export type RetornoTotalResult = {
  plusvaliaPct: number | null
  yieldNetoPct: number | null
  retornoTotalPct: number | null
}

export function parseNumberInput(value: string): number | null {
  const trimmed = value.trim()
  if (trimmed.length === 0) return null

  const parsed = Number(trimmed.replace(/,/g, ''))
  return Number.isFinite(parsed) ? parsed : null
}

function sanitizeNonNegative(value: number): number {
  if (!Number.isFinite(value) || value <= 0) return 0
  return value
}

function buildScenario(monto: number, netRatePct: number, horizonte: number): FibraVsCetesScenario {
  const capitalFinal = monto * (1 + netRatePct / 100) ** horizonte
  const rentaAcumuladaNeta = capitalFinal - monto
  const rendimientoTotalPct = monto > 0 ? ((capitalFinal / monto) - 1) * 100 : 0

  return {
    netRatePct,
    capitalFinal,
    rentaAcumuladaNeta,
    rendimientoTotalPct,
  }
}

export function calcFibraVsCetes(
  monto: number,
  yieldFibraPct: number,
  cetesPct: number,
  horizonte: number,
): FibraVsCetesResult {
  const safeMonto = sanitizeNonNegative(monto)
  const safeYieldFibra = sanitizeNonNegative(yieldFibraPct)
  const safeCetes = sanitizeNonNegative(cetesPct)
  const safeHorizonte = Math.max(Math.floor(Number.isFinite(horizonte) ? horizonte : 0), 0)

  const fibraNetRatePct = safeYieldFibra * 0.70
  const cetesNetRatePct = safeCetes * 0.80

  return {
    fibra: buildScenario(safeMonto, fibraNetRatePct, safeHorizonte),
    cetes: buildScenario(safeMonto, cetesNetRatePct, safeHorizonte),
  }
}

export function calcMetaRenta(
  rentaMensual: number,
  yieldPct: number,
  precioRef?: number,
): MetaRentaResult {
  if (!Number.isFinite(rentaMensual) || !Number.isFinite(yieldPct) || rentaMensual <= 0 || yieldPct <= 0) {
    return {
      capitalNecesario: null,
      rentaMensualBrutaEstimada: null,
      cbfisEstimados: null,
    }
  }

  const capitalNecesario = (rentaMensual * 12) / (yieldPct / 100)
  const rentaMensualBrutaEstimada = capitalNecesario * (yieldPct / 100) / 12

  return {
    capitalNecesario,
    rentaMensualBrutaEstimada,
    cbfisEstimados:
      Number.isFinite(precioRef) && (precioRef ?? 0) > 0
        ? Math.ceil(capitalNecesario / precioRef!)
        : null,
  }
}

export function calcRetornoTotal(
  precioCompra: number,
  precioActual: number,
  distribucionesRecibidas: number,
  isrRetenidoTotal: number,
): RetornoTotalResult {
  if (
    !Number.isFinite(precioCompra) ||
    !Number.isFinite(precioActual) ||
    !Number.isFinite(distribucionesRecibidas) ||
    !Number.isFinite(isrRetenidoTotal) ||
    precioCompra <= 0
  ) {
    return {
      plusvaliaPct: null,
      yieldNetoPct: null,
      retornoTotalPct: null,
    }
  }

  const plusvaliaPct = ((precioActual - precioCompra) / precioCompra) * 100
  const yieldNetoPct = ((Math.max(0, distribucionesRecibidas) - Math.max(0, isrRetenidoTotal)) / precioCompra) * 100

  return {
    plusvaliaPct,
    yieldNetoPct,
    retornoTotalPct: plusvaliaPct + yieldNetoPct,
  }
}

export type RetornoDesdeCompraResult = {
  precioCompra: number | null
  distribucionesRecibidas: number | null
  isrEstimado: number | null
  plusvaliaPct: number | null
  yieldNetoPct: number | null
  retornoTotalPct: number | null
  cagrPct: number | null
}

export function calcRetornoDesdeCompra(
  fechaCompra: string,
  precioActual: number,
  priceHistory: Array<{ date: string; close: number | null }>,
  distributions: Array<{ date: string; amountPerUnit: number; taxableAmountPerUnit: number | null }>,
  isrRate: number,
): RetornoDesdeCompraResult {
  const EMPTY: RetornoDesdeCompraResult = {
    precioCompra: null, distribucionesRecibidas: null, isrEstimado: null,
    plusvaliaPct: null, yieldNetoPct: null, retornoTotalPct: null, cagrPct: null,
  }

  if (!fechaCompra || !Number.isFinite(precioActual) || precioActual <= 0) return EMPTY

  // Precio en o antes de la fecha de compra (cubre fines de semana/festivos)
  const precioCompraEntry = priceHistory
    .filter(p => p.close != null && p.date <= fechaCompra)
    .sort((a, b) => b.date.localeCompare(a.date))[0]

  if (!precioCompraEntry) return EMPTY
  const precioCompra = precioCompraEntry.close!

  // Distribuciones recibidas desde la fecha de compra
  const distsSince = distributions.filter(d => d.date >= fechaCompra)
  const distribucionesRecibidas = distsSince.reduce((s, d) => s + Number(d.amountPerUnit), 0)
  const taxableBase = distsSince.reduce(
    (s, d) => s + Number(d.taxableAmountPerUnit ?? d.amountPerUnit),
    0,
  )
  const isrEstimado = taxableBase * isrRate

  const plusvaliaPct = ((precioActual - precioCompra) / precioCompra) * 100
  const distribucionesNetas = Math.max(0, distribucionesRecibidas) - Math.max(0, isrEstimado)
  const yieldNetoPct = (distribucionesNetas / precioCompra) * 100
  const retornoTotalPct = plusvaliaPct + yieldNetoPct

  // CAGR — requiere al menos 1 mes de tenencia
  const msPerYear = 365.25 * 24 * 60 * 60 * 1000
  const years = (Date.now() - new Date(fechaCompra).getTime()) / msPerYear
  let cagrPct: number | null = null
  if (years >= 1 / 12) {
    const totalFactor = (precioActual + distribucionesNetas) / precioCompra
    if (totalFactor > 0) cagrPct = (Math.pow(totalFactor, 1 / years) - 1) * 100
  }

  return { precioCompra, distribucionesRecibidas, isrEstimado, plusvaliaPct, yieldNetoPct, retornoTotalPct, cagrPct }
}

export function calcAnnualizedRealReturn(
  rendimientoTotalPct: number,
  horizonteYears: number,
  inflationPct: number,
): number {
  if (
    !Number.isFinite(rendimientoTotalPct) ||
    !Number.isFinite(horizonteYears) ||
    horizonteYears <= 0
  ) {
    return 0
  }

  const tae = (Math.pow(1 + rendimientoTotalPct / 100, 1 / horizonteYears) - 1) * 100
  return calcRealReturn(tae, inflationPct)
}
