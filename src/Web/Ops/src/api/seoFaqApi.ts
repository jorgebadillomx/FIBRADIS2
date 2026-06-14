import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const opsClient = createPathBasedClient<paths>({ baseUrl: '' })

export type FaqItemDto = components['schemas']['FaqItemDto']
export type UpsertFaqItemRequest = components['schemas']['UpsertFaqItemRequest']
export type FaqSeedResultDto = components['schemas']['FaqSeedResultDto']

export async function fetchFaqItems(pageType: string, entityKey: string): Promise<FaqItemDto[]> {
  assertOpsAccessToken()

  const { data, error } = await opsClient['/api/v1/ops/seo/faq'].GET({
    headers: getOpsAuthHeaders(),
    params: {
      query: {
        pageType,
        entityKey,
      },
    },
  })

  if (error) {
    throw new Error(getOpsApiErrorMessage(error, `Error al cargar FAQ: ${JSON.stringify(error)}`))
  }

  return data ?? []
}

export async function createFaqItem(request: UpsertFaqItemRequest): Promise<FaqItemDto> {
  assertOpsAccessToken()

  const { data, error } = await opsClient['/api/v1/ops/seo/faq'].POST({
    headers: getOpsAuthHeaders(),
    body: request,
  })

  if (error || !data) {
    throw new Error(getOpsApiErrorMessage(error, `Error al crear FAQ: ${JSON.stringify(error)}`))
  }

  return data
}

export async function updateFaqItem(id: string, request: UpsertFaqItemRequest): Promise<FaqItemDto> {
  assertOpsAccessToken()

  const { data, error } = await opsClient['/api/v1/ops/seo/faq/{id}'].PUT({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
    body: request,
  })

  if (error || !data) {
    throw new Error(getOpsApiErrorMessage(error, `Error al actualizar FAQ: ${JSON.stringify(error)}`))
  }

  return data
}

export async function deactivateFaqItem(id: string): Promise<void> {
  assertOpsAccessToken()

  const { error } = await opsClient['/api/v1/ops/seo/faq/{id}'].DELETE({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
  })

  if (error) {
    throw new Error(getOpsApiErrorMessage(error, `Error al desactivar FAQ: ${JSON.stringify(error)}`))
  }
}

export async function seedFaqItems(): Promise<FaqSeedResultDto> {
  assertOpsAccessToken()

  const { data, error } = await opsClient['/api/v1/ops/seo/faq/seed'].POST({
    headers: getOpsAuthHeaders(),
  })

  if (error || !data) {
    throw new Error(getOpsApiErrorMessage(error, `Error al sembrar FAQ: ${JSON.stringify(error)}`))
  }

  return data
}
