import type { components } from '@fibradis/shared-api-client'
import { apiClient } from '@/api/fibrasApi'

export type PortfolioCalendarEvent = components['schemas']['PortfolioCalendarEventDto']

function defaultWindow(): { from: string; to: string } {
  const today = new Date()
  const y = today.getUTCFullYear()
  const m = today.getUTCMonth()
  return {
    from: new Date(Date.UTC(y, m - 2, 1)).toISOString().slice(0, 10),
    to: new Date(Date.UTC(y, m + 2, 0)).toISOString().slice(0, 10),
  }
}

export async function fetchPortfolioCalendar(
  from?: string,
  to?: string,
): Promise<PortfolioCalendarEvent[]> {
  const window = defaultWindow()
  const { data, error } = await apiClient.GET('/api/v1/portfolio/calendar', {
    params: { query: { from: from ?? window.from, to: to ?? window.to } },
  })
  if (error) throw new Error(`Error al obtener el calendario de distribuciones: ${JSON.stringify(error)}`)
  return data ?? []
}
