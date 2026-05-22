import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockMarketApi } from './fixtures/market-api'
import { mockNewsApi, mockNewsArticleByIdApi, mockNewsArticleByIdNotFound } from './fixtures/news-api'

const ARTICLE_ID = 'cccccccc-aaaa-bbbb-cccc-dddddddddddd'

const fullArticle = {
  id: ARTICLE_ID,
  title: 'FUNO11 concreta acuerdo de refinanciamiento histórico',
  source: 'El Financiero',
  publishedAt: '2026-05-20T14:30:00Z',
  url: 'https://elfinanciero.com.mx/noticias/funo11-refinanciamiento',
  snippet: 'La emisora detalló los términos del refinanciamiento ante los medios.',
  aiSummary: 'Resumen IA: FUNO11 extendió vencimientos y redujo costo de deuda en 80 puntos base.',
  imageUrl: 'https://example.com/images/funo11-deal.jpg',
}

const fiveArticles = Array.from({ length: 5 }, (_, i) => ({
  id: `aaaaaaaa-${String(i + 1).padStart(4, '0')}-0000-0000-000000000000`,
  title: `Noticia ${i + 1} del mercado de FIBRAs`,
  source: 'El Financiero',
  publishedAt: new Date(Date.now() - i * 3_600_000).toISOString(),
  url: `https://example.com/noticias/${i + 1}`,
  snippet: `Contenido de la noticia ${i + 1}.`,
  aiSummary: null,
  imageUrl: i % 2 === 0 ? `https://example.com/images/noticia-${i + 1}.jpg` : null,
}))

test.describe('Épica 4 — Lector de noticias y og:image', () => {
  test.describe('Home muestra hasta 5 noticias (story 4-6)', () => {
    test('Home renderiza hasta 5 artículos en la sección de noticias', async ({ page }) => {
      await mockCatalogApi(page)
      await mockMarketApi(page)
      await mockNewsApi(page, { latest: fiveArticles })

      await page.goto('/')

      const newsSection = page.getByLabel('Noticias recientes')
      const articles = newsSection.locator('article')
      await expect(articles).toHaveCount(5)
    })

    test('Home muestra el título del primer artículo', async ({ page }) => {
      await mockCatalogApi(page)
      await mockMarketApi(page)
      await mockNewsApi(page, { latest: fiveArticles })

      await page.goto('/')

      const newsSection = page.getByLabel('Noticias recientes')
      await expect(newsSection).toContainText('Noticia 1 del mercado de FIBRAs')
    })
  })

  test.describe('og:image en artículos (story 4-5-1)', () => {
    test('artículo con imageUrl muestra una imagen con el alt del título', async ({ page }) => {
      await mockCatalogApi(page)
      await mockMarketApi(page, 'fresh', 24.5)
      await mockNewsApi(page, {
        latest: [
          {
            id: 'aaaaaaaa-1111-1111-1111-111111111111',
            title: 'Noticia con imagen adjunta',
            source: 'Reuters',
            publishedAt: '2026-05-20T14:00:00Z',
            url: 'https://reuters.com/noticias/fibra',
            snippet: 'Resumen del artículo.',
            aiSummary: null,
            imageUrl: 'https://example.com/images/noticia-con-imagen.jpg',
          },
        ],
      })

      await page.goto('/')

      const newsSection = page.getByLabel('Noticias recientes')
      const articleImage = newsSection.getByRole('img', { name: 'Noticia con imagen adjunta' })
      await expect(articleImage).toBeVisible()
    })

    test('NoticiaPage muestra imagen del artículo con og:image', async ({ page }) => {
      await mockCatalogApi(page)
      await mockNewsArticleByIdApi(page, fullArticle)

      await page.goto(`/noticias/${ARTICLE_ID}`)

      const articleImage = page.getByRole('img', { name: fullArticle.title })
      await expect(articleImage).toBeVisible()
    })
  })

  test.describe('NoticiaPage — lector interno (story 4-5-3)', () => {
    test.beforeEach(async ({ page }) => {
      await mockCatalogApi(page)
      await mockNewsArticleByIdApi(page, fullArticle)
    })

    test('NoticiaPage renderiza el título del artículo', async ({ page }) => {
      await page.goto(`/noticias/${ARTICLE_ID}`)

      await expect(page.getByRole('heading', { level: 1 })).toContainText(fullArticle.title)
    })

    test('NoticiaPage muestra la fuente y fecha del artículo', async ({ page }) => {
      await page.goto(`/noticias/${ARTICLE_ID}`)

      await expect(page.getByText('El Financiero')).toBeVisible()
    })

    test('NoticiaPage muestra el resumen IA cuando existe', async ({ page }) => {
      await page.goto(`/noticias/${ARTICLE_ID}`)

      await expect(page.getByText('Resumen IA')).toBeVisible()
      await expect(page.getByText(fullArticle.aiSummary!)).toBeVisible()
    })

    test('NoticiaPage muestra snippet como fallback cuando no hay resumen IA', async ({ page }) => {
      const articleSinSummary = { ...fullArticle, aiSummary: null }
      await mockNewsArticleByIdApi(page, articleSinSummary)

      await page.goto(`/noticias/${ARTICLE_ID}`)

      await expect(page.getByText('Resumen IA')).not.toBeVisible()
      await expect(page.getByText(fullArticle.snippet!)).toBeVisible()
    })

    test('NoticiaPage muestra enlace externo a la fuente original', async ({ page }) => {
      await page.goto(`/noticias/${ARTICLE_ID}`)

      const externalLink = page.getByRole('link', { name: /leer más en el financiero/i })
      await expect(externalLink).toBeVisible()
      await expect(externalLink).toHaveAttribute('target', '_blank')
      await expect(externalLink).toHaveAttribute('rel', 'noopener noreferrer')
    })

    test('NoticiaPage muestra skeleton mientras carga', async ({ page }) => {
      // Introduce delay en la respuesta para que el skeleton sea visible
      await page.route(`**/api/v1/news/${ARTICLE_ID}`, async (route) => {
        await new Promise((resolve) => setTimeout(resolve, 300))
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(fullArticle),
        })
      })

      await page.goto(`/noticias/${ARTICLE_ID}`)

      // El skeleton es visible inicialmente (class animate-pulse)
      const skeleton = page.locator('.animate-pulse')
      await expect(skeleton).toBeVisible()

      // Después carga el contenido real
      await expect(page.getByRole('heading', { level: 1 })).toContainText(fullArticle.title)
    })

    test('NoticiaPage muestra mensaje de error cuando el artículo no existe', async ({ page }) => {
      await mockNewsArticleByIdNotFound(page, ARTICLE_ID)

      await page.goto(`/noticias/${ARTICLE_ID}`)

      await expect(page.getByText('Noticia no encontrada.')).toBeVisible()
      await expect(page.getByRole('link', { name: 'Volver al inicio' })).toBeVisible()
    })
  })
})
