import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockFaqApi } from './fixtures/faq-api'
import { mockFundamentalsApi } from './fixtures/fundamentals-api'

test.describe('FundamentalesPage responsive', () => {
  test.beforeEach(async ({ page }) => {
    await mockCatalogApi(page)
    await mockFundamentalsApi(page)
    await mockFaqApi(page, {})
  })

  test('en 375px no genera overflow horizontal y expande detalles', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 915 })
    await page.goto('/fundamentales')

    await expect(page.locator('[data-testid="fundamentales-mobile-card"]')).toHaveCount(1)

    const metrics = await page.evaluate(() => ({
      scrollWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
      innerWidth: window.innerWidth,
    }))
    expect(metrics.scrollWidth).toBeLessThanOrEqual(metrics.innerWidth)

    const expandButton = page.getByLabel('Expandir detalles de FUNO11')
    await expandButton.click()

    const expandedCard = page.locator('#fundamentales-mobile-FUNO11-Q4-2025')
    await expect(expandedCard).toContainText('LTV')
    await expect(expandedCard).toContainText('NOI Margin')
    await expect(expandedCard).toContainText('Dist. Trimestral')

    // colapsar oculta de nuevo el detalle
    await page.getByLabel('Colapsar detalles de FUNO11').click()
    await expect(page.locator('#fundamentales-mobile-FUNO11-Q4-2025')).toHaveCount(0)
  })

  test('en 1024px oculta las cards móviles y muestra la tabla sin overflow', async ({ page }) => {
    await page.setViewportSize({ width: 1024, height: 900 })
    await page.goto('/fundamentales')

    await expect(page.locator('[data-testid="fundamentales-mobile-card"]').first()).toBeHidden()
    await expect(page.getByRole('columnheader', { name: 'FIBRA', exact: true })).toBeVisible()

    const metrics = await page.evaluate(() => ({
      scrollWidth: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
      innerWidth: window.innerWidth,
    }))
    expect(metrics.scrollWidth).toBeLessThanOrEqual(metrics.innerWidth)
  })
})
