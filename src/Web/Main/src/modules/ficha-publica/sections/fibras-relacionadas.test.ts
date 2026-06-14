import test from 'node:test'
import assert from 'node:assert/strict'
import { shouldShowRelacionadas } from './fibras-relacionadas.ts'

const sample = [
  { ticker: 'FNOVA17', fullName: 'Fibra Nova', shortName: 'Fibra Nova', sector: 'Diversificado' },
]

test('shouldShowRelacionadas: oculta la sección cuando no hay relacionadas', () => {
  assert.equal(shouldShowRelacionadas(undefined), false)
  assert.equal(shouldShowRelacionadas([]), false)
})

test('shouldShowRelacionadas: muestra la sección cuando hay al menos una', () => {
  assert.equal(shouldShowRelacionadas(sample), true)
})
