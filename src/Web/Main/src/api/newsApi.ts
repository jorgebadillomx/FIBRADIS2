import createClient from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'

export type NewsArticle = components['schemas']['NewsArticleDto']
export type NewsAiAnalysis = components['schemas']['NewsAiAnalysisDto']
export type NewsKeyFigure = components['schemas']['NewsKeyFigureDto']

function getApiClient() {
  return createClient<paths>({
    baseUrl: typeof window === 'undefined' ? 'http://localhost' : '',
  })
}

export async function fetchLatestNews() {
  const apiClient = getApiClient()
  const { data, error } = await apiClient.GET('/api/v1/news')
  if (error) throw new Error(`Error al obtener noticias: ${JSON.stringify(error)}`)
  return data ?? []
}

export async function fetchArticleById(id: string) {
  const apiClient = getApiClient()
  const { data, error, response } = await apiClient.GET('/api/v1/news/{id}', {
    params: { path: { id } },
  })

  if (response.status === 404) {
    return null
  }

  if (error) {
    throw new Error(`Error al obtener artículo: ${JSON.stringify(error)}`)
  }

  return data ?? null
}
