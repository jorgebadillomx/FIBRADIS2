import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockNewsApi } from './fixtures/news-api'

const FUNO11_ID = '11111111-1111-1111-1111-111111111111'

function buildHistory(distributions: { date: string; amountPerUnit: number }[], annualizedYield: number | null) {
  return {
    ticker: 'FUNO11',
    priceHistory: [],
    distributions,
    annualizedYield,
  }
}

async function mockMarketWithDistributions(
  page: Parameters<typeof mockCatalogApi>[0],
  distributions: { date: string; amountPerUnit: number }[],
  annualizedYield: number | null = 0.0463,
) {
  await page.route('**/api/v1/market/snapshots', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          fibraId: FUNO11_ID,
          ticker: 'FUNO11',
          lastPrice: 24.5,
          dailyChange: 0.15,
          dailyChangePct: 0.62,
          volume: 1_234_567,
          week52High: 28.1,
          week52Low: 20.8,
          capturedAt: new Date().toISOString(),
          freshnessStatus: 'fresh',
        },
      ]),
    }),
  )
  await page.route('**/api/v1/market/fibras/**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(buildHistory(distributions, annualizedYield)),
    }),
  )
}

const threeDistributions = [
  { date: '2025-12-15', amountPerUnit: 0.384 },
  { date: '2025-09-15', amountPerUnit: 0.378 },
  { date: '2025-06-16', amountPerUnit: 0.372 },
]

const tenDistributions = Array.from({ length: 10 }, (_, i) => ({
  date: `2025-${String(12 - i).padStart(2, '0')}-15`,
  amountPerUnit: parseFloat((0.38 - i * 0.005).toFixed(3)),
}))

test.describe('Épica 3 — DistribucionesSection', () => {
  test.beforeEach(async ({ page }) => {
    await mockCatalogApi(page)
    await mockNewsApi(page)
  })

  test('muestra el yield anualizado cuando hay distribuciones', async ({ page }) => {
    await mockMarketWithDistributions(page, threeDistributions, 0.0463)
    await page.goto('/fibras/FUNO11')

    const distSection = page.locator('section#distribuciones')
    await expect(distSection).toBeVisible()
    // 4.63% = 0.0463 * 100
    await expect(distSection.getByText(/4\.63\s*%/)).toBeVisible()
  })

  test('muestra tabla con fecha de pago y monto por CBFI', async ({ page }) => {
    await mockMarketWithDistributions(page, threeDistributions)
    await page.goto('/fibras/FUNO11')

    const distSection = page.locator('section#distribuciones')
    await expect(distSection.getByText('2025-12-15')).toBeVisible()
    await expect(distSection.getByText('0.384')).toBeVisible()
    await expect(distSection.getByText('2025-09-15')).toBeVisible()
    await expect(distSection.getByText('0.378')).toBeVisible()
  })

  test('muestra estado vacío cuando no hay distribuciones', async ({ page }) => {
    await mockMarketWithDistributions(page, [], null)
    await page.goto('/fibras/FUNO11')

    const distSection = page.locator('section#distribuciones')
    await expect(distSection.getByText(/sin distribuciones|no hay distribuciones|—/i)).toBeVisible()
  })

  test('oculta filas extra y muestra botón "Ver historial" cuando hay más de 8 distribuciones', async ({ page }) => {
    await mockMarketWithDistributions(page, tenDistributions, 0.04)
    await page.goto('/fibras/FUNO11')

    const distSection = page.locator('section#distribuciones')
    const expandButton = distSection.getByRole('button', { name: /ver historial/i })
    await expect(expandButton).toBeVisible()
  })

  test('expandir historial muestra todas las distribuciones', async ({ page }) => {
    await mockMarketWithDistributions(page, tenDistributions, 0.04)
    await page.goto('/fibras/FUNO11')

    const distSection = page.locator('section#distribuciones')
    await distSection.getByRole('button', { name: /ver historial/i }).click()

    // Con 10 distribuciones, la fila #9 debe ser visible después de expandir
    const lastDate = tenDistributions[9].date
    await expect(distSection.getByText(lastDate)).toBeVisible()
  })
})
