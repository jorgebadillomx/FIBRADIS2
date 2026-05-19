import createClient from 'openapi-fetch'
import type { paths } from '@fibradis/shared-api-client'

const apiClient = createClient<paths>({ baseUrl: '' })

export async function fetchFibraNews(fibraId: string) {
  const { data, error } = await apiClient.GET('/api/v1/news/fibras/{fibraId}', {
    params: { path: { fibraId } },
  })
  if (error) throw new Error(`Error al obtener noticias de la FIBRA '${fibraId}': ${JSON.stringify(error)}`)
  return data ?? []
}
