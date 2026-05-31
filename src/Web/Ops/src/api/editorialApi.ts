import createClient, { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const publicClient = createClient<paths>({ baseUrl: '' })
const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type EditorialPageDto = components['schemas']['EditorialPageDto']

export async function fetchEditorialPages(): Promise<EditorialPageDto[]> {
  const { data, error } = await publicClient.GET('/api/v1/pages')
  if (error) throw new Error(`Error al cargar páginas editoriales: ${JSON.stringify(error)}`)
  return data ?? []
}

export async function updateEditorialPage(slug: string, content: string): Promise<void> {
  assertOpsAccessToken()

  const { error } = await apiClient['/api/v1/ops/pages/{slug}'].PUT({
    headers: getOpsAuthHeaders(),
    params: { path: { slug } },
    body: { content },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al guardar página editorial: ${JSON.stringify(error)}`))
}
