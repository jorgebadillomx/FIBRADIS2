import test from 'node:test'
import assert from 'node:assert/strict'
import { filterSnapshots, sortSnapshots, calcRange52Pct, buildUniverseMobileCardData } from './universe-table-logic.ts'

const rows = [
  { ticker: 'FUNO11',   lastPrice: 30, dailyChange: 0.5,  dailyChangePct: 1.7,  volume: 500_000, week52High: 40, week52Low: 20 },
  { ticker: 'FMTY14',   lastPrice: 10, dailyChange: -0.2, dailyChangePct: -1.9, volume: 100_000, week52High: 15, week52Low: 8  },
  { ticker: 'DANHOS13', lastPrice: null, dailyChange: null, dailyChangePct: null, volume: null, week52High: null, week52Low: null },
  { ticker: 'TERRA13',  lastPrice: 20, dailyChange: 0.1,  dailyChangePct: 0.5,  volume: 250_000, week52High: 25, week52Low: 18 },
  { ticker: 'FIBRAMQ12',lastPrice: 5,  dailyChange: -0.05, dailyChangePct: -0.9, volume: 80_000, week52High: 7,  week52Low: 4  },
]

// ── filterSnapshots ──────────────────────────────────────────────────────────

test('filterSnapshots — texto vacío devuelve todos', () => {
  assert.equal(filterSnapshots(rows, '').length, 5)
})

test('filterSnapshots — texto vacío con espacios devuelve todos', () => {
  assert.equal(filterSnapshots(rows, '   ').length, 5)
})

test('filterSnapshots — case-insensitive incluye coincidencia', () => {
  const result = filterSnapshots(rows, 'fun')
  assert.equal(result.length, 1)
  assert.equal(result[0].ticker, 'FUNO11')
})

test('filterSnapshots — sin coincidencias devuelve array vacío', () => {
  const result = filterSnapshots(rows, 'XYZ')
  assert.equal(result.length, 0)
})

test('filterSnapshots — coincidencia parcial en medio del ticker', () => {
  const result = filterSnapshots(rows, 'MQ')
  assert.equal(result.length, 1)
  assert.equal(result[0].ticker, 'FIBRAMQ12')
})

// ── sortSnapshots ────────────────────────────────────────────────────────────

test('sortSnapshots — sin key devuelve copia sin ordenar', () => {
  const result = sortSnapshots(rows, null, 'asc')
  assert.equal(result.length, 5)
  assert.equal(result[0].ticker, 'FUNO11')
  assert.notEqual(result, rows)
})

test('sortSnapshots — key lastPrice asc, nulos al final', () => {
  const result = sortSnapshots(rows, 'lastPrice', 'asc')
  assert.equal(result[0].ticker, 'FIBRAMQ12')
  assert.equal(result[result.length - 1].ticker, 'DANHOS13')
})

test('sortSnapshots — key lastPrice desc, nulos al final', () => {
  const result = sortSnapshots(rows, 'lastPrice', 'desc')
  assert.equal(result[0].ticker, 'FUNO11')
  assert.equal(result[result.length - 1].ticker, 'DANHOS13')
})

test('sortSnapshots — key dailyChangePct asc, nulos al final', () => {
  const result = sortSnapshots(rows, 'dailyChangePct', 'asc')
  assert.equal(result[0].ticker, 'FMTY14')
  assert.equal(result[result.length - 1].ticker, 'DANHOS13')
})

test('sortSnapshots — desempate alfabético por ticker', () => {
  const tied = [
    { ticker: 'ZZZ', lastPrice: 10, dailyChange: 0, dailyChangePct: 0, volume: 0, week52High: 10, week52Low: 10 },
    { ticker: 'AAA', lastPrice: 10, dailyChange: 0, dailyChangePct: 0, volume: 0, week52High: 10, week52Low: 10 },
  ]
  const result = sortSnapshots(tied, 'lastPrice', 'asc')
  assert.equal(result[0].ticker, 'AAA')
})

// ── calcRange52Pct ───────────────────────────────────────────────────────────

test('calcRange52Pct — precio en el centro exacto', () => {
  const pct = calcRange52Pct(50, 100, 0)
  assert.equal(pct, 0.5)
})

test('calcRange52Pct — precio en el mínimo', () => {
  const pct = calcRange52Pct(0, 100, 0)
  assert.equal(pct, 0)
})

test('calcRange52Pct — precio en el máximo', () => {
  const pct = calcRange52Pct(100, 100, 0)
  assert.equal(pct, 1)
})

test('calcRange52Pct — precio por encima del máximo → clampea a 1', () => {
  const pct = calcRange52Pct(120, 100, 0)
  assert.equal(pct, 1)
})

test('calcRange52Pct — precio por debajo del mínimo → clampea a 0', () => {
  const pct = calcRange52Pct(-10, 100, 0)
  assert.equal(pct, 0)
})

test('calcRange52Pct — null lastPrice devuelve null', () => {
  assert.equal(calcRange52Pct(null, 100, 0), null)
})

test('calcRange52Pct — null high devuelve null', () => {
  assert.equal(calcRange52Pct(50, null, 0), null)
})

test('calcRange52Pct — null low devuelve null', () => {
  assert.equal(calcRange52Pct(50, 100, null), null)
})

test('calcRange52Pct — high === low devuelve null (evita división por cero)', () => {
  assert.equal(calcRange52Pct(50, 50, 50), null)
})

// ── buildUniverseMobileCardData ─────────────────────────────────────────────

test('buildUniverseMobileCardData — formatea precios, cambios y estado', () => {
  const result = buildUniverseMobileCardData({
    ticker: 'FUNO11',
    lastPrice: 28.5,
    dailyChange: 0.32,
    dailyChangePct: 1.14,
    freshnessStatus: 'fresh',
    latestPeriod: '2025-Q4',
    volume: 1_250_000,
    rangePct: 0.62,
    week52High: 31,
    week52Low: 24,
    annualizedYield: 8.75,
  })

  assert.equal(result.summary.ticker, 'FUNO11')
  assert.equal(result.summary.price, '28.50')
  assert.equal(result.summary.change, '+0.32')
  assert.equal(result.summary.changePct, '+1.14%')
  assert.equal(result.summary.changeTone, 'positive')
  assert.equal(result.summary.freshnessStatus, 'fresh')
  assert.equal(result.summary.latestPeriod, '2025-Q4')
  assert.equal(result.details.volume, '1.3M')
  assert.equal(result.details.rangePct, 0.62)
  assert.equal(result.details.week52High, '31.00')
  assert.equal(result.details.week52Low, '24.00')
  assert.equal(result.details.annualizedYield, '8.75%')
})

test('buildUniverseMobileCardData — cambios nulos y precio ausente usan em dash', () => {
  const result = buildUniverseMobileCardData({
    ticker: 'FMTY14',
    lastPrice: null,
    dailyChange: null,
    dailyChangePct: null,
    freshnessStatus: null,
    latestPeriod: null,
    volume: null,
    rangePct: null,
    week52High: null,
    week52Low: null,
    annualizedYield: null,
  })

  assert.equal(result.summary.price, '—')
  assert.equal(result.summary.change, '—')
  assert.equal(result.summary.changePct, '—')
  assert.equal(result.summary.changeTone, 'neutral')
  assert.equal(result.summary.freshnessStatus, null)
  assert.equal(result.summary.latestPeriod, '—')
  assert.equal(result.details.volume, '—')
  assert.equal(result.details.week52High, '—')
  assert.equal(result.details.week52Low, '—')
  assert.equal(result.details.annualizedYield, '—')
})
