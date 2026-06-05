import createClient from 'openapi-fetch'
import type { paths } from '@fibradis/shared-api-client'
import { clearMainAccessToken, getStoredMainAccessToken, notifyMainAuthRequired } from '@/modules/auth/mainAuth'

export const apiClient = createClient<paths>({ baseUrl: '' })

apiClient.use({
  onRequest({ request }) {
    const token = getStoredMainAccessToken()
    if (token) {
      request.headers.set('Authorization', `Bearer ${token}`)
    }
    return request
  },
  onResponse({ response }) {
    if (response.status === 401 && getStoredMainAccessToken()) {
      clearMainAccessToken()
      notifyMainAuthRequired()
    }
    return response
  },
})

export async function fetchAllFibras() {
  const pageSize = 100
  const { data: first, error: firstError } = await apiClient.GET('/api/v1/fibras', {
    params: { query: { page: 1, pageSize } },
  })
  if (firstError) throw new Error(`Error al obtener fibras: ${JSON.stringify(firstError)}`)

  const all = [...(first?.items ?? [])]
  let page = 2
  while (all.length > 0 && all.length % pageSize === 0) {
    const { data, error } = await apiClient.GET('/api/v1/fibras', {
      params: { query: { page, pageSize } },
    })
    if (error) throw new Error(`Error al obtener fibras: ${JSON.stringify(error)}`)
    const items = data?.items ?? []
    all.push(...items)
    if (items.length < pageSize) break
    page++
  }

  return all
}

export async function fetchMarketSnapshots() {
  const { data, error } = await apiClient.GET('/api/v1/market/snapshots')
  if (error) throw new Error(`Error al obtener market snapshots: ${JSON.stringify(error)}`)
  return data ?? []
}

export async function fetchFibraHistory(ticker: string, period: '1m' | '3m' | '6m' | '1y') {
  const { data, error } = await apiClient.GET('/api/v1/market/fibras/{ticker}/history', {
    params: { path: { ticker }, query: { period } },
  })
  if (error) throw new Error(`Error al obtener historial de '${ticker}': ${JSON.stringify(error)}`)
  return data
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
