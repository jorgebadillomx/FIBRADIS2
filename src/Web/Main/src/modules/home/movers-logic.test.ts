import test from 'node:test'
import assert from 'node:assert/strict'
import { getTopMovers, splitGainersLosers, formatVolume } from './movers-logic.ts'

const snap = (ticker: string, pct: number | null) => ({
  ticker,
  dailyChangePct: pct,
  lastPrice: null,
  volume: null,
})

// ── getTopMovers ──────────────────────────────────────────────────────────────

test('getTopMovers: lista vacía devuelve []', () => {
  assert.deepEqual(getTopMovers([], 5), [])
})

test('getTopMovers: todos con changePct null → orden alfabético', () => {
  const input = [snap('FUNO11', null), snap('DANHOS13', null), snap('FMTY14', null)]
  const result = getTopMovers(input, 5)
  assert.deepEqual(result.map(s => s.ticker), ['DANHOS13', 'FMTY14', 'FUNO11'])
})

test('getTopMovers: mezcla positivos y negativos → ordena por valor absoluto desc', () => {
  const input = [snap('A', 1.5), snap('B', -3.2), snap('C', 2.0), snap('D', -0.5)]
  const result = getTopMovers(input, 4)
  assert.deepEqual(result.map(s => s.ticker), ['B', 'C', 'A', 'D'])
})

test('getTopMovers: menos de n elementos → devuelve todos sin error', () => {
  const input = [snap('A', 2.0), snap('B', 1.0)]
  const result = getTopMovers(input, 5)
  assert.equal(result.length, 2)
})

test('getTopMovers: trunca a n cuando hay más', () => {
  const input = [snap('A', 5), snap('B', 4), snap('C', 3), snap('D', 2), snap('E', 1), snap('F', 0.5)]
  const result = getTopMovers(input, 3)
  assert.equal(result.length, 3)
  assert.deepEqual(result.map(s => s.ticker), ['A', 'B', 'C'])
})

test('getTopMovers: empate en |changePct| → desempate alfabético', () => {
  const input = [snap('C', 2.0), snap('A', 2.0), snap('B', 2.0)]
  const result = getTopMovers(input, 2)
  assert.deepEqual(result.map(s => s.ticker), ['A', 'B'])
})

// ── splitGainersLosers ────────────────────────────────────────────────────────

test('splitGainersLosers: lista vacía → { gainers: [], losers: [] }', () => {
  const { gainers, losers } = splitGainersLosers([], 5)
  assert.deepEqual(gainers, [])
  assert.deepEqual(losers, [])
})

test('splitGainersLosers: solo positivos → losers vacío', () => {
  const input = [snap('A', 2.0), snap('B', 1.0)]
  const { gainers, losers } = splitGainersLosers(input, 5)
  assert.equal(gainers.length, 2)
  assert.equal(losers.length, 0)
})

test('splitGainersLosers: solo negativos → gainers vacío', () => {
  const input = [snap('A', -2.0), snap('B', -1.0)]
  const { gainers, losers } = splitGainersLosers(input, 5)
  assert.equal(gainers.length, 0)
  assert.equal(losers.length, 2)
})

test('splitGainersLosers: gainers ordenados de mayor a menor', () => {
  const input = [snap('A', 1.0), snap('B', 3.0), snap('C', 2.0)]
  const { gainers } = splitGainersLosers(input, 5)
  assert.deepEqual(gainers.map(s => s.ticker), ['B', 'C', 'A'])
})

test('splitGainersLosers: losers ordenados de más negativo a menos negativo', () => {
  const input = [snap('A', -1.0), snap('B', -3.0), snap('C', -2.0)]
  const { losers } = splitGainersLosers(input, 5)
  assert.deepEqual(losers.map(s => s.ticker), ['B', 'C', 'A'])
})

test('splitGainersLosers: trunca a n en cada lista', () => {
  const input = [
    snap('G1', 5), snap('G2', 4), snap('G3', 3), snap('G4', 2), snap('G5', 1), snap('G6', 0.5),
    snap('L1', -5), snap('L2', -4), snap('L3', -3), snap('L4', -2),
  ]
  const { gainers, losers } = splitGainersLosers(input, 3)
  assert.equal(gainers.length, 3)
  assert.equal(losers.length, 3)
})

test('splitGainersLosers: changePct null se excluye de ambas listas', () => {
  const input = [snap('A', 2.0), snap('B', null), snap('C', -1.0)]
  const { gainers, losers } = splitGainersLosers(input, 5)
  assert.equal(gainers.length, 1)
  assert.equal(losers.length, 1)
  assert.equal(gainers[0].ticker, 'A')
  assert.equal(losers[0].ticker, 'C')
})

test('splitGainersLosers: empate en gainers → desempate alfabético', () => {
  const input = [snap('C', 2.0), snap('A', 2.0), snap('B', -1.0)]
  const { gainers } = splitGainersLosers(input, 2)
  assert.equal(gainers.length, 2)
  assert.equal(gainers[0].ticker, 'A')
  assert.equal(gainers[1].ticker, 'C')
})

test('splitGainersLosers: empate en losers → desempate alfabético', () => {
  const input = [snap('C', -2.0), snap('A', -2.0), snap('B', 1.0)]
  const { losers } = splitGainersLosers(input, 2)
  assert.equal(losers.length, 2)
  assert.equal(losers[0].ticker, 'A')
  assert.equal(losers[1].ticker, 'C')
})

// ── formatVolume ──────────────────────────────────────────────────────────────

test('formatVolume: millones', () => {
  assert.equal(formatVolume(1_500_000), '1.5M')
})

test('formatVolume: miles', () => {
  assert.equal(formatVolume(25_000), '25K')
})

test('formatVolume: menos de mil → número localizado', () => {
  const result = formatVolume(500)
  assert.equal(typeof result, 'string')
  assert.ok(result.includes('500'))
})
