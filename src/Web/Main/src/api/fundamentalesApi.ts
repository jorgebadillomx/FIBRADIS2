import createClient from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'

const apiClient = createClient<paths>({ baseUrl: '' })

export type FundamentalesPublicDto = components['schemas']['FundamentalesPublicDto']

export async function fetchFundamentalesPublic(ticker: string): Promise<FundamentalesPublicDto | null> {
  const { data, response } = await apiClient.GET('/api/v1/fundamentals/{ticker}/latest', {
    params: { path: { ticker } },
  })

  if (response.status === 404) return null
  if (!data) return null
  return data
}
