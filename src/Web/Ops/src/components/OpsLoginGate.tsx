import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { loginOps, refreshOpsSession } from '@/api/authApi'
import {
  OPS_AUTH_REQUIRED_EVENT,
  clearOpsAccessToken,
  getStoredOpsAccessToken,
} from '@/api/opsAuth'

const PROACTIVE_REFRESH_MS = 4 * 60 * 60 * 1000 // 4 hours — well within 8-hour token lifetime

type Props = {
  children: React.ReactNode
}

type AuthStatus = 'checking' | 'authenticated' | 'anonymous'

export function OpsLoginGate({ children }: Props) {
  const queryClient = useQueryClient()
  const [authStatus, setAuthStatus] = useState<AuthStatus>('checking')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  useEffect(() => {
    let active = true

    async function bootstrapSession() {
      try {
        const restored = await refreshOpsSession()

        if (!active) return
        const isAuthenticated = restored || Boolean(getStoredOpsAccessToken())
        setAuthStatus(isAuthenticated ? 'authenticated' : 'anonymous')
        if (isAuthenticated) void queryClient.invalidateQueries()
      } catch {
        if (!active) return
        const isAuthenticated = Boolean(getStoredOpsAccessToken())
        setAuthStatus(isAuthenticated ? 'authenticated' : 'anonymous')
        if (isAuthenticated) void queryClient.invalidateQueries()
      }
    }

    void bootstrapSession()

    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    function handleAuthRequired() {
      clearOpsAccessToken()
      queryClient.clear()
      setPassword('')
      setAuthStatus('anonymous')
    }

    window.addEventListener(OPS_AUTH_REQUIRED_EVENT, handleAuthRequired)
    return () => window.removeEventListener(OPS_AUTH_REQUIRED_EVENT, handleAuthRequired)
  }, [queryClient])

  useEffect(() => {
    if (authStatus !== 'authenticated') return

    let active = true

    const id = setInterval(async () => {
      try {
        const ok = await refreshOpsSession()
        if (!active) return
        if (!ok) {
          clearOpsAccessToken()
          queryClient.clear()
          setPassword('')
          setAuthStatus('anonymous')
        }
      } catch {
        // Network error — keep session; a real 401 on the next API call will handle logout
      }
    }, PROACTIVE_REFRESH_MS)

    return () => {
      active = false
      clearInterval(id)
    }
  }, [authStatus, queryClient])

  const loginMutation = useMutation({
    mutationFn: async () => {
      await loginOps(email.trim(), password)
    },
    onSuccess: async () => {
      setPassword('')
      setAuthStatus('authenticated')
      await queryClient.invalidateQueries()
    },
  })

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (loginMutation.isPending) return

    const normalizedEmail = email.trim()
    if (normalizedEmail.length === 0 || password.length === 0) return
    loginMutation.mutate()
  }

  function handleLogout() {
    clearOpsAccessToken()
    queryClient.clear()
    setPassword('')
    setAuthStatus('anonymous')
  }

  if (authStatus === 'checking') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[radial-gradient(circle_at_top,_rgba(15,118,110,0.14),_transparent_42%),linear-gradient(180deg,_#f8fafc_0%,_#e7f3ef_100%)] px-6 text-foreground">
        <div className="w-full max-w-md rounded-3xl border border-border/80 bg-white/90 p-8 shadow-xl shadow-teal-950/8">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-teal-700">AdminOps</p>
          <h1 className="mt-3 text-2xl font-semibold tracking-tight">Restaurando sesión</h1>
          <p className="mt-3 text-sm text-muted-foreground">
            Verificando si ya existe una sesión válida para el centro operativo.
          </p>
        </div>
      </div>
    )
  }

  if (authStatus === 'anonymous') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[radial-gradient(circle_at_top,_rgba(15,118,110,0.14),_transparent_42%),linear-gradient(180deg,_#f8fafc_0%,_#e7f3ef_100%)] px-6 py-12 text-foreground">
        <div className="w-full max-w-md rounded-3xl border border-border/80 bg-white/95 p-8 shadow-xl shadow-teal-950/8">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-teal-700">AdminOps</p>
          <h1 className="mt-3 text-2xl font-semibold tracking-tight">Acceso a FIBRADIS Ops</h1>
          <p className="mt-3 text-sm text-muted-foreground">
            Inicia sesión como AdminOps para usar el sitio Ops.
          </p>

          <form className="mt-6 flex flex-col gap-4" onSubmit={handleSubmit}>
            <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
              Correo
              <input
                className="h-11 rounded-xl border border-border bg-white px-4 text-sm font-normal outline-none transition focus:border-teal-600"
                onChange={(event) => setEmail(event.target.value)}
                placeholder="adminops@fibradis.mx"
                type="email"
                value={email}
              />
            </label>

            <label className="flex flex-col gap-2 text-sm font-medium text-slate-700">
              Contraseña
              <input
                className="h-11 rounded-xl border border-border bg-white px-4 text-sm font-normal outline-none transition focus:border-teal-600"
                onChange={(event) => setPassword(event.target.value)}
                placeholder="••••••••"
                type="password"
                value={password}
              />
            </label>

            {loginMutation.isError ? (
              <p className="text-sm text-destructive">{loginMutation.error.message}</p>
            ) : null}

            <button
              className="mt-2 h-11 rounded-xl bg-teal-700 px-5 text-sm font-medium text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-teal-400"
              disabled={loginMutation.isPending || email.trim().length === 0 || password.length === 0}
              type="submit"
            >
              {loginMutation.isPending ? 'Entrando...' : 'Entrar a Ops'}
            </button>
          </form>
        </div>
      </div>
    )
  }

  return (
    <>
      <div className="border-b border-border/80 bg-white/80 px-6 py-3 backdrop-blur">
        <div className="mx-auto flex w-full max-w-5xl items-center justify-end">
          <button
            className="rounded-lg border border-border bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:border-teal-600 hover:text-teal-800"
            onClick={handleLogout}
            type="button"
          >
            Cerrar sesión
          </button>
        </div>
      </div>
      {children}
    </>
  )
}
