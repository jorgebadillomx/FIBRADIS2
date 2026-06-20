import { expect, test } from '@playwright/test'

const TRIAL_ENDS_AT = '2026-07-04T00:00:00.0000000+00:00'

test.describe('Confirmación de email resiliente', () => {
  test('status=confirmed renderiza éxito sin llamar a la API', async ({ page }) => {
    let confirmEmailCalls = 0
    await page.route('**/api/v1/auth/confirm-email**', async (route) => {
      confirmEmailCalls += 1
      return route.fallback()
    })

    await page.goto(`/confirmar-email?status=confirmed&t=${encodeURIComponent(TRIAL_ENDS_AT)}`)

    await expect(page.getByRole('heading', { name: '¡Cuenta confirmada!' })).toBeVisible()

    const formattedTrialEndsAt = await page.evaluate(
      (value) =>
        new Intl.DateTimeFormat('es-MX', {
          dateStyle: 'full',
          timeStyle: 'short',
          timeZone: 'UTC',
        }).format(new Date(value)),
      TRIAL_ENDS_AT,
    )

    await expect(
      page.getByText(`Tu prueba de 14 días ha comenzado. Vence el ${formattedTrialEndsAt}.`),
    ).toBeVisible()
    await expect(page.getByRole('link', { name: 'Ir a mi portafolio' })).toBeVisible()
    expect(confirmEmailCalls).toBe(0)
  })

  test('status=expired muestra el CTA de reenvío', async ({ page }) => {
    await page.goto('/confirmar-email?status=expired')

    await expect(page.getByRole('heading', { name: 'El enlace expiró' })).toBeVisible()
    await expect(
      page.getByRole('link', { name: 'Reenviar confirmación' }),
    ).toHaveAttribute('href', '/activar?reason=trial_not_started')
  })

  test('status=already_confirmed muestra acceso al login', async ({ page }) => {
    await page.goto('/confirmar-email?status=already_confirmed')

    await expect(page.getByRole('heading', { name: 'Tu cuenta ya fue confirmada' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Iniciar sesión' })).toHaveAttribute('href', '/login')
  })

  test('status=error muestra error genérico', async ({ page }) => {
    await page.goto('/confirmar-email?status=error')

    await expect(page.getByRole('heading', { name: 'No pudimos confirmar tu cuenta' })).toBeVisible()
  })

  test('legacy token flow sigue llamando a la API', async ({ page }) => {
    let capturedToken = ''
    await page.route('**/api/v1/auth/confirm-email**', async (route) => {
      if (route.request().method() !== 'GET') {
        return route.fallback()
      }

      const url = new URL(route.request().url())
      capturedToken = url.searchParams.get('token') ?? ''

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ trialEndsAt: '2026-07-04T00:00:00.000Z' }),
      })
    })

    await page.goto('/confirmar-email?token=test-token')

    await expect(page.getByRole('heading', { name: '¡Cuenta confirmada!' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Ir a mi portafolio' })).toBeVisible()
    expect(capturedToken).toBe('test-token')
  })
})
