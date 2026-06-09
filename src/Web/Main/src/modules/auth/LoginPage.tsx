import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import { useAuth } from './AuthContext'

export function LoginPage() {
  const { login, status } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isPending, setIsPending] = useState(false)

  useEffect(() => {
    if (status === 'authenticated') {
      void navigate('/portafolio', { replace: true })
    }
  }, [status, navigate])

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (isPending) return

    const normalizedEmail = email.trim()
    if (!normalizedEmail || !password) return

    setError(null)
    setIsPending(true)

    try {
      await login(normalizedEmail, password)
      const rawRedirect = searchParams.get('redirect')
      const redirect =
        rawRedirect?.startsWith('/') && !rawRedirect.startsWith('//')
          ? rawRedirect
          : '/portafolio'
      void navigate(redirect, { replace: true })
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
    <div className="flex min-h-[calc(100vh-3.5rem)] items-center justify-center px-4 py-12">
      <div className="w-full max-w-md rounded-2xl border border-border bg-card p-8 shadow-sm">
        <p className="text-xs font-semibold uppercase tracking-widest text-primary">Fibras Inmobiliarias</p>
        <h1 className="mt-3 font-playfair text-2xl font-semibold tracking-tight">
          Iniciar sesión
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Accede a tu portafolio y funciones privadas.
        </p>

        <form className="mt-6 flex flex-col gap-4" onSubmit={(e) => void handleSubmit(e)}>
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Correo electrónico
            <input
              autoComplete="email"
              className="h-10 rounded-lg border border-border bg-background px-3 text-sm outline-none transition focus:border-primary focus:ring-1 focus:ring-primary"
              onChange={(e) => setEmail(e.target.value)}
              placeholder="tu@correo.mx"
              required
              type="email"
              value={email}
            />
          </label>

          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Contraseña
            <input
              autoComplete="current-password"
              className="h-10 rounded-lg border border-border bg-background px-3 text-sm outline-none transition focus:border-primary focus:ring-1 focus:ring-primary"
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
              type="password"
              value={password}
            />
          </label>

          {error !== null && (
            <p className="text-sm text-destructive" role="alert">
              {error}
            </p>
          )}

          <button
            className="mt-2 h-10 rounded-lg bg-primary px-5 text-sm font-medium text-primary-foreground transition hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-50"
            disabled={isPending || !email.trim() || !password}
            type="submit"
          >
            {isPending ? 'Iniciando sesión...' : 'Iniciar sesión'}
          </button>
        </form>
      </div>
    </div>
  )
}
