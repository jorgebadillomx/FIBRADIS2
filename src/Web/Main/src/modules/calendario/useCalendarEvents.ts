import { useQuery } from '@tanstack/react-query'
import { fetchMarketCalendarEvents } from '@/api/calendarApi'
import { calcMonthRange } from './calendarUtils'

export function useCalendarEvents(year: number, month: number) {
  const { from, to } = calcMonthRange(year, month)
  return useQuery({
    queryKey: ['calendar-events', year, month],
    queryFn: () => fetchMarketCalendarEvents(from, to),
    staleTime: 5 * 60_000,
  })
}
