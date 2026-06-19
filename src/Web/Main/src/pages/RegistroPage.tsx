import { useState } from 'react'
import { Link } from 'react-router'
import { AuthApiError, registerUser } from '@/modules/auth/authApi'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'

const DESCRIPTION =
  'Crea tu cuenta en Fibras Inmobiliarias y comienza tu prueba gratuita de 14 días para acceder a portafolio, reportes, herramientas y más.'

const HOW_DID_YOU_HEAR_OPTIONS = [
  { value: 'Google', label: 'Google' },
  { value: 'RedesSociales', label: 'Redes sociales' },
  { value: 'Recomendacion', label: 'Recomendación' },
  { value: 'Otro', label: 'Otro' },
]

export function RegistroPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [apodo, setApodo] = useState('')
  const [howDidYouHear, setHowDidYouHear] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)

  usePageTitle('Registro | Fibras Inmobiliarias', DESCRIPTION, {
    canonicalPath: '/registro',
    robotsDirectives: 'noindex,nofollow',
  })

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    if (!email.trim()) {
      setError('El email es requerido.')
      return
    }
    if (password.length < 8) {
      setError('La contraseña debe tener al menos 8 caracteres.')
      return
    }

    setIsSubmitting(true)
    try {
      await registerUser(
        email.trim(),
        password,
        apodo.trim() || null,
        howDidYouHear || null,
      )
      setSuccess(true)
    } catch (err) {
      if (err instanceof AuthApiError) {
        if (err.code === 'disposable_email') {
          setError('Este dominio de email no está permitido.')
        } else if (err.code === 'duplicate_email') {
          setError('Este email ya está registrado.')
        } else if (err.code === 'invalid_user_data') {
          setError(err.message)
        } else {
          setError('No se pudo completar el registro. Intenta de nuevo.')
        }
      } else {
        setError('Ocurrió un error inesperado. Intenta de nuevo.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  if (success) {
    return (
      <div className="flex min-h-[calc(100vh-3.5rem)] items-center justify-center px-4 py-12">
        <section className="w-full max-w-xl rounded-3xl border border-emerald-200 bg-emerald-50 p-8 shadow-sm text-emerald-950">
          <div className="flex flex-col items-start gap-5">
            <h1 className="font-playfair text-3xl font-bold leading-tight">¡Registro exitoso!</h1>
            <p className="max-w-prose text-sm leading-6 text-current/80 md:text-base">
              Revisa tu email para confirmar tu cuenta.
            </p>
            <Button asChild variant="outline">
              <Link to="/login">Ir al inicio de sesión</Link>
            </Button>
          </div>
        </section>
      </div>
    )
  }

  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] items-center justify-center px-4 py-12">
      <section className="w-full max-w-md rounded-3xl border border-border bg-card p-8 shadow-sm">
        <div className="mb-6">
          <h1 className="font-playfair text-3xl font-bold leading-tight">Crear cuenta</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Comienza tu prueba gratuita de 14 días.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1.5">
            <label htmlFor="registro-email" className="text-sm font-medium leading-none">Email <span aria-hidden>*</span></label>
            <Input
              id="registro-email"
              name="email"
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="tu@email.com"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="registro-password" className="text-sm font-medium leading-none">Contraseña <span aria-hidden>*</span></label>
            <Input
              id="registro-password"
              name="password"
              type="password"
              autoComplete="new-password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Mínimo 8 caracteres"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="registro-apodo" className="text-sm font-medium leading-none">Nombre (opcional)</label>
            <Input
              id="registro-apodo"
              name="apodo"
              type="text"
              autoComplete="nickname"
              value={apodo}
              onChange={(e) => setApodo(e.target.value)}
              placeholder="¿Cómo te llamamos?"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="registro-how" className="text-sm font-medium leading-none">¿Cómo nos encontraste? (opcional)</label>
            <select
              id="registro-how"
              aria-label="¿Cómo nos encontraste?"
              name="howDidYouHear"
              value={howDidYouHear}
              onChange={(e) => setHowDidYouHear(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            >
              <option value="">Selecciona una opción</option>
              {HOW_DID_YOU_HEAR_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>

          {error ? (
            <p role="alert" className="text-sm text-destructive">
              {error}
            </p>
          ) : null}

          <Button type="submit" disabled={isSubmitting} className="mt-2">
            {isSubmitting ? 'Creando cuenta…' : 'Crear cuenta'}
          </Button>
        </form>

        <p className="mt-4 text-center text-sm text-muted-foreground">
          ¿Ya tienes cuenta?{' '}
          <Link to="/login" className="underline underline-offset-4 hover:text-foreground">
            Inicia sesión
          </Link>
        </p>
      </section>
    </div>
  )
}
