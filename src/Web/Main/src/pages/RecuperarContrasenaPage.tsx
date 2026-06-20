import { useState, type FormEvent } from 'react'
import { forgotPassword } from '@/modules/auth/authApi'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'

const TITLE = 'Recuperar contraseña | Fibras Inmobiliarias'
const DESCRIPTION =
  'Recibe un enlace seguro para restablecer tu contraseña y volver a entrar a tu cuenta en Fibras Inmobiliarias.'

export function RecuperarContrasenaPage() {
  const [email, setEmail] = useState('')
  const [state, setState] = useState<'idle' | 'loading' | 'sent'>('idle')

  usePageTitle(TITLE, DESCRIPTION, {
    canonicalPath: '/recuperar-contrasena',
    robotsDirectives: 'noindex,nofollow',
  })

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (state === 'loading') return

    const normalizedEmail = email.trim()
    if (!normalizedEmail) return

    setState('loading')
    await forgotPassword(normalizedEmail)
    setState('sent')
  }

  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] items-center justify-center px-4 py-12">
      <section className="w-full max-w-xl rounded-3xl border border-border bg-card p-8 shadow-sm">
        <div className="mb-6 space-y-2">
          <h1 className="font-playfair text-3xl font-bold leading-tight">Recuperar contraseña</h1>
          <p className="text-sm leading-6 text-muted-foreground">
            Ingresa tu email para recibir un enlace seguro de restablecimiento.
          </p>
        </div>

        {state === 'sent' ? (
          <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-5 text-emerald-950">
            <p className="text-sm leading-6 font-medium">
              Si ese email está registrado, recibirás un enlace para restablecer tu contraseña.
              Revisa tu bandeja de entrada.
            </p>
          </div>
        ) : (
          <form className="flex flex-col gap-4" onSubmit={(event) => void handleSubmit(event)}>
            <label className="flex flex-col gap-1.5 text-sm font-medium text-foreground">
              Correo electrónico
              <Input
                autoComplete="email"
                className="h-11 rounded-xl"
                onChange={(event) => setEmail(event.target.value)}
                placeholder="tu@correo.mx"
                required
                type="email"
                value={email}
              />
            </label>

            <Button disabled={state === 'loading' || !email.trim()} type="submit">
              {state === 'loading' ? 'Enviando...' : 'Enviar enlace'}
            </Button>
          </form>
        )}
      </section>
    </div>
  )
}
