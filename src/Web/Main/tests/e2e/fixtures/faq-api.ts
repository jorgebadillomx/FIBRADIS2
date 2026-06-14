import type { Page, Route } from '@playwright/test'

type FaqItem = {
  id: string
  pageType: string
  entityKey: string
  question: string
  answer: string
  order: number
  isActive: boolean
  updatedAt: string
  updatedBy: string
}

type FaqRouteMap = Record<string, FaqItem[]>

function fulfillJson(route: Route, status: number, body: unknown) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

export async function mockFaqApi(page: Page, routes: FaqRouteMap) {
  await page.route('**/api/v1/faq**', (route) => {
    const url = new URL(route.request().url())
    const pageType = url.searchParams.get('pageType') ?? ''
    const entityKey = url.searchParams.get('entityKey') ?? ''
    const key = `${pageType}|${entityKey}`

    return fulfillJson(route, 200, routes[key] ?? [])
  })
}
