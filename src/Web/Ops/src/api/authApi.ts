import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import {
  clearOpsAccessToken,
  getOpsApiErrorMessage,
  storeOpsAccessToken,
} from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

type LoginResponse = components['schemas']['LoginResponse']
type RefreshResponse = components['schemas']['RefreshResponse']

async function persistAccessToken(
  response: LoginResponse | RefreshResponse | undefined,
  fallback: string,
): Promise<void> {
  const accessToken = response?.accessToken?.trim()
  if (!accessToken) {
    throw new Error(fallback)
  }

  storeOpsAccessToken(accessToken)
}

export async function loginOps(email: string, password: string): Promise<void> {
  const { data, error } = await apiClient['/api/v1/auth/login'].POST({
    body: { email, password },
  })

  if (error) {
    throw new Error(getOpsApiErrorMessage(error, 'No se pudo iniciar sesión en Ops.', { signOutOn401: false }))
  }
  await persistAccessToken(data, 'La API no devolvió access token para AdminOps.')
}

export async function refreshOpsSession(): Promise<boolean> {
  const { data, error } = await apiClient['/api/v1/auth/refresh'].POST({})

  if (error) {
    if (
      error &&
      typeof error === 'object' &&
      'status' in error &&
      typeof error.status === 'number' &&
      error.status === 401
    ) {
      clearOpsAccessToken()
      return false
    }

    throw new Error(getOpsApiErrorMessage(error, 'No se pudo restaurar la sesión de Ops.'))
  }

  await persistAccessToken(data, 'La API no devolvió access token al refrescar la sesión.')
  return true
}
