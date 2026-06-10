import type { Page, Route } from '@playwright/test'
import type { components } from '@fibradis/shared-api-client'

type PortfolioPositionDto = components['schemas']['PortfolioPositionDto']
type PortfolioKpisDto = components['schemas']['PortfolioKpisDto']
type PortfolioResponseDto = components['schemas']['PortfolioResponseDto']
type PortfolioSnapshotStatusDto = components['schemas']['PortfolioSnapshotStatusDto']
type PortfolioUploadResponseDto = components['schemas']['PortfolioUploadResponseDto']
type PortfolioColumnConfigDto = components['schemas']['PortfolioColumnConfigDto']
type PortfolioConfigDto = components['schemas']['PortfolioConfigDto']
type PortfolioPerformanceResponseDto = components['schemas']['PortfolioPerformanceResponseDto']

export const PORTFOLIO_FIBRA_ID = '11111111-1111-1111-1111-111111111111'
export const PORTFOLIO_FIBRA_ID_2 = '22222222-2222-2222-2222-222222222222'

export const defaultPositions: PortfolioPositionDto[] = [
  {
    fibraId: PORTFOLIO_FIBRA_ID,
    ticker: 'FUNO11',
    nombre: 'Fibra Uno',
    titulos: 1000,
    costoPromedio: 47,
    costoTotalCompra: 47282,
    pctPortafolio: 55.2,
    precioActual: 52.5,
    valorMercado: 52500,
    plusvaliaFilaPct: 11.09,
    plusvaliaFilaMxn: 5218,
    rentaAnual: 1600,
    yoc: 3.383952,
    opportunityScore: 72,
    logoUrl: null,
    freshnessStatus: 'fresh',
    capRate: 7.5,
    navPerCbfi: 120,
    ltv: 42,
    noiMargin: 68,
    ffoMargin: 55,
    dailyChangePct: 0.62,
    week52High: 55.2,
    volume: 1234567,
    week52Low: 44.1,
    week52Avg: 49.5,
    fundamentalsPeriod: 'Q3 2025',
    recentDistributions: [
      { paymentDate: '2026-05-15', amountPerUnit: 0.4 },
      { paymentDate: '2026-02-15', amountPerUnit: 0.4 },
      { paymentDate: '2025-11-15', amountPerUnit: 0.4 },
      { paymentDate: '2025-08-15', amountPerUnit: 0.4 },
    ],
  },
  {
    fibraId: PORTFOLIO_FIBRA_ID_2,
    ticker: 'DANHOS13',
    nombre: 'Fibra Danhos',
    titulos: 500,
    costoPromedio: 38.5,
    costoTotalCompra: 19365,
    pctPortafolio: 44.8,
    precioActual: 41,
    valorMercado: 20500,
    plusvaliaFilaPct: 5.85,
    plusvaliaFilaMxn: 1135,
    rentaAnual: 700,
    yoc: 3.614769,
    opportunityScore: 58,
    logoUrl: null,
    freshnessStatus: 'fresh',
    capRate: 6.8,
    navPerCbfi: 105,
    ltv: 35,
    noiMargin: 72,
    ffoMargin: 60,
    dailyChangePct: -0.24,
    week52High: 43.8,
    volume: 456789,
    week52Low: 36.5,
    week52Avg: 39.2,
    fundamentalsPeriod: 'Q2 2025',
    recentDistributions: [
      { paymentDate: '2026-01-15', amountPerUnit: 0.35 },
      { paymentDate: '2025-07-15', amountPerUnit: 0.35 },
    ],
  },
]

export const defaultKpis: PortfolioKpisDto = {
  inversionTotal: 66647,
  valorTotal: 73000,
  plusvaliaTotal_Pct: 9.53,
  plusvaliaTotal_Mxn: 6353,
  yieldPortafolio: 3.063014,
  ingresoMensual: 166.67,
  rentasAnualesBrutas: 2236,
  rentasRealesBrutas: 2236,
  pctRentasPortafolio: 3.06,
  isPartial: false,
}

const defaultConfig: PortfolioConfigDto = {
  commissionFactor: 0.005,
}

const defaultPerformance: PortfolioPerformanceResponseDto = {
  portfolioSeries: [
    { date: '2026-05-31', valuePct: 0 },
    { date: '2026-06-01', valuePct: 0.8 },
    { date: '2026-06-02', valuePct: 1.2 },
    { date: '2026-06-03', valuePct: 0.7 },
    { date: '2026-06-04', valuePct: 1.5 },
  ],
  ipcSeries: [
    { date: '2026-05-31', valuePct: 0 },
    { date: '2026-06-01', valuePct: 0.3 },
    { date: '2026-06-02', valuePct: 0.6 },
    { date: '2026-06-03', valuePct: 0.9 },
    { date: '2026-06-04', valuePct: 0.7 },
  ],
  sp500Series: [
    { date: '2026-05-31', valuePct: 0 },
    { date: '2026-06-01', valuePct: 0.4 },
    { date: '2026-06-02', valuePct: 0.8 },
    { date: '2026-06-03', valuePct: 1.4 },
    { date: '2026-06-04', valuePct: 2.1 },
  ],
}

function fulfillJson(route: Route, status: number, body: unknown) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

export interface PortfolioApiOptions {
  positions?: PortfolioPositionDto[]
  kpis?: PortfolioKpisDto | null
  empty?: boolean
  uploadResponse?: Partial<PortfolioUploadResponseDto>
  uploadErrors?: Array<{ rowNumber: number; ticker: string; message: string }>
  snapshot?: PortfolioSnapshotStatusDto
  config?: Partial<PortfolioConfigDto> | null
  performance?: PortfolioPerformanceResponseDto | null
}

export async function mockPortfolioApi(page: Page, options: PortfolioApiOptions = {}) {
  const positions = options.empty ? [] : (options.positions ?? defaultPositions)
  const kpis = options.empty ? null : (options.kpis ?? defaultKpis)
  const portfolio: PortfolioResponseDto = { positions, kpis }

  await page.route('**/api/v1/portfolio', async (route) => {
    if (route.request().method() === 'GET') {
      return fulfillJson(route, 200, portfolio)
    }
    return route.continue()
  })

  await page.route('**/api/v1/portfolio/column-config', (route) => {
    const config: PortfolioColumnConfigDto = { columns: [] }
    return fulfillJson(route, 200, config)
  })

  await page.route('**/api/v1/portfolio/config', (route) => {
    const config: PortfolioConfigDto = {
      ...defaultConfig,
      ...(options.config ?? {}),
    }
    return fulfillJson(route, 200, config)
  })

  await page.route('**/api/v1/portfolio/performance**', (route) => {
    const performance = options.performance ?? defaultPerformance
    return fulfillJson(route, 200, performance)
  })

  await page.route('**/api/v1/portfolio/snapshot', (route) => {
    const snapshot: PortfolioSnapshotStatusDto = options.snapshot ?? {
      hasSnapshot: false,
      archivedAt: null,
    }
    return fulfillJson(route, 200, snapshot)
  })

  await page.route('**/api/v1/portfolio/upload**', async (route) => {
    if (options.uploadErrors && options.uploadErrors.length > 0) {
      return route.fulfill({
        status: 422,
        contentType: 'application/json',
        body: JSON.stringify({ errors: options.uploadErrors }),
      })
    }

    const uploadResponse: PortfolioUploadResponseDto = {
      positionCount: options.uploadResponse?.positionCount ?? positions.length,
      duplicateDetected: options.uploadResponse?.duplicateDetected ?? false,
    }
    return fulfillJson(route, 200, uploadResponse)
  })

  await page.route('**/api/v1/portfolio/positions/**', async (route) => {
    const method = route.request().method()
    if (method === 'PATCH' || method === 'DELETE') {
      return fulfillJson(route, 200, {})
    }
    return route.continue()
  })

  await page.route('**/api/v1/portfolio/archive', async (route) => {
    return fulfillJson(route, 200, {})
  })

  await page.route('**/api/v1/portfolio/restore', async (route) => {
    return fulfillJson(route, 200, {})
  })
}
