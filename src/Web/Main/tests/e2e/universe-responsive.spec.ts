import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockNewsApi } from './fixtures/news-api'

async function mockUniversePageData(page: Parameters<typeof mockCatalogApi>[0]) {
  await page.route('**/api/v1/market/snapshots', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          fibraId: '11111111-1111-1111-1111-111111111111',
          ticker: 'FUNO11',
          lastPrice: 25,
          dailyChange: 0.3,
          dailyChangePct: 1.22,
          volume: 1_500_000,
          week52High: 28.1,
          week52Low: 20.8,
          annualizedYield: 4.63,
          capturedAt: new Date().toISOString(),
          freshnessStatus: 'fresh',
        },
        {
          fibraId: '22222222-2222-2222-2222-222222222222',
          ticker: 'DANHOS13',
          lastPrice: 15,
          dailyChange: -0.1,
          dailyChangePct: -0.66,
          volume: 800_000,
          week52High: 18.5,
          week52Low: 12,
          annualizedYield: 5.2,
          capturedAt: new Date().toISOString(),
          freshnessStatus: 'fresh',
        },
        {
          fibraId: '33333333-3333-3333-3333-333333333333',
          ticker: 'FMTY14',
          lastPrice: 20,
          dailyChange: 0.05,
          dailyChangePct: 0.25,
          volume: 600_000,
          week52High: 22,
          week52Low: 16.5,
          annualizedYield: 4.1,
          capturedAt: new Date().toISOString(),
          freshnessStatus: 'stale',
        },
      ]),
    }),
  )

  await page.route('**/api/v1/fundamentals/summary**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { ticker: 'FUNO11', period: 'Q4-2025' },
        { ticker: 'DANHOS13', period: 'Q4-2025' },
        { ticker: 'FMTY14', period: 'Q4-2025' },
      ]),
    }),
  )
}

test.describe('FibraUniverseTable responsive', () => {
  test.beforeEach(async ({ page }) => {
    await mockCatalogApi(page)
    await mockNewsApi(page)
    await mockUniversePageData(page)
  })

  test('en 375px no genera overflow horizontal y expande detalles', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 915 })
    await page.goto('/')

    await expect(page.locator('[data-testid="universe-mobile-card"]')).toHaveCount(3)

    const metrics = await page.evaluate(() => ({
      scrollWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
      innerWidth: window.innerWidth,
    }))
    expect(metrics.scrollWidth).toBeLessThanOrEqual(metrics.innerWidth)

    const expandButton = page.getByLabel('Expandir detalles de FUNO11')
    await expandButton.click()

    const expandedCard = page.locator('#universe-mobile-FUNO11')
    await expect(expandedCard).toContainText('Volumen')
    await expect(expandedCard).toContainText('Rango 52S')
    await expect(expandedCard).toContainText('Yield')

    // colapsar oculta de nuevo el detalle
    await page.getByLabel('Colapsar detalles de FUNO11').click()
    await expect(page.locator('#universe-mobile-FUNO11')).toHaveCount(0)
  })

  test('en 1024px oculta las cards móviles y muestra la tabla sin overflow', async ({ page }) => {
    await page.setViewportSize({ width: 1024, height: 900 })
    await page.goto('/')

    await expect(page.locator('[data-testid="universe-mobile-card"]').first()).toBeHidden()
    await expect(page.getByText('Emisora', { exact: true })).toBeVisible()

    const metrics = await page.evaluate(() => ({
      scrollWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
      innerWidth: window.innerWidth,
    }))
    expect(metrics.scrollWidth).toBeLessThanOrEqual(metrics.innerWidth)
  })
})
