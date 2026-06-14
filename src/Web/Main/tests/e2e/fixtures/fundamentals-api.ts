import type { Page, Route } from '@playwright/test'

function fulfillJson(route: Route, status: number, body: unknown) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

export async function mockFundamentalsApi(page: Page) {
  await page.route('**/api/v1/fundamentals/summary**', (route) =>
    fulfillJson(route, 200, [
      {
        ticker: 'FUNO11',
        name: 'Fibra Uno',
        period: 'Q4-2025',
        capRate: 0.071,
        navPerCbfi: 25.4,
        ltv: 0.31,
        noiMargin: 0.72,
        ffoMargin: 0.64,
        quarterlyDistribution: 0.38,
      },
    ]),
  )

  await page.route('**/api/v1/fundamentals/periods', (route) =>
    fulfillJson(route, 200, ['Q4-2025', 'Q3-2025']),
  )
}
