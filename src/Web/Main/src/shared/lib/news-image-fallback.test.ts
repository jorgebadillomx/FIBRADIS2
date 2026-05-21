import test from 'node:test'
import assert from 'node:assert/strict'
import { getArticleImageUrl, getSectorImageUrl, SECTOR_IMAGES } from './news-image-fallback.ts'

test('getArticleImageUrl prioriza la imagen del articulo', () => {
  assert.equal(
    getArticleImageUrl(
      { imageUrl: 'https://cdn.example.com/article.jpg' },
      { logoUrl: 'https://cdn.example.com/logo.jpg', sector: 'Industrial' },
    ),
    'https://cdn.example.com/article.jpg',
  )
})

test('getArticleImageUrl usa logo de la fibra cuando no hay og:image', () => {
  assert.equal(
    getArticleImageUrl(
      { imageUrl: null },
      { logoUrl: 'https://cdn.example.com/logo.jpg', sector: 'Industrial' },
    ),
    'https://cdn.example.com/logo.jpg',
  )
})

test('getArticleImageUrl cae al asset sectorial y luego a otro', () => {
  assert.equal(
    getArticleImageUrl({ imageUrl: null }, { sector: 'Industrial' }),
    SECTOR_IMAGES.industrial,
  )
  assert.equal(
    getArticleImageUrl({ imageUrl: null }, { sector: 'Hotelero' }),
    SECTOR_IMAGES.otro,
  )
  assert.equal(getSectorImageUrl(null), SECTOR_IMAGES.otro)
})
