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

export async function fetchFibraByTicker(ticker: string) {
  const { data, error, response } = await apiClient.GET('/api/v1/fibras/{ticker}', {
    params: { path: { ticker } },
  })
  if (error) {
    if (response.status === 404) return null
    throw new Error(`Error al obtener FIBRA '${ticker}': ${JSON.stringify(error)}`)
  }
  return data
}
