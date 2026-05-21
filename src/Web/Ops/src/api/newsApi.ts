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
