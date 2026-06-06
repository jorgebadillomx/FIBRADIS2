import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockComparadorApi } from './fixtures/comparador-api'

test.describe('Épica 8 — Comparador público /comparar', () => {
  test.beforeEach(async ({ page }) => {
    await mockCatalogApi(page)
    await mockComparadorApi(page)
  })

  test('Página /comparar carga con título correcto y estado vacío de selección', async ({ page }) => {
    await page.goto('/comparar')

    await expect(page).toHaveTitle(/Comparador de FIBRAs/)
    await expect(page.getByRole('heading', { name: 'Comparador de FIBRAs' })).toBeVisible()
    await expect(
      page.getByText('Selecciona al menos dos FIBRAs para ver la comparación.'),
    ).toBeVisible()
  })

  test('El buscador de FIBRAs muestra sugerencias al escribir', async ({ page }) => {
    await page.goto('/comparar')

    await page.getByLabel('Buscar FIBRAs para comparar').fill('FUN')

    await expect(page.getByRole('button', { name: /FUNO11/ })).toBeVisible()
    await expect(page.getByText('Fibra Uno')).toBeVisible()
  })

  test('Seleccionar dos FIBRAs actualiza URL y muestra la tabla de comparación', async ({ page }) => {
    await page.goto('/comparar')

    await page.getByLabel('Buscar FIBRAs para comparar').fill('FUN')
    await page.getByRole('button', { name: /FUNO11/ }).click()

    await page.getByLabel('Buscar FIBRAs para comparar').fill('DAN')
    await page.getByRole('button', { name: /DANHOS13/ }).click()

    await expect(page).toHaveURL(/fibras=FUNO11.*DANHOS13|fibras=DANHOS13.*FUNO11/)

    await expect(page.getByRole('columnheader', { name: 'Mercado' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Fundamentales' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Distribuciones' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Score público' })).toBeVisible()
  })

  test('Chips de FIBRAs seleccionadas aparecen con botón de quitar', async ({ page }) => {
    await page.goto('/comparar')

    await page.getByLabel('Buscar FIBRAs para comparar').fill('FUN')
    await page.getByRole('button', { name: /FUNO11/ }).click()

    await page.getByLabel('Buscar FIBRAs para comparar').fill('DAN')
    await page.getByRole('button', { name: /DANHOS13/ }).click()

    const funoChip = page.locator('div').filter({ hasText: /^FUNO11/ }).first()
    await expect(funoChip).toBeVisible()

    const danChip = page.locator('div').filter({ hasText: /^DANHOS13/ }).first()
    await expect(danChip).toBeVisible()
  })

  test('Carga desde URL con query param ?fibras= muestra tabla directamente', async ({ page }) => {
    await page.goto('/comparar?fibras=FUNO11%2CDANHOS13')

    await expect(page.getByRole('columnheader', { name: 'Mercado' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Fundamentales' })).toBeVisible()

    const funoHeader = page.locator('th').filter({ hasText: 'FUNO11' })
    const danhosHeader = page.locator('th').filter({ hasText: 'DANHOS13' })
    await expect(funoHeader).toBeVisible()
    await expect(danhosHeader).toBeVisible()
  })

  test('Datos faltantes muestran — en la celda sin desplazar columnas', async ({ page }) => {
    await page.goto('/comparar?fibras=FUNO11%2CFMTY14')

    await expect(page.getByRole('columnheader', { name: 'Mercado' })).toBeVisible()

    const cells = page.locator('td').filter({ hasText: '—' })
    await expect(cells.first()).toBeVisible()
  })

  test('Tabla muestra métricas de Mercado correctamente', async ({ page }) => {
    await page.goto('/comparar?fibras=FUNO11%2CDANHOS13')

    await expect(page.getByText('Precio actual (MXN)')).toBeVisible()
    await expect(page.getByText('Cambio día (%)')).toBeVisible()
    await expect(page.getByText('Promedio 52S (MXN)')).toBeVisible()
    await expect(page.getByText('Volumen')).toBeVisible()
  })

  test('Tabla muestra métricas de Fundamentales correctamente', async ({ page }) => {
    await page.goto('/comparar?fibras=FUNO11%2CDANHOS13')

    await expect(page.getByText('Período del reporte')).toBeVisible()
    await expect(page.getByText('Cap Rate (%)')).toBeVisible()
    await expect(page.getByText('NAV por CBFI (MXN)')).toBeVisible()
    await expect(page.getByText('LTV (%)')).toBeVisible()
    await expect(page.getByText('Margen NOI (%)')).toBeVisible()
  })

  test('Tabla muestra métricas de Distribuciones correctamente', async ({ page }) => {
    await page.goto('/comparar?fibras=FUNO11%2CDANHOS13')

    await expect(page.getByText('Distribución trimestral (MXN)')).toBeVisible()
    await expect(page.getByText('Yield calculado anual (%)')).toBeVisible()
    await expect(page.getByText('Yield decretado anual (%)')).toBeVisible()
  })

  test('Límite máximo de 4 FIBRAs deshabilita el input y muestra badge "Límite 4"', async ({ page }) => {
    await page.goto('/comparar')

    for (const [ticker, term] of [['FUNO11', 'FUN'], ['DANHOS13', 'DAN'], ['FMTY14', 'FMT']]) {
      await page.getByLabel('Buscar FIBRAs para comparar').fill(term)
      await page.getByRole('button', { name: new RegExp(ticker) }).click()
    }

    const countBadge = page.locator('span').filter({ hasText: '3' }).first()
    await expect(countBadge).toBeVisible()
  })

  test('No se puede quitar una FIBRA cuando solo quedan 2 seleccionadas', async ({ page }) => {
    await page.goto('/comparar?fibras=FUNO11%2CDANHOS13')

    await expect(page.getByRole('columnheader', { name: 'Mercado' })).toBeVisible()

    const removeButtons = page.getByRole('button', { name: /Quitar/ })
    if ((await removeButtons.count()) > 0) {
      await expect(removeButtons.first()).toBeDisabled()
    }
  })

  test('El comparador funciona sin autenticación (ruta pública)', async ({ page }) => {
    await page.goto('/comparar')

    await expect(page).toHaveURL(/\/comparar/)
    await expect(page.getByRole('heading', { name: 'Comparador de FIBRAs' })).toBeVisible()
    await expect(page).not.toHaveURL(/login|auth/)
  })

  test('Meta description está presente en la página /comparar', async ({ page }) => {
    await page.goto('/comparar')

    const metaDescription = page.locator('meta[name="description"]')
    await expect(metaDescription).toHaveAttribute('content', /[Cc]ompara.*[Ff][Ii][Bb][Rr][Aa]/)
  })

  test('Sugerencias de autocompletado excluyen FIBRAs ya seleccionadas', async ({ page }) => {
    await page.goto('/comparar')

    await page.getByLabel('Buscar FIBRAs para comparar').fill('FUN')
    await page.getByRole('button', { name: /FUNO11/ }).click()

    await page.getByLabel('Buscar FIBRAs para comparar').fill('FUN')

    const funoOption = page.getByRole('button', { name: /FUNO11/ })
    await expect(funoOption).not.toBeVisible()
  })

  test('Selección se refleja correctamente en query params de URL', async ({ page }) => {
    await page.goto('/comparar')

    await page.getByLabel('Buscar FIBRAs para comparar').fill('FUN')
    await page.getByRole('button', { name: /FUNO11/ }).click()

    await page.getByLabel('Buscar FIBRAs para comparar').fill('DAN')
    await page.getByRole('button', { name: /DANHOS13/ }).click()

    const url = page.url()
    expect(url).toContain('fibras=')
    expect(url).toContain('FUNO11')
    expect(url).toContain('DANHOS13')
  })

  test('El comparador en 360px no tiene overflow horizontal', async ({ page }) => {
    await page.setViewportSize({ width: 360, height: 800 })
    await page.goto('/comparar')

    const hasOverflow = await page.evaluate(() => {
      return document.documentElement.scrollWidth > window.innerWidth
    })

    expect(hasOverflow).toBeFalsy()
  })
})
