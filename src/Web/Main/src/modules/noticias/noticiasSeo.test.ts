import assert from 'node:assert/strict'
import test from 'node:test'
import {
  buildNoticiasCanonicalPath,
  buildNoticiasRobotsDirectives,
  NOTICIAS_PAGINATED_ROBOTS,
} from './noticiasSeo.ts'

test('buildNoticiasCanonicalPath keeps page 1 canonical at /noticias', () => {
  assert.equal(buildNoticiasCanonicalPath(1), '/noticias')
})

test('buildNoticiasCanonicalPath adds query for paginated pages', () => {
  assert.equal(buildNoticiasCanonicalPath(2), '/noticias?page=2')
})

test('buildNoticiasRobotsDirectives returns null for page 1', () => {
  assert.equal(buildNoticiasRobotsDirectives(1), null)
})

test('buildNoticiasRobotsDirectives returns noindex follow for paginated pages', () => {
  assert.equal(buildNoticiasRobotsDirectives(3), NOTICIAS_PAGINATED_ROBOTS)
})
