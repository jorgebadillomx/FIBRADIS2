import type { Page, Route } from '@playwright/test'

function fulfillJson(route: Route, status: number, body: unknown) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

export async function mockEditorialPagesApi(page: Page) {
  await page.route('**/api/v1/pages', (route) =>
    fulfillJson(route, 200, [
      {
        slug: 'que-son-las-fibras',
        title: '¿Qué son las FIBRAs?',
        content: 'Contenido editorial sobre qué son las FIBRAs.',
        updatedAt: '2026-01-01T00:00:00Z',
      },
      {
        slug: 'historia',
        title: 'Historia',
        content: 'Contenido editorial de historia.',
        updatedAt: '2026-01-01T00:00:00Z',
      },
      {
        slug: 'como-se-estructuran',
        title: '¿Cómo se estructuran?',
        content: 'Contenido editorial de estructura.',
        updatedAt: '2026-01-01T00:00:00Z',
      },
      {
        slug: 'por-que-invertir',
        title: '¿Por qué invertir?',
        content: 'Contenido editorial de inversión.',
        updatedAt: '2026-01-01T00:00:00Z',
      },
      {
        slug: 'regimen-fiscal',
        title: '¿Cuál es el régimen fiscal de las FIBRAs?',
        content: 'Contenido editorial fiscal.',
        updatedAt: '2026-01-01T00:00:00Z',
      },
    ]),
  )
}
