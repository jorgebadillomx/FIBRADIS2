import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type AiPromptDto = components['schemas']['AiPromptDto']

export async function fetchAiPrompt(contentType: 'news' | 'kpi_extraction'): Promise<AiPromptDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/ai-prompts/{contentType}'].GET({
    headers: getOpsAuthHeaders(),
    params: { path: { contentType } },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al cargar prompt ${contentType}: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió el prompt solicitado.')
  return data
}

export async function updateAiPrompt(contentType: 'news' | 'kpi_extraction', promptTemplate: string): Promise<void> {
  assertOpsAccessToken()

  const { error } = await apiClient['/api/v1/ops/ai-prompts/{contentType}'].PUT({
    headers: getOpsAuthHeaders(),
    params: { path: { contentType } },
    body: { promptTemplate },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al guardar prompt ${contentType}: ${JSON.stringify(error)}`))
}
