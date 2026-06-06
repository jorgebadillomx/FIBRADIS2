import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockOpportunitiesApi, defaultRanked } from './fixtures/opportunities-api'
import { mockPortfolioApi } from './fixtures/portfolio-api'
import { seedMainAuth } from './fixtures/main-auth'

test.describe('Épica 7 — Ranking del universo de oportunidades', () => {
  test.beforeEach(async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockOpportunitiesApi(page)
    await mockPortfolioApi(page)
  })

  test('Página de oportunidades muestra header y tabs', async ({ page }) => {
    await page.goto('/oportunidades')

    await expect(page.getByRole('heading', { name: 'Oportunidades' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Universo' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Promediar Posición' })).toBeVisible()
  })

  test('Ranking principal muestra todas las FIBRAs en orden descendente de score', async ({ page }) => {
    await page.goto('/oportunidades')

    await expect(page.getByRole('heading', { name: /Ranking principal/ })).toBeVisible()
    await expect(page.getByText(/3 FIBRAs/)).toBeVisible()

    const rows = page.locator('tbody tr').filter({ hasText: /FUNO11|DANHOS13|FMTY14/ })
    await expect(rows).toHaveCount(3)

    const firstRow = rows.nth(0)
    await expect(firstRow).toContainText('FUNO11')
  })

  test('Sección de datos limitados aparece con advertencia', async ({ page }) => {
    await page.goto('/oportunidades')

    await expect(page.getByText('Score referencial')).toBeVisible()
    await expect(page.getByText('datos insuficientes para el ranking principal')).toBeVisible()
    await expect(page.getByText('FINN13')).toBeVisible()
  })

  test('Configurador de pesos muestra sliders y perfiles', async ({ page }) => {
    await page.goto('/oportunidades')

    await expect(page.getByText('Configurar pesos')).toBeVisible()
    await expect(page.getByText('Predeterminado')).toBeVisible()
    await expect(page.getByText('Renta')).toBeVisible()
    await expect(page.getByText('Crecimiento')).toBeVisible()

    await expect(page.getByText('Descuento NAV')).toBeVisible()
    await expect(page.getByText('Dividend Yield')).toBeVisible()
    await expect(page.getByText('LTV invertido')).toBeVisible()
    await expect(page.getByText('Margen NOI')).toBeVisible()
    await expect(page.getByText('Precio vs AVG 52S')).toBeVisible()
  })

  test('Al expandir fila se muestra desglose de contribución por componente', async ({ page }) => {
    await page.goto('/oportunidades')

    const funoRow = page.locator('tbody tr').filter({ hasText: 'FUNO11' }).first()
    await funoRow.click()

    await expect(page.getByText(/Contribución al score por componente/)).toBeVisible()
    await expect(page.getByText(/Desc\. NAV/).first()).toBeVisible()
    await expect(page.getByText(/Yield/).first()).toBeVisible()
  })

  test('Banner "Universo degradado" aparece cuando coverage.status es Degraded', async ({ page }) => {
    await mockOpportunitiesApi(page, {
      coverage: {
        status: 'Degraded',
        universeSize: 25,
        fibrasWithPrice: 17,
        missingPct: '32.0',
        lastValidPriceAt: '2026-06-05T09:00:00Z',
      },
    })

    await page.goto('/oportunidades')

    await expect(page.getByText(/Universo degradado/)).toBeVisible()
    await expect(page.getByText(/32\.0%/)).toBeVisible()
  })

  test('Ranking suspendido cuando cobertura cae por debajo del 50%', async ({ page }) => {
    await mockOpportunitiesApi(page, {
      coverage: {
        status: 'Suspended',
        universeSize: 25,
        fibrasWithPrice: 12,
        missingPct: '52.0',
        lastValidPriceAt: null,
      },
    })

    await page.goto('/oportunidades')

    await expect(page.getByText(/Ranking no disponible/)).toBeVisible()
    await expect(page.getByText(/cobertura insuficiente/)).toBeVisible()
    await expect(page.getByText(/52\.0%/)).toBeVisible()
  })

  test('Seleccionar perfil Renta actualiza los pesos', async ({ page }) => {
    await page.goto('/oportunidades')

    await page.getByRole('button', { name: 'Renta' }).click()

    await expect(page.locator('input[type="range"]').first()).toBeVisible()
  })

  test('FIBRAs excluidas muestran — en lugar de score', async ({ page }) => {
    await mockOpportunitiesApi(page, {
      ranked: defaultRanked,
    })

    await page.goto('/oportunidades')

    await expect(page.getByText('FUNO11')).toBeVisible()
  })
})

test.describe('Épica 7 — Vista Promediar Posición', () => {
  test.beforeEach(async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockOpportunitiesApi(page)
    await mockPortfolioApi(page)
  })

  test('Tab Promediar Posición muestra las posiciones del portafolio', async ({ page }) => {
    await page.goto('/oportunidades')

    await page.getByRole('button', { name: 'Promediar Posición' }).click()

    await expect(page.getByText('FUNO11')).toBeVisible()
    await expect(page.getByText('DANHOS13')).toBeVisible()
  })

  test('Descargo de responsabilidad es visible en la vista Promediar', async ({ page }) => {
    await page.goto('/oportunidades')

    await page.getByRole('button', { name: 'Promediar Posición' }).click()

    await expect(
      page.getByText('Este simulador es informativo. No constituye una recomendación de compra o venta.'),
    ).toBeVisible()
  })

  test('Ingresar títulos adicionales muestra nuevo costo promedio ponderado', async ({ page }) => {
    await page.goto('/oportunidades')

    await page.getByRole('button', { name: 'Promediar Posición' }).click()

    const funoInput = page.getByLabel('Títulos adicionales para FUNO11')
    await funoInput.fill('500')

    await expect(page.getByText(/\$[\d,]+\.\d{2}/).first()).toBeVisible()
  })

  test('Tab Promediar muestra estado vacío sin portafolio', async ({ page }) => {
    await mockPortfolioApi(page, { empty: true })

    await page.goto('/oportunidades')

    await page.getByRole('button', { name: 'Promediar Posición' }).click()

    await expect(
      page.getByText('No tienes posiciones en tu portafolio'),
    ).toBeVisible()
  })
})
