import type { Page, Route } from '@playwright/test'

type ScoreDto = {
  score: string | null
  navDescuentoScore: string | null
  dividendYieldScore: string | null
  ltvScore: string | null
  noiMarginScore: string | null
  priceVs52wScore: string | null
  isLimitedData: boolean
  isExcluded: boolean
}

type MercadoDto = {
  precioActual: string | null
  cambiaDiaPct: string | null
  avg52S: string | null
  volumen: number | null
}

type FundamentalesDto = {
  periodo: string | null
  capRate: string | null
  navPerCbfi: string | null
  ltv: string | null
  noiMargin: string | null
  ffoMargin: string | null
}

type DistribucionesDto = {
  distribucionTrimestral: string | null
  yieldCalculadoPct: string | null
  yieldDecretadoPct: string | null
}

type ComparadorFibraDto = {
  ticker: string
  nombre: string
  mercado: MercadoDto
  fundamentales: FundamentalesDto
  distribuciones: DistribucionesDto
  score: ScoreDto
}

function buildComparadorRow(
  ticker: string,
  nombre: string,
  overrides: Partial<ComparadorFibraDto> = {},
): ComparadorFibraDto {
  return {
    ticker,
    nombre,
    mercado: {
      precioActual: '52.50',
      cambiaDiaPct: '0.62',
      avg52S: '49.50',
      volumen: 1234567,
      ...overrides.mercado,
    },
    fundamentales: {
      periodo: 'Q3 2025',
      capRate: '7.5',
      navPerCbfi: '120.00',
      ltv: '42.0',
      noiMargin: '68.0',
      ffoMargin: '55.0',
      ...overrides.fundamentales,
    },
    distribuciones: {
      distribucionTrimestral: '0.384',
      yieldCalculadoPct: '4.63',
      yieldDecretadoPct: '4.50',
      ...overrides.distribuciones,
    },
    score: {
      score: '73.5',
      navDescuentoScore: '85.0',
      dividendYieldScore: '72.0',
      ltvScore: '68.0',
      noiMarginScore: '75.0',
      priceVs52wScore: '60.0',
      isLimitedData: false,
      isExcluded: false,
      ...overrides.score,
    },
  }
}

export const comparadorFixtures: Record<string, ComparadorFibraDto> = {
  FUNO11: buildComparadorRow('FUNO11', 'Fibra Uno'),
  DANHOS13: buildComparadorRow('DANHOS13', 'Fibra Danhos', {
    mercado: { precioActual: '41.00', cambiaDiaPct: '-0.24', avg52S: '39.20', volumen: 456789 },
    fundamentales: { periodo: 'Q2 2025', capRate: '6.8', navPerCbfi: '105.00', ltv: '35.0', noiMargin: '72.0', ffoMargin: '60.0' },
    distribuciones: { distribucionTrimestral: '0.35', yieldCalculadoPct: '3.54', yieldDecretadoPct: '3.40' },
    score: { score: '62.1', navDescuentoScore: '61.0', dividendYieldScore: '54.0', ltvScore: '78.0', noiMarginScore: '80.0', priceVs52wScore: '48.0', isLimitedData: false, isExcluded: false },
  }),
  FMTY14: buildComparadorRow('FMTY14', 'Fibra Monterrey', {
    mercado: { precioActual: '18.75', cambiaDiaPct: '0.10', avg52S: '18.10', volumen: 200000 },
    fundamentales: { periodo: 'Q1 2025', capRate: null, navPerCbfi: null, ltv: '38.0', noiMargin: null, ffoMargin: null },
    distribuciones: { distribucionTrimestral: null, yieldCalculadoPct: null, yieldDecretadoPct: null },
    score: { score: null, navDescuentoScore: null, dividendYieldScore: null, ltvScore: null, noiMarginScore: null, priceVs52wScore: null, isLimitedData: false, isExcluded: true },
  }),
}

function fulfillJson(route: Route, status: number, body: unknown) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

export async function mockComparadorApi(page: Page) {
  await page.route('**/api/v1/compare**', (route) => {
    const url = new URL(route.request().url())
    const tickersParam = url.searchParams.get('tickers') ?? ''
    const tickers = tickersParam.split(',').map((t) => t.trim().toUpperCase()).filter(Boolean)

    const rows = tickers
      .map((ticker) => comparadorFixtures[ticker])
      .filter((row): row is ComparadorFibraDto => row != null)

    if (rows.length === 0) {
      return fulfillJson(route, 400, {
        title: 'Tickers inválidos',
        status: 400,
        domainCode: 'INVALID_TICKERS',
        correlationId: 'playwright-test',
      })
    }

    return fulfillJson(route, 200, rows)
  })
}
