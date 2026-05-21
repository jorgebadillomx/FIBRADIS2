import assert from 'node:assert/strict'
import test, { afterEach } from 'node:test'
import { fetchArticleById } from './newsApi.ts'

const originalFetch = globalThis.fetch

afterEach(() => {
  globalThis.fetch = originalFetch
})

test('fetchArticleById returns null on 404', async () => {
  globalThis.fetch = async () =>
    new Response(JSON.stringify({ title: 'Not Found', status: 404 }), {
      status: 404,
      headers: { 'Content-Type': 'application/json' },
    })

  const article = await fetchArticleById('missing-id')

  assert.equal(article, null)
})

test('fetchArticleById returns article data on success', async () => {
  const payload = {
    id: '11111111-1111-1111-1111-111111111111',
    title: 'FUNO11 anuncia resultados',
    source: 'Expansión',
    publishedAt: '2026-05-20T12:00:00Z',
    url: 'https://example.com/noticia',
    snippet: 'Snippet',
    imageUrl: 'https://example.com/image.jpg',
    aiSummary: 'Resumen IA',
  }

  globalThis.fetch = async () =>
    new Response(JSON.stringify(payload), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    })

  const article = await fetchArticleById(payload.id)

  assert.deepEqual(article, payload)
})
