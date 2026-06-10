import assert from 'node:assert/strict'
import test from 'node:test'
import type { components } from '@fibradis/shared-api-client'
import { projectNextPayments } from './portfolio-calendar.ts'

type PortfolioPositionDto = components['schemas']['PortfolioPositionDto']

function makePosition(
  fibraId: string,
  ticker: string,
  nombre: string,
  titulos: number,
  recentDistributions: PortfolioPositionDto['recentDistributions'],
): PortfolioPositionDto {
  return {
    fibraId,
    ticker,
    nombre,
    titulos,
    costoPromedio: 100,
    costoTotalCompra: 1000,
    pctPortafolio: 50,
    precioActual: 100,
    valorMercado: 1000,
    plusvaliaFilaPct: 0,
    plusvaliaFilaMxn: 0,
    rentaAnual: 0,
    yoc: 0,
    opportunityScore: null,
    logoUrl: null,
    freshnessStatus: null,
    capRate: null,
    navPerCbfi: null,
    ltv: null,
    noiMargin: null,
    ffoMargin: null,
    dailyChangePct: null,
    week52High: null,
    volume: null,
    week52Low: null,
    week52Avg: null,
    fundamentalsPeriod: null,
    recentDistributions,
  }
}

test('projectNextPayments — proyecta pagos trimestrales dentro de 90 días', () => {
  const today = new Date('2026-06-09T00:00:00Z')
  const result = projectNextPayments([
    makePosition('1', 'FUNO11', 'Fibra Uno', 100, [
      { paymentDate: '2026-05-15', amountPerUnit: 0.4 },
      { paymentDate: '2026-02-15', amountPerUnit: 0.4 },
      { paymentDate: '2025-11-15', amountPerUnit: 0.4 },
      { paymentDate: '2025-08-15', amountPerUnit: 0.4 },
    ]),
  ], today)

  assert.equal(result.length, 1)
  assert.equal(result[0].cadencia, 'trimestral')
  assert.equal(result[0].fechaEstimada.toISOString().slice(0, 10), '2026-08-15')
  assert.equal(result[0].montoPorTitulo, 0.4)
  assert.equal(result[0].montoTotal, 40)
})

test('projectNextPayments — proyecta pagos semestrales dentro de 90 días', () => {
  const today = new Date('2026-06-09T00:00:00Z')
  const result = projectNextPayments([
    makePosition('2', 'DANHOS13', 'Fibra Danhos', 200, [
      { paymentDate: '2026-01-15', amountPerUnit: 0.5 },
      { paymentDate: '2025-07-15', amountPerUnit: 0.5 },
    ]),
  ], today)

  assert.equal(result.length, 1)
  assert.equal(result[0].cadencia, 'semestral')
  assert.equal(result[0].fechaEstimada.toISOString().slice(0, 10), '2026-07-15')
})

test('projectNextPayments — no proyecta pagos fuera de 90 días', () => {
  const today = new Date('2026-06-09T00:00:00Z')
  const result = projectNextPayments([
    makePosition('3', 'TERRA13', 'Fibra Terra', 150, [
      { paymentDate: '2025-10-01', amountPerUnit: 0.6 },
    ]),
  ], today)

  assert.equal(result.length, 0)
})

test('projectNextPayments — proyecta pagos anuales dentro de 90 días', () => {
  const today = new Date('2026-06-09T00:00:00Z')
  const result = projectNextPayments([
    makePosition('5', 'MXCK11', 'Fibra CK', 50, [
      { paymentDate: '2025-07-01', amountPerUnit: 0.8 },
    ]),
  ], today)

  assert.equal(result.length, 1)
  assert.equal(result[0].cadencia, 'anual')
  assert.equal(result[0].fechaEstimada.toISOString().slice(0, 10), '2026-07-01')
  assert.equal(result[0].montoPorTitulo, 0.8)
  assert.equal(result[0].montoTotal, 40)
})

test('projectNextPayments — sin distribuciones retorna vacío', () => {
  const result = projectNextPayments([
    makePosition('4', 'VESTA15', 'Fibra Vesta', 150, []),
  ])

  assert.deepEqual(result, [])
})
