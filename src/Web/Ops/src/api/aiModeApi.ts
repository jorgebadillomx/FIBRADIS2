import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import {
  assertOpsAccessToken,
  getOpsApiErrorMessage,
  getOpsAuthHeaders,
} from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type AiModeDto = components['schemas']['AiModeDto']

export async function fetchAiMode(): Promise<AiModeDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/ai-mode'].GET({
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al obtener AI_MODE: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió el modo AI.')
  return data
}

export async function setAiMode(mode: 'Off' | 'Manual'): Promise<void> {
  assertOpsAccessToken()

  const { error } = await apiClient['/api/v1/ops/ai-mode'].PUT({
    body: { mode },
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al actualizar AI_MODE: ${JSON.stringify(error)}`))
}

export async function triggerAiSummary(articleId: string): Promise<void> {
  assertOpsAccessToken()

  const { error } = await apiClient['/api/v1/ops/news/{articleId}/ai-summary'].POST({
    params: { path: { articleId } },
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al generar resumen: ${JSON.stringify(error)}`))
}
