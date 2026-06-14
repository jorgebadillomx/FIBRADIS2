import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type UrlRedirectDto = components['schemas']['UrlRedirectDto']
export type UpsertUrlRedirectRequest = components['schemas']['UpsertUrlRedirectRequest']

export async function fetchUrlRedirects(): Promise<UrlRedirectDto[]> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/seo/redirects'].GET({
    headers: getOpsAuthHeaders(),
  })

  if (error) {
    throw new Error(getOpsApiErrorMessage(error, `Error al cargar redirects: ${JSON.stringify(error)}`))
  }

  return data ?? []
}

export async function createUrlRedirect(payload: UpsertUrlRedirectRequest): Promise<UrlRedirectDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/seo/redirects'].POST({
    headers: getOpsAuthHeaders(),
    body: payload,
  })

  if (error || !data) {
    throw new Error(getOpsApiErrorMessage(error, `Error al crear redirect: ${JSON.stringify(error)}`))
  }

  return data
}

export async function updateUrlRedirect(id: string, payload: UpsertUrlRedirectRequest): Promise<UrlRedirectDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/seo/redirects/{id}'].PUT({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
    body: payload,
  })

  if (error || !data) {
    throw new Error(getOpsApiErrorMessage(error, `Error al actualizar redirect: ${JSON.stringify(error)}`))
  }

  return data
}

export async function activateUrlRedirect(id: string): Promise<UrlRedirectDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/seo/redirects/{id}/activate'].POST({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
  })

  if (error || !data) {
    throw new Error(getOpsApiErrorMessage(error, `Error al activar redirect: ${JSON.stringify(error)}`))
  }

  return data
}

export async function deactivateUrlRedirect(id: string): Promise<UrlRedirectDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/seo/redirects/{id}/deactivate'].POST({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
  })

  if (error || !data) {
    throw new Error(getOpsApiErrorMessage(error, `Error al desactivar redirect: ${JSON.stringify(error)}`))
  }

  return data
}
