import createClient from 'openapi-fetch'
import type { paths } from '@fibradis/shared-api-client'

const apiClient = createClient<paths>({ baseUrl: '' })

export async function fetchLatestNews() {
  const { data, error } = await apiClient.GET('/api/v1/news')
  if (error) throw new Error(`Error al obtener noticias: ${JSON.stringify(error)}`)
  return data ?? []
}
