# Historia 2.6: Home — Reorganización de secciones y Tabla Universo FIBRAS

Status: done

## Historia

Como visitante público de FIBRADIS,
quiero que el home muestre "Ganadores / Perdedores del día" en la posición principal junto a las noticias, y que debajo aparezca una tabla completa con todos los FIBRAs del universo con columnas de mercado, ordenamiento y filtro por ticker,
para que pueda escanear el universo completo y encontrar rápidamente FIBRAs de interés sin tener que navegar a páginas individuales.

## Criterios de Aceptación

**CA-1: GainersLosers ocupa la posición principal del grid**
Dado que la home carga,
Cuando veo el área principal (debajo del carrusel),
Entonces "Ganadores / Perdedores del día" aparece en `lg:col-span-2` junto a `NewsSection` — exactamente donde antes estaba `TopMovers`. La apariencia interna del componente no cambia.

**CA-2: TopMovers eliminado**
Dado que se accede al home,
Entonces el componente `TopMovers` ya no renderiza ni existe como archivo. El archivo `TopMovers.tsx` es eliminado.

**CA-3: Tabla Universo FIBRAS — columnas correctas**
Dado que la home carga con datos de mercado disponibles,
Cuando veo la sección "Universo FIBRAS" (full-width, debajo del grid principal),
Entonces la tabla muestra una fila por FIBRA con las columnas: **Emisora**, **Precio**, **Var $**, **Var %**, **Volumen**, **Rango 52S** (mini barra visual), **Máx 52S**, **Mín 52S**, **Estado**.

**CA-4: Datos nulos muestran `—`**
Dado que un snapshot tiene campos nulos (`lastPrice`, `dailyChange`, etc.),
Cuando se renderiza la fila,
Entonces los campos nulos muestran `—`, nunca `0` ni cadena vacía.

**CA-5: Sort por columna**
Dado que el visitante hace clic en un encabezado de columna sorteable (Precio, Var $, Var %, Volumen, Máx 52S, Mín 52S),
Cuando hace clic una vez,
Entonces la tabla ordena esa columna ascendente con indicador visual (▲). Clic de nuevo → descendente (▼). Clic en columna diferente → esa columna ascendente.

**CA-6: Filtro por ticker**
Dado que el visitante escribe texto en el campo de filtro encima de la tabla,
Cuando escribe caracteres,
Entonces la tabla filtra las filas mostrando solo FIBRAs cuyo ticker contenga el texto (case-insensitive). Si el filtro no coincide con ninguno, muestra estado vacío con mensaje "Sin resultados para '{texto}'".

**CA-7: Rango 52S — mini barra visual**
Dado que una fila tiene `week52High`, `week52Low` y `lastPrice` disponibles,
Cuando se renderiza la columna Rango 52S,
Entonces muestra una barra de fondo gris con un marcador (div interno) cuya posición horizontal indica dónde está el precio actual dentro del rango anual — 0% = en el mínimo, 100% = en el máximo. Si faltan datos, muestra `—`.

**CA-8: Skeleton loading**
Dado que los datos de mercado aún están cargando,
Cuando la tabla renderiza,
Entonces muestra skeleton rows (8 filas) con `animate-pulse` mientras espera.

**CA-9: Emisora clickeable**
Dado que el visitante hace clic en el ticker de la tabla,
Entonces navega a `/fibras/{ticker}` — igual que el resto de la app.

**CA-10: Lógica pura cubierta por tests unitarios**
Dado que existen funciones puras en `universe-table-logic.ts`,
Cuando se ejecuta `node --experimental-strip-types --test src/modules/home/universe-table-logic.test.ts`,
Entonces todos los tests pasan.

**CA-11: Build sin errores TypeScript**
Dado que se ejecuta `npm run build --workspace=src/Web/Main`,
Entonces termina con 0 errores y 0 advertencias.

## Tareas / Subtareas

- [x] Task 1: Lógica pura `universe-table-logic.ts` (AC: #4, #5, #6, #7)
  - [x] 1.1 Crear `src/Web/Main/src/modules/home/universe-table-logic.ts`
  - [x] 1.2 Definir `type SortKey = 'lastPrice' | 'dailyChange' | 'dailyChangePct' | 'volume' | 'week52High' | 'week52Low'`
  - [x] 1.3 Implementar `filterSnapshots<T extends { ticker: string }>(snapshots: T[], text: string): T[]` — `text` vacío devuelve todos; case-insensitive includes
  - [x] 1.4 Implementar `sortSnapshots<T extends SnapshotRow>(snapshots: T[], key: SortKey | null, dir: 'asc' | 'desc'): T[]` — ordena numéricamente; nulos al final en ambas direcciones; sin key devuelve copia sin ordenar
  - [x] 1.5 Implementar `calcRange52Pct(lastPrice: number | null, high: number | null, low: number | null): number | null` — devuelve [0, 1] o null si falta cualquier dato o high === low

- [x] Task 2: Tests unitarios de lógica pura (AC: #10)
  - [x] 2.1 Crear `src/Web/Main/src/modules/home/universe-table-logic.test.ts`
  - [x] 2.2 Tests para `filterSnapshots`: texto vacío, espacios, case-insensitive, sin coincidencias, parcial en medio
  - [x] 2.3 Tests para `sortSnapshots`: sin key, asc nulos al final, desc nulos al final, desempate alfabético
  - [x] 2.4 Tests para `calcRange52Pct`: centro, mínimo, máximo, clamp sup/inf, null en cada argumento, high===low
  - [x] 2.5 Agregar `src/modules/home/universe-table-logic.test.ts` al script `test` en `package.json`
  - [x] 2.6 Ejecutar los tests nuevos: 19/19 pasan

- [x] Task 3: Crear `FibraUniverseTable.tsx` (AC: #3, #4, #5, #6, #7, #8, #9)
  - [x] 3.1 Crear `src/Web/Main/src/modules/home/FibraUniverseTable.tsx`
  - [x] 3.2 Query `useQuery` con `queryKey: ['market-snapshots']`, `queryFn: fetchMarketSnapshots`, `staleTime: 60_000`, `refetchInterval: 5 * 60_000`
  - [x] 3.3 Estado React: `sortKey`, `sortDir`, `filterText`
  - [x] 3.4 Pipeline de datos: `snapshots → filterSnapshots → sortSnapshots`
  - [x] 3.5 Header: "Universo FIBRAS" + subtítulo + input de filtro inline
  - [x] 3.6 Input de filtro con placeholder "Filtrar por ticker..."
  - [x] 3.7 Encabezados con sort (Precio, Var $, Var %, Volumen, Máx 52S, Mín 52S) y no-sort (Emisora, Rango 52S, Estado)
  - [x] 3.8 Indicadores ▲ ▼ ⇅ en encabezados sorteables
  - [x] 3.9 Filas: todos los campos con color (changePct/change), `—` para nulos
  - [x] 3.10 Mini barra Rango 52S con `calcRange52Pct`
  - [x] 3.11 Estado vacío con mensaje contextual (filtro sin resultados vs. sin datos)
  - [x] 3.12 Skeleton loading 8 filas con `animate-pulse`
  - [x] 3.13 `noUnusedLocals` verificado — build 0 errores

- [x] Task 4: Actualizar `HomePage.tsx` (AC: #1, #2)
  - [x] 4.1 Quitar `import { TopMovers }` y su `<section>` del grid
  - [x] 4.2 Mover `<GainersLosers />` al slot `lg:col-span-2`
  - [x] 4.3 Agregar `<FibraUniverseTable />` en sección full-width con `aria-labelledby="heading-universo"`
  - [x] 4.4 `heading-ranking` preservado para GainersLosers

- [x] Task 5: Eliminar `TopMovers.tsx` (AC: #2)
  - [x] 5.1 Verificado con grep: ningún archivo importa `TopMovers` post-edición de HomePage.tsx
  - [x] 5.2 `src/Web/Main/src/modules/home/TopMovers.tsx` eliminado

- [x] Task 6: Build y verificación final (AC: #11)
  - [x] 6.1 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript, build exitoso
  - [x] 6.2 `npm test --workspace=src/Web/Main` — 56/56 pasan (19 nuevos + 37 existentes, 0 regresiones)
  - [x] 6.3 `sprint-status.yaml` actualizado: `in-progress` → `review`
  - [x] 6.4 File List y Change Log actualizados

## Dev Notes

### Archivos existentes — estado actual

**`HomePage.tsx`** — layout actual:
```tsx
<div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
  <div className="lg:col-span-2">
    <section aria-labelledby="heading-movers">
      <h2 id="heading-movers" className="sr-only">Movimientos del día</h2>
      <TopMovers />
    </section>
  </div>
  <div>
    <section aria-labelledby="heading-noticias">
      <NewsSection />
    </section>
  </div>
</div>
<section aria-labelledby="heading-ranking">
  <h2 id="heading-ranking" className="sr-only">Ganadores y perdedores del día</h2>
  <GainersLosers />
</section>
```

Layout objetivo:
```tsx
<div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
  <div className="lg:col-span-2">
    <section aria-labelledby="heading-ranking">
      <h2 id="heading-ranking" className="sr-only">Ganadores y perdedores del día</h2>
      <GainersLosers />
    </section>
  </div>
  <div>
    <section aria-labelledby="heading-noticias">
      <NewsSection />
    </section>
  </div>
</div>
<section aria-labelledby="heading-universo">
  <h2 id="heading-universo" className="sr-only">Universo FIBRAS</h2>
  <FibraUniverseTable />
</section>
```

**`GainersLosers.tsx`** — no se toca su código interno. Solo cambia su posición en el grid.

**`TopMovers.tsx`** — se elimina por completo. No tiene tests propios (su lógica ya vive en `movers-logic.ts` y seguirá siendo usada implícitamente). El archivo `movers-logic.ts` y sus tests no se tocan.

### Tipo `MarketSnapshotDto` — campos exactos del cliente generado

Del `schema.d.ts` generado en `src/Web/SharedApiClient/`:
```ts
MarketSnapshotDto: {
  fibraId: string;          // uuid
  ticker: string;
  lastPrice: null | number | string;     // usar toNum()
  dailyChange: null | number | string;   // usar toNum()
  dailyChangePct: null | number | string; // usar toNum()
  volume: null | number | string;        // usar toNum()
  week52High: null | number | string;    // usar toNum()
  week52Low: null | number | string;     // usar toNum()
  capturedAt: null | string;
  freshnessStatus: null | string;        // 'fresh' | 'stale' | 'off-hours' | 'critical' | null
}
```

`toNum(val)` viene de `@/shared/lib/format-time` — úsarlo para todos los campos numéricos del snapshot. Nunca `parseFloat` directo.

### FreshnessBadge — uso correcto

```tsx
import { FreshnessBadge, type FreshnessStatus } from '@/shared/ui/freshness-badge'

// En la fila:
{snap.freshnessStatus
  ? <FreshnessBadge status={snap.freshnessStatus as FreshnessStatus} />
  : <span className="text-muted-foreground">—</span>
}
```

`FreshnessStatus` es `'fresh' | 'stale' | 'off-hours' | 'critical'`. El cast es seguro porque el backend solo produce esos valores o null.

### formatVolume — reutilizar de movers-logic.ts

```ts
import { formatVolume } from './movers-logic'
```

No duplicar la función. Ya existe y está testeada.

### Mini barra Rango 52S — implementación

```tsx
// En la celda Rango 52S:
const pct = calcRange52Pct(toNum(snap.lastPrice), toNum(snap.week52High), toNum(snap.week52Low))
{pct != null ? (
  <div className="w-16 h-2 rounded-full bg-muted overflow-hidden">
    <div
      className="h-full bg-muted-foreground/60 rounded-full"
      style={{ width: `${(pct * 100).toFixed(1)}%` }}
    />
  </div>
) : <span className="text-muted-foreground">—</span>}
```

Función pura en `universe-table-logic.ts`:
```ts
export function calcRange52Pct(
  lastPrice: number | null,
  high: number | null,
  low: number | null
): number | null {
  if (lastPrice == null || high == null || low == null) return null
  if (high === low) return null
  return Math.min(1, Math.max(0, (lastPrice - low) / (high - low)))
}
```

### Sort — lógica de encabezados

```tsx
function handleSort(key: SortKey) {
  if (sortKey === key) {
    setSortDir(d => d === 'asc' ? 'desc' : 'asc')
  } else {
    setSortKey(key)
    setSortDir('asc')
  }
}
```

Encabezado sorteable:
```tsx
<button
  onClick={() => handleSort('lastPrice')}
  className="flex items-center gap-1 hover:text-foreground transition-colors"
>
  Precio
  <span className="text-xs opacity-60">
    {sortKey === 'lastPrice' ? (sortDir === 'asc' ? '▲' : '▼') : '⇅'}
  </span>
</button>
```

### Tests unitarios — patrón del proyecto

Los tests frontend usan `node:test` + `node:assert/strict`. Sin Jest ni Vitest.

```ts
// universe-table-logic.test.ts
import test from 'node:test'
import assert from 'node:assert/strict'
import { filterSnapshots, sortSnapshots, calcRange52Pct } from './universe-table-logic.ts'
```

Ejecutar individual: `node --experimental-strip-types --test src/modules/home/universe-table-logic.test.ts`

Script `npm test` en `package.json` agrega el nuevo test file a la lista existente. No reemplazar la lista — agregar al final.

### Convenciones obligatorias del proyecto

- `noUnusedLocals: true` — todo import declarado debe usarse
- Imports absolutos con `@/` (no rutas relativas)
- Nunca mostrar `0` para datos nulos — siempre `—`
- `react-router` v7: import de `'react-router'`, no `'react-router-dom'`
- TanStack Query v5: `useQuery({ queryKey, queryFn, staleTime, refetchInterval })`
- Tailwind v4 — no usar clases de v3 que no existan en v4
- `shadcn/ui` — NO ejecutar `npx shadcn@latest add` — usar componentes ya instalados en `@/shared/ui/`
- NO agregar dependencias npm nuevas — todo es TypeScript/CSS puro

### Historia 2.5 — aprendizajes aplicables

- El patrón de header `px-4 pt-4 pb-2 flex items-center gap-3` con línea divisora `flex-1 h-px bg-border` es el estándar del módulo home.
- El skeleton usa `animate-pulse` con divs `bg-muted rounded`.
- `staleTime: 60_000` + `refetchInterval: 5 * 60_000` es la config estándar para `market-snapshots`.
- `movers-logic.ts` usa interfaz local `Snapshot` para no tener dependencias externas y ser testeable con `node --test`. Seguir el mismo patrón en `universe-table-logic.ts`.

### Checklist WCAG/SEO (rutas públicas)

Esta historia modifica el home (ruta pública `/`). Antes de marcar `done`, verificar:
- Contraste de color en `Var %` positivo/negativo cumple WCAG 2.1 AA (ya existe en la app con `text-positive`/`text-negative`)
- Tabla navegable con teclado: botones de sort son `<button>`, links son `<a>` — ambos accesibles
- `build` pasa con 0 errores TypeScript

### Project structure — archivos relevantes

```
src/Web/Main/src/modules/home/
├── FibraUniverseTable.tsx        ← NUEVO
├── universe-table-logic.ts       ← NUEVO
├── universe-table-logic.test.ts  ← NUEVO
├── GainersLosers.tsx             ← sin cambio de código (solo posición en HomePage)
├── HomePage.tsx                  ← MODIFICAR (layout)
├── TopMovers.tsx                 ← ELIMINAR
├── movers-logic.ts               ← sin cambio (formatVolume se reutiliza)
└── ...
```

Backend: sin cambios. La historia es 100% frontend.

## File List

- `src/Web/Main/src/modules/home/universe-table-logic.ts` — NUEVO
- `src/Web/Main/src/modules/home/universe-table-logic.test.ts` — NUEVO (19 tests)
- `src/Web/Main/src/modules/home/FibraUniverseTable.tsx` — NUEVO
- `src/Web/Main/src/modules/home/HomePage.tsx` — MODIFICADO (layout reorganizado)
- `src/Web/Main/src/modules/home/TopMovers.tsx` — ELIMINADO
- `src/Web/Main/package.json` — MODIFICADO (script test: universe-table-logic.test.ts agregado)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — MODIFICADO

## Change Log

- 2026-05-21: Historia 2.6 implementada — GainersLosers movido al grid principal (reemplaza TopMovers), nueva FibraUniverseTable full-width con sort por 6 columnas, filtro por ticker, mini barra Rango 52S, FreshnessBadge por fila, skeleton loading. TopMovers.tsx eliminado. Tests: 56/56 (19 nuevos). Build: 0 errores.

## Senior Developer Review (AI)

### Review Findings

- [x] [Review][Patch] Column header order mismatch: col 6 del header (6rem) = Máx 52S pero col 6 de las filas de datos (6rem) = Rango 52S — columnas 6-8 desalineadas visualmente [FibraUniverseTable.tsx:76-90]
- [x] [Review][Patch] `isError` ignorado en `useQuery` — fallo de red muestra "Sin datos de mercado disponibles" sin feedback ni opción de retry [FibraUniverseTable.tsx:29]
- [x] [Review][Defer] Sin roles ARIA de tabla (`role="table/row/columnheader/cell"`) — grid CSS sin semántica de tabla accesible [FibraUniverseTable.tsx:52-210] — deferred, pre-existing pattern en el módulo home

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- `sortSnapshots` usa interfaz `SnapshotRow` local (no importa de openapi-fetch) para mantener compatibilidad con `node --test` — mismo patrón que `movers-logic.ts`
- Grid layout: `grid-cols-[1fr_auto_auto_auto_auto_6rem_auto_auto_auto]` — columna Rango 52S fijada en 6rem para que la mini barra tenga ancho consistente

### Completion Notes List

- `universe-table-logic.ts` creado con `SnapshotRow` interface local, `filterSnapshots`, `sortSnapshots` (nulos al final, desempate alfabético), `calcRange52Pct` (clamp [0,1], high===low → null)
- `FibraUniverseTable.tsx`: 9 columnas, sort con indicadores ▲▼⇅, filtro inline en header, mini barra Rango 52S via calcRange52Pct, FreshnessBadge cast seguro, skeleton 8 filas
- `HomePage.tsx` reescrito: TopMovers removido, GainersLosers en lg:col-span-2, FibraUniverseTable como sección full-width
- `TopMovers.tsx` eliminado — `movers-logic.ts` y sus tests permanecen intactos (formatVolume reutilizado por FibraUniverseTable)
- Suite total: 56/56 (19 nuevos universe-table-logic + 37 preexistentes sin regresiones)
