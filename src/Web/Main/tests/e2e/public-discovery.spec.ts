import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'

test.beforeEach(async ({ page }) => {
  await mockCatalogApi(page)
})

test('Home publica permite buscar una FIBRA y navegar a su ficha', async ({ page }) => {
  await page.goto('/')

  await expect(page).toHaveTitle(/FIBRADIS/)
  await expect(page.getByRole('combobox', { name: 'Buscar FIBRA por ticker o nombre' })).toBeVisible()
  await expect(page.getByText('Precios de mercado — disponible en Épica 3')).toBeVisible()
  await expect(page.getByText('Noticias — disponible en Épica 4')).toBeVisible()

  await page.getByRole('combobox', { name: 'Buscar FIBRA por ticker o nombre' }).fill('FUN')

  await expect(page.getByText('FUNO11')).toBeVisible()
  await expect(page.getByText('Fibra Uno')).toBeVisible()

  await page.getByText('FUNO11').click()

  await expect(page).toHaveURL(/\/fibras\/FUNO11$/)
  await expect(page.getByText('Precio de mercado disponible en Épica 3')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Mercado' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Fundamentales' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Distribuciones' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Noticias' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Reportes' })).toBeVisible()
  await expect(page.getByRole('link', { name: /Sitio web/i })).toHaveAttribute('href', 'https://fibra.uno')
  await expect(page.getByRole('link', { name: /Relación con inversionistas/i })).toHaveAttribute('href', 'https://fibra.uno/inversionistas')
})

test('Home publica muestra estado vacio y skip link funcional', async ({ page }) => {
  await page.goto('/')

  await page.keyboard.press('Tab')
  await expect(page.getByRole('link', { name: 'Ir al contenido principal' })).toBeFocused()
  await page.keyboard.press('Enter')
  await expect(page.locator('#main-content')).toBeFocused()

  await page.getByRole('combobox', { name: 'Buscar FIBRA por ticker o nombre' }).fill('ZZZ')
  await expect(page.getByText('Sin resultados encontrados.')).toBeVisible()
})

test('La ficha muestra 404 amigable para tickers inexistentes', async ({ page }) => {
  await page.goto('/fibras/FAKE99')

  await expect(page.getByRole('heading', { name: 'FIBRA no encontrada' })).toBeVisible()
  await expect(page.getByText(/FAKE99/)).toBeVisible()
  await page.getByRole('link', { name: /Volver a la Home/ }).click()
  await expect(page).toHaveURL(/\/$/)
})

test('La Home en 360px mantiene la accion principal visible sin overflow horizontal', async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 800 })
  await page.goto('/')

  await expect(page.getByRole('combobox', { name: 'Buscar FIBRA por ticker o nombre' })).toBeVisible()

  const hasOverflow = await page.evaluate(() => {
    return document.documentElement.scrollWidth > window.innerWidth
  })

  expect(hasOverflow).toBeFalsy()
})
