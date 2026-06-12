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
