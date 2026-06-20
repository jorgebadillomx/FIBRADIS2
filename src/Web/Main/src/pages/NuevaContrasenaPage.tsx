import { useState, type FormEvent, type ReactNode } from 'react'
import { Link, useSearchParams } from 'react-router'
import { AuthApiError, resetPassword } from '@/modules/auth/authApi'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'

const TITLE = 'Nueva contraseña | Fibras Inmobiliarias'
const DESCRIPTION =
  'Crea una nueva contraseña segura para volver a entrar a tu cuenta en Fibras Inmobiliarias.'

function isProblematicStatus(status: string | null): status is 'expired' | 'invalid' {
  return status === 'expired' || status === 'invalid'
}

export function NuevaContrasenaPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')
  const status = searchParams.get('status')
  const initialError = !token || isProblematicStatus(status)
  const [newPassword, setNewPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [submitted, setSubmitted] = useState(false)
  const [tokenError, setTokenError] = useState(initialError)

  usePageTitle(TITLE, DESCRIPTION, {
    canonicalPath: '/nueva-contrasena',
    robotsDirectives: 'noindex,nofollow',
  })

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!token || loading) return

    const normalizedPassword = newPassword.trim()
    if (!normalizedPassword) return

    setLoading(true)
    setFormError(null)

    try {
      await resetPassword(token, newPassword)
      setSubmitted(true)
    } catch (err) {
      if (err instanceof AuthApiError && err.code === 'token_invalid') {
        setTokenError(true)
        return
      }

      setFormError(err instanceof Error ? err.message : 'No se pudo guardar la contraseña.')
    } finally {
      setLoading(false)
    }
  }

  if (submitted) {
    return (
      <StatusCard
        tone="success"
        title="Tu contraseña fue actualizada. Ahora puedes iniciar sesión."
        action={<Button asChild><Link to="/login">Iniciar sesión</Link></Button>}
      />
    )
  }

  if (tokenError) {
    return (
      <StatusCard
        tone="error"
        title="Este enlace no es válido o ya expiró. Solicita uno nuevo."
        action={<Button asChild variant="outline"><Link to="/recuperar-contrasena">Solicitar nuevo enlace</Link></Button>}
      />
    )
  }

  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] items-center justify-center px-4 py-12">
      <section className="w-full max-w-xl rounded-3xl border border-border bg-card p-8 shadow-sm">
        <div className="mb-6 space-y-2">
          <h1 className="font-playfair text-3xl font-bold leading-tight">Nueva contraseña</h1>
          <p className="text-sm leading-6 text-muted-foreground">
            Elige una contraseña segura para restablecer el acceso a tu cuenta.
          </p>
        </div>

        <form className="flex flex-col gap-4" onSubmit={(event) => void handleSubmit(event)}>
          <label className="flex flex-col gap-1.5 text-sm font-medium text-foreground">
            Nueva contraseña
            <Input
              autoComplete="new-password"
              className="h-11 rounded-xl"
              minLength={8}
              onChange={(event) => setNewPassword(event.target.value)}
              placeholder="Mínimo 8 caracteres"
              required
              type="password"
              value={newPassword}
            />
          </label>

          {formError ? (
            <p className="text-sm text-destructive" role="alert">
              {formError}
            </p>
          ) : null}

          <Button disabled={loading || !newPassword.trim()} type="submit">
            {loading ? 'Guardando...' : 'Guardar contraseña'}
          </Button>
        </form>
      </section>
    </div>
  )
}

function StatusCard({
  tone,
  title,
  action,
}: {
  tone: 'success' | 'error'
  title: string
  action: ReactNode
}) {
  const toneClasses = {
    success: 'border-emerald-200 bg-emerald-50 text-emerald-950',
    error: 'border-rose-200 bg-rose-50 text-rose-950',
  }

  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] items-center justify-center px-4 py-12">
      <section className={`w-full max-w-xl rounded-3xl border p-8 shadow-sm ${toneClasses[tone]}`}>
        <div className="space-y-5">
          <h1 className="font-playfair text-3xl font-bold leading-tight">{title}</h1>
          <div>{action}</div>
        </div>
      </section>
    </div>
  )
}
