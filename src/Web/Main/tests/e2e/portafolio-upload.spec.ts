import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockPortfolioApi } from './fixtures/portfolio-api'
import { seedMainAuth } from './fixtures/main-auth'

test.describe('Épica 6 — Carga de portafolio', () => {
  test('Estado vacío muestra zona de carga con instrucciones', async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockPortfolioApi(page, { empty: true })

    await page.goto('/portafolio')

    await expect(page.getByText('Arrastra tu archivo aquí o haz clic para seleccionar')).toBeVisible()
    await expect(page.getByText('Columnas requeridas: Ticker, Qty, AvgCost')).toBeVisible()
    await expect(page.getByText('Formatos aceptados: .xlsx, .csv')).toBeVisible()
  })

  test('Carga exitosa del archivo muestra el portafolio inmediatamente', async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)

    let callCount = 0
    await page.route('**/api/v1/portfolio', async (route) => {
      if (route.request().method() === 'GET') {
        callCount++
        if (callCount === 1) {
          return route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ positions: [], kpis: null }),
          })
        }
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            positions: [
              {
                fibraId: '11111111-1111-1111-1111-111111111111',
                ticker: 'FUNO11',
                nombre: 'Fibra Uno',
                titulos: 500,
                costoPromedio: 47,
                costoTotalCompra: 23641,
                pctPortafolio: 100,
                precioActual: 52.5,
                valorMercado: 26250,
                plusvaliaFilaPct: 11.03,
                plusvaliaFilaMxn: 2609,
                rentaAnual: 768,
                yoc: 3.25,
                opportunityScore: 68,
                logoUrl: null,
                freshnessStatus: 'fresh',
                navPerCbfi: null,
                capRate: null,
                ltv: null,
                noiMargin: null,
                ffoMargin: null,
                dailyChangePct: null,
                week52High: null,
                volume: null,
                week52Low: null,
                week52Avg: null,
                fundamentalsPeriod: null,
                recentDistributions: [],
              },
            ],
            kpis: {
              inversionTotal: 23641,
              valorTotal: 26250,
              plusvaliaTotal_Pct: 11.03,
              plusvaliaTotal_Mxn: 2609,
              yieldPortafolio: 2.93,
              ingresoMensual: 64,
              rentasAnualesBrutas: 768,
              rentasRealesBrutas: 768,
              pctRentasPortafolio: 2.93,
              isPartial: false,
            },
          }),
        })
      }
      return route.continue()
    })

    await page.route('**/api/v1/portfolio/column-config', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ columns: [] }) }),
    )
    await page.route('**/api/v1/portfolio/snapshot', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ hasSnapshot: false, archivedAt: null }),
      }),
    )
    await page.route('**/api/v1/portfolio/upload**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ positionCount: 1, duplicateDetected: false }),
      }),
    )

    await page.goto('/portafolio')

    await expect(page.getByText('Arrastra tu archivo aquí')).toBeVisible()

    const buffer = Buffer.from('Ticker,Qty,AvgCost\nFUNO11,500,47.00\n')
    await page.getByLabel('Seleccionar archivo de portafolio (.xlsx o .csv)').setInputFiles({
      name: 'portafolio.csv',
      mimeType: 'text/csv',
      buffer,
    })

    await expect(page.getByText('FUNO11')).toBeVisible()
  })

  test('Errores de validación muestran tabla de errores por fila', async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockPortfolioApi(page, {
      empty: true,
      uploadErrors: [
        { rowNumber: 2, ticker: 'FAKEXX', message: 'Ticker no encontrado en el catálogo.' },
        { rowNumber: 3, ticker: 'OTRO99', message: 'Ticker no encontrado en el catálogo.' },
      ],
    })

    await page.goto('/portafolio')

    const buffer = Buffer.from('Ticker,Qty,AvgCost\nFAKEXX,100,10.00\nOTRO99,200,5.00\n')
    await page.getByLabel('Seleccionar archivo de portafolio (.xlsx o .csv)').setInputFiles({
      name: 'portafolio.csv',
      mimeType: 'text/csv',
      buffer,
    })

    await expect(page.getByText('Se encontraron 2 errores')).toBeVisible()
    await expect(page.getByText('FAKEXX')).toBeVisible()
    await expect(page.getByText('Ticker no encontrado en el catálogo.').first()).toBeVisible()
  })

  test('Con portafolio activo muestra diálogo de reemplazo antes de subir', async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockPortfolioApi(page)

    await page.goto('/portafolio')

    await expect(page.getByText('FUNO11')).toBeVisible()

    const buffer = Buffer.from('Ticker,Qty,AvgCost\nFUNO11,600,45.00\n')
    await page.getByLabel('Seleccionar archivo de portafolio (.xlsx o .csv)').setInputFiles({
      name: 'nuevo-portafolio.csv',
      mimeType: 'text/csv',
      buffer,
    })

    await expect(page.getByRole('dialog')).toBeVisible()
    await expect(page.getByText('¿Cómo quieres subir este archivo?')).toBeVisible()
    await expect(page.getByText('Actualizar portafolio')).toBeVisible()
    await expect(page.getByText('Agregar al portafolio')).toBeVisible()

    await page.getByRole('button', { name: 'Cancelar' }).click()
    await expect(page.getByRole('dialog')).not.toBeVisible()
  })

  test('Banner de respaldo visible cuando existe snapshot', async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockPortfolioApi(page, {
      snapshot: {
        hasSnapshot: true,
        archivedAt: '2026-05-15T10:00:00Z',
      },
    })

    await page.goto('/portafolio')

    await expect(page.getByText('Tienes un respaldo del')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Restaurar respaldo' })).toBeVisible()
  })
})
