import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type PipelineDashboardDto = components['schemas']['PipelineDashboardDto']
export type RunPipelineTarget = 'market' | 'news' | 'distribution' | 'fundamentals' | 'banxico-sync' | 'banxico-inpc' | 'daily-snapshot'

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

  if (target === 'fundamentals') {
    const { error } = await apiClient['/api/v1/ops/market/fundamentals/run'].POST({ headers })
    if (error) throw new Error(getOpsApiErrorMessage(error, `Error al ejecutar pipeline Fundamentals: ${JSON.stringify(error)}`))
    return
  }

  if (target === 'banxico-sync') {
    const { error } = await apiClient['/api/v1/ops/banxico/sync-tiie/run'].POST({ headers })
    if (error) throw new Error(getOpsApiErrorMessage(error, `Error al ejecutar BanxicoSync: ${JSON.stringify(error)}`))
    return
  }

  if (target === 'banxico-inpc') {
    const { error } = await apiClient['/api/v1/ops/banxico/sync-inpc/run'].POST({ headers })
    if (error) throw new Error(getOpsApiErrorMessage(error, `Error al ejecutar BanxicoInpc: ${JSON.stringify(error)}`))
    return
  }

  if (target === 'daily-snapshot') {
    const { error } = await apiClient['/api/v1/ops/market/daily-snapshot-historical/run'].POST({ headers })
    if (error) throw new Error(getOpsApiErrorMessage(error, `Error al ejecutar DailySnapshot: ${JSON.stringify(error)}`))
    return
  }

  const { error } = await apiClient['/api/v1/ops/market/distribution/run'].POST({ headers })
  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al ejecutar pipeline Distribution: ${JSON.stringify(error)}`))
}
