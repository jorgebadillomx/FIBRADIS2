---
title: 'SEO #11/#16 — Lazy-load HomePage y self-hosting Google Fonts'
type: 'refactor'
created: '2026-06-18'
status: 'done'
baseline_commit: 'bf99e2861a026ca27943d3810a8c628df5ce661a'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `HomePage` es el único componente importado estáticamente en `routes.tsx`, lo que incluye su grafo de dependencias en el bundle inicial e impide que Vite excluya el chunk `vendor-charts` del preload crítico. Adicionalmente, Google Fonts requiere dos handshakes a dominios externos (`fonts.googleapis.com` + `fonts.gstatic.com`) que bloquean el FCP entre 150-300 ms en mobile.

**Approach:** (1) Convertir `HomePage` a `React.lazy()` con el mismo patrón `p()` que usan las demás rutas, eliminando su importación estática. (2) Descargar los woff2 de Playfair Display e IBM Plex Sans, servirlos desde `/assets/fonts/`, declarar `@font-face` en `index.css` y eliminar los link tags de Google Fonts en `index.html`.

## Boundaries & Constraints

**Always:**
- El fallback del Suspense de HomePage debe ser `<PageLoader />` (mismo spinner que las demás rutas) para evitar CLS durante la carga inicial.
- Los archivos woff2 van en `src/Web/Main/public/assets/fonts/` — Vite los copia a `wwwroot` sin hash. Los paths en CSS usan `/assets/fonts/`.
- Mantener exactamente los mismos weights y styles que pide la URL actual de Google Fonts: Playfair Display 400/600/700 normal + 400 italic; IBM Plex Sans 300/400/500/600 normal.
- `font-display: swap` en todos los `@font-face` (equivalente al `display=swap` actual).
- Las declaraciones `@font-face` van antes de las variables `--font-playfair`/`--font-plex` en `index.css`.

**Ask First:**
- Si al descargar los woff2 la fuente de Google incluye rangos unicode separados (latin, latin-ext, etc.), preguntar si incluir solo el subconjunto `latin` o todos los rangos.

**Never:**
- No agregar `preload` en `index.html` para los woff2 locales (ya no aplica; el browser los descubre vía CSS).
- No mover los woff2 a `src/assets/` procesados por Vite (evita complejidad de importación en CSS).
- No cambiar las rutas `/fibras`, `/fibras/:slug` ni ninguna otra — solo `'/'`.
- No eliminar el `@font-face` del fallback `Playfair Display Fallback` que ya existe en `index.css`.

</frozen-after-approval>

## Code Map

- `src/Web/Main/src/app/routes.tsx:4` — importación estática de `HomePage`; línea 45 usa `<HomePage />` sin `p()`
- `src/Web/Main/index.html:9-13` — preconnect + preload + noscript de Google Fonts; reemplazar por nada
- `src/Web/Main/src/index.css:44-45` — variables `--font-playfair`/`--font-plex`; añadir `@font-face` encima
- `src/Web/Main/public/assets/fonts/` — directorio destino de los woff2 (crear si no existe)
- `src/Web/Main/vite.config.ts` — `manualChunks` ya tiene `vendor-charts: ['recharts']`; no necesita cambios

## Tasks & Acceptance

**Execution:**

- [x] `src/Web/Main/src/app/routes.tsx` -- Eliminar `import { HomePage } from '@/modules/home/HomePage'` (línea 4); añadir `const HomePage = lazy(() => import('@/modules/home/HomePage').then(m => ({ default: m.HomePage })))` junto al resto de lazy imports; cambiar `element: <HomePage />` a `element: p(<HomePage />)` en la ruta `'/'` -- alinea HomePage con el patrón de todas las demás páginas

- [x] `src/Web/Main/public/assets/fonts/` -- Crear el directorio y descargar los 8 woff2 via script: (1) hacer GET a la URL de Google Fonts con User-Agent moderno para obtener el CSS con URLs de woff2, (2) descargar cada woff2 con nombre legible: `playfair-display-400.woff2`, `playfair-display-600.woff2`, `playfair-display-700.woff2`, `playfair-display-400-italic.woff2`, `ibm-plex-sans-300.woff2`, `ibm-plex-sans-400.woff2`, `ibm-plex-sans-500.woff2`, `ibm-plex-sans-600.woff2` -- elimina dependencia de red externa en tiempo de render

- [x] `src/Web/Main/src/index.css` -- Añadir 8 bloques `@font-face` antes de las variables `--font-playfair`/`--font-plex`, cada uno con `font-display: swap` y `src: url('/assets/fonts/NOMBRE.woff2') format('woff2')` -- reemplaza la carga externa por archivos locales con misma semántica

- [x] `src/Web/Main/index.html` -- Eliminar las 4 líneas de Google Fonts (preconnect x2 + preload + noscript, líneas 9-13) -- elimina los 2 handshakes externos que bloquean el FCP

- [x] `ACTION-PLAN.md` -- Marcar #11 y #16 como ✅ con nota de implementación

**Acceptance Criteria:**

- Dado que el usuario visita `/`, cuando carga la app por primera vez, entonces el HTML inicial no contiene ninguna referencia a `fonts.googleapis.com` ni `fonts.gstatic.com`
- Dado que el usuario visita `/`, cuando se sirve la página, entonces la red no hace requests a dominios `googleapis.com` o `gstatic.com` para cargar fuentes
- Dado el build de producción (`npm run build` en `src/Web/Main`), cuando compila, entonces el bundle generado no incluye `HomePage` en el chunk de entrada principal (aparece como chunk separado en el output)
- Dado que existe `public/assets/fonts/`, cuando se sirve la app, entonces `GET /assets/fonts/playfair-display-400.woff2` devuelve 200 con `Content-Type: font/woff2`
- Dado que el usuario visita cualquier página pública, cuando se renderiza el texto con clase `font-playfair`, entonces la fuente renderizada es Playfair Display (no Georgia ni fallback del sistema) — verificar en DevTools > Computed > font-family

## Spec Change Log

## Design Notes

**¿Por qué `public/assets/fonts/` en vez de `src/assets/fonts/`?** Los archivos en `public/` se copian tal cual a `wwwroot/` sin que Vite los procese ni les añada hash. Esto simplifica el path en CSS (URL absoluta `/assets/fonts/`). El downside es la falta de cache-busting automático — aceptable porque estos archivos cambian raramente (nueva versión de la fuente = acción consciente).

**¿Lazy-loading HomePage mejora el LCP si es la primera ruta?** Sí: aunque el chunk de HomePage se carga igualmente al entrar en `/`, sacarlo del bundle de entrada reduce el JS que el browser debe parsear/evaluar antes de pintar el primer frame. La ganancia real está en que `vendor-charts` ya no queda en el grafo estático de imports y Vite puede excluirlo del modulepreload inicial.

## Verification

**Commands:**
- `npm run build --prefix src/Web/Main` -- expected: 0 errores; en el output de chunks, `HomePage` aparece como chunk dinámico separado (no en el entry chunk); `vendor-charts` no aparece en la lista de preloads del entry
- verificar manualmente en `src/Web/Main/public/assets/fonts/` que existen los 8 archivos `.woff2` con tamaño > 0

**Manual checks:**
- Abrir DevTools > Network en `/`, filtrar por `Font` — deben aparecer solo requests a `localhost` o el dominio propio, ninguno a `googleapis.com`

## Suggested Review Order

**Lazy-load de HomePage**

- Entry point: ruta `/` ahora usa `lazy()` + `p()` como todas las demás páginas.
  [`routes.tsx:8`](../../src/Web/Main/src/app/routes.tsx#L8)

- Uso en el árbol de rutas — verificar que `p()` envuelve correctamente.
  [`routes.tsx:45`](../../src/Web/Main/src/app/routes.tsx#L45)

**Self-hosting de Google Fonts**

- Declaraciones `@font-face` con paths locales y `font-display:swap`; aquí empieza el cambio de fuentes.
  [`index.css:4`](../../src/Web/Main/src/index.css#L4)

- HTML limpio: sin referencias a `googleapis.com` ni `gstatic.com`.
  [`index.html:1`](../../src/Web/Main/index.html#L1)
