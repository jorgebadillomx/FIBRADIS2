import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type UserSummaryDto = components['schemas']['UserSummaryDto']
export type CreateUserRequest = components['schemas']['CreateUserRequest']

export async function fetchOpsUsers(): Promise<UserSummaryDto[]> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/users'].GET({
    headers: getOpsAuthHeaders(),
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al cargar usuarios: ${JSON.stringify(error)}`))
  return data ?? []
}

export async function createOpsUser(payload: CreateUserRequest): Promise<UserSummaryDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/users'].POST({
    headers: getOpsAuthHeaders(),
    body: payload,
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al crear usuario: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}
