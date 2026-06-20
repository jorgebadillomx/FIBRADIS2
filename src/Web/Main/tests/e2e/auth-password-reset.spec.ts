import { expect, test } from '@playwright/test'
import { mockCatalogApi } from './fixtures/catalog-api'
import { mockFaqApi } from './fixtures/faq-api'
import { mockMarketApi } from './fixtures/market-api'
import { mockAuthResetApi } from './fixtures/auth-reset-api'
import { mockPortfolioApi } from './fixtures/portfolio-api'
import { seedMainAuth } from './fixtures/main-auth'

test.describe('Épica 14.8 - auth CTAs y reset de contraseña', () => {
  test.beforeEach(async ({ page }) => {
    await seedMainAuth(page)
    await mockCatalogApi(page)
    await mockMarketApi(page, 'fresh', 24.5)
    await mockPortfolioApi(page)
    await mockFaqApi(page, { 'StaticPage|/portafolio': [] })
  })

  test('LoginForm muestra CTAs en /login y /portafolio con layout responsivo', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 900 })
    await page.goto('/login')

    const forgotLink = page.getByRole('link', { name: '¿Olvidaste tu contraseña?' })
    const createLink = page.getByRole('link', { name: /¿No tienes cuenta\?\s*Crear cuenta/i })

    await expect(forgotLink).toBeVisible()
    await expect(createLink).toBeVisible()

    const desktopForgot = await forgotLink.boundingBox()
    const desktopCreate = await createLink.boundingBox()
    expect(desktopForgot).not.toBeNull()
    expect(desktopCreate).not.toBeNull()
    expect(Math.abs((desktopForgot?.y ?? 0) - (desktopCreate?.y ?? 0))).toBeLessThan(8)

    await page.setViewportSize({ width: 390, height: 900 })
    await page.reload()

    const mobileForgot = page.getByRole('link', { name: '¿Olvidaste tu contraseña?' })
    const mobileCreate = page.getByRole('link', { name: /¿No tienes cuenta\?\s*Crear cuenta/i })

    await expect(mobileForgot).toBeVisible()
    await expect(mobileCreate).toBeVisible()

    const mobileForgotBox = await mobileForgot.boundingBox()
    const mobileCreateBox = await mobileCreate.boundingBox()
    expect(mobileForgotBox).not.toBeNull()
    expect(mobileCreateBox).not.toBeNull()
    expect((mobileCreateBox?.y ?? 0)).toBeGreaterThan((mobileForgotBox?.y ?? 0))

    await page.goto('/portafolio')
    await expect(page.getByRole('link', { name: '¿Olvidaste tu contraseña?' })).toBeVisible()
    await expect(page.getByRole('link', { name: /¿No tienes cuenta\?\s*Crear cuenta/i })).toBeVisible()
  })

  test('RecuperarContrasenaPage oculta el formulario y muestra confirmación tras enviar', async ({ page }) => {
    await mockAuthResetApi(page)
    await page.goto('/recuperar-contrasena')

    await expect(page.getByRole('heading', { name: 'Recuperar contraseña' })).toBeVisible()
    await page.getByLabel('Correo electrónico').fill('usuario@test.com')
    await page.getByRole('button', { name: 'Enviar enlace' }).click()

    await expect(page.getByText('Si ese email está registrado, recibirás un enlace para restablecer tu contraseña. Revisa tu bandeja de entrada.')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Enviar enlace' })).not.toBeVisible()
  })

  test('NuevaContrasenaPage muestra error inmediato sin API cuando status es invalid o expired', async ({ page }) => {
    let resetCalls = 0
    await page.route('**/api/v1/auth/reset-password', async (route) => {
      resetCalls += 1
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ message: 'noop' }) })
    })

    await page.goto('/nueva-contrasena?status=expired')
    await expect(page.getByText('Este enlace no es válido o ya expiró. Solicita uno nuevo.')).toBeVisible()
    expect(resetCalls).toBe(0)

    await page.goto('/nueva-contrasena?status=invalid')
    await expect(page.getByText('Este enlace no es válido o ya expiró. Solicita uno nuevo.')).toBeVisible()
    expect(resetCalls).toBe(0)

    await page.goto('/nueva-contrasena')
    await expect(page.getByText('Este enlace no es válido o ya expiró. Solicita uno nuevo.')).toBeVisible()
    expect(resetCalls).toBe(0)
  })

  test('NuevaContrasenaPage permite guardar contraseña y luego muestra acceso al login', async ({ page }) => {
    let capturedBody: { token?: string; newPassword?: string } | null = null
    await page.route('**/api/v1/auth/reset-password', async (route) => {
      if (route.request().method() !== 'POST') {
        return route.fallback()
      }

      capturedBody = route.request().postDataJSON() as { token?: string; newPassword?: string }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Contraseña actualizada.' }),
      })
    })

    await page.goto('/nueva-contrasena?token=test-token')

    await expect(page.getByRole('heading', { name: 'Nueva contraseña' })).toBeVisible()
    await page.getByLabel('Nueva contraseña').fill('Nueva1!x')
    await page.getByRole('button', { name: 'Guardar contraseña' }).click()

    await expect(page.getByText('Tu contraseña fue actualizada. Ahora puedes iniciar sesión.')).toBeVisible()
    await expect(page.getByRole('link', { name: 'Iniciar sesión' })).toBeVisible()
    expect(capturedBody?.token).toBe('test-token')
    expect(capturedBody?.newPassword).toBe('Nueva1!x')
  })

  test('NuevaContrasenaPage muestra error si el backend rechaza el token', async ({ page }) => {
    await mockAuthResetApi(page, { resetStatus: 400 })
    await page.goto('/nueva-contrasena?token=test-token')

    await page.getByLabel('Nueva contraseña').fill('Nueva1!x')
    await page.getByRole('button', { name: 'Guardar contraseña' }).click()

    await expect(page.getByText('Este enlace no es válido o ya expiró. Solicita uno nuevo.')).toBeVisible()
  })
})
