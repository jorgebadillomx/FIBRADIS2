import createClient, { wrapAsPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const _baseClient = createClient<paths>({ baseUrl: '' })

// openapi-fetch llama response.json() en todos los 2xx; forzar Content-Length:0 en 202 vacíos evita "Unexpected end of JSON input"
_baseClient.use({
  async onResponse({ response }) {
    if (response.ok && !response.headers.get('content-type')) {
      const headers = new Headers(response.headers)
      headers.set('content-length', '0')
      return new Response(null, { status: response.status, headers })
    }
  },
})

const apiClient = wrapAsPathBasedClient(_baseClient)

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
