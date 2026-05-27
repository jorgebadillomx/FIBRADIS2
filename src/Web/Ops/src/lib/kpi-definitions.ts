export const KPI_DEFINITIONS = {
  capRate: {
    label: 'Cap Rate',
    formula: 'Cap Rate = NOI anualizado / Valor de propiedades de inversión',
    description:
      'Tasa de capitalización: mide el rendimiento operativo del portafolio inmobiliario en relación a su valor.',
  },
  navPerCbfi: {
    label: 'NAV/CBFI',
    formula: 'NAV = Valor de propiedades − Deuda total | NAV/CBFI = NAV / CBFIs en circulación',
    description:
      'Valor Neto de los Activos por certificado.',
  },
  ltv: {
    label: 'LTV',
    formula: 'LTV = Deuda total / Valor de propiedades de inversión',
    description:
      'Loan-to-Value del portafolio.',
  },
  noiMargin: {
    label: 'NOI',
    formula: 'NOI Margin = NOI / Ingresos Totales',
    description:
      'Margen operativo sobre ingresos.',
  },
  ffoMargin: {
    label: 'FFO',
    formula: 'FFO Margin = FFO / Ingresos Totales',
    description:
      'Margen de fondos de operación sobre ingresos.',
  },
  quarterlyDistribution: {
    label: 'Dist. Trim.',
    formula: 'Distribución = Resultado Fiscal Distribuido + Reembolso de Capital',
    description:
      'Distribución trimestral por CBFI.',
  },
} as const

export type KpiKey = keyof typeof KPI_DEFINITIONS
