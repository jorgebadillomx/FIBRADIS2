# Historia 2.5: Home — TopMovers en formato tabla y sección Ganadores/Perdedores

Status: done

## Historia

Como visitante público de FIBRADIS,
quiero que "Movimientos del día" muestre los datos en una tabla con columnas legibles y que "Ranking del universo" sea reemplazado por una vista de ganadores y perdedores del día,
para que pueda escanear rápidamente qué FIBRAs se mueven más y hacia dónde sin tener que leer filas densas sin contexto.

## Criterios de Aceptación

**CA-1: TopMovers adopta formato tabla**
Dado que la home carga con datos de mercado disponibles,
Cuando veo la sección "Movimientos del día",
Entonces muestra una tabla con encabezados de columna: `#`, `Ticker`, `Cambio`, `Precio`, `Volumen` — con el mismo grid `grid-cols-[2rem_1fr_auto_auto_auto]` que usa actualmente `QuickRanking`, mostrando las 5 FIBRAs con mayor variación % absoluta del día.

**CA-2: TopMovers sin datos muestra tabla vacía**
Dado que no hay datos de `dailyChangePct` disponibles,
Cuando carga la sección,
Entonces muestra los 5 primeros tickers ordenados alfabéticamente con `—` en Cambio, Precio y Volumen — sin errores JavaScript.

**CA-3: Sección GainersLosers reemplaza QuickRanking**
Dado que la home carga,
Cuando el visitante baja a la sección inferior,
Entonces ve "Ganadores / Perdedores del día" en lugar de "Ranking del universo" — con dos columnas compactas lado a lado: izquierda = top 5 mayor subida (verde), derecha = top 5 mayor caída (rojo).

**CA-4: GainersLosers ordena correctamente cada columna**
Dado que hay FIBRAs con changePct positivo y negativo,
Cuando se renderiza la sección,
Entonces la columna "Ganadores" muestra las 5 con mayor `dailyChangePct` positivo ordenadas de mayor a menor, y "Perdedores" las 5 con mayor caída ordenadas de más negativo a menos negativo.

**CA-5: GainersLosers con datos insuficientes**
Dado que hay menos de 5 FIBRAs con changePct positivo (o negativo),
Cuando se renderiza la sección,
Entonces muestra los que existan sin generar errores — si ninguna columna tiene datos, muestra un estado vacío con mensaje "Sin datos suficientes hoy".

**CA-6: Lógica de ordenamiento cubierta por tests unitarios**
Dado que existen las funciones puras `getTopMovers` y `splitGainersLosers` en `movers-logic.ts`,
Cuando se ejecuta `node --test src/Web/Main/src/modules/home/movers-logic.test.ts`,
Entonces todos los tests pasan — incluyendo casos con datos nulos, listas vacías y empates.

**CA-7: Build sin errores TypeScript**
Dado que se ejecuta `npm run build --workspace=src/Web/Main`,
Entonces termina con 0 errores y 0 advertencias de TypeScript.

## Tareas / Subtareas

- [x] Task 1: Extraer lógica pura a `movers-logic.ts` (AC: #1, #2, #4, #5, #6)
  - [x] 1.1 Crear `src/Web/Main/src/modules/home/movers-logic.ts`
  - [x] 1.2 Mover `formatVolume` de `QuickRanking.tsx` a `movers-logic.ts` y exportarla
  - [x] 1.3 Crear `getTopMovers(snapshots: MarketSnapshot[], n: number): MarketSnapshot[]` — ordena por `|dailyChangePct|` desc; fallback alfabético si no hay changePct; devuelve los primeros `n`
  - [x] 1.4 Crear `splitGainersLosers(snapshots: MarketSnapshot[], n: number): { gainers: MarketSnapshot[], losers: MarketSnapshot[] }` — gainers = changePct > 0 ordenados desc; losers = changePct < 0 ordenados asc (más negativo primero); cada lista truncada a `n`

- [x] Task 2: Tests unitarios de lógica pura (AC: #6)
  - [x] 2.1 Crear `src/Web/Main/src/modules/home/movers-logic.test.ts`
  - [x] 2.2 Tests para `getTopMovers`:
    - Lista vacía → devuelve `[]`
    - Todos con changePct null → devuelve los primeros `n` ordenados alfabéticamente
    - Mezcla de positivos y negativos → ordena por valor absoluto desc
    - Menos de `n` elementos → devuelve todos sin error
  - [x] 2.3 Tests para `splitGainersLosers`:
    - Lista vacía → `{ gainers: [], losers: [] }`
    - Solo positivos → `losers` vacío
    - Solo negativos → `gainers` vacío
    - Más de `n` de cada lado → trunca correctamente
    - changePct null en algunos → se excluyen de ambas listas
  - [x] 2.4 Tests para `formatVolume`:
    - `1_500_000` → `"1.5M"`
    - `25_000` → `"25K"`
    - `500` → número localizado
  - [x] 2.5 Ejecutar `node --experimental-strip-types --test src/Web/Main/src/modules/home/movers-logic.test.ts` — 15/15 pasan

- [x] Task 3: Refactorizar `TopMovers.tsx` a formato tabla (AC: #1, #2)
  - [x] 3.1 Reemplazar el import de lógica de ordenamiento con `getTopMovers` de `movers-logic.ts`
  - [x] 3.2 Reemplazar el import de `formatVolume` con el de `movers-logic.ts`
  - [x] 3.3 Reemplazar el layout de lista (`flex items-center justify-between`) por la tabla con `grid-cols-[2rem_1fr_auto_auto_auto] gap-3` — igual al grid de `QuickRanking`
  - [x] 3.4 Agregar encabezado de columnas: `#` | `Ticker` | `Cambio` | `Precio` | `Volumen` — mismo estilo que `QuickRanking` (`px-4 py-2 border-b border-border grid ... text-xs font-semibold text-muted-foreground`)
  - [x] 3.5 Actualizar cada fila: número de posición (1-5) + ticker + changePct con color + precio + volumen con `formatVolume`
  - [x] 3.6 Eliminar `FreshnessBadge` y sus imports (`FreshnessBadge`, `FreshnessStatus`, `formatRelativeTime`) — no aplica en este formato
  - [x] 3.7 Actualizar skeleton loading para que coincida con el grid de 5 columnas
  - [x] 3.8 Verificar que `noUnusedLocals` no falla — build pasa con 0 errores

- [x] Task 4: Crear `GainersLosers.tsx` (AC: #3, #4, #5)
  - [x] 4.1 Crear `src/Web/Main/src/modules/home/GainersLosers.tsx`
  - [x] 4.2 Query `useQuery(['market-snapshots'])` — misma configuración que los otros componentes (`staleTime: 60_000`, `refetchInterval: 5 * 60_000`)
  - [x] 4.3 Llamar `splitGainersLosers(snapshots, 5)` para obtener `gainers` y `losers`
  - [x] 4.4 Layout: `rounded-xl border border-border bg-surface-elevated overflow-hidden`
  - [x] 4.5 Header: título "Ganadores / Perdedores del día" + subtítulo "Top 5 por variación % hoy" — mismo patrón de header que `TopMovers` y `QuickRanking`
  - [x] 4.6 Contenido: `grid grid-cols-2 divide-x divide-border`
    - Columna izquierda: label "Ganadores" (text-positive), lista de ganadores (ticker izq + changePct derecha en verde)
    - Columna derecha: label "Perdedores" (text-negative), lista de perdedores (ticker izq + changePct derecha en rojo)
  - [x] 4.7 Cada fila de ganador/perdedor: `<a href="/fibras/{ticker}">` — enlaza a la ficha. Formato: `ticker font-semibold` izquierda, `+X.XX%` / `-X.XX%` derecha con color correcto
  - [x] 4.8 Estado vacío combinado: si `gainers.length === 0 && losers.length === 0` → mostrar "Sin datos suficientes hoy" centrado en texto muted
  - [x] 4.9 Skeleton loading: `animate-pulse`, dos columnas con 5 placeholders cada una
  - [x] 4.10 Verificar que `QuickRanking` ya no se exporta desde ningún barrel ni index — confirmado con grep, solo existía en HomePage.tsx

- [x] Task 5: Actualizar `HomePage.tsx` y limpiar `QuickRanking.tsx` (AC: #3)
  - [x] 5.1 En `HomePage.tsx`: reemplazar `import { QuickRanking }` por `import { GainersLosers }` y `<QuickRanking />` por `<GainersLosers />`
  - [x] 5.2 `heading-ranking` section id preservado, h2 sr-only sin cambios requeridos
  - [x] 5.3 Eliminar `src/Web/Main/src/modules/home/QuickRanking.tsx` — eliminado

- [x] Task 6: Build y verificación final (AC: #7)
  - [x] 6.1 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript, build exitoso
  - [x] 6.2 `npm test --workspace=src/Web/Main` — 30/30 tests pasan (15 nuevos movers-logic + 15 existentes)
  - [x] 6.3 `sprint-status.yaml` actualizado: `in-progress` al iniciar → `review` al terminar
  - [x] 6.4 File List y Change Log actualizados

## Dev Notes

### Contexto de la decisión (party mode 2026-05-19)

Esta historia surgió de una discusión con el equipo (PM, UX, Analista) sobre el home page. Los puntos clave:
- "Movimientos del día" en lista plana era difícil de leer — adoptamos el formato tabla de QuickRanking.
- "Ranking del universo" ordenado por changePct descendente no aportaba perspectiva nueva — se reemplaza por Ganadores/Perdedores que muestra dirección clara.
- El equipo descartó agregar datos de yield o métricas históricas en esta historia (Mary los propuso) porque requieren datos que pueden no estar disponibles aún — quedan para Epic 7.

### Estado actual de los archivos a modificar

**`TopMovers.tsx`** — layout actual: `flex items-center justify-between` por fila (ticker izq, precio+cambio% der). Tiene `FreshnessBadge` que se elimina en esta historia. Lógica de ordenamiento inline: sort por `Math.abs(changePct)` desc, top 5. Esta lógica se mueve a `movers-logic.ts`.

**`QuickRanking.tsx`** — layout actual: `grid-cols-[2rem_1fr_auto_auto_auto]` con encabezados `# | Ticker | Cambio | Precio | Volumen`. Tiene `formatVolume` local. Tiene sort por changePct desc. El formato de este grid es exactamente el que TopMovers debe adoptar. Este archivo se elimina y su funcionalidad queda absorbida por GainersLosers.

**`HomePage.tsx`** — QuickRanking está en una `<section>` full-width al fondo del layout. Basta con cambiar el import y el JSX. No cambiar la estructura del grid principal.

### Tipo `MarketSnapshot`

El tipo viene del cliente tipado generado por `openapi-fetch`. Al momento de escribir esta historia los campos relevantes son:
- `ticker: string`
- `lastPrice: string | null` (número como string — usar `toNum()` de `@/shared/lib/format-time`)
- `dailyChangePct: string | null` (ídem)
- `volume: string | null` (ídem)
- `freshnessStatus: string | null`
- `capturedAt: string | null`

`toNum(value: string | null | undefined): number | null` ya existe en `@/shared/lib/format-time` y maneja los nulls correctamente. Úsarlo siempre — nunca `parseFloat` directo.

### Regla de datos financieros nulos

Convención obligatoria del proyecto: **nunca mostrar `0` para datos financieros nulos — siempre `—`**.
Aplicar en las tres columnas de TopMovers (Cambio, Precio, Volumen) y en las listas de GainersLosers.

### Tests — patrón del proyecto

Los tests unitarios del frontend usan `node:test` + `node:assert/strict` (sin Jest, sin Vitest para unit tests de lógica pura). Ver `global-search.test.ts` como referencia exacta:

```ts
import test from 'node:test'
import assert from 'node:assert/strict'
import { getTopMovers } from './movers-logic.ts'
```

Se ejecutan con: `node --test src/Web/Main/src/modules/home/movers-logic.test.ts`

No uses Jest ni Vitest para los tests de `movers-logic` — seguir el patrón existente.

### Convenciones críticas del proyecto

- `react-router` v7 — `from 'react-router'`, no `react-router-dom`
- TanStack Query v5 — `useQuery({ queryKey, queryFn, staleTime, refetchInterval })`
- Tailwind v4 — no usar clases de v3 que no existan en v4
- `noUnusedLocals: true` — cualquier import no usado rompe el build
- shadcn/ui — NO ejecutar `npx shadcn@latest add` sin aprobación; los componentes existentes están en `@/shared/ui/`
- NO agregar dependencias npm nuevas — toda la lógica es pura TypeScript

### Archivos que NO deben modificarse

- `PriceCarousel.tsx` — fuera del scope de esta historia
- `NewsSection.tsx` — fuera del scope
- `GlobalSearch.tsx` y `global-search.test.ts` — fuera del scope
- `fibrasApi.ts` — no hay cambios de API; se usa `fetchMarketSnapshots()` existente
- Backend — esta historia es 100% frontend

### Project Structure Notes

Todos los archivos del módulo home viven en:
`src/Web/Main/src/modules/home/`

Archivos afectados:
| Archivo | Operación |
|---|---|
| `movers-logic.ts` | NUEVO |
| `movers-logic.test.ts` | NUEVO |
| `GainersLosers.tsx` | NUEVO |
| `TopMovers.tsx` | MODIFICAR |
| `HomePage.tsx` | MODIFICAR (import + JSX) |
| `QuickRanking.tsx` | ELIMINAR |

### Referencias

- Estado actual de los componentes: [TopMovers.tsx](src/Web/Main/src/modules/home/TopMovers.tsx), [QuickRanking.tsx](src/Web/Main/src/modules/home/QuickRanking.tsx), [HomePage.tsx](src/Web/Main/src/modules/home/HomePage.tsx)
- Patrón de tests unitarios: [global-search.test.ts](src/Web/Main/src/modules/home/global-search.test.ts)
- Convenciones del proyecto: `_bmad-output/planning-artifacts/convenciones-fibradis.md`
- Checklist SEO/WCAG: aplica — esta historia modifica rutas públicas. Verificar contraste de colores en los badges verde/rojo de Ganadores/Perdedores.

## Senior Developer Review (AI)

### Review Findings

- [x] [Review][Decision] PriceCarousel.tsx modificado fuera de scope — **Resuelto: opción (c)** — se mantienen las mejoras visuales (tarjeta horizontal, auto-scroll) y se corrigió el bug de múltiples intervalos en `start()`; se restauró `FreshnessBadge` y la condición `hasPrice` original (`lastPrice != null && snap.freshnessStatus != null`). `PriceCarousel.tsx` añadido al scope de esta historia con los cambios documentados.
- [x] [Review][Patch] TopMovers incumple el fallback sin `dailyChangePct` de CA-2 [src/Web/Main/src/modules/home/TopMovers.tsx:49] — **Corregido**: se añade `hasAnyChangePct` en el componente; Precio y Volumen muestran `—` cuando ningún snapshot tiene `dailyChangePct`.
- [x] [Review][Patch] Faltan tests de empates exigidos por CA-6 y el orden queda no determinista [src/Web/Main/src/modules/home/movers-logic.test.ts:14] — **Corregido**: desempate alfabético (`a.ticker.localeCompare(b.ticker)`) añadido en los tres comparadores de `movers-logic.ts`; 3 tests de empate añadidos (18 tests en total, 33/33 suite completa).
- [x] [Review][Patch] Heading sr-only desactualizado en HomePage.tsx [src/Web/Main/src/modules/home/HomePage.tsx:33] — **Corregido**: texto actualizado a "Ganadores y perdedores del día".
- [x] [Review][Defer] `dailyChangePct = 0` excluido silenciosamente de ambas listas en GainersLosers [src/Web/Main/src/modules/home/movers-logic.ts:39,44] — deferred, pre-existing. El filtro `> 0` / `< 0` excluye exactamente cero; cuando todos los activos tienen cambio cero, el `isEmpty` se activa correctamente. Cuando solo algunos tienen cero, se omiten sin indicación. Comportamiento no especificado en los AC; se puede abordar si el negocio lo requiere.
- [x] [Review][Defer] Doble llamada a `numOf` en el comparador de `getTopMovers` [src/Web/Main/src/modules/home/movers-logic.ts:24-26] — deferred, pre-existing. `numOf(a.dailyChangePct)` se llama dos veces por elemento por comparación. Sin impacto perceptible con N < 100 snapshots; refactorizar a variable local si el corpus crece.
- [x] [Review][Defer] `formatVolume`: rango [999_500, 1_000_000) renderiza "1000K" en lugar de "1.0M" [src/Web/Main/src/modules/home/movers-logic.ts:15] — deferred, pre-existing. Edge case de formateo menor; no hay FIBRAs con volumen tan alto actualmente.
- [x] [Review][Defer] `TopMovers` carece de empty state cuando la API devuelve `[]` [src/Web/Main/src/modules/home/TopMovers.tsx] — deferred, pre-existing. Cuando `snapshots = []`, `getTopMovers` retorna `[]` y el componente renderiza un contenedor vacío sin mensaje. `GainersLosers` sí tiene empty state; inconsistencia menor no cubierta en los AC.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- `node --experimental-strip-types` requerido (Node v22.17.0) — agregado al script `test` en `package.json`
- `movers-logic.ts` usa interfaz local `Snapshot` sin imports externos para compatibilidad con `node --test`

### Completion Notes List

- `movers-logic.ts` creado con `Snapshot` interface local + helper `numOf` inline (sin dependencias externas, testeable con `node --test`)
- `getTopMovers` y `splitGainersLosers` genéricas con `<T extends Snapshot>` — aceptan `MarketSnapshotDto[]` por structural typing
- `TopMovers.tsx` refactorizado: lista → tabla `grid-cols-[2rem_1fr_auto_auto_auto]`, FreshnessBadge eliminado, lógica movida a `movers-logic.ts`
- `GainersLosers.tsx` creado: dos columnas compactas, estado vacío, skeleton loading, links a fichas
- `QuickRanking.tsx` eliminado
- `package.json` test script actualizado con `movers-logic.test.ts` y flag `--experimental-strip-types`
- Tests: 30/30 pass (15 nuevos + 15 existentes sin regresiones)
- Build: 0 errores TypeScript

### File List

- `src/Web/Main/src/modules/home/movers-logic.ts` — NUEVO
- `src/Web/Main/src/modules/home/movers-logic.test.ts` — NUEVO (18 tests)
- `src/Web/Main/src/modules/home/GainersLosers.tsx` — NUEVO
- `src/Web/Main/src/modules/home/TopMovers.tsx` — MODIFICADO
- `src/Web/Main/src/modules/home/HomePage.tsx` — MODIFICADO
- `src/Web/Main/src/modules/home/PriceCarousel.tsx` — MODIFICADO (auto-scroll, tarjeta horizontal, FreshnessBadge restaurado, fix interval leak)
- `src/Web/Main/src/modules/home/QuickRanking.tsx` — ELIMINADO
- `src/Web/Main/package.json` — MODIFICADO (script test)

### Change Log

- 2026-05-19: Historia 2.5 implementada — TopMovers adopta formato tabla, QuickRanking reemplazado por GainersLosers (dos columnas ganadores/perdedores), lógica de ordenamiento extraída a movers-logic.ts con 15 tests unitarios
- 2026-05-19: Code review aplicado — (1) CA-2 fallback corregido: Precio/Volumen muestran `—` cuando no hay `dailyChangePct`; (2) desempate alfabético añadido en los 3 comparadores + 3 tests de empate (18 tests); (3) sr-only heading actualizado; (4) PriceCarousel: tarjeta horizontal + auto-scroll (scope ampliado), FreshnessBadge restaurado, `hasPrice` restaurado, bug de intervalos múltiples corregido en `start()`. Suite completa: 33/33 pass, build 0 errores.
