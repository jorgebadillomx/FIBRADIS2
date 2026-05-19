import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })
const opsAccessTokenStorageKey = 'fibradis.ops.accessToken'

export type BlocklistTerm = components['schemas']['BlocklistTermDto']

function getAuthHeaders(): HeadersInit {
  if (typeof window === 'undefined') return {}

  const token =
    window.sessionStorage.getItem(opsAccessTokenStorageKey) ??
    window.localStorage.getItem(opsAccessTokenStorageKey)

  return token ? { Authorization: `Bearer ${token}` } : {}
}

export async function fetchBlocklistTerms(): Promise<BlocklistTerm[]> {
  const { data, error } = await apiClient['/api/v1/news/blocklist-terms'].GET({
    headers: getAuthHeaders(),
  })
  if (error) throw new Error(`Error al obtener blocklist: ${JSON.stringify(error)}`)
  return data ?? []
}

export async function createBlocklistTerm(term: string): Promise<BlocklistTerm> {
  const { data, error } = await apiClient['/api/v1/news/blocklist-terms'].POST({
    body: { term },
    headers: getAuthHeaders(),
  })

  if (error) throw new Error(`Error al crear término: ${JSON.stringify(error)}`)
  if (!data) throw new Error('La API no devolvió el término creado.')
  return data
}

export async function deleteBlocklistTerm(id: string): Promise<void> {
  const { error } = await apiClient['/api/v1/news/blocklist-terms/{id}'].DELETE({
    headers: getAuthHeaders(),
    params: { path: { id } },
  })

  if (error) throw new Error(`Error al eliminar término: ${JSON.stringify(error)}`)
}
