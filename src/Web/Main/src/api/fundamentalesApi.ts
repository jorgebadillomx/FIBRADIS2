import type { components } from '@fibradis/shared-api-client'

export type FundamentalesPublicDto = components['schemas']['FundamentalesPublicDto']
export type FundamentalesSummaryItemDto = components['schemas']['FundamentalesSummaryItemDto']

export async function fetchFundamentalesPublic(ticker: string, period?: string): Promise<FundamentalesPublicDto | null> {
  const url = period
    ? `/api/v1/fundamentals/${encodeURIComponent(ticker)}/latest?period=${encodeURIComponent(period)}`
    : `/api/v1/fundamentals/${encodeURIComponent(ticker)}/latest`

  const response = await fetch(url)
  if (response.status === 404) return null
  if (!response.ok) return null
  return response.json() as Promise<FundamentalesPublicDto>
}

export async function fetchFundamentalesAvailablePeriods(ticker: string): Promise<string[]> {
  const response = await fetch(`/api/v1/fundamentals/${encodeURIComponent(ticker)}/periods`)
  if (!response.ok) return []
  return response.json() as Promise<string[]>
}

export async function fetchFundamentalesSummary(
  opts?: { period?: string; recent?: number }
): Promise<FundamentalesSummaryItemDto[]> {
  let url = '/api/v1/fundamentals/summary'
  if (opts?.recent != null && opts.recent > 0) {
    url += `?recent=${opts.recent}`
  } else if (opts?.period) {
    url += `?period=${encodeURIComponent(opts.period)}`
  }
  const response = await fetch(url)
  if (!response.ok) return []
  return response.json() as Promise<FundamentalesSummaryItemDto[]>
}

export async function fetchAllFundamentalesPeriods(): Promise<string[]> {
  const response = await fetch('/api/v1/fundamentals/periods')
  if (!response.ok) return []
  return response.json() as Promise<string[]>
}
