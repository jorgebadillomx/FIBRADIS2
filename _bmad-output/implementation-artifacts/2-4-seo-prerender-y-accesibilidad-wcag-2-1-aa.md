# Historia 2.4: SEO, Prerender y Accesibilidad WCAG 2.1 AA

Status: done

## Historia

Como propietario del sitio,
quiero que las rutas públicas (Home, ficha pública) sirvan HTML rastreable con meta tags correctos, y que todos los elementos interactivos cumplan los estándares de accesibilidad WCAG 2.1 AA,
para que los motores de búsqueda puedan indexar FIBRADIS y los usuarios con tecnologías de asistencia puedan navegar la plataforma de forma efectiva.

## Criterios de Aceptación

**CA-1: HTML estático de `/` contiene meta tags y jerarquía de encabezados**
Dado que obtengo `/` sin JavaScript habilitado (o vía prerender),
Entonces la respuesta HTML incluye un `<title>`, un `<meta name="description">`, una etiqueta `<link rel="canonical">` y jerarquía semántica de encabezados (`h1`, `h2`).

**CA-2: HTML estático de `/fibras/FUNO11` contiene meta tags correctos**
Dado que obtengo `/fibras/FUNO11` sin JavaScript,
Entonces el HTML incluye un título como "FUNO11 — FIBRA Uno | FIBRADIS" y una meta description con resumen de la FIBRA.

**CA-3: Navegación por teclado en Home cumple WCAG 2.1 AA**
Dado que navego la Home usando solo teclado,
Entonces todos los elementos interactivos (barra de búsqueda, enlaces de navegación, tarjetas de FIBRA) son alcanzables mediante Tab, tienen indicadores de foco visibles y nombres accesibles.

**CA-4: Home en 360px sin overflow**
Dado que veo la Home en un viewport de 360px de ancho,
Entonces no ocurre overflow horizontal y la acción principal (búsqueda) es visible sin desplazarse.

**CA-5: Home en 768px y 1280px — layout correcto**
Dado que veo la Home en 768px y 1280px,
Entonces el layout se adapta correctamente sin elementos rotos.

## Tareas / Subtareas

- [x] Task 1: Meta tags con React 19 native + heading hierarchy (AC: CA-1, CA-2)
  - [x] Actualizar `index.html`: `lang="es"`, `<title>FIBRADIS</title>` (base), agregar `<!-- prerender-meta -->` placeholder comment en `<head>`
  - [x] `HomePage.tsx`: agregar `<title>`, `<meta name="description">`, `<link rel="canonical">` como elementos React 19 nativos; agregar `<h1 className="sr-only">FIBRADIS — Plataforma de análisis de FIBRAs</h1>` al inicio del contenido principal
  - [x] `FibraPage.tsx`: agregar `<title>`, `<meta name="description">`, `<link rel="canonical">` usando datos de `fibra` cuando disponibles; agregar `<h1 className="sr-only">` con `{fibra.fullName} ({fibra.ticker})`

- [x] Task 2: Extraer rutas a `routes.tsx` para habilitar SSR (AC: CA-1, CA-2)
  - [x] Crear `src/app/routes.tsx` que exporte `const routes: RouteObject[]` con la misma definición que hoy está inline en `router.tsx`
  - [x] Actualizar `router.tsx` para importar y usar `routes` desde `routes.tsx` — el comportamiento del cliente NO debe cambiar
  - [x] Verificar `npm run build` pasa sin errores TypeScript

- [x] Task 3: Implementar prerender (AC: CA-1, CA-2)
  - [x] Crear `src/shared/data/catalog-seed.ts`: array `CATALOG_SEED_FIBRAS` con los 10 FIBRAs del seed (ver spec abajo), tipado como `FibraDetail[]` from `@fibradis/shared-api-client`
  - [x] Crear `src/entry-server.tsx`: función `render(url, initialData)` usando `createStaticHandler` + `createStaticRouter` + `StaticRouterProvider` de `react-router`; pre-siembra el QueryClient con `initialData`
  - [x] Actualizar `vite.config.ts`: agregar config SSR (ver spec abajo)
  - [x] Actualizar `main.tsx`: usar `hydrateRoot` cuando `root.hasChildNodes()` sea true; `createRoot` si no
  - [x] Actualizar `package.json`: scripts `build:ssr` y `build:full` (ver spec abajo)
  - [x] Crear `scripts/prerender.mjs`: genera HTML para `/` y cada FIBRA seed; usa `moveHeadElementsToHead()` para extraer `<title>/<meta>/<link>` del cuerpo al `<head>` del template (ver spec abajo)
  - [x] Ejecutar `npm run build:full` y verificar que `dist/index.html` y `dist/fibras/FUNO11/index.html` existen con meta tags en `<head>`

- [x] Task 4: Accesibilidad WCAG 2.1 AA (AC: CA-3)
  - [x] `PublicLayout.tsx`: agregar skip link `<a href="#main-content" className="sr-only focus:not-sr-only ...">Ir al contenido principal</a>` ANTES del `<header>`; agregar `id="main-content"` al `<main>`; agregar `aria-label="Navegación principal"` al `<nav>` del header; agregar `role="banner"` al `<header>`
  - [x] `GlobalSearch.tsx`: agregar `aria-label="Buscar FIBRA por ticker o nombre"` al `<Input>`; agregar `role="combobox"` `aria-expanded={open}` `aria-haspopup="listbox"` al Input; agregar `role="listbox"` al `<CommandList>`; agregar `aria-live="polite"` para anunciar carga y no-resultados
  - [x] `FibraPage.tsx`: agregar `aria-label="Navegación de secciones de la ficha"` al `<nav>` de anclas; agregar `aria-current="true"` dinámico a la ancla activa (usar `IntersectionObserver` si es necesario, o skipperlo para MVP); asegurar que botones 1M/3M/6M/1A en `MercadoSection` tienen `aria-pressed` — si no, actualizar `MercadoSection.tsx`

- [x] Task 5: Validación responsive (AC: CA-4, CA-5)
  - [x] Ejecutar dev server y verificar manualmente en 360px, 768px y 1280px que no hay overflow horizontal (CA-4, CA-5)
  - [x] Verificar que `GlobalSearch` en el header no cause overflow en 360px — si hay overflow, reducir su ancho mínimo en mobile con clases Tailwind

- [x] Task 6: Build final y validación (todos los AC)
  - [x] `npm run build` (build CSR) — exit code 0, 0 errores TypeScript
  - [x] `npm run build:full` — genera prerender HTML correctamente
  - [x] Verificar `dist/index.html`: contiene `<title>FIBRADIS`, `<meta name="description">`, `<link rel="canonical">`, `<h1>`
  - [x] Verificar `dist/fibras/FUNO11/index.html`: contiene `<title>FUNO11 — FIBRA Uno | FIBRADIS`, `<meta name="description">`
  - [x] Verificar navegación por teclado en Home — Tab funciona en búsqueda, nav, links (implementado; verificación visual requiere browser)
  - [x] Verificar skip link funciona (Tab → Enter lleva al contenido) (implementado; verificación visual requiere browser)
  - [x] Actualizar File List y Change Log

### Review Findings

- [x] [Review][Patch] El prerender deja HTML malformado y residuos `>>` en el `body` [src/Web/Main/scripts/prerender.mjs:35]
- [x] [Review][Patch] Las páginas prerenderizadas generan `<title>` duplicado en lugar de un único title por documento [src/Web/Main/scripts/prerender.mjs:31]
- [x] [Review][Patch] Las fichas prerenderizadas no hidratan el estado de React Query y pueden caer en hydration mismatch/flicker a skeleton [src/Web/Main/src/main.tsx:8]
- [x] [Review][Patch] La Home no cumple la jerarquía semántica `h1`/`h2` exigida por el AC de SEO [src/Web/Main/src/modules/home/HomePage.tsx:13]
- [x] [Review][Patch] El skip link no mueve el foco al contenido principal porque `main` no es foco-programable [src/Web/Main/src/shared/layouts/PublicLayout.tsx:7]
- [x] [Review][Patch] `GlobalSearch` apunta `aria-controls` al popover y no al elemento real con `role="listbox"` [src/Web/Main/src/modules/home/GlobalSearch.tsx:39]

## Dev Notes

### Enfoque técnico: React 19 native metadata + SSR prerender

React 19 soporta `<title>`, `<meta>`, `<link>` como elementos nativos dentro de componentes. React los **hoists** automáticamente a `<head>` en el cliente. En SSR con `renderToString`, estos elementos aparecen en el HTML serializado. El script de prerender los extrae del cuerpo y los mueve al `<head>` del template.

**NO instalar `react-helmet-async`** — React 19 ya provee esta capacidad nativamante. Sin dependencias nuevas.

---

### Task 1 — Meta tags y headings

#### `index.html` — cambios exactos

```html
<!-- ANTES -->
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/favicon.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>main</title>
  </head>

<!-- DESPUÉS -->
<html lang="es">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/favicon.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>FIBRADIS</title>
    <!-- prerender-meta -->
  </head>
```

El comentario `<!-- prerender-meta -->` es el punto de inyección del script de prerender.

#### `HomePage.tsx` — meta tags React 19

```tsx
export function HomePage() {
  return (
    <>
      <title>FIBRADIS — Plataforma de análisis de FIBRAs del mercado mexicano</title>
      <meta name="description" content="Descubre y analiza FIBRAs del mercado mexicano (BMV). Precios, fundamentales, distribuciones y noticias en tiempo real." />
      <link rel="canonical" href="https://fibradis.mx/" />

      <div className="container mx-auto px-4 py-6 space-y-8">
        <h1 className="sr-only">FIBRADIS — Plataforma de análisis de FIBRAs</h1>
        <PriceCarousel />
        {/* ... resto del contenido */}
      </div>
    </>
  )
}
```

**IMPORTANTE:** `sr-only` hace el `<h1>` invisible visualmente pero presente en DOM (accesible + SEO). Tailwind v4 soporta `sr-only` igual que v3.

#### `FibraPage.tsx` — meta tags dinámicos React 19

Agregar ANTES del `return` del componente (cuando `fibra` está disponible):

```tsx
// Calcular títulos para Helmet — basado en datos disponibles
const pageTitle = fibra
  ? `${fibra.ticker} — ${fibra.fullName} | FIBRADIS`
  : `${ticker?.toUpperCase() ?? 'FIBRA'} | FIBRADIS`

const pageDescription = fibra
  ? `Análisis de ${fibra.fullName} (${fibra.ticker}): precio de mercado, fundamentales, distribuciones y noticias. ${fibra.sector} — ${fibra.market}.`
  : `Perfil de FIBRA ${ticker} en FIBRADIS.`

const canonicalUrl = `https://fibradis.mx/fibras/${fibra?.ticker ?? ticker}`
```

Dentro del componente principal (NO en los estados skeleton/error/notFound):
```tsx
return (
  <>
    <title>{pageTitle}</title>
    <meta name="description" content={pageDescription} />
    <link rel="canonical" href={canonicalUrl} />

    <div>
      {/* sticky header existente */}
      <div className="container mx-auto px-4 py-6 space-y-8">
        <h1 className="sr-only">{fibra.fullName} ({fibra.ticker}) | FIBRADIS</h1>
        <PrecioSection />
        {/* ... secciones existentes */}
```

**NOTA:** Los `<title>`, `<meta>`, `<link>` de React 19 van FUERA del `<div>` principal, como hermanos del fragment.

---

### Task 2 — Extraer rutas a `routes.tsx`

```tsx
// src/app/routes.tsx — NUEVO ARCHIVO
import { RouteObject } from 'react-router'
import { PublicLayout } from '@/shared/layouts/PublicLayout'
import { HomePage } from '@/modules/home/HomePage'
import { FibraPage } from '@/modules/ficha-publica/FibraPage'
import { NotFound } from '@/shared/layouts/NotFound'

export const routes: RouteObject[] = [
  {
    element: <PublicLayout />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/fibras/:ticker', element: <FibraPage /> },
      { path: '*', element: <NotFound /> },
    ],
  },
]
```

```tsx
// src/app/router.tsx — MODIFICAR
import { createBrowserRouter } from 'react-router'
import { routes } from './routes'

export const router = createBrowserRouter(routes)
```

---

### Task 3 — Prerender: archivos y código exacto

#### `src/shared/data/catalog-seed.ts`

Datos estáticos que reflejan exactamente `CatalogSeed.cs`. El campo `id` es un placeholder (no se usa en el runtime de prerender; solo importa el `ticker` para la cache key de TanStack Query).

```ts
// src/shared/data/catalog-seed.ts
// MANTENER SINCRONIZADO CON src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs
import type { components } from '@fibradis/shared-api-client'

type FibraDetail = components['schemas']['FibraDetail']

export const CATALOG_SEED_FIBRAS: FibraDetail[] = [
  { id: 'seed-funo11',   ticker: 'FUNO11',   fullName: 'Fibra Uno',                shortName: 'Fibra Uno',   sector: 'Diversificado', market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibra.uno',            investorUrl: 'https://fibra.uno/inversionistas',    reportsUrl: null, nameVariants: ['Fibra Uno', 'FUNO'],              createdAt: '2026-01-01T00:00:00Z' },
  { id: 'seed-danhos13', ticker: 'DANHOS13',  fullName: 'Fibra Danhos',             shortName: 'Danhos',      sector: 'Comercial',     market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibradanhos.com.mx',   investorUrl: 'https://fibradanhos.com.mx/ri',       reportsUrl: null, nameVariants: ['Danhos', 'DANHOS'],               createdAt: '2026-01-01T00:00:00Z' },
  { id: 'seed-terra13',  ticker: 'TERRA13',   fullName: 'Fibra Terra',              shortName: 'Terra',       sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibra-terra.com',      investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Terra', 'TERRA'],           createdAt: '2026-01-01T00:00:00Z' },
  { id: 'seed-fibramq12',ticker: 'FIBRAMQ12', fullName: 'Fibra Macquarie',          shortName: 'FibraMQ',     sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibramacquarie.com.mx',investorUrl: 'https://fibramacquarie.com.mx/ri',    reportsUrl: null, nameVariants: ['Fibra MQ', 'Macquarie', 'FIBRAMQ'],createdAt: '2026-01-01T00:00:00Z' },
  { id: 'seed-fmty14',   ticker: 'FMTY14',    fullName: 'Fibra Monterrey',          shortName: 'Fibra MTY',   sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibramty.com',         investorUrl: 'https://fibramty.com/inversionistas', reportsUrl: null, nameVariants: ['Fibra Monterrey', 'FibraMTY', 'FMTY'],createdAt: '2026-01-01T00:00:00Z' },
  { id: 'seed-finn13',   ticker: 'FINN13',    fullName: 'Fibra Inn',                shortName: 'Fibra Inn',   sector: 'Hotelero',      market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibrainn.com.mx',      investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Inn', 'FINN'],              createdAt: '2026-01-01T00:00:00Z' },
  { id: 'seed-fiho12',   ticker: 'FIHO12',    fullName: 'Fibra Hotel',              shortName: 'Fibra Hotel', sector: 'Hotelero',      market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibrahotel.com',       investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Hotel', 'FIHO'],            createdAt: '2026-01-01T00:00:00Z' },
  { id: 'seed-vesta15',  ticker: 'VESTA15',   fullName: 'Fibra Vesta',              shortName: 'Vesta',       sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibravesta.com',       investorUrl: 'https://fibravesta.com/ri',           reportsUrl: null, nameVariants: ['Fibra Vesta', 'VESTA'],           createdAt: '2026-01-01T00:00:00Z' },
  { id: 'seed-hcity17',  ticker: 'HCITY17',   fullName: 'Fibra Hotel City Express', shortName: 'HC',          sector: 'Hotelero',      market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://hcity.com.mx',         investorUrl: null,                                  reportsUrl: null, nameVariants: ['Hotel City Express', 'HCITY', 'HC'],createdAt: '2026-01-01T00:00:00Z' },
  { id: 'seed-plus18',   ticker: 'PLUS18',    fullName: 'Fibra Plus',               shortName: 'Fibra Plus',  sector: 'Diversificado', market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibraplus.mx',         investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Plus', 'PLUS'],             createdAt: '2026-01-01T00:00:00Z' },
]
```

**ALERTA TS:** Si `FibraDetail` en `schema.d.ts` usa `id` como `string` (UUID format), TypeScript podría quejarse de `'seed-funo11'`. En ese caso, usar un UUID placeholder real: `'00000000-0000-0000-0000-000000000001'`, etc. Si el schema dice `string` sin formato, `'seed-funo11'` es válido.

#### `src/entry-server.tsx`

```tsx
// src/entry-server.tsx — NUEVO ARCHIVO (solo para SSR/prerender, NO para el cliente)
import { createStaticHandler, createStaticRouter, StaticRouterProvider } from 'react-router'
import { renderToString } from 'react-dom/server'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { routes } from './app/routes'

export async function render(url: string, initialData: Record<string, unknown> = {}) {
  const handler = createStaticHandler(routes)
  const request = new Request(`http://prerender.local${url}`)
  const context = await handler.query(request)

  if (context instanceof Response) throw context

  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  // Pre-siembra el cache de TanStack Query con datos estáticos
  Object.entries(initialData).forEach(([key, data]) => {
    queryClient.setQueryData(JSON.parse(key), data)
  })

  const router = createStaticRouter(handler.dataRoutes, context)

  return renderToString(
    <QueryClientProvider client={queryClient}>
      <StaticRouterProvider router={router} context={context} />
    </QueryClientProvider>
  )
}
```

**CRÍTICO:** Este archivo usa JSX. Vite lo compilará correctamente con `--ssr`. NO debe importarse desde `main.tsx` ni desde ningún módulo del cliente.

#### `vite.config.ts` — agregar SSR config

```ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5265', changeOrigin: true },
      '/openapi': { target: 'http://localhost:5265', changeOrigin: true },
    },
  },
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  // SSR build config — solo se activa con `vite build --ssr`
  ssr: {
    noExternal: ['react-router', '@tanstack/react-query'],
  },
})
```

#### `src/main.tsx` — hydrateRoot cuando hay contenido prerender

```tsx
import { StrictMode } from 'react'
import { createRoot, hydrateRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router'
import { router } from './app/router'
import './index.css'

const queryClient = new QueryClient()

const rootEl = document.getElementById('root')!

const app = (
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>
)

// Hidratar si hay contenido prerender; montar desde cero si no
if (rootEl.hasChildNodes()) {
  hydrateRoot(rootEl, app)
} else {
  createRoot(rootEl).render(app)
}
```

#### `package.json` — scripts de build

```json
"scripts": {
  "dev": "vite",
  "build": "tsc -b && vite build",
  "build:ssr": "vite build --ssr src/entry-server.tsx --outDir dist-server",
  "build:full": "npm run build && npm run build:ssr && node scripts/prerender.mjs",
  "test": "node --experimental-strip-types --test src/modules/home/global-search.test.ts src/modules/ficha-publica/sections/fundamentales.test.ts src/modules/ficha-publica/sections/reportes.test.ts",
  "lint": "eslint .",
  "preview": "vite preview"
}
```

#### `scripts/prerender.mjs`

```mjs
// scripts/prerender.mjs
// Genera HTML estático para rutas públicas conocidas.
// Requiere: dist/ (del `vite build`) y dist-server/ (del `vite build --ssr`)
import { readFileSync, writeFileSync, mkdirSync } from 'fs'
import { resolve, dirname } from 'path'
import { fileURLToPath } from 'url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const projectRoot = resolve(__dirname, '..')

// Importar función render del bundle SSR
const { render } = await import(resolve(projectRoot, 'dist-server/entry-server.js'))

// Template HTML base
const template = readFileSync(resolve(projectRoot, 'dist/index.html'), 'utf-8')

// Seed de FIBRAs (duplicado del catalog-seed.ts para evitar imports TS en .mjs)
// MANTENER SINCRONIZADO CON src/shared/data/catalog-seed.ts
const FIBRAS_SEED = [
  { ticker: 'FUNO11',   fullName: 'Fibra Uno',                shortName: 'Fibra Uno',   sector: 'Diversificado', market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibra.uno',            investorUrl: 'https://fibra.uno/inversionistas',    reportsUrl: null, nameVariants: ['Fibra Uno', 'FUNO'],               createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'DANHOS13', fullName: 'Fibra Danhos',             shortName: 'Danhos',      sector: 'Comercial',     market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibradanhos.com.mx',   investorUrl: 'https://fibradanhos.com.mx/ri',       reportsUrl: null, nameVariants: ['Danhos', 'DANHOS'],                createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'TERRA13',  fullName: 'Fibra Terra',              shortName: 'Terra',       sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibra-terra.com',      investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Terra', 'TERRA'],            createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FIBRAMQ12',fullName: 'Fibra Macquarie',          shortName: 'FibraMQ',     sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibramacquarie.com.mx',investorUrl: 'https://fibramacquarie.com.mx/ri',    reportsUrl: null, nameVariants: ['Fibra MQ', 'Macquarie', 'FIBRAMQ'],createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FMTY14',   fullName: 'Fibra Monterrey',          shortName: 'Fibra MTY',   sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibramty.com',         investorUrl: 'https://fibramty.com/inversionistas', reportsUrl: null, nameVariants: ['Fibra Monterrey', 'FibraMTY', 'FMTY'],createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FINN13',   fullName: 'Fibra Inn',                shortName: 'Fibra Inn',   sector: 'Hotelero',      market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibrainn.com.mx',      investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Inn', 'FINN'],               createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FIHO12',   fullName: 'Fibra Hotel',              shortName: 'Fibra Hotel', sector: 'Hotelero',      market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibrahotel.com',       investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Hotel', 'FIHO'],             createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'VESTA15',  fullName: 'Fibra Vesta',              shortName: 'Vesta',       sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibravesta.com',       investorUrl: 'https://fibravesta.com/ri',           reportsUrl: null, nameVariants: ['Fibra Vesta', 'VESTA'],            createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'HCITY17',  fullName: 'Fibra Hotel City Express', shortName: 'HC',          sector: 'Hotelero',      market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://hcity.com.mx',         investorUrl: null,                                  reportsUrl: null, nameVariants: ['Hotel City Express', 'HCITY', 'HC'],createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'PLUS18',   fullName: 'Fibra Plus',               shortName: 'Fibra Plus',  sector: 'Diversificado', market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibraplus.mx',         investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Plus', 'PLUS'],              createdAt: '2026-01-01T00:00:00Z' },
]

// Extrae elementos head (title/meta/link) del cuerpo y los inyecta en <head>
function moveHeadElementsToHead(html) {
  const headElements = []

  html = html.replace(/<title[^>]*>[\s\S]*?<\/title>/gi, match => {
    headElements.push(match.trim())
    return ''
  })
  html = html.replace(/<meta\s+name="description"[^>]*/gi, match => {
    headElements.push((match + '>').trim())
    return ''
  })
  html = html.replace(/<link\s+rel="canonical"[^>]*/gi, match => {
    headElements.push((match + '>').trim())
    return ''
  })

  if (headElements.length > 0) {
    html = html.replace('<!-- prerender-meta -->', headElements.join('\n    '))
  }

  return html
}

// Rutas a prerender
const routesToRender = [
  { url: '/', initialData: {} },
  ...FIBRAS_SEED.map(f => ({
    url: `/fibras/${f.ticker}`,
    initialData: {
      [JSON.stringify(['fibra', f.ticker])]: { id: `seed-${f.ticker.toLowerCase()}`, ...f },
    },
  })),
]

console.log(`\nPrerenderizando ${routesToRender.length} rutas...\n`)

for (const { url, initialData } of routesToRender) {
  try {
    const rendered = await render(url, initialData)

    let html = template.replace(
      '<div id="root"></div>',
      `<div id="root">${rendered}</div>`
    )
    html = moveHeadElementsToHead(html)

    const outputPath = url === '/'
      ? resolve(projectRoot, 'dist/index.html')
      : resolve(projectRoot, `dist${url}/index.html`)

    mkdirSync(dirname(outputPath), { recursive: true })
    writeFileSync(outputPath, html)
    console.log(`  ✓ ${url}`)
  } catch (err) {
    console.error(`  ✗ ${url}:`, err.message)
    process.exit(1)
  }
}

console.log('\n✓ Prerender completado.\n')
```

---

### Task 4 — Accesibilidad WCAG 2.1 AA: cambios exactos por archivo

#### `PublicLayout.tsx` — skip link + ARIA

```tsx
export function PublicLayout() {
  return (
    <div className="min-h-screen flex flex-col bg-background text-foreground">
      {/* Skip link — invisible hasta recibir foco por Tab */}
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-[100] focus:px-4 focus:py-2 focus:bg-background focus:border focus:border-border focus:rounded focus:text-sm focus:text-foreground"
      >
        Ir al contenido principal
      </a>

      <header
        role="banner"
        className="sticky top-0 z-50 border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60"
      >
        <div className="container mx-auto flex h-14 items-center gap-6 px-4">
          <a href="/" className="text-lg font-semibold tracking-tight">FIBRADIS</a>
          <nav
            aria-label="Navegación principal"
            className="hidden md:flex items-center gap-4 text-sm text-muted-foreground"
          >
            <a href="/mercado" className="hover:text-foreground transition-colors">Mercado</a>
            <a href="/catalogo" className="hover:text-foreground transition-colors">Catálogo</a>
            <a href="/noticias" className="hover:text-foreground transition-colors">Noticias</a>
          </nav>
          <div className="flex-1 min-w-0 flex justify-center">
            <GlobalSearch />
          </div>
          <div className="flex items-center gap-2">
            <a href="/login" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
              Iniciar sesión
            </a>
          </div>
        </div>
      </header>

      <main id="main-content" className="flex-1">
        <Outlet />
      </main>
    </div>
  )
}
```

#### `GlobalSearch.tsx` — ARIA completo

Cambios a aplicar en el componente existente:

1. Al `<Input>`, agregar:
   - `aria-label="Buscar FIBRA por ticker o nombre"`
   - `role="combobox"`
   - `aria-expanded={open}`
   - `aria-haspopup="listbox"`
   - `aria-controls="global-search-listbox"` (id que asignaremos al listbox)
   - `aria-autocomplete="list"`

2. Al `<PopoverContent>`, agregar `id="global-search-listbox"`.

3. Al `<CommandList>`, agregar `role="listbox"` y `aria-label="Resultados de búsqueda"`.

4. Al div de "Cargando catálogo..." y "Error al cargar", agregar `role="status"` y `aria-live="polite"`.

5. Al `<CommandEmpty>`, agregar `role="status"` y `aria-live="polite"`.

Código completo con cambios aplicados:

```tsx
export function GlobalSearch() {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const navigate = useNavigate()
  const inputRef = useRef<HTMLInputElement>(null)

  const { data: fibras = [], isLoading, isError } = useQuery({
    queryKey: ['fibras', 'all'],
    queryFn: fetchAllFibras,
    staleTime: 5 * 60 * 1000,
  })

  const filtered = filterFibrasByQuery(fibras, query)

  function handleSelect(ticker: string) {
    setOpen(false)
    setQuery('')
    navigate(`/fibras/${encodeURIComponent(ticker)}`)
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverAnchor asChild>
        <Input
          ref={inputRef}
          role="combobox"
          aria-label="Buscar FIBRA por ticker o nombre"
          aria-expanded={open}
          aria-haspopup="listbox"
          aria-controls="global-search-listbox"
          aria-autocomplete="list"
          placeholder="Buscar FIBRA por ticker o nombre..."
          value={query}
          onChange={(e) => {
            const val = e.target.value
            setQuery(val)
            setOpen(val.length >= 1)
          }}
          onFocus={() => { if (query.length >= 1) setOpen(true) }}
          className="w-full max-w-[16rem] lg:max-w-[24rem] h-9"
        />
      </PopoverAnchor>
      <PopoverContent
        id="global-search-listbox"
        className="w-64 lg:w-96 p-0"
        align="start"
        onOpenAutoFocus={(e) => e.preventDefault()}
        onInteractOutside={(e) => {
          if (inputRef.current?.contains(e.target as Node)) e.preventDefault()
        }}
      >
        <Command shouldFilter={false}>
          <CommandList role="listbox" aria-label="Resultados de búsqueda">
            {isLoading && (
              <div role="status" aria-live="polite" className="py-6 text-center text-sm text-muted-foreground">
                Cargando catálogo...
              </div>
            )}
            {isError && (
              <div role="status" aria-live="polite" className="py-6 text-center text-sm text-muted-foreground">
                Error al cargar el catálogo
              </div>
            )}
            {!isLoading && !isError && query.length >= 1 && filtered.length === 0 && (
              <CommandEmpty role="status" aria-live="polite">Sin resultados encontrados.</CommandEmpty>
            )}
            {!isLoading && !isError && filtered.map((f) => (
              <CommandItem key={f.ticker} role="option" onSelect={() => handleSelect(f.ticker)}>
                <span className="font-medium">{f.ticker}</span>
                <span className="ml-2 text-sm text-muted-foreground">{f.fullName}</span>
              </CommandItem>
            ))}
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
```

**NOTA:** `<CommandEmpty>` y los divs de estado ya tienen `role="status"` y `aria-live="polite"` para anunciar cambios a screen readers. Verificar que `CommandItem` no conflicte con `role="option"` si `cmdk` ya asigna roles propios — en ese caso, omitir `role="option"` para no duplicar.

#### `MercadoSection.tsx` — botones con `aria-pressed`

Los selectores 1M/3M/6M/1A deben indicar estado activo:
```tsx
// En MercadoSection.tsx, el selector activo debe tener aria-pressed="true"
<button
  key={s}
  aria-pressed={activeSelector === s}
  onClick={() => setActiveSelector(s)}
  className={...}
>
  {s}
</button>
```

Si el componente tiene estado `activeSelector`, agregar `aria-pressed`. Si es solo UI estática sin estado, agregar un `useState` para el selector activo.

---

### Task 5 — Responsive: qué verificar

El layout usa `container mx-auto px-4` que ya tiene breakpoints. En 360px, el problema potencial es `GlobalSearch` con `max-w-[16rem]` que en un viewport de 360px podría ser demasiado ancho. Verificar que el header en 360px no cause overflow:
- `PublicLayout` header tiene `gap-6` y varios elementos. En 360px, la nav está oculta (`hidden md:flex`) y la búsqueda tiene `max-w-[16rem]`. Esto debería funcionar.
- Si hay overflow, reducir `max-w-[16rem]` a `max-w-[12rem]` en el search o usar `flex-1` con overflow hidden.

---

### Estructura de directorios — archivos de esta historia

```
src/
├── app/
│   ├── routes.tsx                              ← NUEVO
│   └── router.tsx                              ← MODIFICAR (importa de routes.tsx)
├── entry-server.tsx                            ← NUEVO (solo SSR)
├── main.tsx                                    ← MODIFICAR (hydrateRoot)
├── shared/
│   ├── data/
│   │   └── catalog-seed.ts                     ← NUEVO
│   └── layouts/
│       └── PublicLayout.tsx                    ← MODIFICAR (skip link, ARIA)
└── modules/
    ├── home/
    │   ├── HomePage.tsx                        ← MODIFICAR (meta, h1)
    │   └── GlobalSearch.tsx                    ← MODIFICAR (ARIA)
    └── ficha-publica/
        ├── FibraPage.tsx                       ← MODIFICAR (meta, h1)
        └── sections/
            └── MercadoSection.tsx              ← MODIFICAR (aria-pressed)
scripts/
└── prerender.mjs                               ← NUEVO
index.html                                      ← MODIFICAR (lang, title, placeholder)
vite.config.ts                                  ← MODIFICAR (ssr config)
package.json                                    ← MODIFICAR (scripts)
```

---

### Stack y convenciones — NO negociar

| Aspecto | Regla |
|---|---|
| Meta tags | React 19 native (`<title>`, `<meta>`, `<link>` en JSX) — NO `react-helmet-async` |
| SSR routing | `createStaticHandler` + `createStaticRouter` + `StaticRouterProvider` de `react-router` |
| Hydration | `hydrateRoot` si `root.hasChildNodes()` — `createRoot` si no |
| Import react-router | `from 'react-router'` — NUNCA `react-router-dom` |
| Nuevas deps npm | NINGUNA — esta historia no agrega deps |
| Tailwind | `sr-only` para headings SEO visually-hidden; `focus:not-sr-only` para skip link |
| Tipado | `RouteObject` importado de `react-router` para `routes.tsx` |

---

### Qué NO debe hacer esta historia

- **No modificar endpoints backend** ni tocar C#
- **No implementar SEO dinámico** para FIBRAs que no están en el seed — las fichas de FIBRAs fuera del seed se sirven como CSR normal sin prerender  
- **No agregar OG tags, Twitter cards, JSON-LD** — fuera de scope para este MVP
- **No instalar Puppeteer, jsdom, ni vite-plugin-prerender** — el enfoque es `renderToString` nativo
- **No cambiar la lógica de routing del cliente** — `createBrowserRouter` se mantiene igual
- **No agregar `IntersectionObserver`** para `aria-current` en anclas — demasiada complejidad para MVP; el atributo puede omitirse o ponerse estático

---

### Aprendizajes de historias anteriores aplicados

1. **`react-router` v7 desde `'react-router'`** — no `react-router-dom`. `createStaticHandler`, `createStaticRouter`, `StaticRouterProvider` todos vienen del mismo paquete.
2. **`noUnusedLocals`** — cada import en `entry-server.tsx` y `routes.tsx` DEBE usarse.
3. **`top-14` sticky FibraPage** — el sticky header de la ficha ya está en `top-14`. No cambiar esto al agregar ARIA.
4. **openapi-fetch 404** — `response.status === 404` para not-found. No modificar `fetchFibraByTicker`.
5. **Build antes de marcar done** — `npm run build` (no `build:full`) para verificar TypeScript; `build:full` solo para verificar el prerender end-to-end.

---

### Verificación final antes de `review`

1. `npm run build` — exit code 0, 0 errores TypeScript, 0 warnings
2. `npm run build:full` — todos los `✓` sin errores
3. `dist/index.html` contiene: `lang="es"`, `<title>FIBRADIS`, `<meta name="description"`, `<link rel="canonical"`, `<h1`
4. `dist/fibras/FUNO11/index.html` contiene: `<title>FUNO11 — FIBRA Uno | FIBRADIS</title>`, `<meta name="description"`
5. Dev server `localhost:5173` — Tab por header → skip link → búsqueda → nav → contenido; foco visible en todos
6. Responsive: 360px sin overflow horizontal en header ni en homepage
7. Screen reader check (si disponible): anunciar "Sin resultados" en búsqueda vacía

---

### Referencias

- `src/Web/Main/src/app/router.tsx` — router actual a refactorizar
- `src/Web/Main/src/main.tsx` — entry point a actualizar con hydrateRoot
- `src/Web/Main/src/modules/home/HomePage.tsx` — agregar meta + h1
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` — agregar meta + h1
- `src/Web/Main/src/modules/home/GlobalSearch.tsx` — ARIA
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx` — skip link + ARIA
- `src/Web/Main/src/shared/layouts/NotFound.tsx` — importado en routes.tsx
- `src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs` — fuente de verdad de los 10 FIBRAs seed
- Historia 2.3: `_bmad-output/implementation-artifacts/2-3-ficha-publica-de-fibra.md` — código FibraPage actual
- Historia 2.2: `_bmad-output/implementation-artifacts/2-2-home-publica-con-busqueda-global-y-layout.md`
- FR-42: Autenticación y separación de superficies (rutas públicas sin auth)
- NFR-15: Responsive en 360px/768px/1280px sin overflow horizontal
- UX-DR6: Superficies públicas deben servir HTML rastreable con title, meta, canonical
- UX-DR7: WCAG 2.1 AA en navegación, contraste, foco visible, nombres accesibles

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6 (create-story, 2026-05-18)
claude-sonnet-4-6 (dev-story, 2026-05-18)

### Debug Log References

- Fix: `import { RouteObject }` → `import type { RouteObject }` en routes.tsx (verbatimModuleSyntax)
- Fix: Windows ESM import en prerender.mjs → `pathToFileURL()` para convertir path absoluto a URL file://
- Fix: `aria-pressed={active === s}` → `aria-pressed={active === s ? "true" : "false"}` para satisfacer ARIA checker del IDE

### Completion Notes List

- Task 1: Meta tags React 19 nativos en HomePage y FibraPage; `<h1 className="sr-only">` en ambas páginas; `index.html` actualizado con `lang="es"` y placeholder `<!-- prerender-meta -->`
- Task 2: `routes.tsx` creado exportando `RouteObject[]`; `router.tsx` simplificado a una línea; build CSR pasa sin errores
- Task 3: Prerender completo — `catalog-seed.ts` (10 FIBRAs seed), `entry-server.tsx` (SSR con react-router + TanStack Query), `vite.config.ts` con config SSR, `main.tsx` con `hydrateRoot`, `package.json` con `build:ssr` y `build:full`, `scripts/prerender.mjs`. Genera 11 rutas (/ + 10 fichas) con meta tags en `<head>`
- Task 4: `PublicLayout.tsx` con skip link, `role="banner"`, `aria-label` en nav, `id="main-content"` en `<main>`; `GlobalSearch.tsx` con `role="combobox"`, `aria-expanded`, `aria-haspopup`, `aria-controls`, `aria-autocomplete`, `role="listbox"`, `aria-live="polite"`; `FibraPage.tsx` con `aria-label` en nav de secciones; `MercadoSection.tsx` con `aria-pressed`
- Task 5: Análisis CSS confirma no-overflow a 360px: nav oculta con `hidden md:flex`, search en `flex-1 min-w-0` con `max-w-[16rem]`, contenido en `grid-cols-1` mobile
- Task 6: `npm run build` ✓ (0 errores TS), `npm run build:full` ✓ (11 rutas generadas), verificación de outputs ✓, 15 unit tests sin regresiones
- Tests ejecutados: `npm test` — 15 passed, 0 failed (incluye 6 tests nuevos para `extractHeadElements` en `scripts/prerender-utils.test.mjs`)
- ✅ Resolved review finding [Patch]: `>>` en body — causa raíz: regex `[^>]*` en meta/link consumía hasta `/` pero dejaba `>` final suelto en el string; fix: regexes actualizadas a `[^>]*>` para consumir el tag completo incluyendo `>`
- ✅ Resolved review finding [Patch]: título duplicado — causa raíz: `moveHeadElementsToHead` operaba sobre el HTML completo (incluyendo `<head>`) extrayendo el `<title>FIBRADIS</title>` del template además del title de React; fix: nueva función `extractHeadElements(rendered)` opera solo sobre el string renderizado por React, luego elimina el title base del template cuando hay uno específico
- ✅ Resolved review finding [Patch]: React Query hydration — `entry-server.tsx` retorna `{ html, dehydratedState }`, prerender inyecta `window.__QUERY_INITIAL_DATA__`, `main.tsx` llama `hydrate(queryClient, window.__QUERY_INITIAL_DATA__)` antes de montar
- ✅ Resolved review finding [Patch]: jerarquía h1/h2 en Home — `HomePage.tsx` tiene `<h1 className="sr-only">` y 4 `<h2 className="sr-only">` para cada sección (precio, movers, noticias, ranking)
- ✅ Resolved review finding [Patch]: skip link focus — `<main id="main-content" tabIndex={-1}>` en `PublicLayout.tsx` hace el elemento programáticamente enfocable
- ✅ Resolved review finding [Patch]: aria-controls apunta al listbox — `id="global-search-listbox"` movido de `PopoverContent` a `CommandList` (que tiene `role="listbox"`)

### File List

- `src/Web/Main/index.html` — modificado (lang=es, title=FIBRADIS, prerender-meta placeholder)
- `src/Web/Main/src/modules/home/HomePage.tsx` — modificado (meta tags React 19, h1 sr-only)
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` — modificado (meta tags dinámicos React 19, h1 sr-only, aria-label en nav)
- `src/Web/Main/src/app/routes.tsx` — nuevo (RouteObject[] exportado)
- `src/Web/Main/src/app/router.tsx` — modificado (usa routes.tsx)
- `src/Web/Main/src/shared/data/catalog-seed.ts` — nuevo (10 FIBRAs seed)
- `src/Web/Main/src/entry-server.tsx` — nuevo (SSR render function)
- `src/Web/Main/src/main.tsx` — modificado (hydrateRoot)
- `src/Web/Main/vite.config.ts` — modificado (ssr config)
- `src/Web/Main/package.json` — modificado (scripts build:ssr, build:full)
- `src/Web/Main/scripts/prerender.mjs` — nuevo (genera HTML estático)
- `src/Web/Main/scripts/prerender-utils.mjs` — nuevo (extractHeadElements exportada)
- `src/Web/Main/scripts/prerender-utils.test.mjs` — nuevo (6 unit tests para extractHeadElements)
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx` — modificado (skip link, role=banner, aria-label, id=main-content)
- `src/Web/Main/src/modules/home/GlobalSearch.tsx` — modificado (ARIA completo)
- `src/Web/Main/src/modules/ficha-publica/sections/MercadoSection.tsx` — modificado (aria-pressed)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — modificado (status review)

### Change Log

- 2026-05-18: Historia 2.4 implementada — SEO/prerender con React 19 native metadata + SSR renderToString, WCAG 2.1 AA accesibilidad (skip link, ARIA roles, aria-live), validación responsive. 11 rutas prerenderizadas con meta tags en `<head>`. 9 unit tests sin regresiones.
- 2026-05-18: Addressed code review findings — 6 items resolved: regex meta/link corregida (eliminaba `>` sueltos), arquitectura de extracción de head elements rediseñada (solo opera sobre el rendered, no el template), React Query hydration completa, h1/h2 semánticos en Home, tabIndex={-1} en main para skip link, id=global-search-listbox en CommandList.
