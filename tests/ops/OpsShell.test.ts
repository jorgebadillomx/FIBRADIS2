import assert from 'node:assert/strict'
import test from 'node:test'
import { OPS_NAVIGATION_SECTIONS } from '../../src/Web/Ops/src/components/ops-navigation.ts'

test('Ops navigation keeps the six expected sections and 17 live routes', () => {
  assert.deepEqual(
    OPS_NAVIGATION_SECTIONS.map((section) => section.title),
    ['Operación', 'Datos', 'Contenido', 'SEO', 'IA', 'Sistema'],
  )

  const itemCount = OPS_NAVIGATION_SECTIONS.reduce((count, section) => count + section.items.length, 0)
  assert.equal(itemCount, 17)
})

test('Ops navigation exposes every live route, including the Épica 12 SEO admin pages', () => {
  const paths = OPS_NAVIGATION_SECTIONS.flatMap((section) => section.items.map((item) => item.to))

  assert.deepEqual(paths, [
    '/dashboard',
    '/pipeline-logs',
    '/ai-call-logs',
    '/catalog',
    '/distribuciones',
    '/fundamentals',
    '/editorial',
    '/noticias',
    '/blocklist',
    '/seo/organization',
    '/seo/faq',
    '/seo/robots',
    '/seo/redirects',
    '/ai-config',
    '/ai-prompts',
    '/config',
    '/users',
  ])
})

test('Ops navigation preserves tooltip descriptions for the sidebar items', () => {
  const dashboard = OPS_NAVIGATION_SECTIONS[0].items[0]
  const noticias = OPS_NAVIGATION_SECTIONS[2].items[1]

  assert.equal(dashboard.description, 'Estado de pipelines, errores y disparos manuales.')
  assert.equal(noticias.description, 'Curación de body text y resúmenes IA.')
})
