import type { Page } from '@playwright/test'

function buildFakeJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
  const body = btoa(JSON.stringify(payload))
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
  return `${header}.${body}.fake-signature`
}

const FAKE_MAIN_TOKEN = buildFakeJwt({
  sub: 'playwright-user',
  email: 'test@playwright.test',
  role: 'User',
  hasAcceptedTerms: true,
  exp: Math.floor(Date.now() / 1000) + 3600,
})

export async function seedMainAuth(page: Page) {
  await page.addInitScript((token: string) => {
    window.sessionStorage.setItem('fibradis.main.accessToken', token)
  }, FAKE_MAIN_TOKEN)

  await page.route('**/api/v1/auth/refresh', (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({
        status: 401,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Unauthorized', status: 401 }),
      })
    }
    return route.fallback()
  })

  await page.route('**/api/v1/favorites**', async (route) => {
    const method = route.request().method()
    if (method === 'GET') {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      })
    }
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({}) })
  })

  await page.route('**/api/v1/site-content**', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({}) }),
  )
}
