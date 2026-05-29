import { assertOpsAccessToken, getOpsAuthHeaders } from '@/api/opsAuth'

export interface AiCallLogDto {
  id: string
  timestamp: string
  operation: string
  provider: string
  modelId: string
  promptLength: number
  durationMs: number
  success: boolean
  inputPreview: string | null
  responseRaw: string | null
  errorMessage: string | null
  context: string | null
  createdAt: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export type AiCallOperation = 'all' | 'KpiExtraction' | 'News' | 'Document'

export async function fetchAiCallLogs(
  operation: AiCallOperation,
  page: number,
  pageSize: number,
): Promise<PagedResult<AiCallLogDto>> {
  assertOpsAccessToken()

  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (operation !== 'all') params.set('operation', operation)

  const response = await fetch(`/api/v1/ops/ai-call-logs?${params}`, {
    headers: getOpsAuthHeaders(),
  })

  if (!response.ok) throw new Error(`Error ${response.status}`)
  return response.json() as Promise<PagedResult<AiCallLogDto>>
}
