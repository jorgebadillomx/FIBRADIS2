import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockMarketApi } from './fixtures/market-api'

test.describe('Sección de historial de precios en ficha de FIBRA', () => {
  test('muestra los 4 botones de período de historial', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await page.goto('/fibras/FUNO11')

    await expect(page.getByRole('button', { name: '1M' })).toBeVisible()
    await expect(page.getByRole('button', { name: '3M' })).toBeVisible()
    await expect(page.getByRole('button', { name: '6M' })).toBeVisible()
    await expect(page.getByRole('button', { name: '1A' })).toBeVisible()
  })

  test('el botón 1M está activo por defecto', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await page.goto('/fibras/FUNO11')

    await expect(page.getByRole('button', { name: '1M' })).toHaveAttribute('aria-pressed', 'true')
    await expect(page.getByRole('button', { name: '3M' })).toHaveAttribute('aria-pressed', 'false')
    await expect(page.getByRole('button', { name: '6M' })).toHaveAttribute('aria-pressed', 'false')
    await expect(page.getByRole('button', { name: '1A' })).toHaveAttribute('aria-pressed', 'false')
  })

  test('clic en 3M actualiza el período activo', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await page.goto('/fibras/FUNO11')

    await page.getByRole('button', { name: '3M' }).click()

    await expect(page.getByRole('button', { name: '3M' })).toHaveAttribute('aria-pressed', 'true')
    await expect(page.getByRole('button', { name: '1M' })).toHaveAttribute('aria-pressed', 'false')
  })

  test('clic en 1A actualiza el período activo', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await page.goto('/fibras/FUNO11')

    await page.getByRole('button', { name: '1A' }).click()

    await expect(page.getByRole('button', { name: '1A' })).toHaveAttribute('aria-pressed', 'true')
    await expect(page.getByRole('button', { name: '1M' })).toHaveAttribute('aria-pressed', 'false')
  })

  test('muestra los labels de métricas de semana 52 y volumen', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await page.goto('/fibras/FUNO11')

    await expect(page.getByText('Máx. 52 sem.')).toBeVisible()
    await expect(page.getByText('Mín. 52 sem.')).toBeVisible()
    await expect(page.getByText('Volumen')).toBeVisible()
  })

  test('muestra los valores numéricos de las métricas cuando hay datos', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await page.goto('/fibras/FUNO11')

    // week52High = 28.1, week52Low = 20.8, volume = 1,234,567 → "1.2M"
    await expect(page.getByText('28.10')).toBeVisible()
    await expect(page.getByText('20.80')).toBeVisible()
    await expect(page.getByText('1.2M')).toBeVisible()
  })

  test('muestra guiones en métricas cuando no hay datos de mercado', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, null, null)
    await page.goto('/fibras/FUNO11')

    // Sin datos: week52High, week52Low, volume aparecen como "—"
    const dashes = page.locator('p.tabular-nums', { hasText: '—' })
    await expect(dashes.first()).toBeVisible()
  })

  test('muestra sección mercado y no muestra error de historial', async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await page.goto('/fibras/FUNO11')

    await expect(page.locator('section#mercado')).toBeVisible()

    // Con datos mocked correctamente, no debe aparecer el mensaje de error
    await expect(page.getByText('Error al cargar historial de precios')).not.toBeVisible()
  })
})
