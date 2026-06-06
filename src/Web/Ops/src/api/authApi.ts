import { createPathBasedClient } from 'openapi-fetch'
import type { paths } from '@fibradis/shared-api-client'
import {
  clearOpsAccessToken,
  decodeTokenRole,
  getOpsApiErrorMessage,
  storeOpsAccessToken,
} from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })


export async function loginOps(email: string, password: string): Promise<void> {
  const { data, error } = await apiClient['/api/v1/auth/login'].POST({
    body: { email, password },
  })

  if (error) {
    throw new Error(getOpsApiErrorMessage(error, 'No se pudo iniciar sesión en Ops.', { signOutOn401: false }))
  }

  const accessToken = data?.accessToken?.trim()
  if (!accessToken) {
    throw new Error('La API no devolvió access token para AdminOps.')
  }

  if (decodeTokenRole(accessToken) !== 'AdminOps') {
    throw new Error('Solo cuentas AdminOps pueden acceder al centro operativo.')
  }

  storeOpsAccessToken(accessToken)
}

// Deduplicates concurrent calls (e.g. React Strict Mode double-mount) so only one
// HTTP request reaches the backend per refresh cycle, preventing token rotation races.
let _refreshInFlight: Promise<boolean> | null = null

export function refreshOpsSession(): Promise<boolean> {
  if (_refreshInFlight) return _refreshInFlight

  _refreshInFlight = (async () => {
    const { data, error } = await apiClient['/api/v1/auth/refresh'].POST({})

    if (error) {
      if (
        error &&
        typeof error === 'object' &&
        'status' in error &&
        typeof error.status === 'number' &&
        error.status === 401
      ) {
        // Do not call clearOpsAccessToken() here — the access token in sessionStorage
        // may still be valid. Callers decide whether to clear based on their context.
        return false
      }

      throw new Error(getOpsApiErrorMessage(error, 'No se pudo restaurar la sesión de Ops.', { signOutOn401: false }))
    }

    const accessToken = data?.accessToken?.trim()
    if (!accessToken) {
      throw new Error('La API no devolvió access token al refrescar la sesión.')
    }

    if (decodeTokenRole(accessToken) !== 'AdminOps') {
      clearOpsAccessToken()
      return false
    }

    storeOpsAccessToken(accessToken)
    return true
  })().finally(() => {
    _refreshInFlight = null
  })

  return _refreshInFlight
}
