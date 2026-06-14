import createClient from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'

const apiClient = createClient<paths>({ baseUrl: '' })

export type FaqItemDto = components['schemas']['FaqItemDto']

export async function fetchFaqItems(pageType: string, entityKey: string): Promise<FaqItemDto[]> {
  const { data, error } = await apiClient.GET('/api/v1/faq', {
    params: {
      query: {
        pageType,
        entityKey,
      },
    },
  })

  if (error) {
    throw new Error(`Error al cargar FAQ: ${JSON.stringify(error)}`)
  }

  return data ?? []
}
