import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import {
  assertOpsAccessToken,
  getOpsApiErrorMessage,
  getOpsAuthHeaders,
} from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type AiModeDto = components['schemas']['AiModeDto']
export type AiProviderConfigDto = components['schemas']['AiProviderConfigDto']
export type AiProviderOptionDto = components['schemas']['AiProviderOptionDto']

export async function fetchAiMode(): Promise<AiModeDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/ai-mode'].GET({
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al obtener AI_MODE: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió el modo AI.')
  return data
}

export async function setAiMode(mode: 'Off' | 'On'): Promise<void> {
  return setAiConfig({ mode })
}

export async function setAiConfig(payload: {
  mode?: 'Off' | 'On'
  newsModel?: 'gemini-2.5-flash' | 'gemini-2.5-pro'
}): Promise<void> {
  assertOpsAccessToken()

  const { error } = await apiClient['/api/v1/ops/ai-mode'].PUT({
    body: { mode: payload.mode ?? null, newsModel: payload.newsModel ?? null },
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

export async function fetchAiProvider(): Promise<AiProviderConfigDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/ai-provider'].GET({
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al obtener proveedor AI: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió la configuración del proveedor AI.')
  return data
}

export async function setAiProvider(provider: string, modelId: string): Promise<void> {
  assertOpsAccessToken()

  const { error } = await apiClient['/api/v1/ops/ai-provider'].PUT({
    body: { provider, modelId },
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al actualizar proveedor AI: ${JSON.stringify(error)}`))
}
