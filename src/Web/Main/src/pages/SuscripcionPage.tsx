import { Navigate } from 'react-router'
import { useAuth } from '@/modules/auth/AuthContext'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { resolveSubscriptionState } from './suscripcion-logic'
import type { SubscriptionState } from './suscripcion-logic'
import { NotifyWithReceiptButton } from './NotifyWithReceiptButton'
import { PLANES, PAYMENT_INFO } from './payment-plans'

export { resolveSubscriptionState, type SubscriptionState }

const DESCRIPTION =
  'Consulta el estado de tu suscripción en Fibras Inmobiliarias, conoce tu plan activo y las instrucciones de pago.'

function formatDate(isoString: string): string {
  if (!isoString) return '—'
  const date = new Date(isoString)
  if (isNaN(date.getTime())) return '—'
  return date.toLocaleDateString('es-MX', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function StatusBanner({ state }: { state: SubscriptionState }) {
  if (state.kind === 'trial') {
    return (
      <div className="mb-6 rounded-2xl border border-amber-300 bg-amber-50 px-5 py-4">
        <p className="font-semibold text-amber-800">
          Período de prueba activo — vence el {formatDate(state.trialEndsAt)}{' '}
          ({state.daysRemaining} {state.daysRemaining === 1 ? 'día restante' : 'días restantes'})
        </p>
      </div>
    )
  }

  if (state.kind === 'active') {
    const label = state.subscriptionType === 'Monthly' ? 'Monthly' : 'Annual'
    return (
      <div className="mb-6 rounded-2xl border border-emerald-300 bg-emerald-50 px-5 py-4">
        <p className="font-semibold text-emerald-800">
          Plan {label} activo — vence el {formatDate(state.subscriptionEndsAt)}
        </p>
      </div>
    )
  }

  if (state.kind === 'lifetime') {
    return (
      <div className="mb-6 rounded-2xl border border-emerald-300 bg-emerald-50 px-5 py-4">
        <p className="font-semibold text-emerald-800">Acceso de por vida</p>
      </div>
    )
  }

  const message = state.hadTrial ? 'Tu prueba ha terminado' : 'Tu acceso ha expirado'
  return (
    <div className="mb-6 rounded-2xl border border-border bg-muted/40 px-5 py-4">
      <p className="font-semibold text-muted-foreground">{message}</p>
    </div>
  )
}

function PaymentSection() {
  return (
    <div>
      <div className="mb-8 grid gap-4 sm:grid-cols-3">
        {PLANES.map((plan) => (
          <div
            key={plan.nombre}
            className="rounded-2xl border border-border bg-background p-5 flex flex-col gap-1"
          >
            <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              {plan.nombre}
            </span>
            <span className="text-xl font-bold">{plan.precio}</span>
            <span className="text-sm text-muted-foreground">{plan.descripcion}</span>
          </div>
        ))}
      </div>

      <div className="mb-8 rounded-2xl border border-border bg-muted/30 p-5">
        <h2 className="mb-3 text-base font-semibold">Instrucciones de pago</h2>
        <dl className="flex flex-col gap-2 text-sm">
          <div className="flex gap-2">
            <dt className="font-medium text-muted-foreground w-16 shrink-0">CLABE:</dt>
            <dd className="font-mono text-amber-700 font-semibold">{PAYMENT_INFO.clabe}</dd>
          </div>
          <div className="flex gap-2">
            <dt className="font-medium text-muted-foreground w-16 shrink-0">Banco:</dt>
            <dd>{PAYMENT_INFO.banco}</dd>
          </div>
          <div className="flex gap-2">
            <dt className="font-medium text-muted-foreground w-16 shrink-0">Concepto:</dt>
            <dd>{PAYMENT_INFO.concepto}</dd>
          </div>
          <div className="flex gap-2">
            <dt className="font-medium text-muted-foreground w-16 shrink-0">Contacto:</dt>
            <dd>{PAYMENT_INFO.contacto}</dd>
          </div>
        </dl>
      </div>

      <NotifyWithReceiptButton />
    </div>
  )
}

export function SuscripcionPage() {
  const { status, isActive, trialEndsAt, subscriptionType, subscriptionEndsAt } = useAuth()

  usePageTitle('Mi suscripción | Fibras Inmobiliarias', DESCRIPTION, {
    robotsDirectives: 'noindex,nofollow',
  })

  if (status === 'anonymous') {
    return <Navigate to="/login?redirect=%2Fsuscripcion" replace />
  }

  if (status === 'checking') {
    return (
      <div className="flex min-h-screen items-start justify-center pt-24">
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent" />
      </div>
    )
  }

  const state = resolveSubscriptionState(isActive, subscriptionType, trialEndsAt, subscriptionEndsAt)
  const showPayment = state.kind !== 'lifetime'

  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] items-start justify-center px-4 py-12">
      <section className="w-full max-w-2xl rounded-3xl border border-border bg-card p-8 shadow-sm">
        <div className="mb-6">
          <h1 className="font-playfair text-3xl font-bold leading-tight">Mi suscripción</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Estado actual de tu acceso a Fibras Inmobiliarias.
          </p>
        </div>

        <StatusBanner state={state} />

        {showPayment ? <PaymentSection /> : null}
      </section>
    </div>
  )
}
