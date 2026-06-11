import createClient from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'

const apiClient = createClient<paths>({
  baseUrl: typeof window === 'undefined' ? 'http://localhost' : '',
})

export type MarketCalendarEvent = components['schemas']['CalendarEventDto']

export async function fetchMarketCalendarEvents(from: string, to: string): Promise<MarketCalendarEvent[]> {
  const { data, error } = await apiClient.GET('/api/v1/market/events', {
    params: { query: { from, to } },
  })

  if (error) throw new Error(`Error al obtener eventos del calendario: ${JSON.stringify(error)}`)
  return data ?? []
}
