import type { Page, Route } from '@playwright/test'

export type FreshnessStatus = 'fresh' | 'stale' | 'off-hours' | 'critical'

type MarketSnapshot = {
  fibraId: string
  ticker: string
  lastPrice: number | null
  dailyChange: number | null
  dailyChangePct: number | null
  volume: number | null
  week52High: number | null
  week52Low: number | null
  capturedAt: string | null
  freshnessStatus: FreshnessStatus | null
}

type PricePoint = { date: string; close: number | null }
type DistributionPoint = { date: string; amountPerUnit: number }
type FibraHistory = {
  ticker: string
  priceHistory: PricePoint[]
  distributions: DistributionPoint[]
  annualizedYield: number | null
}

function buildSnapshot(
  ticker: string,
  freshnessStatus: FreshnessStatus | null,
  lastPrice: number | null,
): MarketSnapshot {
  const hasData = lastPrice != null && freshnessStatus != null
  return {
    fibraId: '11111111-1111-1111-1111-111111111111',
    ticker,
    lastPrice: hasData ? lastPrice : null,
    dailyChange: hasData ? 0.15 : null,
    dailyChangePct: hasData ? 0.62 : null,
    volume: hasData ? 1_234_567 : null,
    week52High: hasData ? 28.1 : null,
    week52Low: hasData ? 20.8 : null,
    capturedAt: hasData ? new Date().toISOString() : null,
    freshnessStatus,
  }
}

function buildHistory(ticker: string): FibraHistory {
  const today = new Date()
  const priceHistory: PricePoint[] = Array.from({ length: 30 }, (_, i) => ({
    date: new Date(today.getTime() - (29 - i) * 86_400_000).toISOString().slice(0, 10),
    close: parseFloat((24 + Math.random()).toFixed(2)),
  }))
  return {
    ticker,
    priceHistory,
    distributions: [
      { date: '2025-12-15', amountPerUnit: 0.384 },
      { date: '2025-09-15', amountPerUnit: 0.378 },
      { date: '2025-06-16', amountPerUnit: 0.372 },
    ],
    annualizedYield: 0.0463,
  }
}

function fulfillJson(route: Route, status: number, body: unknown) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

export async function mockMarketApi(
  page: Page,
  freshnessStatus: FreshnessStatus | null = 'fresh',
  lastPrice: number | null = 24.5,
) {
  await page.route('**/api/v1/market/snapshots', (route) =>
    fulfillJson(route, 200, [buildSnapshot('FUNO11', freshnessStatus, lastPrice)]),
  )

  await page.route('**/api/v1/market/fibras/**', (route) => {
    const url = new URL(route.request().url())
    const parts = url.pathname.split('/')
    const idx = parts.indexOf('fibras')
    const ticker = (idx >= 0 ? parts[idx + 1] : 'FUNO11')?.toUpperCase() ?? 'FUNO11'
    return fulfillJson(route, 200, buildHistory(ticker))
  })
}
