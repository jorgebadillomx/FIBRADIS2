const opsAccessTokenStorageKey = 'fibradis.ops.accessToken'
export const OPS_AUTH_REQUIRED_EVENT = 'fibradis:ops-auth-required'

type ApiErrorShape = {
  detail?: string
  message?: string
  status?: number
  title?: string
  errors?: Record<string, string[]>
}

type OpsApiErrorOptions = {
  signOutOn401?: boolean
}

export function getStoredOpsAccessToken(): string | null {
  if (typeof window === 'undefined') return null

  return (
    window.sessionStorage.getItem(opsAccessTokenStorageKey) ??
    window.localStorage.getItem(opsAccessTokenStorageKey)
  )
}

export function storeOpsAccessToken(token: string): void {
  if (typeof window === 'undefined') return
  window.sessionStorage.setItem(opsAccessTokenStorageKey, token)
}

export function clearOpsAccessToken(): void {
  if (typeof window === 'undefined') return

  window.sessionStorage.removeItem(opsAccessTokenStorageKey)
  window.localStorage.removeItem(opsAccessTokenStorageKey)
}

export function getOpsAuthHeaders(): HeadersInit {
  const token = getStoredOpsAccessToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

export function assertOpsAccessToken(): void {
  if (getStoredOpsAccessToken()) return
  notifyOpsAuthRequired()
  throw new Error('Inicia sesión como AdminOps para usar el sitio Ops.')
}

export function notifyOpsAuthRequired(): void {
  if (typeof window === 'undefined') return
  window.dispatchEvent(new Event(OPS_AUTH_REQUIRED_EVENT))
}

export function getOpsApiErrorMessage(
  error: unknown,
  fallback: string,
  options: OpsApiErrorOptions = {},
): string {
  const { signOutOn401 = true } = options

  if (error && typeof error === 'object') {
    const typedError = error as ApiErrorShape

    if (typedError.status === 401) {
      if (signOutOn401) {
        clearOpsAccessToken()
        notifyOpsAuthRequired()
        return 'La sesión de AdminOps no es válida o expiró. Inicia sesión de nuevo.'
      }
    }

    if (typeof typedError.detail === 'string' && typedError.detail.length > 0) {
      return typedError.detail
    }

    if (typeof typedError.message === 'string' && typedError.message.length > 0) {
      return typedError.message
    }

    if (typedError.errors && typeof typedError.errors === 'object') {
      const firstField = Object.values(typedError.errors).find((msgs) => msgs.length > 0)
      if (firstField) return firstField[0]
    }

    if (typeof typedError.title === 'string' && typedError.title.length > 0) {
      return typedError.title
    }
  }

  return fallback
}
