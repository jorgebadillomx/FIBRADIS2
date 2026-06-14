export const FIBRA_PAGE_LOADING_TABS = [
  'Mercado',
  'Fundamentales',
  'Distribuciones',
  'Noticias',
  'Enlaces',
] as const

export const FIBRA_PAGE_LOADING_COUNTS = {
  marketMetricCards: 3,
  marketRangeButtons: 3,
  fundamentalsRows: 6,
  // Coincide con INITIAL_GROUPS de DistribucionesSection (filas mostradas antes de "ver más"):
  // reservar las filas típicas evita que la tabla crezca y empuje el footer (CLS).
  distributionRows: 8,
  distributionSummaryLines: 3,
  newsLines: 3,
  descriptionLines: 3,
  reportLines: 3,
} as const

// `priceWidthClass` (skeleton) y `priceFallbackWidthClass` (estado cargado/—) deben reservar
// el MISMO ancho para no provocar shift horizontal al intercambiar skeleton ↔ contenido (CLS).
// w-32 = min-w-32 = 8rem.
export const PRECIO_SECTION_LOADING_SHELL = {
  priceWidthClass: 'w-32',
  priceFallbackWidthClass: 'min-w-32',
  metadataWidthClass: 'min-w-[11rem]',
  badgeWidthClass: 'w-24',
  detailWidthClass: 'w-32',
} as const

// Tokens del precio en el header sticky (texto text-2xl). El skeleton (`priceSkeletonWidthClass`)
// y el estado cargado (`priceReserveClass`) reservan el mismo ancho — w-24 = min-w-24 = 6rem.
export const FIBRA_HEADER_LOADING_SHELL = {
  containerWidthClass: 'min-w-[12rem]',
  priceSkeletonWidthClass: 'w-24',
  priceReserveClass: 'min-w-24',
  yieldBadgeWidthClass: 'w-12',
  freshnessBadgeWidthClass: 'w-20',
} as const

export const FUNDAMENTALES_SECTION_LOADING_SHELL = {
  headerTitleWidthClass: 'w-24',
  headerMetaWidthClass: 'w-36',
  rowLabelWidthClass: 'w-40',
  rowValueWidthClass: 'w-16',
  rowNoteWidthClass: 'w-28',
} as const
