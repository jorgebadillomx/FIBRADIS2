# Deferred Work

Items deferred from story reviews. Each entry includes the source story, the finding, and why it was deferred.

---

## From: spec-fix-ficha-precio-tooltip-distribuciones-mejoras (2026-06-12)

### D1 — Float accumulation en sumas de distribuciones mensuales

**Archivo:** `distribuciones.ts` — `groupDistributionsByPeriod`

IEEE-754 produce errores de redondeo al sumar 12 pagos mensuales (ej. 12× $0.15 = $1.8000000000000002). Con `.toFixed(4)` el impacto visual es menor, pero `calcPeriodDiff` puede producir diffs con error en el último decimal.

**Posible fix:** Acumular en enteros (×10000) y dividir al renderizar, o usar Kahan summation.

---

### D2 — IsrCalculatorWidget pre-rellena con pago individual, no con total del periodo

**Archivo:** `FibraPage.tsx` — `<IsrCalculatorWidget lastDistribution={toNum(history.distributions[0]?.amountPerUnit)} />`

El widget usa el pago individual más reciente (`distributions[0]?.amountPerUnit`), pero la tabla agrupada ahora muestra la suma del periodo. Para FIBRAs con múltiples pagos por periodo el valor pre-llenado es inconsistente con el total mostrado en la tabla.

**Posible fix:** Pasar `allGroups[0]?.total` al widget, o añadir nota en la UI explicando que es el último pago individual.
