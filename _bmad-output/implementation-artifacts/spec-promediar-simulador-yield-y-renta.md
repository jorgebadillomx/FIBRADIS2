---
title: 'Promediar Posición — Yield, costo de compra y calculadora de renta'
type: 'feature'
created: '2026-06-12'
status: 'done'
context: []
baseline_commit: '6edc4540e6fcc469a50187e1dfe56c22c6c716b8'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El simulador de promediar posición muestra el impacto en costo promedio y plusvalía, pero el inversor no sabe cuánto dinero desembolsará en la compra, qué yield anual tiene la FIBRA ni cuánta renta mensual recibirá tras agregar títulos — información clave para decidir si promediar vale la pena.

**Approach:** Agregar en la tabla existente las columnas "Yield" (DividendYieldPct) y "Costo compra" (adicionales × precio); en la sección "¿Qué pasaría si...?" incorporar (1) MetricCard de renta mensual proyectada con los títulos ingresados y (2) calculadora inversa: dado un objetivo de renta mensual, calcular cuántos títulos adicionales son necesarios y la inversión estimada.

## Boundaries & Constraints

**Always:**
- 100 % frontend — todos los datos ya existen en `OpportunityFibraRowDto.DividendYieldPct` y `PortfolioPositionDto.RentaAnual/Titulos`; cero cambios en API o backend.
- Las nuevas funciones puras deben tener guards contra denominador cero y yield null (fallback a `rentaAnual/titulos` como proxy).
- El disclaimer existente permanece visible y sin cambios.

**Ask First:**
- Si en la calculadora inversa el yield de la FIBRA es null y `RentaAnual = 0` (no hay datos de renta), no se puede calcular el objetivo — mostrar "Datos de renta no disponibles" y esperar confirmación del usuario si el mensaje no es suficientemente claro.

**Never:**
- No agregar llamadas adicionales a la API.
- No emitir recomendaciones de compra o venta; el disclaimer cubre todo.
- No romper las columnas o el comportamiento existente de la tabla.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output | Error Handling |
|---|---|---|---|
| Yield disponible | `opportunityRow.DividendYieldPct = 5.2` | Columna "Yield" → `5.2%` | — |
| Yield no disponible | `opportunityRow = null` | Columna "Yield" → `—` | — |
| Costo compra con adicionales | 500 adicionales, precio $100 | Columna "Costo compra" → `$50,000` | — |
| Renta proyectada, yield disponible | 1000 títulos, rentaAnual=$11,000, precio=$100, yield=5.2%, 500 adicionales | Renta mensual estimada = (11,000 + 500×100×0.052)/12 = (11,000+2,600)/12 ≈ $1,133/mes | — |
| Renta proyectada, yield null | yield null, 1000 títulos, rentaAnual=$11,000, 500 adicionales | Usa rentaPerTitle=$11: (11,000+500×11)/12 ≈ $1,375/mes | — |
| Calculadora inversa, yield disponible | objetivo $2,000/mes, precio $100, yield 5.2%, titulos actuales=1000 | Títulos totales=ceil(24,000/5.2)=4,616; adicionales=3,616 | — |
| Calculadora inversa, yield null | yield null, 1000 títulos, rentaAnual=$11,000, objetivo $2,000/mes | rentaPerTitle=$11; totales=ceil(24,000/11)=2,182; adicionales=1,182 | — |
| Objetivo ya cumplido | renta actual $1,500/mes, objetivo $1,000/mes | "Ya cumples el objetivo con tus posiciones actuales." | — |
| Datos insuficientes | yield null, titulos=0 o rentaAnual=0 | "Datos de renta no disponibles para esta FIBRA." | — |

</frozen-after-approval>

## Code Map

- `src/Web/Main/src/modules/oportunidades/simulador-logic.ts` — funciones puras existentes; agregar `calcRentaProyectadaAnual` y `calcTitulosParaRentaTarget`
- `src/Web/Main/src/modules/oportunidades/simulador-logic.test.ts` — tests existentes; agregar casos del I/O matrix
- `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` — componente principal; columnas nuevas en tabla + MetricCard renta + calculadora inversa

## Tasks & Acceptance

**Execution:**

- [x] `src/Web/Main/src/modules/oportunidades/simulador-logic.ts` -- agregar dos funciones puras exportadas:

  ```ts
  // Renta anual proyectada (con adicionales). Fallback a rentaPerTitle si yield null.
  export function calcRentaProyectadaAnual(
    currentRentaAnual: number,
    additionalTitulos: number,
    precioActual: number,
    dividendYieldPct: number | null | undefined,
    currentTitulos: number,
  ): number

  // Títulos totales necesarios para alcanzar targetMensual. null si no hay datos de renta.
  export function calcTitulosParaRentaTarget(
    targetMensual: number,
    precioActual: number,
    dividendYieldPct: number | null | undefined,
    currentTitulos: number,
    currentRentaAnual: number,
  ): number | null
  ```
  
  Guards obligatorios: denominador cero → devuelve 0 o null según la función; `additionalTitulos ≤ 0` → devuelve `currentRentaAnual` sin modificar.

- [x] `src/Web/Main/src/modules/oportunidades/simulador-logic.test.ts` -- agregar tests cubriendo todos los escenarios del I/O matrix: happy path yield disponible, fallback yield null, objetivo ya cumplido (resultado < titulos actuales → adicionales = 0 no negativo), denominador cero, target ≤ 0

- [x] `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` -- tres bloques de cambios:

  1. **Tabla — columna Yield** (siempre visible): header `"Yield"`, cell = `opportunityRow?.dividendYieldPct != null ? `${dividendYieldPct.toFixed(1)}%` : '—'`; insertar entre "Score" y "Títulos adicionales"

  2. **Tabla — columna Costo compra** (visible solo cuando `hasSimulacion`): header `"Costo compra"`, cell = `adicionalesNum × precioActual` formateado como MXN sin decimales; insertar al final de las columnas de simulación

  3. **Sección "¿Qué pasaría si...?"**:
     - Agregar 4.º MetricCard `"Renta mensual estimada"` en el grid (resultado de `calcRentaProyectadaAnual` dividido/12, formateado MXN 2 dec) — visible solo cuando `canSimulateWhatIf`
     - Agregar estado `whatIfTargetRenta: string` y subsección "Calcular objetivo de renta" debajo del grid:
       - Input `"Renta mensual objetivo (MXN)"` + cálculo live de `calcTitulosParaRentaTarget`
       - Mostrar resultado: `"Necesitas X títulos adicionales (~$Y de inversión)"` donde `Y = titulosAdicionales × precioActual × (1 + commissionFactor)`
       - Si ya cumple: `"Ya cumples el objetivo con tus posiciones actuales."`
       - Si datos insuficientes (retorna null): `"Datos de renta no disponibles para esta FIBRA."`

**Acceptance Criteria:**

- Dado que la FIBRA tiene `DividendYieldPct` disponible, cuando el usuario ve la tabla, entonces la columna "Yield" muestra el porcentaje con 1 decimal para esa fila.
- Dado que el usuario ingresa títulos adicionales en la tabla y hay precio actual, cuando `adicionales > 0`, entonces la columna "Costo compra" muestra `adicionales × precioActual` en MXN.
- Dado que el usuario selecciona FIBRA y escribe títulos en "¿Qué pasaría si...?" con precio disponible, cuando `títulos > 0`, entonces el MetricCard "Renta mensual estimada" muestra la renta mensual proyectada.
- Dado que el usuario escribe una renta mensual objetivo, cuando hay suficientes datos de renta, entonces aparece el número de títulos adicionales necesarios y la inversión estimada.
- Dado que la renta actual ya supera el objetivo, cuando se calcula, entonces aparece "Ya cumples el objetivo con tus posiciones actuales."
- Dado que la FIBRA no tiene datos de renta, cuando se calcula el objetivo, entonces aparece "Datos de renta no disponibles para esta FIBRA."

## Spec Change Log

## Design Notes

- `calcRentaProyectadaAnual`: si `dividendYieldPct > 0` usa `currentRentaAnual + additionalTitulos × precioActual × (dividendYieldPct/100)`. Si yield null y `currentTitulos > 0 && currentRentaAnual > 0`, usa `rentaPerTitle = currentRentaAnual/currentTitulos` como proxy — es el yield implícito basado en las distribuciones reales registradas en el portafolio.
- `calcTitulosParaRentaTarget`: devuelve el total de títulos necesarios (no los adicionales) para mantener la aritmética pura. El caller calcula `adicionales = max(0, result - currentTitulos)`.
- El 4.º MetricCard expande el `grid-cols-3` actual a `grid-cols-4` en sm+ — o `sm:grid-cols-2 lg:grid-cols-4` para mantener el diseño responsive.
- La subsección inversa va dentro del mismo `<section>` del "¿Qué pasaría si...?", separada por un `<hr>` o borde fino, para no requerir nueva superficie.

## Suggested Review Order

**Lógica pura (entrada al diseño)**

- Punto de partida: función de renta proyectada con guarda `!= null` (no `> 0`) para yield=0
  [`simulador-logic.ts:38`](../../src/Web/Main/src/modules/oportunidades/simulador-logic.ts#L38)

- Calculadora inversa: devuelve total de títulos (no adicionales); el caller calcula el delta
  [`simulador-logic.ts:56`](../../src/Web/Main/src/modules/oportunidades/simulador-logic.ts#L56)

**Derivaciones en PromediarTab — sección "¿Qué pasaría si...?"**

- Obtención de dividendYieldPct y currentRentaAnual del row seleccionado
  [`PromediarTab.tsx:173`](../../src/Web/Main/src/modules/oportunidades/PromediarTab.tsx#L173)

- rentaAnualEstimada y conversión a mensual; guardado con canSimulateWhatIf
  [`PromediarTab.tsx:176`](../../src/Web/Main/src/modules/oportunidades/PromediarTab.tsx#L176)

- Calculadora inversa: parseFloat → titulosTotalesParaTarget → adicionales → costo; flags ya_cumplido y sin_datos
  [`PromediarTab.tsx:187`](../../src/Web/Main/src/modules/oportunidades/PromediarTab.tsx#L187)

**UI — "¿Qué pasaría si...?" ampliada**

- Grid expandido a 4 columnas; 4.º MetricCard "Renta mensual estimada"
  [`PromediarTab.tsx:379`](../../src/Web/Main/src/modules/oportunidades/PromediarTab.tsx#L379)

- Subsección "Calcular objetivo de renta": input + tres estados condicionales (cumplido / sin datos / resultado)
  [`PromediarTab.tsx:404`](../../src/Web/Main/src/modules/oportunidades/PromediarTab.tsx#L404)

**UI — tabla**

- Headers nuevos "Yield" y "Costo compra" insertados en thead
  [`PromediarTab.tsx:220`](../../src/Web/Main/src/modules/oportunidades/PromediarTab.tsx#L220)

- yieldPct y costoCompra calculados en el loop de filas
  [`PromediarTab.tsx:239`](../../src/Web/Main/src/modules/oportunidades/PromediarTab.tsx#L239)

**Tests**

- Casos de renta proyectada: yield disponible, fallback rentaPerTitle, yield explícito 0%, adicionales ≤ 0
  [`simulador-logic.test.ts:71`](../../src/Web/Main/src/modules/oportunidades/simulador-logic.test.ts#L71)

- Casos de calculadora inversa: yield disponible, fallback, objetivo ya cubierto, sin datos, target ≤ 0
  [`simulador-logic.test.ts:100`](../../src/Web/Main/src/modules/oportunidades/simulador-logic.test.ts#L100)

## Verification

**Commands:**
- `cd src/Web/Main && npm test` -- expected: todos los tests pasan incluyendo ≥5 nuevos casos en `simulador-logic.test.ts`
- `cd src/Web/Main && npx tsc --noEmit` -- expected: 0 errores
