export type SignalStatus = 'verde' | 'amarillo' | 'rojo' | 'gris'

export interface SignalBadgeProps {
  navPerCbfi: number | string | null | undefined
  precioActual: number | string | null | undefined
}

export function calcSignal(
  navPerCbfi: number | string | null | undefined,
  precioActual: number | string | null | undefined
): { status: SignalStatus; tooltip: string } {
  const nav = typeof navPerCbfi === 'string' ? Number(navPerCbfi) : navPerCbfi
  const price = typeof precioActual === 'string' ? Number(precioActual) : precioActual

  if (!nav || !price || nav <= 0 || price <= 0) {
    return { status: 'gris', tooltip: 'Sin datos de NAV' }
  }

  const discount = (nav - price) / nav
  const pct = Math.abs(discount * 100).toFixed(1)

  if (discount > 0.1) {
    return { status: 'verde', tooltip: `Cotiza con descuento de ${pct}% respecto al NAV` }
  }

  if (discount < -0.1) {
    return { status: 'rojo', tooltip: `Cotiza con prima de ${pct}% respecto al NAV` }
  }

  const sign = discount === 0 ? '±' : discount > 0 ? '-' : '+'
  return {
    status: 'amarillo',
    tooltip: `Cotiza dentro de ±10% del NAV (${sign}${pct}%)`,
  }
}
