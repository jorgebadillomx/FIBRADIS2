import { ChevronRight } from 'lucide-react'
import { calcSignal, type SignalBadgeProps, type SignalStatus } from '@/modules/portafolio/signal-badge'

const BADGE_CLASSES: Record<SignalStatus, string> = {
  verde: 'bg-green-100 text-green-800 border-green-300',
  amarillo: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  rojo: 'bg-red-100 text-red-800 border-red-300',
  gris: 'bg-slate-100 text-slate-700 border-slate-200',
}

const BADGE_LABELS: Record<SignalStatus, string> = {
  verde: 'Descuento',
  amarillo: 'Neutro',
  rojo: 'Prima',
  gris: 'Sin NAV',
}

export function SignalBadge({ navPerCbfi, precioActual }: SignalBadgeProps) {
  const { status, tooltip } = calcSignal(navPerCbfi, precioActual)

  return (
    <span
      className={`inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-medium ${BADGE_CLASSES[status]}`}
      title={tooltip}
      aria-label={tooltip}
    >
      {BADGE_LABELS[status]}
    </span>
  )
}

export function PortfolioExpandIcon({ isExpanded }: { isExpanded: boolean }) {
  return (
    <ChevronRight
      className={`size-4 transition-transform ${isExpanded ? 'rotate-90' : ''}`}
      aria-hidden="true"
    />
  )
}
