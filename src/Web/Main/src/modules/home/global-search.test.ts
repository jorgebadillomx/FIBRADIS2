import test from 'node:test'
import assert from 'node:assert/strict'
import { filterFibrasByQuery } from './global-search.ts'

const fibras = [
  { ticker: 'FUNO11', fullName: 'Fibra Uno' },
  { ticker: 'DANHOS13', fullName: 'Fibra Danhos' },
  { ticker: 'FMTY14', fullName: 'Fibra Monterrey' },
  { ticker: 'TERRA13', fullName: 'Fibra Terra' },
  { ticker: 'VESTA15', fullName: 'Fibra Vesta' },
  { ticker: 'FINN13', fullName: 'Fibra Inn' },
  { ticker: 'FIHO12', fullName: 'Fibra Hotel' },
  { ticker: 'PLUS18', fullName: 'Fibra Plus' },
  { ticker: 'HCITY17', fullName: 'Fibra Hotel City Express' },
] as const

test('filterFibrasByQuery matches by ticker and full name without case sensitivity', () => {
  assert.deepEqual(filterFibrasByQuery(fibras, 'fun').map((fibra) => fibra.ticker), ['FUNO11'])
  assert.deepEqual(filterFibrasByQuery(fibras, 'monterrey').map((fibra) => fibra.ticker), ['FMTY14'])
})

test('filterFibrasByQuery returns no results for empty or whitespace-only queries', () => {
  assert.deepEqual(filterFibrasByQuery(fibras, ''), [])
  assert.deepEqual(filterFibrasByQuery(fibras, '   '), [])
})

test('filterFibrasByQuery limits suggestions to eight matches', () => {
  const results = filterFibrasByQuery(fibras, 'fibra')

  assert.equal(results.length, 8)
  assert.deepEqual(results.map((fibra) => fibra.ticker), [
    'FUNO11',
    'DANHOS13',
    'FMTY14',
    'TERRA13',
    'VESTA15',
    'FINN13',
    'FIHO12',
    'PLUS18',
  ])
})

test('filterFibrasByQuery returns an empty list when nothing matches', () => {
  assert.deepEqual(filterFibrasByQuery(fibras, 'zzzzz'), [])
})
