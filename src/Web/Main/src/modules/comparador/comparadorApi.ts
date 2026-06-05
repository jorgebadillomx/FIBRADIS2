import type { components } from '@fibradis/shared-api-client'
import { apiClient } from '@/api/fibrasApi'

type ComparadorFibraDto = components['schemas']['ComparadorFibraDto']

export async function fetchComparacion(tickers: string[]): Promise<ComparadorFibraDto[]> {
  const { data, error } = await apiClient.GET('/api/v1/compare', {
    params: {
      query: {
        tickers: tickers.join(','),
      },
    },
  })

  if (error || !data) {
    const detail = typeof error === 'object' && error && 'detail' in error ? (error as { detail?: string }).detail : null
    throw new Error(detail ?? 'No se pudo cargar la comparación.')
  }

  return data
}
