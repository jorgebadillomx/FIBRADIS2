import test from 'node:test'
import assert from 'node:assert/strict'
import {
  INDEXABLE_ROBOTS_DIRECTIVES,
  INDEX_WITHOUT_SNIPPET_ROBOTS_DIRECTIVES,
  NOINDEX_ROBOTS_DIRECTIVES,
  buildRobotsDirectives,
  createDefaultRobotsDraft,
  parseRobotsDirectives,
  ROBOTS_PRESETS,
} from './robotsDirectives.ts'

test('returns a minimal indexable draft when blank (no verbose override)', () => {
  const draft = parseRobotsDirectives('')

  // Vacío equivale a indexable pero NO se infla a la cadena recomendada completa:
  // guardar una fila default sin tocar nada no debe persistir un override verboso.
  assert.equal(buildRobotsDirectives(draft), 'index,follow')
  assert.equal(draft.index, true)
  assert.equal(draft.follow, true)
  assert.deepEqual(draft.extras, [])
})

test('blank and "index,follow" produce the same draft (consistent default)', () => {
  assert.equal(buildRobotsDirectives(parseRobotsDirectives('')), buildRobotsDirectives(parseRobotsDirectives('index,follow')))
})

test('normalizes the recommended preset', () => {
  const draft = parseRobotsDirectives('INDEX, FOLLOW, max-image-preview:LARGE, max-snippet:-1, max-video-preview:-1')

  assert.equal(buildRobotsDirectives(draft), INDEXABLE_ROBOTS_DIRECTIVES)
})

test('normalizes noindex preset', () => {
  const draft = parseRobotsDirectives('noindex,nofollow')

  assert.equal(buildRobotsDirectives(draft), NOINDEX_ROBOTS_DIRECTIVES)
})

test('normalizes index without snippet preset', () => {
  const draft = parseRobotsDirectives('index,follow,max-snippet:0')

  assert.equal(buildRobotsDirectives(draft), INDEX_WITHOUT_SNIPPET_ROBOTS_DIRECTIVES)
})

test('builds exact preset list', () => {
  assert.equal(ROBOTS_PRESETS[0].value, INDEXABLE_ROBOTS_DIRECTIVES)
  assert.equal(ROBOTS_PRESETS[1].value, NOINDEX_ROBOTS_DIRECTIVES)
  assert.equal(ROBOTS_PRESETS[2].value, INDEX_WITHOUT_SNIPPET_ROBOTS_DIRECTIVES)
})

test('createDefaultRobotsDraft is the minimal indexable draft', () => {
  const draft = createDefaultRobotsDraft()

  assert.equal(buildRobotsDirectives(draft), 'index,follow')
})

test('preserves passthrough directives without UI controls (round-trip)', () => {
  const draft = parseRobotsDirectives('index,follow,noarchive,nosnippet,noimageindex')

  assert.deepEqual(draft.extras, ['noarchive', 'nosnippet', 'noimageindex'])
  assert.equal(buildRobotsDirectives(draft), 'index,follow,noarchive,nosnippet,noimageindex')
})

test('maps all and none compound directives', () => {
  assert.equal(buildRobotsDirectives(parseRobotsDirectives('all')), 'index,follow')
  assert.equal(buildRobotsDirectives(parseRobotsDirectives('none')), 'noindex,nofollow')
})
