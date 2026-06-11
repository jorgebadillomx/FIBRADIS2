import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type DistributionAdminDto = components['schemas']['DistributionAdminDto']
export type DistributionUpsertRequest = components['schemas']['DistributionUpsertRequest']

export async function fetchOpsDistributions(): Promise<DistributionAdminDto[]> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/distributions'].GET({
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al cargar distribuciones: ${JSON.stringify(error)}`))
  return data ?? []
}

export async function syncOpsDistributions(): Promise<void> {
  assertOpsAccessToken()

  const { error } = await apiClient['/api/v1/ops/distributions/sync'].POST({
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al disparar sincronización: ${JSON.stringify(error)}`))
}

export async function createOpsDistribution(payload: DistributionUpsertRequest): Promise<DistributionAdminDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/distributions'].POST({
    headers: getOpsAuthHeaders(),
    body: payload,
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al crear distribución: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió la distribución creada.')
  return data
}

export async function updateOpsDistribution(id: string, payload: DistributionUpsertRequest): Promise<DistributionAdminDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/distributions/{id}'].PUT({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
    body: payload,
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al actualizar distribución: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió la distribución actualizada.')
  return data
}

export async function deleteOpsDistribution(id: string): Promise<void> {
  assertOpsAccessToken()

  const { error } = await apiClient['/api/v1/ops/distributions/{id}'].DELETE({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al eliminar distribución: ${JSON.stringify(error)}`))
}
