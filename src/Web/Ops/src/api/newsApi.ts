import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import {
  assertOpsAccessToken,
  getOpsApiErrorMessage,
  getOpsAuthHeaders,
} from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type BlocklistTerm = components['schemas']['BlocklistTermDto']

export async function fetchBlocklistTerms(): Promise<BlocklistTerm[]> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/news/blocklist-terms'].GET({
    headers: getOpsAuthHeaders(),
  })
  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al obtener blocklist: ${JSON.stringify(error)}`))
  return data ?? []
}

export async function createBlocklistTerm(term: string): Promise<BlocklistTerm> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/news/blocklist-terms'].POST({
    body: { term },
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al crear término: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió el término creado.')
  return data
}

export async function deleteBlocklistTerm(id: string): Promise<void> {
  assertOpsAccessToken()

  const { error } = await apiClient['/api/v1/news/blocklist-terms/{id}'].DELETE({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al eliminar término: ${JSON.stringify(error)}`))
}

export type OpsNewsArticle = components['schemas']['OpsNewsArticleDto']
export type OpsNewsPage = components['schemas']['PagedResultOfOpsNewsArticleDto']

export async function fetchOpsNewsList(
  page = 1,
  pageSize = 20,
  search?: string,
  hasAiSummary?: boolean,
  isManuallyEdited?: boolean,
): Promise<OpsNewsPage> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/news'].GET({
    headers: getOpsAuthHeaders(),
    params: { query: { page, pageSize, search, hasAiSummary, isManuallyEdited } },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al obtener noticias: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}

export type OpsNewsBody = components['schemas']['OpsNewsBodyDto']

export async function fetchOpsNewsBody(id: string): Promise<OpsNewsBody> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/news/{articleId}'].GET({
    headers: getOpsAuthHeaders(),
    params: { path: { articleId: id } },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al obtener body text: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}

export async function updateNewsBodyText(id: string, bodyText: string | null): Promise<void> {
  assertOpsAccessToken()

  const { error } = await apiClient['/api/v1/ops/news/{articleId}/body-text'].PUT({
    headers: getOpsAuthHeaders(),
    params: { path: { articleId: id } },
    body: { bodyText: bodyText ?? null },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al guardar body text: ${JSON.stringify(error)}`))
}
