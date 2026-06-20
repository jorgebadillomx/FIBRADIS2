export const PLANES = [
  { nombre: 'Mensual', precio: '$39 MXN / mes', descripcion: 'Acceso completo mensual' },
  { nombre: 'Anual', precio: '$390 MXN / año', descripcion: 'Ahorra un 17% vs. mensual' },
  { nombre: 'Lifetime', precio: '$990 MXN', descripcion: 'Pago único, acceso de por vida' },
]

export const PAYMENT_INFO = {
  clabe: '722969010321418243',
  banco: 'Mercado Pago',
  concepto: 'Suscripción Fibras Inmobiliarias',
  contacto: 'contacto@fibrasinmobiliarias.com',
} as const

export const MAX_RECEIPT_BYTES = 5 * 1024 * 1024
export const RECEIPT_ACCEPT = 'image/*,application/pdf'
