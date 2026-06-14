import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockEditorialPagesApi } from './fixtures/editorial-pages-api'
import { mockFaqApi } from './fixtures/faq-api'
import { mockFundamentalsApi } from './fixtures/fundamentals-api'

test.describe('FAQ accordion público', () => {
  test('Conoce las FIBRAs muestra preguntas frecuentes visibles y accesibles', async ({ page }) => {
    await mockEditorialPagesApi(page)
    await mockFaqApi(page, {
      'StaticPage|/conoce-las-fibras': [
        {
          id: 'faq-1',
          pageType: 'StaticPage',
          entityKey: '/conoce-las-fibras',
          question: '¿Qué son las FIBRAs?',
          answer: 'Son fideicomisos de inversión en bienes raíces que cotizan en bolsa.',
          order: 1,
          isActive: true,
          updatedAt: '2026-01-01T00:00:00Z',
          updatedBy: 'system',
        },
        {
          id: 'faq-2',
          pageType: 'StaticPage',
          entityKey: '/conoce-las-fibras',
          question: '¿Cuál es la historia de las FIBRAs?',
          answer: 'Nacieron para canalizar capital hacia el sector inmobiliario.',
          order: 2,
          isActive: true,
          updatedAt: '2026-01-01T00:00:00Z',
          updatedBy: 'system',
        },
      ],
    })

    await page.goto('/conoce-las-fibras')

    await expect(page.getByRole('button', { name: /¿Qué son las FIBRAs\?/i })).toHaveAttribute('aria-expanded', 'true')
    await expect(page.getByRole('region', { name: /¿Qué son las FIBRAs\?/i })).toContainText('Son fideicomisos de inversión en bienes raíces')
    await expect(page.getByText('FAQ editorial')).toBeVisible()
  })

  test('Fundamentales muestra FAQ de métricas con el primer item expandido', async ({ page }) => {
    await mockCatalogApi(page)
    await mockFundamentalsApi(page)
    await mockFaqApi(page, {
      'StaticPage|/fundamentales': [
        {
          id: 'faq-3',
          pageType: 'StaticPage',
          entityKey: '/fundamentales',
          question: '¿Qué es Cap Rate?',
          answer: 'Cap Rate = NOI anualizado / Valor de propiedades de inversión',
          order: 1,
          isActive: true,
          updatedAt: '2026-01-01T00:00:00Z',
          updatedBy: 'system',
        },
        {
          id: 'faq-4',
          pageType: 'StaticPage',
          entityKey: '/fundamentales',
          question: '¿Qué es NAV por CBFI?',
          answer: 'NAV/CBFI = NAV / CBFIs en circulación',
          order: 2,
          isActive: true,
          updatedAt: '2026-01-01T00:00:00Z',
          updatedBy: 'system',
        },
      ],
    })

    await page.goto('/fundamentales')

    await expect(page.getByRole('button', { name: /¿Qué es Cap Rate\?/i })).toHaveAttribute('aria-expanded', 'true')
    await expect(page.getByRole('region', { name: /¿Qué es Cap Rate\?/i })).toContainText('NOI anualizado / Valor de propiedades de inversión')
    await expect(page.getByText('FAQ de fundamentales')).toBeVisible()
  })
})
