# Story 13.9: Reducir FCP y LCP del homepage (vendor-chunks eagerness + CSS)

Status: ready-for-dev

## Story

As a **visitante público de Fibras Inmobiliarias**,
I want **que la página de inicio cargue más rápido y muestre contenido visible en menos de 3 segundos**,
so that **no abandono antes de ver el contenido, y el sitio mejora su posición en buscadores (LCP es un Core Web Vital directo de ranking)**.

## Contexto del problema

Reporte PageSpeed Insights (móvil, Lighthouse 13.4, 2026-06-18):
- **FCP: 5,1 s** — deficiente (umbral bueno: < 1,8 s; umbral aceptable: < 3,0 s)
- **LCP: 7,7 s** — deficiente (umbral bueno: < 2,5 s; umbral aceptable: < 4,0 s)
- **Performance score: 62/100**
- TBT: 50 ms ✅ | CLS: 0 ✅

### Causa raíz identificada — `vendor-charts` en modulepreload innecesario

El build actual (`dist/index.html`) contiene:

```html
<script type="module" crossorigin src="/assets/index-CNnUh9NE.js"></script>
<link rel="modulepreload" crossorigin href="/assets/vendor-query-seWSRzOJ.js">
<link rel="modulepreload" crossorigin href="/assets/vendor-charts-COY5DSSL.js">  <!-- PROBLEMA -->
<link rel="modulepreload" crossorigin href="/assets/vendor-react-BcAhrv1C.js">
<link rel="stylesheet" crossorigin href="/assets/index-BAMRr2mo.css">
```

`vendor-charts-COY5DSSL.js` (recharts, **102 KB transferido / 373 KB sin comprimir**) está en `modulepreload`, lo que hace que el browser lo descargue y parsee en el arranque de CADA página, incluyendo el homepage donde recharts solo se usa marginalmente (81,4% del bundle = 82 KB son bytes desperdiciados según Lighthouse). Esto consume bandwidth de red crítico y tiempo de parse del main thread **antes** de que el LCP element pueda pintarse.

**Por qué ocurre:** `vite.config.ts` tiene `manualChunks: { 'vendor-charts': ['recharts'] }`. Esta declaración explícita hace que Rollup considere `vendor-charts` como parte del grafo de módulos estático del entry point. Aunque `routes.tsx` usa `lazy()` para todos los componentes de página, algo en la cadena de importación estática (posiblemente `PublicLayout`, `PortafolioRoute` o sus dependencias transitivas) importa un módulo que a su vez importa recharts — o la heurística de modulepreload de Vite preload todos los manualChunks explícitos.

### Segundo hallazgo — `index-CNnUh9NE.js` con 44% de código sin usar

El entry bundle principal (103 KB transferido, 351 KB sin comprimir) tiene 45 KB sin usar (44%). Lleva 195 ms de CPU al arrancar (166 ms scripting + 17 ms parse/compile). Esto sugiere que el bundle del entry arrastra código que solo se necesita en rutas específicas.

### Causa extralimitada — Cloudflare Web Analytics (no accionable en código)

Las URLs `/metrics/fwVy…` y `/metrics/` (inyectadas por Cloudflare CDN, ~297 KB combinados, no son rutas del SPA) generan 2 tareas largas en el main thread (102 ms + 55 ms a t=5.2–6.7 s). Para eliminarlos: desactivar **Cloudflare Web Analytics** en el Cloudflare dashboard. No requiere cambio de código.

## Acceptance Criteria

### AC-1: vendor-charts NO está en modulepreload del homepage

**Dado que** se hace `npm run build` en `src/Web/Main/`,
**Entonces** el `dist/index.html` resultante NO contiene `<link rel="modulepreload" … vendor-charts…>`.
El archivo vendor-charts sigue existiendo como chunk (Rollup lo sigue generando para las rutas que usan recharts), pero se carga de forma diferida.

### AC-2: Lighthouse FCP ≤ 3,5 s y LCP ≤ 5,5 s en modo simulado (mejora medible)

**Dado que** se ejecuta Lighthouse (Moto G Power, 4G lenta) sobre el build de producción local o sobre `https://fibrasinmobiliarias.com` después del despliegue,
**Entonces** FCP ≤ 3,5 s (bajó de 5,1 s) y LCP ≤ 5,5 s (bajó de 7,7 s).
> Nota: los objetivos finales (bueno: FCP < 1,8 s, LCP < 2,5 s) requieren más optimizaciones; esta historia apunta a mejora demostrable sobre la línea base.

### AC-3: Todas las páginas que usan recharts siguen funcionando

**Dado que** se navega a `/fibras/:slug` (gráfica de precio), `/fundamentales`, `/portafolio` (PerformanceChart) y `/oportunidades`,
**Entonces** los gráficos renderizan correctamente (carga diferida, sin error de consola).

### AC-4: Build y tests verdes

- `dotnet build FIBRADIS.slnx` — 0 errores (no hay cambios en backend, pero confirmar que build completo verde)
- `npm run build --workspace=src/Web/Main` — 0 errores, 0 warnings nuevos
- `npm run test --workspace=src/Web/Main` — todos los tests verdes (sin regresión)

## Dev Notes

### Contexto de archivos clave

| Archivo | Estado | Rol en esta historia |
|---|---|---|
| `src/Web/Main/vite.config.ts` | UPDATE | Eliminar `'vendor-charts': ['recharts']` de manualChunks |
| `src/Web/Main/dist/index.html` | OUTPUT | Se regenera con `npm run build`; verificar ausencia de vendor-charts modulepreload |
| `src/Web/Main/src/app/routes.tsx` | READ-ONLY (diagnóstico) | Confirmar que todas las rutas son lazy; investigar importaciones estáticas |
| `src/Web/Main/src/shared/layouts/PublicLayout.tsx` | READ → posible UPDATE | Verificar si importa recharts transitivamente |
| `src/Web/Main/src/modules/portafolio/PortafolioRoute.tsx` | READ-ONLY | Ya usa lazy() internamente — no importa recharts directamente |

### Tarea T1 (DIAGNÓSTICO — hacer primero): trazar por qué vendor-charts está en modulepreload

Antes de hacer el fix, ejecutar:

```bash
# 1. Ver qué archivos del build estático importan vendor-charts
grep -r "vendor-charts" "src/Web/Main/dist/assets/" --include="*.js" -l

# 2. Buscar imports de recharts en módulos no-lazy
grep -rn "from 'recharts'\|from \"recharts\"\|price-chart\|PriceChart\|PerformanceChart" \
  src/Web/Main/src/shared/ \
  src/Web/Main/src/modules/portafolio/portafolio-route.ts \
  src/Web/Main/src/modules/auth/
```

Si grep encuentra recharts en `src/shared/` o en un módulo estáticamente importado, **la solución es lazy-importar ese módulo** (en lugar de solo modificar `manualChunks`).

### Tarea T2 (FIX PRINCIPAL): Eliminar `vendor-charts` de manualChunks

En `vite.config.ts`, eliminar la línea `'vendor-charts': ['recharts']`:

```typescript
// ANTES
manualChunks: {
  'vendor-react': ['react', 'react-dom', 'react-router'],
  'vendor-query': ['@tanstack/react-query'],
  'vendor-charts': ['recharts'],          // ← ELIMINAR
  'vendor-markdown': ['react-markdown', 'remark-gfm'],
},

// DESPUÉS
manualChunks: {
  'vendor-react': ['react', 'react-dom', 'react-router'],
  'vendor-query': ['@tanstack/react-query'],
  'vendor-markdown': ['react-markdown', 'remark-gfm'],
},
```

**Qué hace este cambio:** Rollup seguirá creando un chunk para recharts automáticamente (como dependencia compartida de las rutas que lo usan), pero ya no estará en la lista estática de modulepreload del entry point. Se cargará solo cuando se navegue a una ruta que lo necesite.

**Validar resultado:**
```bash
npm run build --workspace=src/Web/Main
grep "vendor-charts" src/Web/Main/dist/index.html   # debe estar vacío
ls src/Web/Main/dist/assets/ | grep chart           # sigue existiendo como chunk dinámico
```

### Tarea T2b (SI T1 encontró un import estático de recharts): lazy-importar el componente culpable

Si el grep de T1 encuentra un archivo en `src/shared/` o similar que importa recharts, además de eliminar la entrada en manualChunks, hay que romper la cadena estática. Ejemplo si `SharedPortafolioWidget` importa recharts:

```typescript
// src/shared/layouts/PublicLayout.tsx — ANTES (hipotético)
import { SharedPortafolioWidget } from '../widgets/SharedPortafolioWidget'  // recharts transitivo

// DESPUÉS
const SharedPortafolioWidget = lazy(() => import('../widgets/SharedPortafolioWidget').then(m => ({ default: m.SharedPortafolioWidget })))
```

### Tarea T3 (VERIFICACIÓN): medir mejora con Lighthouse antes/después

Obtener baseline actual:
```bash
# Instalar Lighthouse CLI si no está disponible
npx lighthouse https://fibrasinmobiliarias.com --form-factor=mobile --throttling-method=simulate --output=json --output-path=/tmp/before.json 2>&1 | grep -E "FCP|LCP|Performance"
```

O comparar con el reporte baseline del 18 jun 2026: FCP 5,1 s, LCP 7,7 s, score 62/100.

Después del fix, verificar:
1. `npm run build` → abrir `dist/index.html` y confirmar que vendor-charts no está en modulepreload
2. Desplegar al entorno de producción (o usar `npx serve dist/`) con throttling
3. Ejecutar Lighthouse y comparar FCP y LCP con los valores baseline

### Contexto de métricas actuales (baseline del 18 jun 2026)

Datos Lighthouse (móvil, Moto G Power, 4G lenta):

| Métrica | Valor | Umbral bueno |
|---|---|---|
| FCP | 5,1 s | < 1,8 s |
| LCP | 7,7 s | < 2,5 s |
| TBT | 50 ms | < 200 ms ✅ |
| CLS | 0 | < 0,1 ✅ |
| SI | 5,6 s | < 3,4 s |
| TTI | 7,7 s | < 3,8 s |

Top JS sin usar identificado por Lighthouse:
- `gtm.js` — 88 KB unused / 74,7% (ya diferido con requestIdleCallback — difícil de reducir más)
- `vendor-charts` — **82 KB unused / 81,4%** ← TARGET de esta historia
- `/metrics/` (Cloudflare RUM) — 73 KB unused (externo, no accionable en código)
- `index-CNnUh9NE.js` — 45 KB unused / 44,1% (scope de mejora futura)
- `vendor-react` — 20 KB unused / 70,9% (normal para React, no accionable)

Main thread work total: 625 ms (306 ms Script Eval + 166 ms Parse/Compile + 99 ms Other + 53 ms Style/Layout).

### Out of scope (esta historia)

- **Cloudflare Web Analytics** (`/metrics/*`): inyectado por Cloudflare CDN. Para desactivarlo, ir al Cloudflare dashboard → fibrasinmobiliarias.com → Analytics → Web Analytics → desactivar. Ahorra 2 long tasks (102 ms + 55 ms). No requiere cambio de código.
- **Critical CSS inline**: ver análisis detallado abajo (T7).
- **`index-CNnUh9NE.js` 44% unused**: reducir este entry bundle requiere investigar qué módulos en la cadena estática de `PublicLayout` o el router arrastra código no usado en la homepage. Seguimiento si los números después del fix principal justifican más trabajo.

### Análisis Critical CSS (T7) — contexto de decisión

**Situación actual del CSS bloqueante:**

El build genera `<link rel="stylesheet" crossorigin href="/assets/index-HASH.css">` en el `<head>` del HTML. El archivo pesa ~18.5 KB y bloquea el render 337ms en mobile (135ms desktop). Es el warning "Eliminate render-blocking resources" de Lighthouse.

**Por qué `vite-plugin-critical` NO funciona para esta SPA:**

`vite-plugin-critical` extrae CSS crítico lanzando un headless browser, renderizando la página y capturando solo las reglas CSS visibles above-the-fold. El problema: esta app sirve `<div id="root"></div>` sin SSR — el headless browser ve HTML vacío sin ejecutar JavaScript, por lo que extrae prácticamente cero CSS útil. La herramienta está diseñada para sitios SSR/estáticos, no SPAs client-rendered.

**Alternativa viable — "preload trick" vía plugin Vite personalizado:**

Convertir el `<link rel="stylesheet">` bloqueante en preload + swap no-bloqueante mediante un plugin de ~15 líneas en `vite.config.ts`:

```html
<!-- ANTES (bloqueante): -->
<link rel="stylesheet" href="/assets/index-HASH.css">

<!-- DESPUÉS (no-bloqueante): -->
<link rel="preload" as="style" href="/assets/index-HASH.css"
      onload="this.onload=null;this.rel='stylesheet'">
<noscript><link rel="stylesheet" href="/assets/index-HASH.css"></noscript>
```

**Trade-off real para una SPA:**

Hacer el CSS no-bloqueante elimina el warning de Lighthouse y mejora el score, pero el impacto en LCP percibido es marginal: los usuarios siguen viendo pantalla en blanco mientras React ejecuta (~5s mobile). La mejora visible real ya viene de quitar `vendor-charts` del modulepreload (T2 de esta historia). El FOUC (flash sin estilos) es prácticamente invisible porque `<div id="root">` ya está vacío sin React.

**Recomendación:** Implementar T7 solo si tras el fix de T2 el AC-2 (FCP ≤ 3.5s, LCP ≤ 5.5s) no se cumple y se necesita mejorar el score de Lighthouse. El gain es principalmente métrico, no de UX real.

### Convenciones aplicables

- Branch: `story/13-9-fcp-lcp-homepage`
- Sin cambios de rutas, sin cambios de backend
- Tests obligatorios antes de mover a `review` (ver AC-4)
- `workflow-rules.md`: merge a `main` solo al completar y pasar code review

## Tasks / Subtasks

- [ ] T1: Ejecutar diagnóstico — grep para recharts en imports estáticos; documentar hallazgo en Dev Notes del story file
- [ ] T2: Eliminar `'vendor-charts': ['recharts']` de `manualChunks` en `vite.config.ts`
- [ ] T2b: Si T1 encontró import estático de recharts → lazy-importar el módulo culpable
- [ ] T3: `npm run build` y verificar que `dist/index.html` NO contiene `vendor-charts` en modulepreload
- [ ] T4: Navegar a `/fibras/:slug`, `/fundamentales`, `/portafolio` — confirmar que gráficas siguen funcionando
- [ ] T5: Ejecutar tests `npm run test --workspace=src/Web/Main` — todos verdes
- [ ] T6: Medir mejora de FCP/LCP con Lighthouse (local o prod) — documentar valores obtenidos aquí
- [ ] T7: (Opcional si AC-2 no se cumple) CSS no-bloqueante vía preload trick en plugin Vite — NO usar `vite-plugin-critical` (no funciona en SPAs sin SSR; ver análisis en Dev Notes)

## Story Progress Notes

### Agent Model Used: claude-sonnet-4-6
### Completion Notes
Story creada a partir del análisis PageSpeed Insights del 18 jun 2026 (FCP 5,1 s / LCP 7,7 s / score 62). Diagnóstico realizado directamente con Chrome DevTools MCP + API Lighthouse JSON embebido en pagespeed.web.dev. Root cause confirmado: `dist/index.html` tiene `<link rel="modulepreload" … vendor-charts…>` aunque recharts solo se usa en rutas lazy-loaded. Fix principal: 1 línea en vite.config.ts. Mejora esperada: –82 KiB JS sin usar en homepage, reducción de parse time en mobile, FCP/LCP mejoran ~0.5–1.5 s en simulación.

### Change Log
| Date | Status | Notes |
|---|---|---|
| 2026-06-18 | ready-for-dev | Story creada — análisis PSI + diagnóstico código completados |
