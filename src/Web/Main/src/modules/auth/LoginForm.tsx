import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { Link } from 'react-router'
import { useAuth } from './AuthContext'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'
import { cn } from '@/shared/lib/utils'
import { DEFAULT_LOGIN_REDIRECT, resolveLoginRedirect } from './login-redirect'

type LoginFormProps = {
  redirectTo?: string
  onBeforeSubmit?: () => void
  titleAs?: 'h1' | 'h2' | 'h3'
  className?: string
}

export function LoginForm({
  redirectTo = DEFAULT_LOGIN_REDIRECT,
  onBeforeSubmit,
  titleAs = 'h2',
  className,
}: LoginFormProps) {
  const { login, status } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isPending, setIsPending] = useState(false)
  const resolvedRedirect = resolveLoginRedirect(redirectTo)
  const TitleTag = titleAs

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (isPending) return

    const normalizedEmail = email.trim()
    if (!normalizedEmail || !password) return

    onBeforeSubmit?.()
    setError(null)
    setIsPending(true)

    try {
      await login(normalizedEmail, password)
      void navigate(resolvedRedirect, { replace: true })
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Credenciales incorrectas. Verifica tu correo y contraseña.',
      )
    } finally {
      setIsPending(false)
    }
  }

  if (status === 'checking') return null

  return (
    <section
      className={cn(
        'w-full max-w-md rounded-3xl border border-border bg-card p-8 shadow-sm backdrop-blur',
        className,
      )}
    >
      <p className="text-xs font-semibold uppercase tracking-[0.28em] text-primary">
        Fibras Inmobiliarias
      </p>
      <TitleTag className="mt-3 font-playfair text-2xl font-semibold tracking-tight text-foreground">
        Iniciar sesión
      </TitleTag>
      <p className="mt-1 text-sm leading-6 text-muted-foreground">
        Accede a tu portafolio, oportunidades, reportes y herramientas privadas.
      </p>

      <form className="mt-6 flex flex-col gap-4" onSubmit={(event) => void handleSubmit(event)}>
        <label className="flex flex-col gap-1.5 text-sm font-medium text-foreground">
          Correo electrónico
          <Input
            id="login-email"
            name="email"
            autoComplete="email"
            className="h-11 rounded-xl"
            onChange={(event) => setEmail(event.target.value)}
            placeholder="tu@correo.mx"
            required
            type="email"
            value={email}
          />
        </label>

        <label className="flex flex-col gap-1.5 text-sm font-medium text-foreground">
          Contraseña
          <Input
            id="login-password"
            name="password"
            autoComplete="current-password"
            className="h-11 rounded-xl"
            onChange={(event) => setPassword(event.target.value)}
            placeholder="••••••••"
            required
            type="password"
            value={password}
          />
        </label>

        {error !== null ? (
          <p className="text-sm text-destructive" role="alert">
            {error}
          </p>
        ) : null}

        <Button
          className="mt-2 w-full"
          disabled={isPending || !email.trim() || !password}
          size="lg"
          type="submit"
        >
          {isPending ? 'Iniciando sesión...' : 'Iniciar sesión'}
        </Button>

        <div className="flex flex-col gap-2 text-sm sm:flex-row sm:items-center sm:justify-between">
          <Link
            to="/recuperar-contrasena"
            className="text-muted-foreground transition-colors hover:text-foreground"
          >
            ¿Olvidaste tu contraseña?
          </Link>
          <Link to="/registro" className="text-foreground transition-colors hover:underline">
            ¿No tienes cuenta? <span className="font-semibold">Crear cuenta</span>
          </Link>
        </div>
      </form>
    </section>
  )
}
