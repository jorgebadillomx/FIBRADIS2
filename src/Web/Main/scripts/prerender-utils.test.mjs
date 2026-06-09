import { test } from 'node:test'
import assert from 'node:assert/strict'
import { extractHeadElements } from './prerender-utils.mjs'

test('extractHeadElements extrae title, meta y link del rendered y deja cleanBody sin ellos', () => {
  const rendered =
    '<title>FUNO11 — Fibra Uno | Fibras Inmobiliarias</title>' +
    '<meta name="description" content="Análisis de Fibra Uno." />' +
    '<link rel="canonical" href="https://fibradis.mx/fibras/FUNO11" />' +
    '<div class="container">contenido</div>'

  const { headElements, cleanBody } = extractHeadElements(rendered)

  assert.equal(headElements.length, 3)
  assert.match(headElements[0], /^<title>FUNO11/)
  assert.match(headElements[1], /^<meta name="description"/)
  assert.match(headElements[2], /^<link rel="canonical"/)
  assert.equal(cleanBody, '<div class="container">contenido</div>')
})

test('extractHeadElements no deja > sueltos cuando los tags son self-closing con />', () => {
  const rendered =
    '<title>Página</title>' +
    '<meta name="description" content="Desc"/>' +
    '<link rel="canonical" href="https://fibradis.mx/"/>' +
    '<div>body</div>'

  const { cleanBody } = extractHeadElements(rendered)

  assert.ok(!cleanBody.startsWith('>'), `cleanBody no debe iniciar con '>': ${JSON.stringify(cleanBody.slice(0, 10))}`)
  assert.equal(cleanBody, '<div>body</div>')
})

test('extractHeadElements no deja > sueltos con meta sin barra de cierre', () => {
  const rendered =
    '<meta name="description" content="Sin barra">' +
    '<div>body</div>'

  const { headElements, cleanBody } = extractHeadElements(rendered)

  assert.equal(headElements.length, 1)
  assert.equal(cleanBody, '<div>body</div>')
  assert.ok(!cleanBody.startsWith('>'))
})

test('extractHeadElements devuelve headElements vacío y cleanBody igual cuando no hay elementos head', () => {
  const rendered = '<div class="app"><p>Solo contenido</p></div>'

  const { headElements, cleanBody } = extractHeadElements(rendered)

  assert.equal(headElements.length, 0)
  assert.equal(cleanBody, rendered)
})

test('extractHeadElements es case-insensitive para los tags', () => {
  const rendered =
    '<TITLE>Mayúsculas</TITLE>' +
    '<META NAME="description" CONTENT="Test">' +
    '<LINK REL="canonical" HREF="https://fibradis.mx/">' +
    '<div>cuerpo</div>'

  const { headElements, cleanBody } = extractHeadElements(rendered)

  assert.equal(headElements.length, 3)
  assert.equal(cleanBody, '<div>cuerpo</div>')
})

test('extractHeadElements extrae el title cuando el contenido tiene caracteres especiales como —', () => {
  const rendered =
    '<title>Fibras Inmobiliarias — Plataforma de análisis de FIBRAs del mercado mexicano</title>' +
    '<div>home</div>'

  const { headElements, cleanBody } = extractHeadElements(rendered)

  assert.equal(headElements.length, 1)
  assert.equal(headElements[0], '<title>Fibras Inmobiliarias — Plataforma de análisis de FIBRAs del mercado mexicano</title>')
  assert.equal(cleanBody, '<div>home</div>')
})
