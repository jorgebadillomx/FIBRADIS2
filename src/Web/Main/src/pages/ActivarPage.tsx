import { useState } from 'react'
import { useSearchParams } from 'react-router'
import { resendConfirmation } from '@/modules/auth/authApi'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Button } from '@/shared/ui/button'
import { NotifyWithReceiptButton } from './NotifyWithReceiptButton'
import { PLANES, PAYMENT_INFO } from './payment-plans'

const DESCRIPTION =
  'Activa tu cuenta en Fibras Inmobiliarias para acceder al portafolio, reportes trimestrales, oportunidades y herramientas de análisis.'

export function ActivarPage() {
  const [searchParams] = useSearchParams()
  const reason = searchParams.get('reason')
  usePageTitle('Activa tu cuenta | Fibras Inmobiliarias', DESCRIPTION, {
    robotsDirectives: 'noindex,nofollow',
  })

  if (reason === 'trial_expired') {
    return <TrialExpiredView />
  }

  // trial_not_started o reason ausente/desconocido
  return <TrialNotStartedView />
}

function TrialExpiredView() {
  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] items-center justify-center px-4 py-12">
      <section className="w-full max-w-2xl rounded-3xl border border-border bg-card p-8 shadow-sm">
        <div className="mb-6">
          <h1 className="font-playfair text-3xl font-bold leading-tight">
            Tu prueba de 14 días ha terminado
          </h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Elige un plan para continuar accediendo a portafolio, reportes y herramientas.
          </p>
        </div>

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
      </section>
    </div>
  )
}

function TrialNotStartedView() {
  const [email, setEmail] = useState('')
  const [resendStatus, setResendStatus] = useState<'idle' | 'sending' | 'sent'>('idle')

  async function handleResend() {
    if (!email.trim()) return
    setResendStatus('sending')
    try {
      await resendConfirmation(email.trim())
    } catch {
      // resendConfirmation nunca falla — es anti-enumeration
    }
    setResendStatus('sent')
  }

  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] items-center justify-center px-4 py-12">
      <section className="w-full max-w-xl rounded-3xl border border-border bg-card p-8 shadow-sm">
        <div className="mb-6">
          <h1 className="font-playfair text-3xl font-bold leading-tight">
            Confirma tu email para comenzar tu prueba gratuita
          </h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Tienes 14 días de acceso gratuito a portafolio, reportes trimestrales, oportunidades y
            herramientas de análisis. Solo necesitas confirmar tu email.
          </p>
        </div>

        {resendStatus === 'sent' ? (
          <p className="text-sm text-emerald-700 font-medium">
            ✓ Si el email existe, recibirás un enlace de confirmación.
          </p>
        ) : (
          <div className="flex flex-col gap-3">
            <div className="flex flex-col gap-1.5">
              <label htmlFor="activar-email" className="text-sm font-medium leading-none">
                Tu email
              </label>
              <input
                id="activar-email"
                name="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="tu@email.com"
                className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              />
            </div>
            <Button
              onClick={handleResend}
              disabled={resendStatus === 'sending' || !email.trim()}
            >
              {resendStatus === 'sending' ? 'Enviando…' : 'Reenviar email de confirmación'}
            </Button>
          </div>
        )}
      </section>
    </div>
  )
}
