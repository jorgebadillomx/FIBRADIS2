---
title: 'Ajustes UI main: carrusel global, ganadores/perdedores y tabla universo'
type: 'feature'
created: '2026-06-12'
status: 'done'
baseline_commit: '5aef2504d01d787c4298523412b3272f4dc9cc9b'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El carrusel de precios solo aparece en el Home; Ganadores/Perdedores carece de logo de FIBRA e indicador de variación en pesos; la tabla Universo FIBRAS no muestra yield ni el último período de fundamentales y su contenido luce comprimido horizontalmente.

**Approach:** Mover `PriceCarousel` a `PublicLayout` para que aparezca en todas las páginas debajo del header; enriquecer cada fila de `GainersLosers` con `FibraLogo` y columna `Var $`; añadir columnas `Yield` y `Último Reporte` a `FibraUniverseTable` y darle soporte de scroll horizontal.

## Boundaries & Constraints

**Always:**
- El carrusel en `PublicLayout` va en su propio `<div>` entre `</header>` y `<main>`, NO dentro de `<main>` (separación semántica / accesibilidad).
- En `GainersLosers` se reutiliza el componente `FibraLogo` existente (`size="sm"`); no crear componentes de imagen nuevos.
- La columna "Último Reporte" muestra el campo `period` tal como lo devuelve el endpoint (no formatearlo ni traducirlo).
- `annualizedYield` ya está en `MarketSnapshotDto`; se extiende `SnapshotRow` y `SortKey` en `universe-table-logic.ts` para soportarlo.
- `fetchFundamentalesSummary` ya existe en `fundamentalesApi.ts`; importar desde ahí.

**Ask First:**
- Si al renderizar el carrusel en `PublicLayout` alguna página queda visualmente rota (p. ej. doble carrusel, alto header conflictivo).

**Never:**
- No añadir el carrusel como sticky/fijo (debe scrollear con la página).
- No cambiar el backend ni los endpoints.
- No modificar `FibraLogo` (sin nuevos tamaños para este ticket).

## I/O & Edge-Case Matrix

| Scenario | Input / Estado | Comportamiento esperado | Manejo de error |
|----------|---------------|-------------------------|----------------|
| FIBRA sin `siteUrl` en GainersLosers | `siteUrl: null` | `FibraLogo` muestra badge de color con ticker | Manejado internamente por `FibraLogo` |
| FIBRA sin fundamentales | ticker no aparece en summary | Columna "Último Reporte" muestra "—" | — |
| Summary API falla | error en React Query | Columna "Último Reporte" muestra "—" en todas las filas | React Query error state → fallback "—" |
| `annualizedYield` null | campo nulo en snapshot | Celda Yield muestra "—" | — |

</frozen-after-approval>

## Code Map

- `src/Web/Main/src/shared/layouts/PublicLayout.tsx` — layout envolvente; aquí se inserta `PriceCarousel` entre `</header>` y `<main>`
- `src/Web/Main/src/modules/home/HomePage.tsx` — eliminar la sección `PriceCarousel` (ya la provee `PublicLayout`)
- `src/Web/Main/src/modules/home/GainersLosers.tsx` — añadir `FibraLogo` + columna `Var $`; necesita `fetchAllFibras` para obtener `siteUrl`
- `src/Web/Main/src/modules/home/FibraUniverseTable.tsx` — añadir columnas Yield y Último Reporte; fix de ancho horizontal
- `src/Web/Main/src/modules/home/universe-table-logic.ts` — extender `SnapshotRow` con `annualizedYield`; añadir `'annualizedYield'` a `SortKey`
- `src/Web/Main/src/api/fundamentalesApi.ts` — (solo lectura) `fetchFundamentalesSummary` ya existe aquí

## Tasks & Acceptance

**Execution:**

- [x] `src/Web/Main/src/modules/home/universe-table-logic.ts` -- añadir `annualizedYield: null | number | string` a `SnapshotRow`; añadir `'annualizedYield'` a `SortKey` -- prerequisito para ordenar la columna Yield en la tabla

- [x] `src/Web/Main/src/shared/layouts/PublicLayout.tsx` -- importar `PriceCarousel` desde `@/modules/home/PriceCarousel`; renderizarlo en `<div className="border-b border-border bg-background/95 backdrop-blur"><div className="container mx-auto px-4 py-2"><PriceCarousel /></div></div>` entre `</header>` y `<main>` -- carrusel global en todas las páginas públicas

- [x] `src/Web/Main/src/modules/home/HomePage.tsx` -- eliminar el bloque `<section aria-labelledby="heading-precio">...</section>` que contiene `<PriceCarousel />` -- evitar doble carrusel en el Home

- [x] `src/Web/Main/src/modules/home/GainersLosers.tsx` -- añadir `useQuery({ queryKey: ['fibras'], queryFn: fetchAllFibras, staleTime: 5*60_000 })` y construir `fibraByTicker` map; en cada fila añadir `<FibraLogo size="sm" siteUrl={fibraByTicker[snap.ticker]?.siteUrl ?? null} ticker={snap.ticker} />` al inicio; añadir celda `dailyChange` (con color positivo/negativo) entre el ticker y el pct; reducir `py-2.5` a `py-1.5` para compensar la altura del logo

- [x] `src/Web/Main/src/modules/home/FibraUniverseTable.tsx` -- añadir `useQuery({ queryKey: ['fundamentals-summary'], queryFn: () => fetchFundamentalesSummary() })` y construir mapa `ticker → period`; añadir `'annualizedYield'` a `SORT_COLUMNS` con label `'Yield'`; añadir columna no-sort `'Último Rep.'`; actualizar `grid-cols` del header, skeleton y filas para incluir los dos campos nuevos; envolver la zona del grid en `<div className="overflow-x-auto">` con `min-w-[56rem]` en el grid interior para evitar compresión en viewports estrechos

**Acceptance Criteria:**

- Dado que el usuario visita cualquier página pública (home, /fibras, /noticias, ficha de FIBRA, etc.), cuando la página carga, entonces el PriceCarousel aparece justo debajo del header en todas ellas.
- Dado que el carrusel ya está en `PublicLayout`, cuando el usuario visita la ruta `/`, entonces el carrusel no aparece dos veces.
- Dado que la sección Ganadores/Perdedores tiene datos, cuando se renderiza, entonces cada fila muestra: logo de FIBRA, ticker, Var $ (en pesos, con color positivo/negativo) y Var % (con color positivo/negativo).
- Dado que la tabla Universo FIBRAS tiene datos, cuando se renderiza, entonces la columna Yield muestra `annualizedYield` como `"X.XX%"` (o `"—"` si null) y la columna Último Reporte muestra el campo `period` del summary de fundamentales (o `"—"` si no hay dato).
- Dado que el viewport es estrecho, cuando se ve la tabla Universo FIBRAS, entonces es posible desplazarla horizontalmente sin desbordamiento visual.

## Design Notes

**Yield format:** `annualizedYield` se muestra como `${val.toFixed(2)}%`. No añadir signo `+`; es yield anualizado, no variación.

**Carousel en PublicLayout:** el wrapper externo hereda el estilo del header (`bg-background/95 backdrop-blur`) y tiene `border-b border-border` para separarlo visualmente del contenido de la página. El inner wrapper usa las mismas clases `container mx-auto px-4` que el header.

**GainersLosers con logo:** cada fila ahora tiene 4 elementos en lugar de 2; el nuevo flex layout debe mantener el ancho del componente usando `justify-between` o un grid de 3 columnas `[auto_1fr_auto_auto]`.

## Verification

**Commands:**
- `npm run build --prefix src/Web/Main` -- expected: sin errores TypeScript/compilación

**Manual checks:**
- Abrir `/fibras` o `/noticias`: carrusel visible debajo del header.
- Abrir `/`: carrusel aparece solo una vez.
- Sección Ganadores/Perdedores: logo, ticker, Var $, Var % visibles en cada fila.
- Tabla Universo FIBRAS: columnas Yield y Último Reporte visibles; scroll horizontal en viewport ~768px.

## Suggested Review Order

**Carrusel global (layout change)**

- Barra del carousel insertada entre `</header>` y `<main>`, nunca sticky
  [`PublicLayout.tsx:175`](../../src/Web/Main/src/shared/layouts/PublicLayout.tsx#L175)

- Import eliminado; carousel no se renderiza dos veces en home
  [`HomePage.tsx:1`](../../src/Web/Main/src/modules/home/HomePage.tsx#L1)

**Ganadores / Perdedores**

- Nuevo layout de fila: FibraLogo + ticker + Var $ + Var % con colores
  [`GainersLosers.tsx:72`](../../src/Web/Main/src/modules/home/GainersLosers.tsx#L72)

- Query `fetchAllFibras` para construir mapa `ticker → siteUrl`
  [`GainersLosers.tsx:15`](../../src/Web/Main/src/modules/home/GainersLosers.tsx#L15)

**Tabla Universo FIBRAS**

- Wrapper `overflow-x-auto` + `min-w-[56rem]` + grid de 11 columnas con Yield y Último Rep.
  [`FibraUniverseTable.tsx:87`](../../src/Web/Main/src/modules/home/FibraUniverseTable.tsx#L87)

- Query de fundamentals summary; mapa `ticker → period` para Último Reporte
  [`FibraUniverseTable.tsx:37`](../../src/Web/Main/src/modules/home/FibraUniverseTable.tsx#L37)

- Celdas Yield y Último Rep. en cada fila de datos
  [`FibraUniverseTable.tsx:237`](../../src/Web/Main/src/modules/home/FibraUniverseTable.tsx#L237)

**Tipos**

- `annualizedYield` añadido a `SortKey` y `SnapshotRow`
  [`universe-table-logic.ts:1`](../../src/Web/Main/src/modules/home/universe-table-logic.ts#L1)
