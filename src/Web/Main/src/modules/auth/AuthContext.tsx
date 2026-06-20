import { createContext, useContext, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { fetchProfile, loginMain, logoutMain, refreshMainSession } from './authApi'
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
  isActive: boolean
  trialEndsAt: string | null
  subscriptionType: string | null
  subscriptionEndsAt: string | null
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
  const [isActive, setIsActive] = useState(true)
  const [trialEndsAt, setTrialEndsAt] = useState<string | null>(null)
  const [subscriptionType, setSubscriptionType] = useState<string | null>(null)
  const [subscriptionEndsAt, setSubscriptionEndsAt] = useState<string | null>(null)

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
          try {
            const profile = await fetchProfile()
            if (!active) return
            setIsActive(profile.isActive)
            setTrialEndsAt(profile.trialEndsAt ?? null)
            setSubscriptionType(profile.subscriptionType ?? null)
            setSubscriptionEndsAt(profile.subscriptionEndsAt ?? null)
          } catch {
            if (!active) return
            // Si falla el perfil, asumir activo (degraded mode)
            setIsActive(true)
            setTrialEndsAt(null)
            setSubscriptionType(null)
            setSubscriptionEndsAt(null)
          }
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
      setIsActive(true)
      setTrialEndsAt(null)
      setSubscriptionType(null)
      setSubscriptionEndsAt(null)
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
          setIsActive(true)
          setTrialEndsAt(null)
          setSubscriptionType(null)
          setSubscriptionEndsAt(null)
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
    try {
      const profile = await fetchProfile()
      setIsActive(profile.isActive)
      setTrialEndsAt(profile.trialEndsAt ?? null)
      setSubscriptionType(profile.subscriptionType ?? null)
      setSubscriptionEndsAt(profile.subscriptionEndsAt ?? null)
    } catch {
      setIsActive(true)
      setTrialEndsAt(null)
      setSubscriptionType(null)
      setSubscriptionEndsAt(null)
    }
  }

  function logout() {
    logoutMain()
    queryClient.clear()
    setStatus('anonymous')
    setHasAcceptedTerms(false)
    setIsActive(true)
    setTrialEndsAt(null)
    setSubscriptionType(null)
    setSubscriptionEndsAt(null)
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
        isActive,
        trialEndsAt,
        subscriptionType,
        subscriptionEndsAt,
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
