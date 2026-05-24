import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type PipelineDashboardDto = components['schemas']['PipelineDashboardDto']
export type RunPipelineTarget = 'market' | 'news' | 'distribution'

export async function fetchPipelineDashboard(): Promise<PipelineDashboardDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/dashboard'].GET({
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al obtener dashboard operativo: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió el dashboard operativo.')
  return data
}

export async function runPipeline(target: RunPipelineTarget): Promise<void> {
  assertOpsAccessToken()

  const headers = getOpsAuthHeaders()

  if (target === 'market') {
    const { error } = await apiClient['/api/v1/ops/market/run'].POST({ headers })
    if (error) throw new Error(getOpsApiErrorMessage(error, `Error al ejecutar pipeline Market: ${JSON.stringify(error)}`))
    return
  }

  if (target === 'news') {
    const { error } = await apiClient['/api/v1/ops/news-pipeline/run'].POST({ headers })
    if (error) throw new Error(getOpsApiErrorMessage(error, `Error al ejecutar pipeline News: ${JSON.stringify(error)}`))
    return
  }

  const { error } = await apiClient['/api/v1/ops/market/distribution/run'].POST({ headers })
  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al ejecutar pipeline Distribution: ${JSON.stringify(error)}`))
}
