import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockMarketApi } from './fixtures/market-api'
import { mockNewsApi } from './fixtures/news-api'

test.describe('Noticias con AI summary', () => {
  test.beforeEach(async ({ page }) => {
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await mockNewsApi(page)
  })

  test('Home muestra aiSummary cuando existe y snippet cuando no hay resumen', async ({ page }) => {
    await page.goto('/')

    const newsSection = page.getByLabel('Noticias recientes')
    await expect(newsSection.getByRole('link', { name: 'FUNO11 concreta refinanciamiento estratégico' })).toBeVisible()
    await expect(newsSection.getByText('Resumen IA: FUNO11 refinanció pasivos para extender vencimientos y preservar liquidez.')).toBeVisible()

    await expect(newsSection.getByRole('link', { name: 'FMTY14 reporta nueva ocupación industrial' })).toBeVisible()
    await expect(newsSection.getByText('La ocupación industrial aumentó durante el trimestre.')).toBeVisible()
  })

  test('Ficha pública de la FIBRA mantiene fallback a snippet cuando aiSummary es null', async ({ page }) => {
    await page.goto('/fibras/FUNO11')

    const noticiasSection = page.locator('section#noticias')
    await expect(noticiasSection.getByRole('heading', { name: 'Noticias' })).toBeVisible()
    await expect(noticiasSection.getByText('Resumen IA: FUNO11 busca reciclar capital y concentrarse en activos con mayor rentabilidad.')).toBeVisible()
    await expect(noticiasSection.getByText('El consenso ajustó estimados tras la última guía trimestral.')).toBeVisible()
  })
})
