import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type UserSummaryDto = components['schemas']['UserSummaryDto']
export type CreateUserRequest = components['schemas']['CreateUserRequest']
export type SetUserActiveRequest = components['schemas']['SetUserActiveRequest']
export type ChangePasswordRequest = components['schemas']['ChangePasswordRequest']
export type UpdatePaymentRequest = components['schemas']['UpdatePaymentRequest']
export type UpdateSubscriptionRequest = components['schemas']['UpdateSubscriptionRequest']

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

export async function setUserActive(id: string, isActive: boolean): Promise<UserSummaryDto> {
  assertOpsAccessToken()
  const { data, error } = await apiClient['/api/v1/ops/users/{id}/active'].PATCH({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
    body: { isActive },
  })
  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al actualizar estado: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}

export async function changeUserPassword(id: string, newPassword: string): Promise<void> {
  assertOpsAccessToken()
  const { error } = await apiClient['/api/v1/ops/users/{id}/password'].PATCH({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
    body: { newPassword },
  })
  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al cambiar contraseña: ${JSON.stringify(error)}`))
}

export async function updateUserPayment(
  id: string,
  pago: number | null,
  fechaPago: string | null,
): Promise<UserSummaryDto> {
  assertOpsAccessToken()
  const { data, error } = await apiClient['/api/v1/ops/users/{id}/payment'].PATCH({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
    body: { pago, fechaPago },
  })
  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al actualizar pago: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}

export async function updateUserSubscription(
  id: string,
  body: UpdateSubscriptionRequest,
): Promise<UserSummaryDto> {
  assertOpsAccessToken()
  const { data, error } = await apiClient['/api/v1/ops/users/{id}/subscription'].PATCH({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
    body,
  })
  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al actualizar suscripción: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}
