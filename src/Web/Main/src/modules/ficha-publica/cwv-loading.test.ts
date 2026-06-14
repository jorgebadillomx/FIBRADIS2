import test from 'node:test'
import assert from 'node:assert/strict'
import {
  FIBRA_HEADER_LOADING_SHELL,
  FIBRA_PAGE_LOADING_COUNTS,
  FIBRA_PAGE_LOADING_TABS,
  FUNDAMENTALES_SECTION_LOADING_SHELL,
  PRECIO_SECTION_LOADING_SHELL,
} from './cwv-layout.ts'

test('cwv layout config reserves the expected number of shell elements', () => {
  assert.deepEqual(FIBRA_PAGE_LOADING_TABS, [
    'Mercado',
    'Fundamentales',
    'Distribuciones',
    'Noticias',
    'Enlaces',
  ])

  assert.equal(FIBRA_PAGE_LOADING_COUNTS.marketMetricCards, 3)
  assert.equal(FIBRA_PAGE_LOADING_COUNTS.marketRangeButtons, 3)
  assert.equal(FIBRA_PAGE_LOADING_COUNTS.fundamentalsRows, 6)
  assert.equal(FIBRA_PAGE_LOADING_COUNTS.distributionRows, 8)
  assert.equal(FIBRA_PAGE_LOADING_COUNTS.distributionSummaryLines, 3)
  assert.equal(FIBRA_PAGE_LOADING_COUNTS.newsLines, 3)
  assert.equal(FIBRA_PAGE_LOADING_COUNTS.descriptionLines, 3)
  assert.equal(FIBRA_PAGE_LOADING_COUNTS.reportLines, 3)
})

test('cwv layout config preserves the intended width classes', () => {
  assert.equal(PRECIO_SECTION_LOADING_SHELL.priceWidthClass, 'w-32')
  assert.equal(PRECIO_SECTION_LOADING_SHELL.priceFallbackWidthClass, 'min-w-32')
  assert.equal(PRECIO_SECTION_LOADING_SHELL.metadataWidthClass, 'min-w-[11rem]')
  assert.equal(PRECIO_SECTION_LOADING_SHELL.badgeWidthClass, 'w-24')
  assert.equal(PRECIO_SECTION_LOADING_SHELL.detailWidthClass, 'w-32')

  assert.equal(FUNDAMENTALES_SECTION_LOADING_SHELL.headerTitleWidthClass, 'w-24')
  assert.equal(FUNDAMENTALES_SECTION_LOADING_SHELL.headerMetaWidthClass, 'w-36')
  assert.equal(FUNDAMENTALES_SECTION_LOADING_SHELL.rowLabelWidthClass, 'w-40')
  assert.equal(FUNDAMENTALES_SECTION_LOADING_SHELL.rowValueWidthClass, 'w-16')
  assert.equal(FUNDAMENTALES_SECTION_LOADING_SHELL.rowNoteWidthClass, 'w-28')
})

test('header skeleton and loaded price reserve the same width (no CLS shift)', () => {
  // El precio del header: el bar del skeleton (w-24) y el ancho reservado del cargado (min-w-24)
  // deben mapear al mismo valor de Tailwind (6rem) para no desplazar el layout al intercambiar.
  assert.equal(FIBRA_HEADER_LOADING_SHELL.priceSkeletonWidthClass, 'w-24')
  assert.equal(FIBRA_HEADER_LOADING_SHELL.priceReserveClass, 'min-w-24')
  assert.equal(FIBRA_HEADER_LOADING_SHELL.containerWidthClass, 'min-w-[12rem]')
  assert.equal(FIBRA_HEADER_LOADING_SHELL.yieldBadgeWidthClass, 'w-12')
  assert.equal(FIBRA_HEADER_LOADING_SHELL.freshnessBadgeWidthClass, 'w-20')

  // El precio de la tarjeta: skeleton (w-32) y cargado (min-w-32) → ambos 8rem.
  assert.equal(
    PRECIO_SECTION_LOADING_SHELL.priceWidthClass.replace(/^w-/, ''),
    PRECIO_SECTION_LOADING_SHELL.priceFallbackWidthClass.replace(/^min-w-/, ''),
  )
})
