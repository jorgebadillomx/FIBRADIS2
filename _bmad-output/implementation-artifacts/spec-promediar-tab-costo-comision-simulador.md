---
title: 'PromediarTab — costo real con comisión+IVA, renta en tabla, bidireccional y panel retorno 2 años'
type: 'feature'
created: '2026-06-18'
status: 'done'
baseline_commit: 'f93ba9552980b9a975c6bf5d82cc08aaaca85da5'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La tabla de PromediarTab en /oportunidades muestra "Costo compra" sin comisión ni IVA, no tiene columna de "Renta mensual estimada", el campo commission_factor en Ops no acepta 4 decimales (0.0025), los encabezados no tienen tooltip descriptivo, la sección ¿Qué pasaría si? no muestra IVA ni tiene tarjeta de costo real, los campos Títulos y Renta mensual no son bidireccionales, el selector de FIBRA solo lista posiciones del portafolio, y no hay panel de retorno histórico.

**Approach:** Cambios frontend en PromediarTab.tsx (layout, lógica y UX) + nueva función `calcCostoPurchase` en simulador-logic + fix `step="0.0001"` en Ops ConfigPage + agregar período "2y" al endpoint `/market/fibras/{ticker}/history` para el panel de retorno.

## Boundaries & Constraints

**Always:**
- Fórmula costo compra: `costoTotal = (precio × cantidad) × (1 + commissionFactor × 1.16)` — IVA es 16% solo sobre la comisión
- IVA hardcodeado a 0.16 (constante, no configurable)
- Bidireccionalidad sin loop infinito: cada `onChange` hace set del campo propio y recalcula el derivado directamente en el mismo handler
- El selector de FIBRA en ¿Qué pasaría si? usa `allOpportunityRows` (ranked + limitedData); si la FIBRA no está en portafolio, `currentTitulos=0 / currentAvgCost=0 / currentRentaAnual=0`
- Tooltips de encabezados: atributo `title` nativo HTML en cada `<th>` (persiste mientras el cursor esté sobre el encabezado)
- Panel de retorno 2 años: solo se muestra cuando la query de history retorna datos; estado loading/error silencioso (no bloquea el resto)

**Ask First:**
- Si el backend no tiene DailySnapshots para la FIBRA seleccionada en el rango 2 años (pricePoints vacío), el panel muestra "Sin datos históricos suficientes" — ¿aceptable o debe ocultarse?

**Never:**
- Cambiar la validación backend de commissionFactor (ya acepta 0 < x ≤ 0.1; el bug es solo el `step` del input)
- Agregar nuevos endpoints más allá del case "2y" en el switch de period
- Modificar la tabla superior de PromediarTab más allá de agregar columnas y corregir la fórmula de costo compra

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Fórmula costo compra | precio=28.38, cantidad=10, commissionFactor=0.0025 | costoTotal = 284.62 | — |
| Commission display | commissionFactor=0.0025 | "Comisión aplicada: 0.25% · IVA: 16%" | — |
| Bidireccional títulos→renta | user escribe 10 en Títulos a comprar | whatIfTargetRenta se actualiza con rentaMensualEstimada | Si dividendYield null, renta = 0 |
| Bidireccional renta→títulos | user escribe 5000 en Renta mensual objetivo | whatIfTitulos se actualiza con titulosAdicionalesParaTarget | Si sin datos de renta, título queda en blanco |
| FIBRA sin posición en combo | usuario selecciona FIBRA no en portafolio | currentTitulos=0, calcula desde cero | — |
| Panel retorno sin snapshots | FIBRA sin history 2y | Muestra "Sin datos históricos" | No bloquea el resto de la UI |
| Panel con invertido=0 | whatIfTargetRenta vacío | CBFIs=0, todos los valores = $0.00 | Muestra panel con ceros |

</frozen-after-approval>

## Code Map

- `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` — componente principal; todos los cambios de UI y estado
- `src/Web/Main/src/modules/oportunidades/simulador-logic.ts` — agregar `calcCostoPurchase(precio, cantidad, commissionFactor, ivaFactor)`
- `src/Web/Main/src/modules/oportunidades/simulador-logic.test.ts` — tests para `calcCostoPurchase`
- `src/Web/Ops/src/pages/ConfigPage.tsx` — cambiar `step="0.001"` a `step="0.0001"` en commission_factor input
- `src/Server/Api/Endpoints/Public/MarketEndpoints.cs` — agregar `"2y" => 730` en el switch de period (línea ~76)

## Tasks & Acceptance

**Execution:**
- [ ] `src/Web/Main/src/modules/oportunidades/simulador-logic.ts` — agregar `export const IVA_FACTOR = 0.16` y `export function calcCostoPurchase(precio: number, cantidad: number, commissionFactor: number, ivaFactor = IVA_FACTOR): number { return precio * cantidad * (1 + commissionFactor * (1 + ivaFactor)) }` — fórmula canónica reutilizable
- [ ] `src/Web/Main/src/modules/oportunidades/simulador-logic.test.ts` — agregar tests: (a) 28.38 × 10 × 0.0025 + IVA → 284.623, (b) commissionFactor=0 → precio × cantidad exacto, (c) cantidad=0 → 0
- [ ] `src/Server/Api/Endpoints/Public/MarketEndpoints.cs` — en el `switch(period?.ToLowerInvariant())` agregar `"2y" => 730` — habilita historial de 2 años para el panel
- [ ] `src/Web/Ops/src/pages/ConfigPage.tsx` — cambiar `step="0.001"` a `step="0.0001"` en el input de commission_factor — permite guardar 0.0025
- [ ] `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` — **Tabla listing**: (1) corregir `costoCompra`: usar `calcCostoPurchase(precioActual, adicionalesNum, commissionFactor)` en lugar de `adicionalesNum * precioActual`; (2) agregar columna "Renta mens." entre "Nueva plusvalía" y "Costo compra" calculada con `calcRentaProyectadaAnual(toNum(position.rentaAnual), adicionalesNum, precioActual!, yieldPct, titulos) / 12` mostrando `—` si no hay datos; (3) agregar `title="..."` descriptivo a cada `<th>` de la tabla
- [ ] `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` — **Sección ¿Qué pasaría si?**: (1) reemplazar línea "Comisión aplicada" con `<span>Comisión aplicada: <b>{(commissionFactor * 100).toFixed(2)}%</b></span> <span>IVA: <b>16%</b></span>`; (2) agregar 5° `<MetricCard>` "Costo compra" usando `calcCostoPurchase(currentPrice!, additionalWhatIfTitles, commissionFactor)` entre "Renta mensual estimada" y el borde del grid (grid pasa a `sm:grid-cols-3 lg:grid-cols-5` o `2 rows`)
- [ ] `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` — **Bidireccional**: en `onChange` de "Títulos a comprar" recalcular `rentaMensualEstimada` y hacer `setWhatIfTargetRenta(String(Math.round(rentaMensualEstimada ?? 0)))`; en `onChange` de "Renta mensual objetivo" recalcular `titulosAdicionalesParaTarget` y hacer `setWhatIfTitulos(String(titulosAdicionalesParaTarget ?? ''))`
- [ ] `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` — **Combo todas las fibras**: cambiar el `<select>` options de `promediarRows.map(...)` a `allOpportunityRows.map(r => <option key={r.fibraId} value={r.fibraId}>{r.ticker} - {r.nombre}</option>)`; actualizar la lógica de `selectedWhatIfRow` para buscar en `allOpportunityRows` y caer back en `promediarRows` para los datos de posición
- [ ] `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` — **Panel retorno 2 años**: agregar `useQuery` para `GET /api/v1/market/fibras/{ticker}/history?period=2y` usando el ticker de `selectedWhatIfRow`; calcular `precioInicial = pricePoints[0].close`, `cbfisComprados = Math.floor((targetRenta || 0) / precioInicial)`, `dividendosPorCBFI = sum(dist.amountPerUnit where paymentDate >= hace2Años)`, `dividendosRecibidos = cbfisComprados * dividendosPorCBFI`, `valorHoy = cbfisComprados * (currentPrice || 0)`, `variacionCapital = valorHoy - cbfisComprados * precioInicial`, `rendimientoTotal = variacionCapital + dividendosRecibidos`; renderizar al lado derecho de la sección "Calcular objetivo de renta" (panel compacto con 4 secciones: Inversión inicial, Capital, Dividendos, Rendimiento total) en un `<aside>` que no altera el layout izquierdo

**Acceptance Criteria:**
- Dado commission_factor=0.0025 guardado en Ops, cuando se calcula costo compra de 10 títulos a $28.38, entonces el resultado es $284.62 (± 0.01)
- Dado que se escribe en "Títulos a comprar", cuando el valor cambia, entonces "Renta mensual objetivo" se actualiza automáticamente con la renta estimada
- Dado que se escribe en "Renta mensual objetivo", cuando el valor cambia, entonces "Títulos a comprar" se actualiza con los títulos adicionales necesarios
- Dado el input commission_factor en Ops con step="0.0001", cuando se escribe 0.0025, entonces el campo lo acepta sin error de validación
- Dado que se hace hover sobre un encabezado de la tabla, cuando se mantiene el cursor, entonces el tooltip se mantiene visible y desaparece solo al mover el cursor
- Dado un combo en la sección ¿Qué pasaría si?, cuando se abre el selector, entonces aparecen todas las FIBRAs del ranking (no solo las del portafolio)
- Dado whatIfTargetRenta=5000 y una FIBRA seleccionada, cuando el panel de retorno carga, entonces muestra "Inversión inicial", "Inversión del Capital", "Dividendos recibidos" y "Rendimiento total" correctamente calculados

## Design Notes

**Panel de retorno**: Estructura similar a la imagen del calculador existente — 4 secciones en fila dentro de un `<div className="rounded-xl border bg-card p-4">`. Usar la misma paleta de tarjetas que los MetricCard existentes. El panel se ubica a la DERECHA de la sección "Calcular objetivo de renta" dentro del grid existente (`lg:grid-cols-[1fr_1fr]` en la fila del border-t).

**Bidireccionalidad**: Ambos `onChange` son handlers síncronos. No usar `useEffect` para evitar renders extra. El valor del input controlado siempre refleja el estado.

**Columna Renta mens. en tabla listing**: Mostrar `fmtMxnNoDecimals(rentaMensual)` cuando hay simulación activa (adicionalesNum > 0), `—` si no hay adicionados o si no hay datos de yield.

## Spec Change Log

## Verification

**Commands:**
- `npm run test --workspace=packages/main-web -- simulador-logic` — expected: todos los tests verdes incluyendo los nuevos calcCostoPurchase
- `dotnet build FIBRADIS.slnx` — expected: 0 errores
- `npm run build --workspace=packages/main-web` — expected: 0 errores TypeScript
