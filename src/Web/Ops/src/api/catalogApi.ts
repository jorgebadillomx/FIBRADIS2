import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type FibraDetail = components['schemas']['FibraDetail'] & { yahooTicker: string }
export type CreateFibraRequest = components['schemas']['CreateFibraRequest']
export type UpdateFibraRequest = components['schemas']['UpdateFibraRequest']

export async function fetchOpsCatalog(): Promise<FibraDetail[]> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/catalog'].GET({
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al cargar catálogo: ${JSON.stringify(error)}`))
  return data ?? []
}

export async function createFibra(payload: CreateFibraRequest): Promise<FibraDetail> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/catalog'].POST({
    headers: getOpsAuthHeaders(),
    body: payload,
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al crear FIBRA: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}

export async function updateFibra(ticker: string, payload: UpdateFibraRequest): Promise<FibraDetail> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/catalog/{ticker}'].PUT({
    headers: getOpsAuthHeaders(),
    params: { path: { ticker } },
    body: payload,
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al actualizar FIBRA: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}

export async function deactivateFibra(ticker: string): Promise<FibraDetail> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/catalog/{ticker}/deactivate'].POST({
    headers: getOpsAuthHeaders(),
    params: { path: { ticker } },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al desactivar FIBRA: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}

export async function activateFibra(ticker: string): Promise<FibraDetail> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/catalog/{ticker}/activate'].POST({
    headers: getOpsAuthHeaders(),
    params: { path: { ticker } },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al activar FIBRA: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}
