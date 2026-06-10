# Story 10.2: Calculadora ISR Integrada en Distribuciones y Herramientas

Status: ready-for-dev

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

- [ ] T0 — Backend: exponer campos fiscales en `DistributionPointDto` (requiere Historia 10.1 en BD)
  - [ ] T0.1 — En `SharedApiContracts`, agregar a `DistributionPointDto`:
    ```csharp
    public decimal? TaxableAmountPerUnit { get; init; }
    public decimal? CapitalReturnAmountPerUnit { get; init; }
    ```
  - [ ] T0.2 — En el endpoint `GET /api/v1/market/fibras/{ticker}/history`, mapear desde `Distribution.TaxableAmount` y `Distribution.CapitalReturnAmount` al DTO
  - [ ] T0.3 — Regenerar el cliente TypeScript (`npm run codegen:api`) y verificar que aparecen los nuevos campos opcionales en `schema.d.ts`

- [ ] T1 — Función pura de cálculo ISR (AC: 2, 3, 4)
  - [ ] T1.1 — Crear `src/Web/Main/src/modules/herramientas/isrCalculator.ts` con:

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

  - [ ] T1.2 — Unit tests en `isrCalculator.test.ts`:
    - Con desglose: dist=0.62, taxable=0.40, units=500 → taxableGross=200, isr=60, capitalReturn=110, net=250, isEstimate=false
    - Sin desglose: dist=0.62, units=500 → taxableGross=310, isr=93, capitalReturn=0, net=217, isEstimate=true
    - Units=0 → safeUnits=1 (valores por unidad)
    - taxablePerUnit > distPerUnit → capitalBase=0 (Math.max protege de negativo)
    - dist=0 → todos cero, sin error

- [ ] T2 — Componente `IsrCalculatorWidget.tsx` en ficha de FIBRA (AC: 1, 2, 3, 4, 6)
  - [ ] T2.1 — Crear `src/Web/Main/src/modules/ficha-publica/IsrCalculatorWidget.tsx`
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
  - [ ] T2.2 — UI: card con título "Calculadora ISR". Dos inputs (paso 0.0001 y 1). Tabla de resultados:
    - Si `isEstimate=false`: fila "Resultado Fiscal (bruto)" + fila "ISR retenido (30%)" + fila "Reembolso de Capital *" + fila negrita "Distribución neta". Nota al pie: "* Reembolso de capital no sujeto a retención. Reduce tu costo base fiscal para la venta futura de CBFIs."
    - Si `isEstimate=true`: banner amarillo de advertencia (AC3), y tabla con: fila "Distribución bruta (est.)" + fila "ISR estimado (30%)" + fila "Neto estimado". Sin fila de reembolso de capital.
  - [ ] T2.3 — Nota informativa fija debajo de los resultados: "Tasa de retención provisional del 30% sobre resultado fiscal para personas físicas residentes en México (LISR Art. 188). No considera deducciones adicionales ni regímenes especiales."
  - [ ] T2.4 — Integrar en `FibraPage.tsx`, pasando desde la última distribución de `distributions[0]`: `amountPerUnit`, `taxableAmountPerUnit` y `capitalReturnAmountPerUnit`

- [ ] T3 — Página `/herramientas` (AC: 5, 7)
  - [ ] T3.1 — Crear módulo `src/Web/Main/src/modules/herramientas/` con `HerramientasPage.tsx`
  - [ ] T3.2 — Sección "Calculadora Yield": inputs Precio actual (MXN) y Distribución trimestral (MXN), output Yield anualizado %. Lógica: `yield = (dist * 4) / precio`. Mostrar `—` si precio = 0.
  - [ ] T3.3 — Sección "Calculadora ISR": standalone sin pre-fill, siempre en modo estimado (`taxablePerUnit=undefined`). Muestra banner de advertencia del AC3 de forma permanente con texto: "La calculadora asume que el 100% es resultado fiscal. Para un cálculo exacto, ingresa el desglose fiscal de tu brokerage."
  - [ ] T3.4 — Registrar ruta `/herramientas` en `App.tsx` (pública)
  - [ ] T3.5 — Agregar enlace "Herramientas" al nav principal

- [ ] T4 — SEO y meta tags para `/herramientas` (AC: 7)
  - [ ] T4.1 — `<title>Herramientas para Inversores en FIBRAs — FIBRADIS</title>`
  - [ ] T4.2 — `<meta name="description">` 120-160 chars con mención de calculadora ISR y yield
  - [ ] T4.3 — Tags OG básicos
  - [ ] T4.4 — Verificar ruta responde 200 en hit directo (prerender)

- [ ] T5 — Unit tests adicionales (AC: 2, 3, 5)
  - [ ] T5.1 — Test `calcYield`: yield=null cuando precio=0; yield correcto con valores típicos
  - [ ] T5.2 — Test `calcIsr` casos límite: dist negativa → bruto 0; taxablePerUnit > distPerUnit → capitalReturn=0; units negativas → safeUnits=1

- [ ] T6 — Build final y validación
  - [ ] T6.1 — `npm run build --workspace=src/Web/Main` sin errores TypeScript
  - [ ] T6.2 — Verificar que la ficha de una FIBRA con desglose fiscal disponible muestra la tabla completa (resultado fiscal + reembolso + ISR + neto)
  - [ ] T6.3 — Verificar que la ficha de una FIBRA SIN desglose muestra el banner amarillo y la tabla estimada
  - [ ] T6.4 — Verificar `/herramientas` en browser: ambas calculadoras funcionan, sin flash de contenido

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

### Completion Notes List

### File List
