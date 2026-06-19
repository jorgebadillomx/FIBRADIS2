import assert from 'node:assert/strict'
import test from 'node:test'
import {
  calcCostoPurchase,
  calcNewAvgCost,
  calcNuevoAvg,
  calcNuevaPlusvaliaPct,
  calcNuevoValor,
  calcRentaProyectadaAnual,
  calcTitulosParaRentaTarget,
} from './simulador-logic.ts'

test('calcNuevoAvg — happy path: 1000×$110 + 500×$100 → $106.67', () => {
  const result = calcNuevoAvg(1000, 110, 500, 100)
  assert.ok(Math.abs(result - 106.6667) < 0.001)
})

test('calcNuevoAvg — mismo precio no cambia el promedio', () => {
  const result = calcNuevoAvg(1000, 100, 500, 100)
  assert.equal(result, 100)
})

test('calcNuevaPlusvaliaPct — compra bajo avg mejora plusvalía', () => {
  // avg original $110, precio actual $100 → plusvalía negativa -9.09%
  // comprar a $100: nuevo avg $106.67 → plusvalía mejora a aprox -6.25%
  const nuevoAvg = calcNuevoAvg(1000, 110, 500, 100)
  const pct = calcNuevaPlusvaliaPct(nuevoAvg, 100)
  // Debe ser más cercano a 0 que -9.09
  assert.ok(pct > -9.09)
  assert.ok(Math.abs(pct - (-6.25)) < 0.01)
})

test('calcNuevaPlusvaliaPct — compra sobre avg empeora plusvalía', () => {
  // avg original $100, precio actual $110 → plusvalía positiva +10%
  // comprar a $110: nuevo avg sube → plusvalía no mejora
  const nuevoAvg = calcNuevoAvg(1000, 100, 500, 110)
  const pct = calcNuevaPlusvaliaPct(nuevoAvg, 110)
  // Plusvalía sigue siendo positiva pero menor
  assert.ok(pct > 0)
  const originalPct = ((110 - 100) / 100) * 100
  assert.ok(pct < originalPct)
})

test('calcNuevoValor — cálculo correcto', () => {
  const result = calcNuevoValor(1000, 500, 100)
  assert.equal(result, 150000)
})

test('calcNuevoAvg — denominador cero retorna 0 sin lanzar excepción', () => {
  const result = calcNuevoAvg(0, 110, 0, 100)
  assert.equal(result, 0)
})

test('calcNuevaPlusvaliaPct — nuevoAvg cero retorna 0 sin lanzar excepción', () => {
  const result = calcNuevaPlusvaliaPct(0, 100)
  assert.equal(result, 0)
})

test('calcNewAvgCost — aplica comisión sobre las nuevas adquisiciones', () => {
  const result = calcNewAvgCost(1000, 110, 100, 500, 0.01)
  assert.ok(Math.abs(result - 107) < 0.0001)
})

test('calcNewAvgCost — comisión cero coincide con el promedio ponderado normal', () => {
  const result = calcNewAvgCost(1000, 110, 100, 500, 0)
  assert.ok(Math.abs(result - 106.6666666667) < 0.0001)
})

test('calcNewAvgCost — títulos nuevos cero retorna el promedio actual', () => {
  const result = calcNewAvgCost(1000, 110, 100, 0, 0.01)
  assert.equal(result, 110)
})

// calcRentaProyectadaAnual

test('calcRentaProyectadaAnual — yield disponible: usa dividendYieldPct para proyectar renta adicional', () => {
  // 1000 títulos, rentaAnual $11,000, precio $100, yield 5.2%, +500 títulos
  // adicional = 500 × 100 × 0.052 = $2,600 → proyectada = $13,600
  const result = calcRentaProyectadaAnual(11000, 500, 100, 5.2, 1000)
  assert.equal(result, 13600)
})

test('calcRentaProyectadaAnual — yield null, fallback rentaPerTitle', () => {
  // rentaPerTitle = 11000/1000 = $11; adicional = 500×11 = $5,500 → proyectada = $16,500
  const result = calcRentaProyectadaAnual(11000, 500, 100, null, 1000)
  assert.equal(result, 16500)
})

test('calcRentaProyectadaAnual — adicionales ≤ 0 devuelve renta sin cambios', () => {
  const result = calcRentaProyectadaAnual(11000, 0, 100, 5.2, 1000)
  assert.equal(result, 11000)
})

test('calcRentaProyectadaAnual — yield null y currentTitulos=0 devuelve renta sin cambios', () => {
  const result = calcRentaProyectadaAnual(0, 500, 100, null, 0)
  assert.equal(result, 0)
})

test('calcRentaProyectadaAnual — yield explícito de 0% proyecta cero ingresos adicionales (no usa rentaPerTitle)', () => {
  // dividendYieldPct=0 significa yield real=0%; nuevos títulos no generan renta adicional
  const result = calcRentaProyectadaAnual(11000, 500, 100, 0, 1000)
  assert.equal(result, 11000)
})

// calcTitulosParaRentaTarget

test('calcTitulosParaRentaTarget — yield disponible: calcula títulos totales necesarios', () => {
  // target $2,000/mes → $24,000/año; precio $100, yield 5.2% → por título=$5.2
  // total = ceil(24000/5.2) = ceil(4615.38) = 4616
  const result = calcTitulosParaRentaTarget(2000, 100, 5.2, 1000, 11000)
  assert.equal(result, 4616)
})

test('calcTitulosParaRentaTarget — yield null fallback rentaPerTitle', () => {
  // rentaPerTitle = 11000/1000 = $11; target $2,000/mes → $24,000/año
  // total = ceil(24000/11) = ceil(2181.8) = 2182
  const result = calcTitulosParaRentaTarget(2000, 100, null, 1000, 11000)
  assert.equal(result, 2182)
})

test('calcTitulosParaRentaTarget — objetivo ya cubierto por posición actual', () => {
  // target $400/mes → $4,800/año; precio $100, yield 5.2%
  // total = ceil(4800/5.2) = ceil(923.07) = 924; currentTitulos=1000 >= 924 → adicionales=0
  const result = calcTitulosParaRentaTarget(400, 100, 5.2, 1000, 5200)
  assert.equal(result, 924)
  assert.equal(Math.max(0, 924 - 1000), 0)
})

test('calcTitulosParaRentaTarget — sin datos de renta devuelve null', () => {
  const result = calcTitulosParaRentaTarget(2000, 100, null, 0, 0)
  assert.equal(result, null)
})

test('calcTitulosParaRentaTarget — target ≤ 0 devuelve null', () => {
  const result = calcTitulosParaRentaTarget(0, 100, 5.2, 1000, 11000)
  assert.equal(result, null)
})

// calcCostoPurchase

test('calcCostoPurchase — ejemplo real: 10 × $28.38 × commission 0.0025 + IVA 16%', () => {
  const result = calcCostoPurchase(28.38, 10, 0.0025)
  // 283.80 + 0.7095 + 0.1135 = 284.623
  assert.ok(Math.abs(result - 284.623) < 0.001)
})

test('calcCostoPurchase — comisión cero: solo precio × cantidad', () => {
  const result = calcCostoPurchase(28.38, 10, 0)
  assert.ok(Math.abs(result - 283.80) < 0.001)
})

test('calcCostoPurchase — cantidad cero devuelve 0', () => {
  const result = calcCostoPurchase(28.38, 0, 0.0025)
  assert.equal(result, 0)
})
