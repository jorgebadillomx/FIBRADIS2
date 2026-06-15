import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type SeoMetadataDto = components['schemas']['SeoMetadataDto']
export type UpdateSeoMetadataRequest = components['schemas']['UpdateSeoMetadataRequest']
export type SeoBackfillResultDto = components['schemas']['SeoBackfillResultDto']

export type SeoMetadataQuery = {
  pageType?: string
  search?: string
}

export async function fetchSeoMetadata(query: SeoMetadataQuery = {}): Promise<SeoMetadataDto[]> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/seo'].GET({
    headers: getOpsAuthHeaders(),
    params: {
      query: {
        pageType: query.pageType,
        search: query.search,
      },
    },
  })

  if (error) {
    throw new Error(getOpsApiErrorMessage(error, `Error al cargar SEO: ${JSON.stringify(error)}`))
  }

  return data ?? []
}

export async function fetchSeoMetadataRow(id: string): Promise<SeoMetadataDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/seo/{id}'].GET({
    headers: getOpsAuthHeaders(),
    params: {
      path: { id },
    },
  })

  if (error || !data) {
    throw new Error(getOpsApiErrorMessage(error, `Error al cargar la fila SEO: ${JSON.stringify(error)}`))
  }

  return data
}

export async function updateSeoMetadata(id: string, request: UpdateSeoMetadataRequest): Promise<SeoMetadataDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/seo/{id}'].PUT({
    headers: getOpsAuthHeaders(),
    params: {
      path: { id },
    },
    body: request,
  })

  if (error || !data) {
    throw new Error(getOpsApiErrorMessage(error, `Error al actualizar SEO: ${JSON.stringify(error)}`))
  }

  return data
}

export async function backfillSeoMetadata(): Promise<SeoBackfillResultDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/seo/backfill'].POST({
    headers: getOpsAuthHeaders(),
  })

  if (error || !data) {
    throw new Error(getOpsApiErrorMessage(error, `Error al ejecutar el backfill SEO: ${JSON.stringify(error)}`))
  }

  return data
}
