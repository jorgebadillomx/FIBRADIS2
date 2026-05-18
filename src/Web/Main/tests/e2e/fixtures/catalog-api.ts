import type { Page, Route } from '@playwright/test'

type FibraListItem = {
  id: string
  ticker: string
  fullName: string
  shortName: string
  sector: string
  market: string
  currency: string
  state: string
  siteUrl: string | null
}

type FibraDetail = FibraListItem & {
  investorUrl: string | null
  reportsUrl: string | null
  nameVariants: string[]
  createdAt: string
}

const fibraDetails: Record<string, FibraDetail> = {
  FUNO11: {
    id: '11111111-1111-1111-1111-111111111111',
    ticker: 'FUNO11',
    fullName: 'Fibra Uno',
    shortName: 'Fibra Uno',
    sector: 'Diversificado',
    market: 'BMV',
    currency: 'MXN',
    state: 'Active',
    siteUrl: 'https://fibra.uno',
    investorUrl: 'https://fibra.uno/inversionistas',
    reportsUrl: null,
    nameVariants: ['Fibra Uno', 'FUNO'],
    createdAt: '2026-01-01T00:00:00Z',
  },
  DANHOS13: {
    id: '22222222-2222-2222-2222-222222222222',
    ticker: 'DANHOS13',
    fullName: 'Fibra Danhos',
    shortName: 'Danhos',
    sector: 'Comercial',
    market: 'BMV',
    currency: 'MXN',
    state: 'Active',
    siteUrl: 'https://fibradanhos.com.mx',
    investorUrl: 'https://fibradanhos.com.mx/ri',
    reportsUrl: null,
    nameVariants: ['Danhos', 'DANHOS'],
    createdAt: '2026-01-01T00:00:00Z',
  },
  FMTY14: {
    id: '33333333-3333-3333-3333-333333333333',
    ticker: 'FMTY14',
    fullName: 'Fibra Monterrey',
    shortName: 'Fibra MTY',
    sector: 'Industrial',
    market: 'BMV',
    currency: 'MXN',
    state: 'Active',
    siteUrl: 'https://fibramty.com',
    investorUrl: 'https://fibramty.com/inversionistas',
    reportsUrl: null,
    nameVariants: ['Fibra Monterrey', 'FibraMTY', 'FMTY'],
    createdAt: '2026-01-01T00:00:00Z',
  },
}

const fibras: FibraListItem[] = Object.values(fibraDetails).map((fibra) => ({
  id: fibra.id,
  ticker: fibra.ticker,
  fullName: fibra.fullName,
  shortName: fibra.shortName,
  sector: fibra.sector,
  market: fibra.market,
  currency: fibra.currency,
  state: fibra.state,
  siteUrl: fibra.siteUrl,
}))

function fulfillJson(route: Route, status: number, body: unknown) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

export async function mockCatalogApi(page: Page) {
  await page.route('**/api/v1/fibras**', async (route) => {
    const url = new URL(route.request().url())
    const path = url.pathname

    if (path === '/api/v1/fibras') {
      return fulfillJson(route, 200, {
        items: fibras,
        page: 1,
        pageSize: 100,
        total: fibras.length,
      })
    }

    const ticker = decodeURIComponent(path.replace('/api/v1/fibras/', '')).toUpperCase()
    const fibra = fibraDetails[ticker]

    if (fibra) {
      return fulfillJson(route, 200, fibra)
    }

    return fulfillJson(route, 404, {
      title: 'FIBRA no encontrada',
      detail: `No existe una FIBRA con ticker '${ticker}'.`,
      status: 404,
      domainCode: 'FIBRA_NOT_FOUND',
      correlationId: 'playwright-test',
    })
  })
}
