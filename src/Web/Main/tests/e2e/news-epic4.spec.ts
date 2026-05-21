import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockMarketApi } from './fixtures/market-api'
import { mockNewsApi } from './fixtures/news-api'
import { mockOpsAuthApi, mockOpsNewsApi, seedOpsAuth } from './fixtures/ops-news-api'

test.describe('Épica 4 - Noticias y Contenido', () => {
  test.describe('Mundo público', () => {
    test.beforeEach(async ({ page }) => {
      await mockCatalogApi(page)
      await mockMarketApi(page, 'fresh', 24.5)
      await mockNewsApi(page, {
        latest: [
          {
            id: 'dddddddd-1111-1111-1111-111111111111',
            title: 'Mercado de FIBRAs capta interés extranjero',
            source: 'El Economista',
            publishedAt: '2026-05-20T15:00:00Z',
            url: 'https://example.com/noticias/mercado-fibras',
            snippet: 'El flujo internacional regresó al sector durante mayo.',
            aiSummary: null,
          },
          {
            id: 'dddddddd-2222-2222-2222-222222222222',
            title: 'FUNO11 concreta refinanciamiento estratégico',
            source: 'El Financiero',
            publishedAt: '2026-05-20T14:30:00Z',
            url: 'https://example.com/noticias/funo11-refinanciamiento',
            snippet: 'La emisora detalló los términos del refinanciamiento.',
            aiSummary: 'Resumen IA: FUNO11 refinanció pasivos para extender vencimientos y preservar liquidez.',
          },
        ],
        byFibraId: {
          '11111111-1111-1111-1111-111111111111': [
            {
              id: 'eeeeeeee-1111-1111-1111-111111111111',
              title: 'FUNO11 acelera desinversión de activos no estratégicos',
              source: 'Reuters',
              publishedAt: '2026-05-20T09:15:00Z',
              url: 'https://example.com/noticias/funo11-desinversion',
              snippet: 'La emisora planea reciclar capital hacia proyectos logísticos.',
              aiSummary: 'Resumen IA: FUNO11 busca reciclar capital y concentrarse en activos con mayor rentabilidad.',
            },
            {
              id: 'eeeeeeee-2222-2222-2222-222222222222',
              title: 'Analistas ajustan estimados para FUNO11',
              source: 'Bloomberg Línea',
              publishedAt: '2026-05-18T16:45:00Z',
              url: 'https://example.com/noticias/funo11-analistas',
              snippet: 'El consenso ajustó estimados tras la última guía trimestral.',
              aiSummary: null,
            },
          ],
          '22222222-2222-2222-2222-222222222222': [],
        },
      })
    })

    test('Home muestra el feed general ordenado y conserva artículos sin asociación específica', async ({ page }) => {
      await page.goto('/')

      const newsSection = page.getByLabel('Noticias recientes')
      const articles = newsSection.locator('article')

      await expect(articles).toHaveCount(2)
      await expect(articles.nth(0)).toContainText('Mercado de FIBRAs capta interés extranjero')
      await expect(articles.nth(0)).toContainText('El flujo internacional regresó al sector durante mayo.')
      await expect(articles.nth(1)).toContainText('FUNO11 concreta refinanciamiento estratégico')
      await expect(articles.nth(1)).toContainText('Resumen IA: FUNO11 refinanció pasivos para extender vencimientos y preservar liquidez.')
    })

    test('Ficha pública muestra solo noticias asociadas y estado vacío cuando la FIBRA no tiene noticias', async ({ page }) => {
      await page.goto('/fibras/FUNO11')

      const noticiasSection = page.locator('section#noticias')
      await expect(noticiasSection.getByText('Resumen IA: FUNO11 busca reciclar capital y concentrarse en activos con mayor rentabilidad.')).toBeVisible()
      await expect(noticiasSection.getByText('El consenso ajustó estimados tras la última guía trimestral.')).toBeVisible()
      await expect(noticiasSection.getByText('Mercado de FIBRAs capta interés extranjero')).not.toBeVisible()

      await page.goto('/fibras/DANHOS13')
      await expect(page.locator('section#noticias').getByText('Sin noticias disponibles')).toBeVisible()
    })
  })

  test.describe('Centro de procesos Ops', () => {
    test.beforeEach(async ({ page }) => {
      await seedOpsAuth(page)
      await mockOpsNewsApi(page)
    })

    test('Ops permite administrar blocklist sin redespliegue', async ({ page }) => {
      await page.goto('http://127.0.0.1:4174/')

      await expect(page.getByRole('heading', { name: 'Blocklist de noticias' })).toBeVisible()
      await expect(page.getByText('fibra óptica')).toBeVisible()

      await page.getByLabel('Nuevo término').fill('fibra satelital')
      await page.getByRole('button', { name: 'Agregar término' }).click()

      await expect(page.getByText('fibra satelital')).toBeVisible()

      await page.getByRole('button', { name: 'Eliminar' }).first().click()
      await expect(page.getByText('fibra óptica')).not.toBeVisible()
    })

    test('Ops permite cambiar AI_MODE, muestra auditoría y dispara resumen manual en modo Manual', async ({ page }) => {
      await page.goto('http://127.0.0.1:4174/')

      await expect(page.getByText('Cambia AI_MODE a Manual para habilitar este disparo.')).toBeVisible()

      await page.getByRole('button', { name: 'Manual - disparar desde Ops' }).click()
      await page.getByRole('button', { name: 'Guardar cambio' }).click()

      await expect(page.getByText(/por qa\.ops@test\.com/)).toBeVisible()
      await expect(page.getByText(/anterior: Off/)).toBeVisible()

      await page.getByPlaceholder('GUID del artículo de noticias').fill('no-es-guid')
      await expect(page.getByText('El ID debe ser un GUID válido')).toBeVisible()

      await page.getByPlaceholder('GUID del artículo de noticias').fill('bbbbbbbb-0000-0000-0000-000000000001')
      await page.getByRole('button', { name: 'Generar resumen' }).click()

      await expect(page.getByText('Resumen solicitado correctamente.')).toBeVisible()
    })

    test('Ops pide login cuando no hay sesión y permite entrar como AdminOps', async ({ page }) => {
      await mockOpsAuthApi(page)
      await mockOpsNewsApi(page)

      await page.goto('http://127.0.0.1:4174/')

      await expect(page.getByText('Inicia sesión como AdminOps para usar el sitio Ops.')).toBeVisible()
      await expect(page.getByText('Error al obtener AI_MODE')).not.toBeVisible()

      await page.getByLabel('Correo').fill('adminops@test.com')
      await page.getByLabel('Contraseña').fill('admin456')
      await page.getByRole('button', { name: 'Entrar a Ops' }).click()

      await expect(page.getByRole('heading', { name: 'Modo AI de Noticias' })).toBeVisible()
      await expect(page.getByRole('button', { name: 'Cerrar sesión' })).toBeVisible()
    })
  })
})
