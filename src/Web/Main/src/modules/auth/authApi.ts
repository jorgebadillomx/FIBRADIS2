import { createPathBasedClient } from 'openapi-fetch'
import type { paths } from '@fibradis/shared-api-client'
import {
  clearMainAccessToken,
  notifyMainAuthRequired,
  storeMainAccessToken,
} from './mainAuth'

const authClient = createPathBasedClient<paths>({ baseUrl: '' })

let _refreshInFlight: Promise<boolean> | null = null

export async function loginMain(email: string, password: string): Promise<void> {
  const { data, error } = await authClient['/api/v1/auth/login'].POST({
    body: { email, password },
  })

  if (error) {
    const detail =
      error && typeof error === 'object' && 'detail' in error && typeof error.detail === 'string'
        ? error.detail
        : null
    throw new Error(detail ?? 'Credenciales incorrectas. Verifica tu correo y contraseña.')
  }

  const token = data?.accessToken?.trim()
  if (!token) throw new Error('La API no devolvió access token.')
  storeMainAccessToken(token)
}

export function refreshMainSession(): Promise<boolean> {
  if (_refreshInFlight) return _refreshInFlight

  _refreshInFlight = (async () => {
    const { data, error } = await authClient['/api/v1/auth/refresh'].POST({})

    if (error) {
      const status =
        error && typeof error === 'object' && 'status' in error && typeof error.status === 'number'
          ? error.status
          : null
      if (status === 401) return false
      throw new Error('No se pudo restaurar la sesión.')
    }

    const token = data?.accessToken?.trim()
    if (!token) return false
    storeMainAccessToken(token)
    return true
  })().finally(() => {
    _refreshInFlight = null
  })

  return _refreshInFlight
}

export function logoutMain(): void {
  clearMainAccessToken()
  notifyMainAuthRequired()
}
