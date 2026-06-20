import { Link, useSearchParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { AlertCircle, CheckCircle2, MailWarning, LoaderCircle } from 'lucide-react'
import { confirmEmail, AuthApiError } from '@/modules/auth/authApi'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Button } from '@/shared/ui/button'
import type { ReactNode } from 'react'

const DESCRIPTION =
  'Confirma tu correo para activar tu prueba gratuita de 14 días en Fibras Inmobiliarias y completar el registro de tu cuenta.'

function formatTrialEndsAt(value: string): string | null {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return null

  return new Intl.DateTimeFormat('es-MX', {
    dateStyle: 'full',
    timeStyle: 'short',
    timeZone: 'UTC',
  }).format(date)
}

export function ConfirmarEmailPage() {
  const [searchParams] = useSearchParams()
  const status = searchParams.get('status')
  const token = searchParams.get('token')
  const trialEndsAtParam = searchParams.get('t')

  usePageTitle('Confirma tu email | Fibras Inmobiliarias', DESCRIPTION, {
    canonicalPath: '/confirmar-email',
    robotsDirectives: 'noindex,follow',
  })

  const confirmationQuery = useQuery({
    queryKey: ['auth', 'confirm-email', token],
    queryFn: () => confirmEmail(token ?? ''),
    enabled: Boolean(token) && status === null,
    retry: false,
    staleTime: Number.POSITIVE_INFINITY,
  })

  if (status !== null) {
    if (status === 'confirmed') {
      const trialEndsAt = trialEndsAtParam ? formatTrialEndsAt(trialEndsAtParam) : null

      return (
        <ConfirmCard
          icon={<CheckCircle2 className="size-6" />}
          title="¡Cuenta confirmada!"
          description={
            trialEndsAt
              ? `Tu prueba de 14 días ha comenzado. Vence el ${trialEndsAt}.`
              : 'Tu prueba de 14 días ha comenzado.'
          }
          tone="success"
          action={
            <Button asChild>
              <Link to="/portafolio">Ir a mi portafolio</Link>
            </Button>
          }
        />
      )
    }

    if (status === 'expired') {
      return (
        <ConfirmCard
          icon={<MailWarning className="size-6" />}
          title="El enlace expiró"
          description="Solicita un nuevo correo de confirmación para activar tu cuenta."
          tone="warning"
          action={
            <Button asChild>
              <Link to="/activar?reason=trial_not_started">Reenviar confirmación</Link>
            </Button>
          }
        />
      )
    }

    if (status === 'already_confirmed') {
      return (
        <ConfirmCard
          icon={<CheckCircle2 className="size-6" />}
          title="Tu cuenta ya fue confirmada"
          description="Puedes iniciar sesión con tu correo y contraseña para entrar a tu portafolio."
          tone="success"
          action={
            <Button asChild>
              <Link to="/login">Iniciar sesión</Link>
            </Button>
          }
        />
      )
    }

    return (
      <ConfirmCard
        icon={<AlertCircle className="size-6" />}
        title="No pudimos confirmar tu cuenta"
        description="Vuelve a intentarlo en unos minutos o solicita un nuevo correo de confirmación."
        tone="error"
      />
    )
  }

  if (!token) {
    return (
      <ConfirmCard
        icon={<AlertCircle className="size-6" />}
        title="Enlace inválido"
        description="El enlace de confirmación no incluye un token válido."
        tone="error"
      />
    )
  }

  if (confirmationQuery.isPending) {
    return (
      <ConfirmCard
        icon={<LoaderCircle className="size-6 animate-spin" />}
        title="Confirmando tu email"
        description="Estamos validando tu enlace. Esto tarda solo unos segundos."
        tone="loading"
      />
    )
  }

  if (confirmationQuery.isError) {
    const apiError = confirmationQuery.error instanceof AuthApiError ? confirmationQuery.error : null

    if (apiError?.code === 'token_expired') {
      return (
        <ConfirmCard
          icon={<MailWarning className="size-6" />}
          title="El enlace expiró"
          description="Solicita un nuevo correo de confirmación para activar tu cuenta."
          tone="warning"
          action={
            <Button asChild>
              <Link to="/activar?reason=trial_not_started">Reenviar confirmación</Link>
            </Button>
          }
        />
      )
    }

    if (apiError?.code === 'token_already_used') {
      return (
        <ConfirmCard
          icon={<CheckCircle2 className="size-6" />}
          title="Tu cuenta ya fue confirmada"
          description="Puedes iniciar sesión con tu correo y contraseña para entrar a tu portafolio."
          tone="success"
          action={
            <Button asChild>
              <Link to="/login">Iniciar sesión</Link>
            </Button>
          }
        />
      )
    }

    return (
      <ConfirmCard
        icon={<AlertCircle className="size-6" />}
        title="No pudimos confirmar tu cuenta"
        description="Vuelve a intentarlo en unos minutos o solicita un nuevo correo de confirmación."
        tone="error"
      />
    )
  }

  const trialEndsAt = confirmationQuery.data?.trialEndsAt

  return (
    <ConfirmCard
      icon={<CheckCircle2 className="size-6" />}
      title="¡Cuenta confirmada!"
      description={
        trialEndsAt
          ? `Tu prueba de 14 días ha comenzado. Vence el ${formatTrialEndsAt(trialEndsAt)}.`
          : 'Tu prueba de 14 días ha comenzado.'
      }
      tone="success"
      action={
        <Button asChild>
          <Link to="/portafolio">Ir a mi portafolio</Link>
        </Button>
      }
    />
  )
}

function ConfirmCard({
  icon,
  title,
  description,
  tone,
  action,
}: {
  icon: ReactNode
  title: string
  description: string
  tone: 'success' | 'warning' | 'error' | 'loading'
  action?: ReactNode
}) {
  const toneClasses: Record<'success' | 'warning' | 'error' | 'loading', string> = {
    success: 'border-emerald-200 bg-emerald-50 text-emerald-950',
    warning: 'border-amber-200 bg-amber-50 text-amber-950',
    error: 'border-rose-200 bg-rose-50 text-rose-950',
    loading: 'border-sky-200 bg-sky-50 text-sky-950',
  }

  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] items-center justify-center px-4 py-12">
      <section className={`w-full max-w-xl rounded-3xl border p-8 shadow-sm ${toneClasses[tone]}`}>
        <div className="flex flex-col items-start gap-5">
          <div className="inline-flex size-14 items-center justify-center rounded-2xl bg-background/70">
            {icon}
          </div>
          <div className="space-y-2">
            <h1 className="font-playfair text-3xl font-bold leading-tight">{title}</h1>
            <p className="max-w-prose text-sm leading-6 text-current/80 md:text-base">
              {description}
            </p>
          </div>
          {action ? <div className="pt-2">{action}</div> : null}
        </div>
      </section>
    </div>
  )
}
