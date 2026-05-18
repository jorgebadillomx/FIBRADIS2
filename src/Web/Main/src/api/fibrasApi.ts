import createClient from 'openapi-fetch'
import type { paths } from '@fibradis/shared-api-client'

const apiClient = createClient<paths>({ baseUrl: '' })

export async function fetchAllFibras() {
  const { data, error } = await apiClient.GET('/api/v1/fibras', {
    params: { query: { page: 1, pageSize: 100 } },
  })
  if (error) throw new Error(`Error al obtener fibras: ${JSON.stringify(error)}`)
  return data?.items ?? []
}
