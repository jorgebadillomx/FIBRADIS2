import type { Page, Route } from '@playwright/test'

type NewsArticle = {
  id: string
  title: string
  source: string
  publishedAt: string
  url: string
  snippet: string | null
  aiSummary: string | null
}

type NewsFixtureOptions = {
  latest?: NewsArticle[]
  byFibraId?: Record<string, NewsArticle[]>
}

const defaultLatestNews: NewsArticle[] = [
  {
    id: 'aaaaaaaa-1111-1111-1111-111111111111',
    title: 'FUNO11 concreta refinanciamiento estratégico',
    source: 'El Financiero',
    publishedAt: '2026-05-20T14:30:00Z',
    url: 'https://example.com/noticias/funo11-refinanciamiento',
    snippet: 'La emisora detalló los términos del refinanciamiento.',
    aiSummary: 'Resumen IA: FUNO11 refinanció pasivos para extender vencimientos y preservar liquidez.',
  },
  {
    id: 'aaaaaaaa-2222-2222-2222-222222222222',
    title: 'FMTY14 reporta nueva ocupación industrial',
    source: 'Expansión',
    publishedAt: '2026-05-19T18:00:00Z',
    url: 'https://example.com/noticias/fmty14-ocupacion',
    snippet: 'La ocupación industrial aumentó durante el trimestre.',
    aiSummary: null,
  },
]

const defaultFibraNews: Record<string, NewsArticle[]> = {
  '11111111-1111-1111-1111-111111111111': [
    {
      id: 'bbbbbbbb-1111-1111-1111-111111111111',
      title: 'FUNO11 acelera desinversión de activos no estratégicos',
      source: 'Reuters',
      publishedAt: '2026-05-20T09:15:00Z',
      url: 'https://example.com/noticias/funo11-desinversion',
      snippet: 'La emisora planea reciclar capital hacia proyectos logísticos.',
      aiSummary: 'Resumen IA: FUNO11 busca reciclar capital y concentrarse en activos con mayor rentabilidad.',
    },
    {
      id: 'bbbbbbbb-2222-2222-2222-222222222222',
      title: 'Analistas ajustan estimados para FUNO11',
      source: 'Bloomberg Línea',
      publishedAt: '2026-05-18T16:45:00Z',
      url: 'https://example.com/noticias/funo11-analistas',
      snippet: 'El consenso ajustó estimados tras la última guía trimestral.',
      aiSummary: null,
    },
  ],
}

function fulfillJson(route: Route, status: number, body: unknown) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

export async function mockNewsApi(page: Page, options: NewsFixtureOptions = {}) {
  const latest = options.latest ?? defaultLatestNews
  const byFibraId = options.byFibraId ?? defaultFibraNews

  await page.route('**/api/v1/news', (route) => fulfillJson(route, 200, latest))

  await page.route('**/api/v1/news/fibras/**', (route) => {
    const url = new URL(route.request().url())
    const parts = url.pathname.split('/')
    const fibraId = parts[parts.length - 1] ?? ''
    return fulfillJson(route, 200, byFibraId[fibraId] ?? [])
  })
}
