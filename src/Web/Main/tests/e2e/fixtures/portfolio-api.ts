import type { Page, Route } from '@playwright/test'

type PortfolioPositionDto = {
  fibraId: string
  ticker: string
  nombre: string
  titulos: number
  costoPromedio: string
  costoTotalCompra: string
  precioActual: string | null
  valorMercado: string | null
  plusvaliaPct: string | null
  plusvaliaAbsoluta: string | null
  rentaAnualBruta: string | null
  rentaRealBruta: string | null
  pctPortafolio: string | null
  signal: 'Buy' | 'Hold' | 'Sell' | null
  navPerCbfi: string | null
  capRate: string | null
  ltv: string | null
  noiMargin: string | null
  ffoMargin: string | null
  periodo: string | null
  distribucionTrimestral: string | null
  yieldCalculadoPct: string | null
  avg52w: string | null
  high52w: string | null
  low52w: string | null
  dailyChangePct: string | null
  volume: number | null
}

type PortfolioKpisDto = {
  inversionTotal: string
  valorTotal: string
  plusvaliaPct: string
  plusvaliaAbsoluta: string
  rentasAnualesBrutas: string
  rentasRealesBrutas: string
  pctRentasPortafolio: string
  hasPartialData: boolean
}

type PortfolioResponseDto = {
  positions: PortfolioPositionDto[]
  kpis: PortfolioKpisDto | null
}

type PortfolioSnapshotStatusDto = {
  hasSnapshot: boolean
  archivedAt: string | null
}

type PortfolioUploadResponseDto = {
  positionCount: number
  duplicateDetected: boolean
}

type PortfolioColumnConfigDto = {
  columns: string[]
}

export const PORTFOLIO_FIBRA_ID = '11111111-1111-1111-1111-111111111111'
export const PORTFOLIO_FIBRA_ID_2 = '22222222-2222-2222-2222-222222222222'

export const defaultPositions: PortfolioPositionDto[] = [
  {
    fibraId: PORTFOLIO_FIBRA_ID,
    ticker: 'FUNO11',
    nombre: 'Fibra Uno',
    titulos: 1000,
    costoPromedio: '47.00',
    costoTotalCompra: '47282.00',
    precioActual: '52.50',
    valorMercado: '52500.00',
    plusvaliaPct: '11.09',
    plusvaliaAbsoluta: '5218.00',
    rentaAnualBruta: '1536.00',
    rentaRealBruta: '1536.00',
    pctPortafolio: '55.20',
    signal: 'Buy',
    navPerCbfi: '120.00',
    capRate: '7.5',
    ltv: '42.0',
    noiMargin: '68.0',
    ffoMargin: '55.0',
    periodo: 'Q3 2025',
    distribucionTrimestral: '0.384',
    yieldCalculadoPct: '4.63',
    avg52w: '49.50',
    high52w: '55.20',
    low52w: '44.10',
    dailyChangePct: '0.62',
    volume: 1234567,
  },
  {
    fibraId: PORTFOLIO_FIBRA_ID_2,
    ticker: 'DANHOS13',
    nombre: 'Fibra Danhos',
    titulos: 500,
    costoPromedio: '38.50',
    costoTotalCompra: '19365.00',
    precioActual: '41.00',
    valorMercado: '20500.00',
    plusvaliaPct: '5.85',
    plusvaliaAbsoluta: '1135.00',
    rentaAnualBruta: '700.00',
    rentaRealBruta: '700.00',
    pctPortafolio: '44.80',
    signal: 'Hold',
    navPerCbfi: '105.00',
    capRate: '6.8',
    ltv: '35.0',
    noiMargin: '72.0',
    ffoMargin: '60.0',
    periodo: 'Q2 2025',
    distribucionTrimestral: '0.35',
    yieldCalculadoPct: '3.54',
    avg52w: '39.20',
    high52w: '43.80',
    low52w: '36.50',
    dailyChangePct: '-0.24',
    volume: 456789,
  },
]

export const defaultKpis: PortfolioKpisDto = {
  inversionTotal: '66647.00',
  valorTotal: '73000.00',
  plusvaliaPct: '9.53',
  plusvaliaAbsoluta: '6353.00',
  rentasAnualesBrutas: '2236.00',
  rentasRealesBrutas: '2236.00',
  pctRentasPortafolio: '3.06',
  hasPartialData: false,
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
