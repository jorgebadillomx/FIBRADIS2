import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })
const opsAccessTokenStorageKey = 'fibradis.ops.accessToken'

export type AiModeDto = components['schemas']['AiModeDto']
type ApiErrorShape = {
  detail?: string
  message?: string
}

function getAuthHeaders(): HeadersInit {
  if (typeof window === 'undefined') return {}

  const token =
    window.sessionStorage.getItem(opsAccessTokenStorageKey) ??
    window.localStorage.getItem(opsAccessTokenStorageKey)

  return token ? { Authorization: `Bearer ${token}` } : {}
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const typedError = error as ApiErrorShape

    if (typeof typedError.detail === 'string' && typedError.detail.length > 0) {
      return typedError.detail
    }

    if (typeof typedError.message === 'string' && typedError.message.length > 0) {
      return typedError.message
    }
  }

  return fallback
}

export async function fetchAiMode(): Promise<AiModeDto> {
  const { data, error } = await apiClient['/api/v1/ops/ai-mode'].GET({
    headers: getAuthHeaders(),
  })

  if (error) throw new Error(getErrorMessage(error, `Error al obtener AI_MODE: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió el modo AI.')
  return data
}

export async function setAiMode(mode: 'Off' | 'Manual'): Promise<void> {
  const { error } = await apiClient['/api/v1/ops/ai-mode'].PUT({
    body: { mode },
    headers: getAuthHeaders(),
  })

  if (error) throw new Error(getErrorMessage(error, `Error al actualizar AI_MODE: ${JSON.stringify(error)}`))
}

export async function triggerAiSummary(articleId: string): Promise<void> {
  const { error } = await apiClient['/api/v1/ops/news/{articleId}/ai-summary'].POST({
    params: { path: { articleId } },
    headers: getAuthHeaders(),
  })

  if (error) throw new Error(getErrorMessage(error, `Error al generar resumen: ${JSON.stringify(error)}`))
}
