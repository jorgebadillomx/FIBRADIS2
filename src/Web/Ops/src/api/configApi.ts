import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type OperationalConfigDto = components['schemas']['OperationalConfigDto'] & {
  fibraNewsMonths?: number | string | null
}
export type UpdateOperationalConfigRequest = components['schemas']['UpdateOperationalConfigRequest'] & {
  fibraNewsMonths?: number | string | null
}
export type ConfigAuditLogDto = components['schemas']['ConfigAuditLogDto']

export async function fetchOpsConfig(): Promise<OperationalConfigDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/config'].GET({
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al cargar configuración: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió configuración.')
  return data
}

export async function updateOpsConfig(payload: Partial<UpdateOperationalConfigRequest>): Promise<void> {
  assertOpsAccessToken()

  const { error } = await apiClient['/api/v1/ops/config'].PUT({
    headers: getOpsAuthHeaders(),
    body: payload as UpdateOperationalConfigRequest,
  })

  if (error) {
    throw new Error(getOpsApiErrorMessage(error, `Error al guardar configuración: ${JSON.stringify(error)}`))
  }
}

export async function fetchAuditLog(): Promise<ConfigAuditLogDto[]> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/audit-log'].GET({
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al cargar auditoría: ${JSON.stringify(error)}`))
  return data ?? []
}
