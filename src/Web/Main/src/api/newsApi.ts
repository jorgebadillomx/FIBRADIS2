import createClient from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'

export type LinkedFibra = { id: string; ticker: string }
export type NewsArticle = components['schemas']['NewsArticleDto'] & {
  linkedFibras?: LinkedFibra[] | null
}
export type NewsAiAnalysis = components['schemas']['NewsAiAnalysisDto']
export type NewsKeyFigure = components['schemas']['NewsKeyFigureDto']
export type NewsPagedResult = components['schemas']['NewsPagedResultDto']

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

export async function fetchNewsPaged(
  page: number,
  pageSize: number,
  q?: string,
  fibraId?: string
): Promise<NewsPagedResult> {
  const apiClient = getApiClient()
  const { data, error } = await apiClient.GET('/api/v1/news/paged', {
    params: {
      query: {
        pageNumber: page,
        pageSize,
        q: q || undefined,
        fibraId: fibraId || undefined,
      },
    },
  })

  if (error) throw new Error(`Error al obtener noticias paginadas: ${JSON.stringify(error)}`)
  return data
}

export async function fetchRelatedNews(articleId: string): Promise<NewsArticle[]> {
  const response = await fetch(`/api/v1/news/${articleId}/related`)
  if (!response.ok) return []
  return response.json() as Promise<NewsArticle[]>
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
