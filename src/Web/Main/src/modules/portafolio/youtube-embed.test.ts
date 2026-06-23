import assert from 'node:assert/strict'
import test from 'node:test'
import { PORTAFOLIO_VIDEO_ID, youtubeEmbedUrl } from './youtube-embed.ts'

test('youtubeEmbedUrl builds a privacy-enhanced embed URL with rel=0', () => {
  assert.equal(
    youtubeEmbedUrl('_nArOCSpPz4'),
    'https://www.youtube-nocookie.com/embed/_nArOCSpPz4?rel=0',
  )
})

test('youtubeEmbedUrl trims surrounding whitespace from the id', () => {
  assert.equal(
    youtubeEmbedUrl('  _nArOCSpPz4  '),
    'https://www.youtube-nocookie.com/embed/_nArOCSpPz4?rel=0',
  )
})

test('PORTAFOLIO_VIDEO_ID is the configured portfolio help video', () => {
  assert.equal(PORTAFOLIO_VIDEO_ID, '_nArOCSpPz4')
})
