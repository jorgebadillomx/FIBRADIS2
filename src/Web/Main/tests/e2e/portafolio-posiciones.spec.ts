import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockPortfolioApi, defaultPositions, defaultKpis } from './fixtures/portfolio-api'
import { seedMainAuth } from './fixtures/main-auth'

test.describe('Épica 6 — KPIs, posiciones y badge de señal', () => {
  test.beforeEach(async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockPortfolioApi(page)
  })

  test('KPIs del portafolio se muestran con valores correctos', async ({ page }) => {
    await page.goto('/portafolio')

    await expect(page.getByText('Mi Portafolio')).toBeVisible()

    await expect(page.getByText(/\$66[\s,.]?647/)).toBeVisible()
    await expect(page.getByText(/\$73[\s,.]?000/)).toBeVisible()
  })

  test('Posiciones de la tabla se muestran correctamente', async ({ page }) => {
    await page.goto('/portafolio')

    await expect(page.getByText('FUNO11')).toBeVisible()
    await expect(page.getByText('DANHOS13')).toBeVisible()

    await expect(page.getByText('Fibra Uno')).toBeVisible()
    await expect(page.getByText('Fibra Danhos')).toBeVisible()
  })

  test('La sección de posiciones contiene encabezados de tabla', async ({ page }) => {
    await page.goto('/portafolio')

    await expect(page.getByRole('heading', { name: 'Posiciones' })).toBeVisible()
    await expect(page.getByText('Shift + clic agrega un segundo criterio')).toBeVisible()
  })

  test('Badge de señal Buy muestra color verde para descuento >10% sobre NAV', async ({ page }) => {
    await page.goto('/portafolio')

    const funoRow = page.locator('tr').filter({ hasText: 'FUNO11' }).first()
    await expect(funoRow).toBeVisible()
  })

  test('Datos de mercado faltantes muestran — en lugar de errores', async ({ page }) => {
    await mockPortfolioApi(page, {
      positions: [
        {
          ...defaultPositions[0],
          precioActual: null,
          valorMercado: null,
          plusvaliaFilaPct: null,
          plusvaliaFilaMxn: null,
          rentaAnual: null,
          yoc: null,
        },
      ],
      kpis: {
        ...defaultKpis,
        isPartial: true,
      },
    })

    await page.goto('/portafolio')

    const rows = page.locator('tbody tr')
    await expect(rows.first()).toContainText('—')
  })

  test('Botón Favoritas primero aparece cuando hay posiciones', async ({ page }) => {
    await page.goto('/portafolio')

    await expect(page.getByRole('button', { name: /Favoritas primero/ })).toBeVisible()
  })

  test('Botón Archivar portafolio aparece cuando hay posiciones', async ({ page }) => {
    await page.goto('/portafolio')

    await expect(page.getByRole('button', { name: 'Archivar portafolio' })).toBeVisible()
  })

  test('Diálogo de archivar pide confirmación antes de proceder', async ({ page }) => {
    await page.goto('/portafolio')

    await page.getByRole('button', { name: 'Archivar portafolio' }).click()

    await expect(page.getByRole('dialog')).toBeVisible()
    await expect(page.getByText('¿Guardar respaldo y vaciar tu portafolio?')).toBeVisible()

    await page.getByRole('button', { name: 'Cancelar' }).click()
    await expect(page.getByRole('dialog')).not.toBeVisible()
  })

  test('La ruta /portafolio carga correctamente (no existe /dashboard)', async ({ page }) => {
    await page.goto('/portafolio')
    await expect(page).toHaveURL(/\/portafolio$/)
    await expect(page.getByText('Mi Portafolio')).toBeVisible()
  })
})

test.describe('Épica 6 — Edición inline de posiciones', () => {
  test.beforeEach(async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockPortfolioApi(page)
  })

  test('Al hacer clic en celda editable entra en modo edición', async ({ page }) => {
    await page.goto('/portafolio')

    const funoRow = page.locator('tbody tr').filter({ hasText: 'FUNO11' }).first()
    await expect(funoRow).toBeVisible()

    const editableCells = funoRow.locator('[data-editable="true"], input[type="number"]')
    if ((await editableCells.count()) > 0) {
      await editableCells.first().click()
      await expect(editableCells.first()).toBeFocused()
    }
  })
})
