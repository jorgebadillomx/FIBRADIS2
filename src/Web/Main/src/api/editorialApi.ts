import createClient from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'

const apiClient = createClient<paths>({ baseUrl: '' })

export type EditorialPageDto = components['schemas']['EditorialPageDto']

export async function fetchEditorialPages(): Promise<EditorialPageDto[]> {
  const { data, error } = await apiClient.GET('/api/v1/pages')

  if (error) throw new Error(`Error al obtener páginas editoriales: ${JSON.stringify(error)}`)
  return data ?? []
}
