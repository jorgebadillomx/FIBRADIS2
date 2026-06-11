# Story 10.2: Calculadora ISR Integrada en Distribuciones y Herramientas

Status: done

## Story

Como visitante o usuario,
quiero una calculadora que me muestre cuánto ISR retiene mi brokerage sobre cada distribución de FIBRA y cuánto recibo neto,
para que pueda estimar mi ingreso real después de impuestos sin necesitar conocer la legislación fiscal.

## Acceptance Criteria

1. **Dado que** veo la sección de distribuciones en la ficha pública de una FIBRA y la última distribución tiene monto registrado, **Entonces** bajo la tabla de historial aparece el bloque "Calculadora ISR" con: campo Distribución por CBFI (pre-llenado con el último valor), campo Tus CBFIs (vacío, opcional) y nota de tasa ISR 30% (no editable).

2. **Dado que** la última distribución tiene desglose fiscal disponible (taxableAmountPerUnit y capitalReturnAmountPerUnit en la API), e ingreso "500" en Tus CBFIs con distribución total $0.62 (resultado fiscal $0.40 / reembolso $0.22), **Entonces** la calculadora muestra en tiempo real:
   - Resultado Fiscal bruto: $200.00
   - ISR retenido (30%): $60.00
   - Reembolso de Capital: $110.00 *(no sujeto a retención)*
   - **Distribución neta: $250.00**

3. **Dado que** la última distribución NO tiene desglose fiscal (taxableAmountPerUnit es null), **Entonces** la calculadora muestra un banner de advertencia "⚠️ Desglose fiscal no disponible — ISR calculado sobre monto total (estimado conservador)" y aplica el 30% sobre la distribución completa. Con 500 CBFIs y dist=$0.62: Bruto $310.00, ISR est. (30%) $93.00, Neto est. $217.00.

4. **Dado que** dejo Tus CBFIs vacío o en 0, **Entonces** la calculadora muestra los valores por unidad usando la misma lógica de desglose o estimado según disponibilidad.

5. **Dado que** navego a `/herramientas`, **Cuando** carga la página, **Entonces** veo dos secciones: "Calculadora Yield" (precio y distribución trimestral como inputs → yield anualizado) y "Calculadora ISR" (distribución por CBFI y número de CBFIs → bruto/ISR/neto). La calculadora ISR standalone asume el monto completo como base (sin desglose) y muestra la nota de estimado. Ambas secciones son públicas.

6. **Dado que** la distribución por CBFI no está disponible para una FIBRA (null en la API), **Entonces** el bloque "Calculadora ISR" en la ficha muestra el estado "Sin datos de distribución disponibles" sin romper el layout.

7. La ruta `/herramientas` es pública, tiene `<title>` y `<meta description>` correctos para SEO.

## Dependencia

Esta historia requiere que la **Historia 10.1 esté completada** antes de implementar T2–T3, ya que depende de los campos `taxableAmountPerUnit?` y `capitalReturnAmountPerUnit?` en `DistributionPointDto`. La tarea T0 actualiza el DTO del backend para exponer estos campos.

## Tasks / Subtasks

- [x] T0 — Backend: exponer campos fiscales en `DistributionPointDto` (requiere Historia 10.1 en BD)
  - [x] T0.1 — En `SharedApiContracts`, agregar a `DistributionPointDto`:
    ```csharp
    public decimal? TaxableAmountPerUnit { get; init; }
    public decimal? CapitalReturnAmountPerUnit { get; init; }
    ```
  - [x] T0.2 — En el endpoint `GET /api/v1/market/fibras/{ticker}/history`, mapear desde `Distribution.TaxableAmount` y `Distribution.CapitalReturnAmount` al DTO
  - [x] T0.3 — Regenerar el cliente TypeScript (`npm run codegen:api`) y verificar que aparecen los nuevos campos opcionales en `schema.d.ts`

- [x] T1 — Función pura de cálculo ISR (AC: 2, 3, 4)
  - [x] T1.1 — Crear `src/Web/Main/src/modules/herramientas/isrCalculator.ts` con:

    ```typescript
    export const ISR_RATE = 0.30;

    export interface IsrResult {
      taxableGross: number;    // resultado fiscal * units
      capitalReturn: number;   // reembolso de capital * units
      isr: number;             // taxableGross * ISR_RATE
      net: number;             // taxableGross - isr + capitalReturn
      unitsUsed: number;
      isEstimate: boolean;     // true si taxablePerUnit no disponible
    }

    export function calcIsr(
      distPerUnit: number,
      units = 1,
      taxablePerUnit?: number | null
    ): IsrResult {
      const safeUnits = Math.max(units, 1);
      const isEstimate = taxablePerUnit == null;
      const taxableBase = isEstimate ? distPerUnit : taxablePerUnit;
      const capitalBase = isEstimate ? 0 : Math.max(0, distPerUnit - taxablePerUnit);
      const taxableGross = taxableBase * safeUnits;
      const capitalReturn = capitalBase * safeUnits;
      const isr = taxableGross * ISR_RATE;
      return { taxableGross, capitalReturn, isr, net: taxableGross - isr + capitalReturn, unitsUsed: safeUnits, isEstimate };
    }
    ```

  - [x] T1.2 — Unit tests en `isrCalculator.test.ts`:
    - Con desglose: dist=0.62, taxable=0.40, units=500 → taxableGross=200, isr=60, capitalReturn=110, net=250, isEstimate=false
    - Sin desglose: dist=0.62, units=500 → taxableGross=310, isr=93, capitalReturn=0, net=217, isEstimate=true
    - Units=0 → safeUnits=1 (valores por unidad)
    - taxablePerUnit > distPerUnit → capitalBase=0 (Math.max protege de negativo)
    - dist=0 → todos cero, sin error

- [x] T2 — Componente `IsrCalculatorWidget.tsx` en ficha de FIBRA (AC: 1, 2, 3, 4, 6)
  - [x] T2.1 — Crear `src/Web/Main/src/modules/ficha-publica/IsrCalculatorWidget.tsx`
    - Props:

      ```typescript
      interface IsrCalculatorWidgetProps {
        lastDistribution?: number | null;
        taxableAmountPerUnit?: number | null;
        capitalReturnAmountPerUnit?: number | null;
      }
      ```

    - State local: `distPerUnit` (inicializado a `lastDistribution ?? ''`), `units` (string, vacío por defecto)
    - Cálculo `useMemo` sobre `calcIsr(distPerUnit, units, taxableAmountPerUnit)`
    - Si `lastDistribution` es null/undefined → mostrar empty state del AC6
  - [x] T2.2 — UI: card con título "Calculadora ISR". Dos inputs (paso 0.0001 y 1). Tabla de resultados:
    - Si `isEstimate=false`: fila "Resultado Fiscal (bruto)" + fila "ISR retenido (30%)" + fila "Reembolso de Capital *" + fila negrita "Distribución neta". Nota al pie: "* Reembolso de capital no sujeto a retención. Reduce tu costo base fiscal para la venta futura de CBFIs."
    - Si `isEstimate=true`: banner amarillo de advertencia (AC3), y tabla con: fila "Distribución bruta (est.)" + fila "ISR estimado (30%)" + fila "Neto estimado". Sin fila de reembolso de capital.
  - [x] T2.3 — Nota informativa fija debajo de los resultados: "Tasa de retención provisional del 30% sobre resultado fiscal para personas físicas residentes en México (LISR Art. 188). No considera deducciones adicionales ni regímenes especiales."
  - [x] T2.4 — Integrar en `FibraPage.tsx`, pasando desde la última distribución de `distributions[0]`: `amountPerUnit`, `taxableAmountPerUnit` y `capitalReturnAmountPerUnit`

- [x] T3 — Página `/herramientas` (AC: 5, 7)
  - [x] T3.1 — Crear módulo `src/Web/Main/src/modules/herramientas/` con `HerramientasPage.tsx`
  - [x] T3.2 — Sección "Calculadora Yield": inputs Precio actual (MXN) y Distribución trimestral (MXN), output Yield anualizado %. Lógica: `yield = (dist * 4) / precio`. Mostrar `—` si precio = 0.
  - [x] T3.3 — Sección "Calculadora ISR": standalone sin pre-fill, siempre en modo estimado (`taxablePerUnit=undefined`). Muestra banner de advertencia del AC3 de forma permanente con texto: "La calculadora asume que el 100% es resultado fiscal. Para un cálculo exacto, ingresa el desglose fiscal de tu brokerage."
  - [x] T3.4 — Registrar ruta `/herramientas` en `App.tsx` (pública)
  - [x] T3.5 — Agregar enlace "Herramientas" al nav principal

- [x] T4 — SEO y meta tags para `/herramientas` (AC: 7)
  - [x] T4.1 — `<title>Herramientas para Inversores en FIBRAs — FIBRADIS</title>`
  - [x] T4.2 — `<meta name="description">` 120-160 chars con mención de calculadora ISR y yield
  - [x] T4.3 — Tags OG básicos
  - [x] T4.4 — Verificar ruta responde 200 en hit directo (prerender)

- [x] T5 — Unit tests adicionales (AC: 2, 3, 5)
  - [x] T5.1 — Test `calcYield`: yield=null cuando precio=0; yield correcto con valores típicos
  - [x] T5.2 — Test `calcIsr` casos límite: dist negativa → bruto 0; taxablePerUnit > distPerUnit → capitalReturn=0; units negativas → safeUnits=1

- [x] T6 — Build final y validación
  - [x] T6.1 — `npm run build --workspace=src/Web/Main` sin errores TypeScript
  - [x] T6.2 — Verificar que la ficha de una FIBRA con desglose fiscal disponible muestra la tabla completa (resultado fiscal + reembolso + ISR + neto)
  - [x] T6.3 — Verificar que la ficha de una FIBRA SIN desglose muestra el banner amarillo y la tabla estimada
  - [x] T6.4 — Verificar `/herramientas` en browser: ambas calculadoras funcionan, sin flash de contenido

## Dev Notes

### Contexto competitivo

FibraBase tiene una "Calculadora de Rendimiento" trivial (precio ÷ distribución) y un "Simulador de Retención ISR" marcado como coming soon. Esta historia llega primero y va más lejos: integra el cálculo ISR directamente en la ficha de cada FIBRA con datos pre-llenados de la API, y es la única que distingue entre resultado fiscal y reembolso de capital.

### Cálculo ISR — base legal

**LISR Art. 187-188** (no LGTF — corrección importante respecto al diseño anterior):

La FIBRA retiene ISR **únicamente sobre el Resultado Fiscal** de la distribución, a una tasa provisional del **30%**. Esta retención es provisional (pago provisional); el impuesto definitivo se calcula en la declaración anual según la tarifa del contribuyente (1.92%–35%).

El **Reembolso de Capital** (parte de la distribución que excede el resultado fiscal) **no está sujeto a retención de ISR** al momento del pago. En cambio, reduce el costo comprobado de adquisición (CUCA) del tenedor — el ISR se difiere hasta que el inversionista venda sus CBFIs (ganancia de capital).

**Cuando hay desglose (taxableAmountPerUnit disponible):**
```
taxableGross = taxableAmountPerUnit * units
capitalReturn = (distPerUnit - taxableAmountPerUnit) * units
isr           = taxableGross * 0.30
neto          = taxableGross - isr + capitalReturn
```

**Cuando NO hay desglose (estimado conservador):**
```
bruto = distPerUnit * units
isr   = bruto * 0.30   // peor caso: todo es resultado fiscal
neto  = bruto - isr
```
→ Mostrar siempre el banner de advertencia en este caso.

### Qué sucede si masdividendos deja de estar disponible

Si la fuente masdividendos.mx deja de funcionar (Historia 10.1), las distribuciones seguirán llegando vía Yahoo Finance sin desglose. En ese caso `taxableAmountPerUnit` y `capitalReturnAmountPerUnit` serán `null` en la API. La calculadora degradará automáticamente al modo estimado (AC3 / banner amarillo) — sin romper la UI ni dar números incorrectos.

El operador puede ingresar el desglose manualmente vía Ops (Historia 10.1 T8) para que la calculadora muestre el desglose exacto.

### Función pura `calcIsr`

La función acepta `taxablePerUnit` opcional. Si se omite o es null, opera en modo estimado.

```typescript
export const ISR_RATE = 0.30;

export interface IsrResult {
  taxableGross: number;
  capitalReturn: number;
  isr: number;
  net: number;
  unitsUsed: number;
  isEstimate: boolean;
}

export function calcIsr(
  distPerUnit: number,
  units = 1,
  taxablePerUnit?: number | null
): IsrResult {
  const safeUnits = Math.max(units, 1);
  const isEstimate = taxablePerUnit == null;
  const taxableBase = isEstimate ? distPerUnit : taxablePerUnit;
  const capitalBase = isEstimate ? 0 : Math.max(0, distPerUnit - taxablePerUnit);
  const taxableGross = taxableBase * safeUnits;
  const capitalReturn = capitalBase * safeUnits;
  const isr = taxableGross * ISR_RATE;
  return {
    taxableGross,
    capitalReturn,
    isr,
    net: taxableGross - isr + capitalReturn,
    unitsUsed: safeUnits,
    isEstimate,
  };
}
```

Tests obligatorios:
```typescript
it('calcula con desglose: dist=0.62, taxable=0.40, 500 units', () => {
  const r = calcIsr(0.62, 500, 0.40);
  expect(r.taxableGross).toBeCloseTo(200);
  expect(r.isr).toBeCloseTo(60);
  expect(r.capitalReturn).toBeCloseTo(110);
  expect(r.net).toBeCloseTo(250);
  expect(r.isEstimate).toBe(false);
});

it('calcula en modo estimado (sin desglose): dist=0.62, 500 units', () => {
  const r = calcIsr(0.62, 500);
  expect(r.taxableGross).toBeCloseTo(310);
  expect(r.isr).toBeCloseTo(93);
  expect(r.capitalReturn).toBe(0);
  expect(r.net).toBeCloseTo(217);
  expect(r.isEstimate).toBe(true);
});

it('usa 1 unidad cuando units=0 (denominador cero)', () => {
  const r = calcIsr(0.62, 0);
  expect(r.unitsUsed).toBe(1);
  expect(r.taxableGross).toBeCloseTo(0.62);
});
```

### Función pura `calcYield` (Calculadora Yield en `/herramientas`)

```typescript
export function calcYield(quarterlyDist: number, currentPrice: number): number | null {
  if (currentPrice <= 0) return null;
  return (quarterlyDist * 4) / currentPrice;
}
```

Asume frecuencia trimestral fija (4 pagos/año). Es aceptable para la calculadora standalone donde el usuario ingresa manualmente.

### Integración en FibraPage — dónde colocar el widget

La ficha pública (`FibraPage.tsx`) ya tiene una sección de distribuciones que muestra la tabla histórica. El widget `IsrCalculatorWidget` va **debajo de la tabla de distribuciones**, antes del cierre de la sección.

Para obtener los datos:
- La API `/api/v1/market/fibras/{ticker}/history` devuelve `FibraHistoryDto` con `distributions: DistributionPointDto[]`
- Ordenar por fecha descendente y tomar `distributions[0]`
- Pasar `amountPerUnit`, `taxableAmountPerUnit` y `capitalReturnAmountPerUnit` como props

```typescript
const lastDist = [...(history?.distributions ?? [])].sort(
  (a, b) => b.date.localeCompare(a.date)
)[0];

<IsrCalculatorWidget
  lastDistribution={lastDist?.amountPerUnit}
  taxableAmountPerUnit={lastDist?.taxableAmountPerUnit}
  capitalReturnAmountPerUnit={lastDist?.capitalReturnAmountPerUnit}
/>
```

### Formato de moneda MXN

```typescript
const fmtUnit  = new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN', minimumFractionDigits: 4 });
const fmtTotal = new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN', minimumFractionDigits: 2 });
```

### `/herramientas` — layout

Página pública simple, dos cards side-by-side en desktop, stack en móvil (Tailwind grid `md:grid-cols-2`). No agregar dependencias externas para las calculadoras.

### Checklist de seguridad — completar antes del primer commit

- [ ] **TOCTOU doble-request**: no aplica — calculadoras son 100% frontend estático, sin escrituras a BD.
- [ ] **Auth-gating**: calculadoras son públicas. El widget en FibraPage no requiere auth — verificar que no se rompe para usuarios anónimos.
- [ ] **Denominador cero**: `calcYield(dist, 0)` → null; `calcIsr(dist, 0)` → safeUnits=1. Primer test obligatorio para ambas.
- [ ] **taxablePerUnit > distPerUnit**: `Math.max(0, distPerUnit - taxablePerUnit)` previene capitalReturn negativo.

### Project Structure Notes

Archivos a crear (NEW):
- `src/Web/Main/src/modules/herramientas/HerramientasPage.tsx`
- `src/Web/Main/src/modules/herramientas/isrCalculator.ts`
- `src/Web/Main/src/modules/herramientas/isrCalculator.test.ts`
- `src/Web/Main/src/modules/ficha-publica/IsrCalculatorWidget.tsx`

Archivos a modificar (UPDATE):

- `src/Server/SharedApiContracts/Market/DistributionPointDto.cs` — agregar TaxableAmountPerUnit y CapitalReturnAmountPerUnit (T0)
- `src/Server/Api/Endpoints/Public/MarketEndpoints.cs` — mapear nuevos campos en history endpoint (T0)
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` — integrar IsrCalculatorWidget (T2.4)
- `src/Web/Main/src/App.tsx` — agregar ruta `/herramientas`
- Nav — agregar enlace Herramientas

### References

- Patrón funciones puras testables: [convenciones-fibradis.md — Testing — Funciones de Cálculo Financiero]
- Patrón `calcDifPct` en portafolio: [src/Web/Main/src/modules/portafolio/]
- API distribuciones: [src/Server/Api/Endpoints/Public/MarketEndpoints.cs — GET /fibras/{ticker}/history]
- Historia 10.1: campos Distribution.TaxableAmount y Distribution.CapitalReturnAmount
- LISR Art. 188 — tasa de retención provisional 30% solo sobre resultado fiscal
- Checklist SEO: [convenciones-fibradis.md — Checklist de cierre para historias públicas con SSR/SEO]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6 (bmad-create-story + revisión legal)

### Debug Log References

- `npm run codegen:api`
- `npm run build --workspace=src/Web/Main`
- `npm test --workspace=src/Web/Main`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~MarketHistoryEndpointTests"`
- `Playwright` against `http://127.0.0.1:4173/herramientas` confirmed `200`, the SEO title/description, and both calculator cards in the rendered DOM.

### Completion Notes List

- Expuse `taxableAmountPerUnit` y `capitalReturnAmountPerUnit` end-to-end desde el dominio hasta `DistributionPointDto`, endpoint de historial y `schema.d.ts`.
- Implementé `calcIsr` y `calcYield` con tests unitarios para desgloses exactos, modo estimado y bordes numéricos.
- Añadí `IsrCalculatorWidget` en la ficha pública, la página `/herramientas`, la ruta pública y el enlace del nav.
- Dejé SEO para `/herramientas` con `title`, `description` y OG tags, y agregué la ruta al prerender estático.
- Validé con build frontend, tests del SPA, tests de integración del endpoint y verificación de navegador para `/herramientas`.

### Change Log

- 2026-06-10: Backend, cliente OpenAPI y frontend actualizados para la calculadora ISR integrada, página pública `/herramientas`, SEO y validaciones.

### File List

- `_bmad-output/implementation-artifacts/10-2-calculadora-isr.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `scripts/codegen/Api.json`
- `src/Server/Api/Endpoints/Public/MarketEndpoints.cs`
- `src/Server/SharedApiContracts/Market/FibraHistoryDto.cs`
- `src/Web/Main/package.json`
- `src/Web/Main/scripts/prerender.mjs`
- `src/Web/Main/src/app/routes.tsx`
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx`
- `src/Web/Main/src/modules/ficha-publica/IsrCalculatorWidget.tsx`
- `src/Web/Main/src/modules/herramientas/HerramientasPage.tsx`
- `src/Web/Main/src/modules/herramientas/isrCalculator.ts`
- `src/Web/Main/src/modules/herramientas/isrCalculator.test.ts`
- `src/Web/Main/src/modules/herramientas/yieldCalculator.ts`
- `src/Web/Main/src/modules/herramientas/yieldCalculator.test.ts`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Integration/Api.Tests/MarketHistoryEndpointTests.cs`

## Senior Developer Review (AI)

### Review Findings — 2026-06-10

**Resultado:** 0 `decision-needed` · 10 `patch` · 4 `defer` · 9 descartados

#### Patches

- [x] `patch` **P1 MEDIUM — `taxableBase` cap no documentado (desviación de spec):** `isrCalculator.ts:34` usa `Math.min(safeDist, Math.max(0, taxableValue))`, que limita la base fiscal al monto distribuido. La spec dice usar `taxablePerUnit` sin límite. El test `calcIsr(0.40, 500, 0.62)` aserta `taxableGross=200` (con límite), no 310 (spec). El comportamiento es más conservador y correcto para retención real (no se puede retener más de lo distribuido), pero la divergencia con la spec y el test que la confirma deben documentarse con un comentario `// Cap: ISR no puede exceder la distribución recibida`. No cambiar la lógica.
- [x] `patch` **P2 MEDIUM — `calcYield` devuelve porcentaje sin documentar:** `yieldCalculator.ts:1` devuelve 0–100 (e.g. 2.48) pero la spec define fracción (0.0248). Internamente consistente con `formatPercent`, pero ningún comentario advierte al caller. Añadir JSDoc: `/** Devuelve el rendimiento anualizado en porcentaje (0–100), e.g. 2.48 para 2.48%. */`
- [x] `patch` **P3 MEDIUM — `calcYield(0, precio)` muestra "0.00%" en lugar de "—":** Cuando el campo distribución está vacío, `parseInput('')=0`, `calcYield(0,100)=0`, y el widget muestra "0.00%" como si el dato fuera conocido. Fix: retornar `null` cuando `safeDist <= 0` en `yieldCalculator.ts:4`. Actualizar test correspondiente.
- [x] `patch` **P4 MEDIUM — Fila "bruta" en modo estimado suma `capitalReturn` (addend siempre 0):** `IsrCalculatorWidget.tsx:128` muestra `formatMoney(result.taxableGross + result.capitalReturn)`. En modo estimado `capitalReturn` es siempre 0, por lo que la suma es redundante pero frágil ante un cambio futuro en `calcIsr`. Fix: usar `result.taxableGross` directamente en la rama de estimado.
- [x] `patch` **P5 MEDIUM — `ISR_RATE` no se usa en las etiquetas de la UI:** La tasa "30%" está hardcodeada como string en badges y filas de tabla en `IsrCalculatorWidget.tsx` y `HerramientasPage.tsx`. Si la tasa cambia, las etiquetas se desincronizarán. Fix: derivar con `${(ISR_RATE * 100).toFixed(0)}%` importando la constante.
- [x] `patch` **P6 LOW — `og:url` ausente en `HerramientasPage.tsx`:** T4.3 requiere OG básico; falta `og:url`. Fix: añadir `<meta property="og:url" content="https://fibradis.mx/herramientas" />`.
- [x] `patch` **P7 LOW — `parseInput` duplicada:** Función idéntica en `IsrCalculatorWidget.tsx:18` y `HerramientasPage.tsx:6`. Fix: exportar desde `isrCalculator.ts` o un util compartido e importar en ambos componentes.
- [x] `patch` **P8 LOW — `reportedTaxablePerUnit` código muerto en modo estimado:** `IsrCalculatorWidget.tsx:53-64` calcula `reportedTaxablePerUnit` y `reportedCapitalReturnPerUnit` incluso cuando `result.isEstimate=true`, pero el bloque que los usa está protegido por `{!result.isEstimate ? ... }`. Fix: mover el cálculo dentro de la rama de no-estimado o eliminar el fallback innecesario.
- [x] `patch` **P9 LOW — El campo `units` acepta valores fraccionarios:** `step="1"` en el `<input type="number">` no impide tipear "1.5"; `normalizeUnits(1.5)=1.5` produce totales con fracciones de CBFI. Fix: aplicar `Math.floor(Math.max(value, 1))` en `normalizeUnits` en `isrCalculator.ts:18`.
- [x] `patch` **P10 LOW — Sin indicador "por CBFI" cuando unidades=0 (AC4):** Cuando el campo unidades está vacío, `safeUnits=1` y los resultados son por unidad, pero no hay etiqueta que lo indique al usuario. Fix: mostrar "(por CBFI)" en el encabezado de resultados del widget cuando `parseInput(units) === 0`.

#### Deferred

- [x] `defer` W1: Multiplicador trimestral fijo (×4) en `calcYield` — la spec documenta explícitamente esta suposición. `yieldCalculator.ts` — pre-existente, aceptable.
- [x] `defer` W2: Inicialización de `distPerUnit` desde prop — anti-patrón React, pero seguro en uso actual porque el widget se desmonta al cambiar de ticker (history→undefined). `IsrCalculatorWidget.tsx:35` — pre-existente, sin riesgo inmediato.
- [x] `defer` W3: `taxableAmountPerUnit=0` en modo desglose muestra ISR=$0 sin banner — comportamiento correcto para FIBRAs con 100% retorno de capital (null=sin desglose, 0=resultado fiscal cero). Contrato de la API. `isrCalculator.ts:29`.
- [x] `defer` W4: Inconsistencia de nombres `CalendarEventDto.TaxableAmount` vs `DistributionPointDto.TaxableAmountPerUnit` — problema cross-story introducido en 10-1, fuera del alcance de esta historia. `FibraHistoryDto.cs`.
