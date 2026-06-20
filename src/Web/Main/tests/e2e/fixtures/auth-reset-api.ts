import type { Page, Route } from '@playwright/test'

function fulfillJson(route: Route, status: number, body: unknown) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

export async function mockAuthResetApi(page: Page, options: { resetStatus?: 200 | 400 } = {}) {
  const resetStatus = options.resetStatus ?? 200

  await page.route('**/api/v1/auth/forgot-password', async (route) => {
    if (route.request().method() !== 'POST') {
      return route.fallback()
    }

    return fulfillJson(route, 200, {
      message: 'Si ese email está registrado, recibirás un enlace.',
    })
  })

  await page.route('**/api/v1/auth/reset-password', async (route) => {
    if (route.request().method() !== 'POST') {
      return route.fallback()
    }

    if (resetStatus === 200) {
      return fulfillJson(route, 200, { message: 'Contraseña actualizada.' })
    }

    return fulfillJson(route, 400, { code: 'token_invalid' })
  })
}
