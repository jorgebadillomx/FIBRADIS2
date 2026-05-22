import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockNewsApi } from './fixtures/news-api'

// Mock market/snapshots with 3 FIBRAs so the table has sortable/filterable rows
async function mockMarketSnapshotsMulti(page: Parameters<typeof mockCatalogApi>[0]) {
  await page.route('**/api/v1/market/snapshots', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          fibraId: '11111111-1111-1111-1111-111111111111',
          ticker: 'FUNO11',
          lastPrice: 25.0,
          dailyChange: 0.30,
          dailyChangePct: 1.22,
          volume: 1_500_000,
          week52High: 28.1,
          week52Low: 20.8,
          capturedAt: new Date().toISOString(),
          freshnessStatus: 'fresh',
        },
        {
          fibraId: '22222222-2222-2222-2222-222222222222',
          ticker: 'DANHOS13',
          lastPrice: 15.0,
          dailyChange: -0.10,
          dailyChangePct: -0.66,
          volume: 800_000,
          week52High: 18.5,
          week52Low: 12.0,
          capturedAt: new Date().toISOString(),
          freshnessStatus: 'fresh',
        },
        {
          fibraId: '33333333-3333-3333-3333-333333333333',
          ticker: 'FMTY14',
          lastPrice: 20.0,
          dailyChange: 0.05,
          dailyChangePct: 0.25,
          volume: 600_000,
          week52High: 22.0,
          week52Low: 16.5,
          capturedAt: new Date().toISOString(),
          freshnessStatus: 'stale',
        },
      ]),
    }),
  )
  // History endpoint needed by ficha pages (not Home), fallback is fine
  await page.route('**/api/v1/market/fibras/**', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ ticker: 'FUNO11', priceHistory: [], distributions: [], annualizedYield: null }) }),
  )
}

test.describe('Épica 2 — FibraUniverseTable', () => {
  test.beforeEach(async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketSnapshotsMulti(page)
    await mockNewsApi(page)
  })

  test('tabla renderiza las 9 columnas de encabezado', async ({ page }) => {
    await page.goto('/')

    const table = page.getByRole('table')
    await expect(table).toBeVisible()

    for (const heading of ['Emisora', 'Precio', 'Var$', 'Var%', 'Volumen', 'Máx 52S', 'Mín 52S', 'Estado']) {
      await expect(table.getByText(heading)).toBeVisible()
    }
  })

  test('tabla muestra una fila por snapshot recibido', async ({ page }) => {
    await page.goto('/')

    const rows = page.getByRole('table').getByRole('row')
    // 1 header row + 3 data rows
    await expect(rows).toHaveCount(4)
    await expect(page.getByRole('table')).toContainText('FUNO11')
    await expect(page.getByRole('table')).toContainText('DANHOS13')
    await expect(page.getByRole('table')).toContainText('FMTY14')
  })

  test('filtro por ticker reduce las filas visibles', async ({ page }) => {
    await page.goto('/')

    const filterInput = page.getByPlaceholder(/filtrar|buscar|ticker/i)
    await filterInput.fill('FUNO')

    const table = page.getByRole('table')
    await expect(table).toContainText('FUNO11')
    await expect(table).not.toContainText('DANHOS13')
    await expect(table).not.toContainText('FMTY14')
  })

  test('limpiar filtro restaura todas las filas', async ({ page }) => {
    await page.goto('/')

    const filterInput = page.getByPlaceholder(/filtrar|buscar|ticker/i)
    await filterInput.fill('FUNO')
    await filterInput.clear()

    const table = page.getByRole('table')
    await expect(table).toContainText('FUNO11')
    await expect(table).toContainText('DANHOS13')
    await expect(table).toContainText('FMTY14')
  })

  test('filtro sin resultados muestra estado vacío', async ({ page }) => {
    await page.goto('/')

    await page.getByPlaceholder(/filtrar|buscar|ticker/i).fill('XXXXXX')

    const table = page.getByRole('table')
    await expect(table.getByRole('row')).toHaveCount(2) // header + 1 empty-state row
  })

  test('columna Precio es ordenable y el clic cambia la dirección', async ({ page }) => {
    await page.goto('/')

    const precioHeader = page.getByRole('table').getByRole('columnheader', { name: /precio/i })
    await expect(precioHeader).toBeVisible()

    // Primera pulsación → orden ascendente (menor precio primero)
    await precioHeader.click()
    const rows = page.getByRole('table').getByRole('row')
    const firstDataRowAfterAsc = rows.nth(1)
    await expect(firstDataRowAfterAsc).toContainText('DANHOS13') // precio 15.0 es el menor

    // Segunda pulsación → orden descendente (mayor precio primero)
    await precioHeader.click()
    await expect(rows.nth(1)).toContainText('FUNO11') // precio 25.0 es el mayor
  })
})
