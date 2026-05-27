export const KPI_DEFINITIONS = {
  capRate: {
    label: 'Cap Rate',
    formula: 'Cap Rate = NOI anualizado / Valor de propiedades de inversión',
    description:
      'Tasa de capitalización: mide el rendimiento operativo del portafolio inmobiliario en relación a su valor. ' +
      'Un Cap Rate más alto implica mayor rendimiento y generalmente más riesgo; uno bajo refleja activos premium en alta demanda.',
  },
  navPerCbfi: {
    label: 'NAV por CBFI',
    formula: 'NAV = Valor de propiedades − Deuda total | NAV/CBFI = NAV / CBFIs en circulación',
    description:
      'Valor Neto de los Activos por certificado. Indica si el precio de mercado cotiza con descuento o premio ' +
      'respecto al valor real de los activos que respaldan cada CBFI.',
  },
  ltv: {
    label: 'LTV',
    formula: 'LTV = Deuda total / Valor de propiedades de inversión',
    description:
      'Loan-to-Value: nivel de apalancamiento en relación al valor inmobiliario. ' +
      'LTV bajo indica solidez financiera; LTV alto señala mayor exposición al riesgo.',
  },
  noiMargin: {
    label: 'NOI Margin',
    formula: 'NOI Margin = NOI / Ingresos Totales',
    description:
      'Margen de Ingreso Neto Operativo: porcentaje de ingresos que queda tras descontar gastos directos de operación. ' +
      'Mide la eficiencia operativa del portafolio.',
  },
  ffoMargin: {
    label: 'FFO Margin',
    formula: 'FFO Margin = FFO / Ingresos Totales | FFO = Utilidad Neta + ajustes por valuación − ganancias cambiarias',
    description:
      'Fondos de Operación sobre ingresos. El FFO corrige la utilidad neta eliminando distorsiones contables ' +
      'para mostrar cuánto genera realmente el portafolio en operación.',
  },
  quarterlyDistribution: {
    label: 'Dist. Trimestral',
    formula: 'Distribución = Resultado Fiscal Distribuido + Reembolso de Capital',
    description:
      'Pago en efectivo por CBFI cada trimestre. Puede componerse de utilidades fiscales y reembolso de capital.',
  },
} as const

export type KpiKey = keyof typeof KPI_DEFINITIONS
