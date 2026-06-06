import type { Page, Route } from '@playwright/test'

type OpportunityFibraRowDto = {
  fibraId: string
  ticker: string
  nombre: string
  navDiscountScore: string | null
  dividendYieldScore: string | null
  ltvInvertedScore: string | null
  noiMarginScore: string | null
  pricevs52wScore: string | null
  navDiscountPct: string | null
  dividendYieldPct: string | null
  ltvPct: string | null
  noiMarginPct: string | null
  priceVsAvg52wPct: string | null
  precioActual: string | null
  navPerCbfi: string | null
  avg52w: string | null
}

type CoverageDto = {
  status: 'Ok' | 'Degraded' | 'Suspended'
  universeSize: number
  fibrasWithPrice: number
  missingPct: string
  lastValidPriceAt: string | null
}

type WeightsDto = {
  navDiscount: number
  dividendYield: number
  ltvInverted: number
  noiMargin: number
  pricevs52w: number
  profile: string
}

type OpportunityRankingResponseDto = {
  ranked: OpportunityFibraRowDto[]
  limitedData: OpportunityFibraRowDto[]
  weights: WeightsDto
  coverage: CoverageDto
}

function fulfillJson(route: Route, status: number, body: unknown) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

export const defaultRanked: OpportunityFibraRowDto[] = [
  {
    fibraId: '11111111-1111-1111-1111-111111111111',
    ticker: 'FUNO11',
    nombre: 'Fibra Uno',
    navDiscountScore: '85.0',
    dividendYieldScore: '72.0',
    ltvInvertedScore: '68.0',
    noiMarginScore: '75.0',
    pricevs52wScore: '60.0',
    navDiscountPct: '14.5',
    dividendYieldPct: '4.63',
    ltvPct: '42.0',
    noiMarginPct: '68.0',
    priceVsAvg52wPct: '6.1',
    precioActual: '52.50',
    navPerCbfi: '120.00',
    avg52w: '49.50',
  },
  {
    fibraId: '22222222-2222-2222-2222-222222222222',
    ticker: 'DANHOS13',
    nombre: 'Fibra Danhos',
    navDiscountScore: '61.0',
    dividendYieldScore: '54.0',
    ltvInvertedScore: '78.0',
    noiMarginScore: '80.0',
    pricevs52wScore: '48.0',
    navDiscountPct: '8.1',
    dividendYieldPct: '3.54',
    ltvPct: '35.0',
    noiMarginPct: '72.0',
    priceVsAvg52wPct: '-4.6',
    precioActual: '41.00',
    navPerCbfi: '105.00',
    avg52w: '39.20',
  },
  {
    fibraId: '33333333-3333-3333-3333-333333333333',
    ticker: 'FMTY14',
    nombre: 'Fibra Monterrey',
    navDiscountScore: '45.0',
    dividendYieldScore: '48.0',
    ltvInvertedScore: '55.0',
    noiMarginScore: '50.0',
    pricevs52wScore: '42.0',
    navDiscountPct: '3.2',
    dividendYieldPct: '2.85',
    ltvPct: '38.0',
    noiMarginPct: '62.0',
    priceVsAvg52wPct: '-2.1',
    precioActual: '18.75',
    navPerCbfi: '19.40',
    avg52w: '18.10',
  },
]

export const defaultLimitedData: OpportunityFibraRowDto[] = [
  {
    fibraId: '44444444-4444-4444-4444-444444444444',
    ticker: 'FINN13',
    nombre: 'Fibra Inn',
    navDiscountScore: null,
    dividendYieldScore: '38.0',
    ltvInvertedScore: null,
    noiMarginScore: '42.0',
    pricevs52wScore: null,
    navDiscountPct: null,
    dividendYieldPct: '2.10',
    ltvPct: null,
    noiMarginPct: '55.0',
    priceVsAvg52wPct: null,
    precioActual: '9.50',
    navPerCbfi: null,
    avg52w: null,
  },
]

export const defaultWeights: WeightsDto = {
  navDiscount: 30,
  dividendYield: 30,
  ltvInverted: 20,
  noiMargin: 10,
  pricevs52w: 10,
  profile: 'default',
}

export const defaultCoverage: CoverageDto = {
  status: 'Ok',
  universeSize: 4,
  fibrasWithPrice: 4,
  missingPct: '0.0',
  lastValidPriceAt: '2026-06-05T10:00:00Z',
}

export interface OpportunitiesApiOptions {
  ranked?: OpportunityFibraRowDto[]
  limitedData?: OpportunityFibraRowDto[]
  weights?: Partial<WeightsDto>
  coverage?: Partial<CoverageDto>
}

export async function mockOpportunitiesApi(page: Page, options: OpportunitiesApiOptions = {}) {
  const ranked = options.ranked ?? defaultRanked
  const limitedData = options.limitedData ?? defaultLimitedData
  const weights = { ...defaultWeights, ...(options.weights ?? {}) }
  const coverage = { ...defaultCoverage, ...(options.coverage ?? {}) }

  const response: OpportunityRankingResponseDto = { ranked, limitedData, weights, coverage }

  await page.route('**/api/v1/opportunities', async (route) => {
    if (route.request().method() === 'GET') {
      return fulfillJson(route, 200, response)
    }
    return route.continue()
  })

  await page.route('**/api/v1/opportunities/weights', async (route) => {
    if (route.request().method() === 'PUT') {
      return fulfillJson(route, 200, {})
    }
    return route.continue()
  })
}
