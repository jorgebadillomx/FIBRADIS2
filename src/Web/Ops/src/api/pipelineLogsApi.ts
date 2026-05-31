import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type PipelineLogPipeline = 'all' | 'Market' | 'News' | 'Distribution' | 'Fundamentals' | 'BodyTextRetry' | 'ManualAiSummary' | 'KpiExtraction'
export type PipelineErrorLog = components['schemas']['PipelineErrorLogDto']
export type PipelineErrorLogPage = components['schemas']['PagedResultOfPipelineErrorLogDto']

export async function fetchPipelineLogs(
  pipeline: PipelineLogPipeline,
  page: number,
  pageSize: number,
): Promise<PipelineErrorLogPage> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/pipeline-logs'].GET({
    headers: getOpsAuthHeaders(),
    params: { query: { pipeline, page, pageSize } },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al obtener logs: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió logs del pipeline.')
  return data
}
