import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockMarketApi } from './fixtures/market-api'

test.describe('FreshnessBadge en ficha de FIBRA', () => {
  test('muestra badge Fresh cuando freshnessStatus es fresh', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await page.goto('/fibras/FUNO11')

    await expect(page.getByText('Fresh').first()).toBeVisible()
  })

  test('muestra badge Stale cuando freshnessStatus es stale', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'stale', 24.5)
    await page.goto('/fibras/FUNO11')

    await expect(page.getByText('Stale').first()).toBeVisible()
  })

  test('muestra badge Fuera de horario cuando freshnessStatus es off-hours', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'off-hours', 24.5)
    await page.goto('/fibras/FUNO11')

    await expect(page.getByText('Fuera de horario').first()).toBeVisible()
  })

  test('muestra badge Crítico cuando freshnessStatus es critical', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'critical', 24.5)
    await page.goto('/fibras/FUNO11')

    await expect(page.getByText('Crítico').first()).toBeVisible()
  })

  test('no muestra ningún badge cuando freshnessStatus es null', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, null, null)
    await page.goto('/fibras/FUNO11')

    await expect(page.getByText('Fresh')).not.toBeVisible()
    await expect(page.getByText('Stale')).not.toBeVisible()
    await expect(page.getByText('Crítico')).not.toBeVisible()
    await expect(page.getByText('Fuera de horario')).not.toBeVisible()
  })

  test('muestra el precio junto con el badge de frescura', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await page.goto('/fibras/FUNO11')

    await expect(page.getByText('24.50').first()).toBeVisible()
    await expect(page.getByText('Fresh').first()).toBeVisible()
  })

  test('sin datos de mercado muestra guión en lugar de precio', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, null, null)
    await page.goto('/fibras/FUNO11')

    // El PrecioSection muestra "—" cuando hasMarketPrice es false
    const dashes = page.locator('text=—')
    await expect(dashes.first()).toBeVisible()
  })
})
