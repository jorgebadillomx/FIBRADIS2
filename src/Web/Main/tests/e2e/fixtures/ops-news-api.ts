import type { Page, Route } from '@playwright/test'

type BlocklistTerm = {
  id: string
  term: string
  createdAt: string
}

type AiModeState = {
  mode: 'Off' | 'Manual'
  updatedAt: string
  updatedBy: string | null
  previousMode: 'Off' | 'Manual' | null
}

type OpsNewsApiOptions = {
  blocklistTerms?: BlocklistTerm[]
  aiMode?: AiModeState
}

function fulfillJson(route: Route, status: number, body: unknown) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

export async function seedOpsAuth(page: Page) {
  await page.addInitScript(() => {
    window.sessionStorage.setItem('fibradis.ops.accessToken', 'playwright-adminops-token')
  })
}

export async function mockOpsAuthApi(page: Page) {
  await page.route('**/api/v1/auth/refresh', async (route) => {
    if (route.request().method() !== 'POST') {
      return route.fallback()
    }

    return route.fulfill({
      status: 401,
      contentType: 'application/problem+json',
      body: JSON.stringify({
        title: 'Unauthorized',
        status: 401,
      }),
    })
  })

  await page.route('**/api/v1/auth/login', async (route) => {
    if (route.request().method() !== 'POST') {
      return route.fallback()
    }

    const body = route.request().postDataJSON() as { email?: string; password?: string } | null
    const email = body?.email?.trim() ?? ''
    const password = body?.password ?? ''

    if (email === 'adminops@test.com' && password === 'admin456') {
      return fulfillJson(route, 200, {
        accessToken: 'playwright-adminops-token',
      })
    }

    return route.fulfill({
      status: 401,
      contentType: 'application/problem+json',
      body: JSON.stringify({
        title: 'Unauthorized',
        status: 401,
        detail: 'Credenciales inválidas.',
      }),
    })
  })
}

export async function mockOpsNewsApi(page: Page, options: OpsNewsApiOptions = {}) {
  let blocklistTerms = options.blocklistTerms ?? [
    {
      id: 'cccccccc-1111-1111-1111-111111111111',
      term: 'fibra óptica',
      createdAt: '2026-05-20T12:00:00Z',
    },
  ]

  let aiMode = options.aiMode ?? {
    mode: 'Off' as const,
    updatedAt: '2026-05-20T12:05:00Z',
    updatedBy: 'qa.seed@test.com',
    previousMode: null,
  }

  await page.route('**/api/v1/news/blocklist-terms', async (route) => {
    const method = route.request().method()

    if (method === 'GET') {
      return fulfillJson(route, 200, blocklistTerms)
    }

    if (method === 'POST') {
      const body = route.request().postDataJSON() as { term?: string } | null
      const term = body?.term?.trim() ?? ''
      const created = {
        id: `cccccccc-${String(blocklistTerms.length + 2).padStart(4, '0')}-1111-1111-111111111111`,
        term,
        createdAt: '2026-05-20T12:30:00Z',
      }

      blocklistTerms = [...blocklistTerms, created]
      return fulfillJson(route, 200, created)
    }

    return route.fallback()
  })

  await page.route('**/api/v1/news/blocklist-terms/*', async (route) => {
    if (route.request().method() !== 'DELETE') {
      return route.fallback()
    }

    const url = new URL(route.request().url())
    const id = url.pathname.split('/').at(-1)
    blocklistTerms = blocklistTerms.filter((term) => term.id !== id)

    return route.fulfill({ status: 204 })
  })

  await page.route('**/api/v1/ops/ai-mode', async (route) => {
    const method = route.request().method()

    if (method === 'GET') {
      return fulfillJson(route, 200, aiMode)
    }

    if (method === 'PUT') {
      const body = route.request().postDataJSON() as { mode?: 'Off' | 'Manual' } | null
      const nextMode = body?.mode ?? aiMode.mode

      aiMode = {
        mode: nextMode,
        updatedAt: '2026-05-20T13:00:00Z',
        updatedBy: 'qa.ops@test.com',
        previousMode: aiMode.mode,
      }

      return route.fulfill({ status: 204 })
    }

    return route.fallback()
  })

  await page.route('**/api/v1/ops/news/*/ai-summary', async (route) => {
    if (route.request().method() !== 'POST') {
      return route.fallback()
    }

    if (aiMode.mode !== 'Manual') {
      return fulfillJson(route, 400, {
        title: 'Bad Request',
        status: 400,
        detail: 'La generación de resumen solo está disponible cuando AI_MODE=Manual.',
      })
    }

    return route.fulfill({ status: 204 })
  })
}
