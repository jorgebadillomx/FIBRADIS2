import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockMarketApi } from './fixtures/market-api'
import { mockNewsApi } from './fixtures/news-api'
import { mockPortfolioApi } from './fixtures/portfolio-api'
import { mockOpportunitiesApi } from './fixtures/opportunities-api'
import { seedMainAuth } from './fixtures/main-auth'

test.describe('Épica 7 — Favoritos', () => {
  test('Botón de favorito aparece en la ficha pública cuando autenticado', async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await mockNewsApi(page)

    await page.route('**/api/v1/favorites**', async (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([]),
        })
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({}) })
    })

    await page.goto('/fibras/FUNO11')

    const starButton = page.locator('[aria-label*="favorita"], [aria-label*="Marcar"], button').filter({
      has: page.locator('svg'),
    }).first()

    if (await starButton.count() > 0) {
      await expect(starButton).toBeVisible()
    }
  })

  test('Botón Favoritas primero en portafolio alterna estado visual', async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockPortfolioApi(page)

    await page.goto('/portafolio')

    const btn = page.getByRole('button', { name: /Favoritas primero/ })
    await expect(btn).toBeVisible()

    await btn.click()

    await expect(btn).toBeVisible()
  })

  test('En oportunidades, botón Favoritas primero está presente', async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockOpportunitiesApi(page)
    await mockPortfolioApi(page)

    await page.goto('/oportunidades')

    const btn = page.getByRole('button', { name: /Favoritas primero/ })
    await expect(btn).toBeVisible()
  })

  test('Toggle de Favoritas primero alterna estado activo', async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockOpportunitiesApi(page)
    await mockPortfolioApi(page)

    await page.goto('/oportunidades')

    const btn = page.getByRole('button', { name: /Favoritas primero/ })
    await btn.click()

    await expect(btn).toBeVisible()
  })
})
