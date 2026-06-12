import { createContext, useContext, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { loginMain, logoutMain, refreshMainSession } from './authApi'
import {
  MAIN_AUTH_REQUIRED_EVENT,
  clearMainAccessToken,
  getMainTokenClaims,
  getStoredMainAccessToken,
  hasSessionCookie,
} from './mainAuth'
import { acceptTermsApi } from './authApi'

type AuthStatus = 'checking' | 'anonymous' | 'authenticated'

interface AuthContextValue {
  status: AuthStatus
  isAuthenticated: boolean
  hasAcceptedTerms: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  acceptTerms: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

const PROACTIVE_REFRESH_MS = 4 * 60 * 60 * 1000

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [status, setStatus] = useState<AuthStatus>('checking')
  const [hasAcceptedTerms, setHasAcceptedTerms] = useState(false)

  useEffect(() => {
    let active = true

    async function bootstrapSession() {
      if (!hasSessionCookie()) {
        setStatus('anonymous')
        return
      }
      try {
        const restored = await refreshMainSession()
        if (!active) return
        const isAuth = restored || Boolean(getStoredMainAccessToken())
        setStatus(isAuth ? 'authenticated' : 'anonymous')
        if (isAuth) {
          setHasAcceptedTerms(getMainTokenClaims()?.hasAcceptedTerms ?? false)
          void queryClient.invalidateQueries()
        }
      } catch {
        if (!active) return
        const isAuth = Boolean(getStoredMainAccessToken())
        setStatus(isAuth ? 'authenticated' : 'anonymous')
        if (isAuth) setHasAcceptedTerms(getMainTokenClaims()?.hasAcceptedTerms ?? false)
      }
    }

    void bootstrapSession()
    return () => { active = false }
  }, [queryClient])

  useEffect(() => {
    function handleAuthRequired() {
      clearMainAccessToken()
      queryClient.clear()
      setStatus('anonymous')
      setHasAcceptedTerms(false)
    }

    window.addEventListener(MAIN_AUTH_REQUIRED_EVENT, handleAuthRequired)
    return () => window.removeEventListener(MAIN_AUTH_REQUIRED_EVENT, handleAuthRequired)
  }, [queryClient])

  useEffect(() => {
    if (status !== 'authenticated') return

    let active = true
    const id = setInterval(async () => {
      try {
        const ok = await refreshMainSession()
        if (!active) return
        if (!ok) {
          clearMainAccessToken()
          queryClient.clear()
          setStatus('anonymous')
          setHasAcceptedTerms(false)
        } else {
          setHasAcceptedTerms(getMainTokenClaims()?.hasAcceptedTerms ?? false)
        }
      } catch {
        // Network error — keep session
      }
    }, PROACTIVE_REFRESH_MS)

    return () => {
      active = false
      clearInterval(id)
    }
  }, [status, queryClient])

  async function login(email: string, password: string) {
    await loginMain(email, password)
    setStatus('authenticated')
    setHasAcceptedTerms(getMainTokenClaims()?.hasAcceptedTerms ?? false)
    await queryClient.invalidateQueries()
  }

  function logout() {
    logoutMain()
    queryClient.clear()
    setStatus('anonymous')
    setHasAcceptedTerms(false)
  }

  async function acceptTerms() {
    await acceptTermsApi()
    try {
      await refreshMainSession()
    } catch {
      // Network error — the acceptance was persisted; keep going
    }
    setHasAcceptedTerms(true)
  }

  return (
    <AuthContext.Provider
      value={{
        status,
        isAuthenticated: status === 'authenticated',
        hasAcceptedTerms,
        login,
        logout,
        acceptTerms,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
