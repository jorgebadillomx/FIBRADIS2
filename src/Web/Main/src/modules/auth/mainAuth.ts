const AUTH_TOKEN_KEY = 'fibradis.main.accessToken'
export const MAIN_AUTH_REQUIRED_EVENT = 'fibradis:main-auth-required'

export function getStoredMainAccessToken(): string | null {
  if (typeof window === 'undefined') return null
  return window.sessionStorage.getItem(AUTH_TOKEN_KEY)
}

export function storeMainAccessToken(token: string): void {
  if (typeof window === 'undefined') return
  window.sessionStorage.setItem(AUTH_TOKEN_KEY, token)
}

export function clearMainAccessToken(): void {
  if (typeof window === 'undefined') return
  window.sessionStorage.removeItem(AUTH_TOKEN_KEY)
}

export function getMainAuthHeaders(): { Authorization: string } | Record<string, never> {
  const token = getStoredMainAccessToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

export function getMainTokenClaims(): { hasAcceptedTerms: boolean } | null {
  const token = getStoredMainAccessToken()
  if (!token) return null
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')))
    return {
      hasAcceptedTerms:
        payload.hasAcceptedTerms === 'true' || payload.hasAcceptedTerms === true,
    }
  } catch {
    return null
  }
}

export function notifyMainAuthRequired(): void {
  if (typeof window === 'undefined') return
  window.dispatchEvent(new Event(MAIN_AUTH_REQUIRED_EVENT))
}
