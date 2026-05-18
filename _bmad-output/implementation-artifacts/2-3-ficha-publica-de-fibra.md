# Historia 2.3: Ficha Pública de FIBRA

Status: review

## Historia

Como visitante público,
quiero ver la página de perfil público completo de una FIBRA mostrando sus datos de encabezado, gráfica de historial de precios, fundamentales del último período disponible, últimas 8 distribuciones, últimas 10 noticias asociadas y lista de reportes oficiales,
para que pueda investigar una FIBRA de forma completa sin navegar fuera de su ficha.

## Criterios de Aceptación

**CA-1: Ficha pública carga con todas las secciones**
Dado que navego a `/fibras/FUNO11`,
Cuando la página carga,
Entonces veo:
- Encabezado sticky con nombre completo, ticker, sector, mercado, moneda y estado de la FIBRA; el precio muestra un placeholder claro hasta la Épica 3
- Anclas de navegación en el header hacia: Mercado, Fundamentales, Distribuciones, Noticias, Reportes
- Sección Mercado: gráfica con selectores 1M/3M/6M/1A en estado vacío (sin datos de mercado aún)
- Sección Fundamentales: estado vacío con mensaje descriptivo ("disponible en Épica 5")
- Sección Distribuciones: estado vacío con mensaje descriptivo ("disponible en Épica 3")
- Sección Noticias: placeholder ("disponible en Épica 4")
- Sección Reportes: links reales a siteUrl, investorUrl y reportsUrl si existen en el catálogo; `—` si null

**CA-2: Advertencia de fundamentales desactualizados**
Dado que los datos de fundamentales de FUNO11 son de hace 3 trimestres,
Entonces se muestra una advertencia visible: "Último reporte disponible: hace 3 periodos — datos podrían estar desactualizados."
_(Nota: en MVP no habrá datos de fundamentales reales — el componente debe estar preparado para renderizar esta advertencia cuando se reciban datos en Épica 5)_

**CA-3: Métrica de fundamentales null muestra `—`**
Dado que una métrica de fundamentales es null para el último período,
Entonces esa métrica muestra `—` en la UI — sin error, sin sustitución por cero.
_(Nota: comportamiento preparado en la estructura del componente aunque no haya datos reales aún)_

**CA-4: Ticker inexistente muestra página de no encontrado**
Dado que navego a `/fibras/FAKE99`,
Cuando la API devuelve 404,
Entonces veo una página clara de "FIBRA no encontrada" con un enlace de regreso a la Home — sin error de JavaScript.

**CA-5: Estado de carga durante fetch**
Dado que navego a cualquier ficha,
Mientras se carga el detalle de la FIBRA,
Entonces veo un skeleton/estado de carga — sin contenido vacío ni flash de "no encontrado".

## Tareas / Subtareas

- [x] Task 1: Agregar `fetchFibraByTicker` a `fibrasApi.ts` (AC: CA-1, CA-4, CA-5)
  - [x] Llamada tipada a `GET /api/v1/fibras/{ticker}` usando el cliente openapi-fetch existente
  - [x] Retornar `null` si el status es 404 (not found); lanzar error para otros fallos
  - [x] Retornar `data` (tipo `components["schemas"]["FibraDetail"]`) si 200

- [x] Task 2: Crear `FibraNotFound.tsx` para ticker inexistente (AC: CA-4)
  - [x] Mostrar mensaje "FIBRA no encontrada" con el ticker buscado
  - [x] Enlace de regreso a la Home usando `<Link to="/">`
  - [x] Responsive en 360px/768px/1280px

- [x] Task 3: Crear secciones de la ficha (AC: CA-1, CA-2, CA-3)
  - [x] `PrecioSection.tsx` — placeholder con mensaje "Precio de mercado disponible en Épica 3"
  - [x] `MercadoSection.tsx` — selectores 1M/3M/6M/1A + estado vacío de gráfica
  - [x] `FundamentalesSection.tsx` — estructura de tabla preparada, estado vacío "disponible en Épica 5"; lógica de advertencia de períodos desactualizados y render de `—` para nulos ya implementada
  - [x] `DistribucionesSection.tsx` — lista preparada, estado vacío "disponible en Épica 3"
  - [x] `NoticiasSection.tsx` — placeholder "disponible en Épica 4"
  - [x] `ReportesSection.tsx` — links reales a `siteUrl`, `investorUrl`, `reportsUrl`; muestra `—` si todos son null

- [x] Task 4: Crear `FibraPage.tsx` como página principal (AC: CA-1, CA-2, CA-3, CA-4, CA-5)
  - [x] `useParams<{ ticker: string }>()` para leer el ticker de la URL
  - [x] `useQuery` con `queryKey: ['fibra', ticker]` y `queryFn: () => fetchFibraByTicker(ticker)`
  - [x] Estado loading → mostrar skeleton
  - [x] Estado error → mostrar mensaje de error genérico
  - [x] `data === null` → mostrar `<FibraNotFound ticker={ticker} />`
  - [x] `data !== null && data !== undefined` → renderizar layout completo con todas las secciones
  - [x] Header sticky con ticker, nombre, anclas de navegación
  - [x] Pasar `fibra` (FibraDetail) como prop a cada sección

- [x] Task 5: Actualizar `router.tsx` para usar `FibraPage` (AC: CA-1)
  - [x] Cambiar import de `FichaPlaceholder` a `FibraPage`
  - [x] Actualizar la ruta `/fibras/:ticker` para usar `<FibraPage />`
  - [x] Eliminar `FichaPlaceholder.tsx` (ya no se usa)

- [x] Task 6: Validación final (NFR-15, todos los CA)
  - [x] `npm run build --workspace=src/Web/Main` — 0 errores TypeScript, 0 warnings
  - [x] Verificar en 360px, 768px y 1280px que no hay overflow horizontal (NFR-15)
  - [x] Verificar CA-1: `/fibras/FUNO11` carga con todas las secciones
  - [x] Verificar CA-4: `/fibras/FAKE99` muestra "FIBRA no encontrada"
  - [x] Verificar CA-5: hay skeleton visible durante carga (simular red lenta)

## Dev Notes

### Backend — Endpoint existente, NO modificar

El endpoint `GET /api/v1/fibras/{ticker}` ya está implementado y funcional desde Historia 2.1. **No crear ni modificar ningún endpoint backend en esta historia.**

Contrato de respuesta 200:
```typescript
// components["schemas"]["FibraDetail"] en schema.d.ts
{
  id: string          // uuid
  ticker: string
  fullName: string
  shortName: string
  sector: string
  market: string
  currency: string
  state: string       // "Active" | "Inactive"
  siteUrl: null | string
  investorUrl: null | string
  reportsUrl: null | string
  nameVariants: string[]
  createdAt: string   // ISO date-time
}
```

Contrato de respuesta 404 (ProblemDetails):
```json
{
  "title": "FIBRA no encontrada",
  "detail": "No existe una FIBRA con ticker 'FAKE99'.",
  "status": 404,
  "domainCode": "FIBRA_NOT_FOUND"
}
```

### Task 1: `fetchFibraByTicker` — patrón correcto para openapi-fetch con 404

```typescript
// src/Web/Main/src/api/fibrasApi.ts — AGREGAR esta función
export async function fetchFibraByTicker(ticker: string) {
  const { data, error, response } = await apiClient.GET('/api/v1/fibras/{ticker}', {
    params: { path: { ticker } }
  })
  if (error) {
    if (response.status === 404) return null
    throw new Error(`Error al obtener FIBRA '${ticker}': ${JSON.stringify(error)}`)
  }
  return data
}
```

**CRÍTICO:** La respuesta 404 está mapeada en `schema.d.ts` (`.ProducesProblem(StatusCodes.Status404NotFound)` en el backend). openapi-fetch coloca el cuerpo del error en `error` y la respuesta HTTP en `response`. Usar `response.status === 404` para distinguir not-found de errores reales.

### Task 2: `FibraNotFound.tsx`

Componente sencillo. Usar `<Link to="/">` de `react-router` (NO `<a href>`).

```tsx
// src/Web/Main/src/modules/ficha-publica/FibraNotFound.tsx
import { Link } from 'react-router'

interface Props { ticker: string }

export function FibraNotFound({ ticker }: Props) {
  return (
    <div className="container mx-auto px-4 py-16 text-center">
      <h1 className="text-2xl font-semibold mb-2">FIBRA no encontrada</h1>
      <p className="text-muted-foreground mb-6">
        No existe una FIBRA con ticker <span className="font-mono font-medium">{ticker.toUpperCase()}</span> en el catálogo.
      </p>
      <Link to="/" className="text-sm text-primary hover:underline">← Volver a la Home</Link>
    </div>
  )
}
```

### Task 3: Estructura de secciones

**`PrecioSection.tsx`** — precio placeholder:
```tsx
// No recibe props de precio (no hay datos en MVP)
export function PrecioSection() {
  return (
    <div className="rounded-lg border border-border bg-muted/30 px-4 py-3 flex items-center gap-3">
      <span className="text-2xl font-semibold text-muted-foreground">—</span>
      <span className="text-sm text-muted-foreground">Precio de mercado disponible en Épica 3</span>
    </div>
  )
}
```

**`MercadoSection.tsx`** — gráfica con selectores, estado vacío:
```tsx
// Selectores visibles pero gráfica con empty state
const SELECTORS = ['1M', '3M', '6M', '1A'] as const
```
Los selectores deben tener estado visual (botones con el activo resaltado), pero el área de gráfica muestra un empty state: "Historial de precios disponible en Épica 3".

**`FundamentalesSection.tsx`** — preparada para datos futuros:

Esta sección debe estar lista para recibir datos cuando llegue Épica 5. Para MVP muestra empty state. La lógica de advertencia de períodos y render de `—` debe estar comentada o con comprobaciones de null-safety.

Etiqueta de período esperada: `"Cap Rate — Q3 2024"` → formato `"${label} — ${period}"`

Advertencia de desactualización (CA-2): si `periodsAgo >= 3` → mostrar banner de advertencia amarillo.

**`DistribucionesSection.tsx`** — tabla preparada, estado vacío actualmente.

**`NoticiasSection.tsx`** — texto placeholder sin datos.

**`ReportesSection.tsx`** — ÚNICA sección con datos reales del MVP:
```tsx
interface Props {
  siteUrl: string | null
  investorUrl: string | null
  reportsUrl: string | null
}

// Mostrar solo los links que no son null
// Si todos son null → mostrar "—"
// Abrir en nueva tab con rel="noopener noreferrer"
```

### Task 4: `FibraPage.tsx` — lógica de estados

```tsx
import { useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchFibraByTicker } from '@/api/fibrasApi'
import { FibraNotFound } from './FibraNotFound'
// ... demás imports de secciones

export function FibraPage() {
  const { ticker } = useParams<{ ticker: string }>()

  const { data: fibra, isLoading, isError } = useQuery({
    queryKey: ['fibra', ticker],
    queryFn: () => fetchFibraByTicker(ticker!),
    enabled: !!ticker,
  })

  if (isLoading) return <FibraPageSkeleton />
  if (isError) return <FibraErrorState />
  if (fibra === null) return <FibraNotFound ticker={ticker!} />

  return (
    <div>
      {/* Sticky header con ticker + nombre + anclas */}
      <header className="sticky top-14 z-40 border-b border-border bg-background/95 backdrop-blur ...">
        <div className="container mx-auto px-4 py-3">
          <div className="flex items-center justify-between">
            <div>
              <span className="text-lg font-semibold">{fibra.ticker}</span>
              <span className="ml-2 text-sm text-muted-foreground">{fibra.fullName}</span>
            </div>
            {/* Anclas */}
            <nav className="hidden md:flex gap-4 text-sm text-muted-foreground">
              <a href="#mercado">Mercado</a>
              <a href="#fundamentales">Fundamentales</a>
              <a href="#distribuciones">Distribuciones</a>
              <a href="#noticias">Noticias</a>
              <a href="#reportes">Reportes</a>
            </nav>
          </div>
        </div>
      </header>
      
      <div className="container mx-auto px-4 py-6 space-y-8">
        {/* Precio */}
        <PrecioSection />
        
        {/* Metadatos */}
        <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
          <span>Sector: {fibra.sector}</span>
          <span>Mercado: {fibra.market}</span>
          <span>Moneda: {fibra.currency}</span>
          <span>Estado: {fibra.state}</span>
        </div>

        {/* Secciones ancladas */}
        <section id="mercado"><MercadoSection /></section>
        <section id="fundamentales"><FundamentalesSection /></section>
        <section id="distribuciones"><DistribucionesSection /></section>
        <section id="noticias"><NoticiasSection /></section>
        <section id="reportes">
          <ReportesSection
            siteUrl={fibra.siteUrl}
            investorUrl={fibra.investorUrl}
            reportsUrl={fibra.reportsUrl}
          />
        </section>
      </div>
    </div>
  )
}
```

**`top-14` en el sticky header de ficha:** el header principal del layout (`PublicLayout.tsx`) tiene `h-14` y es sticky. El header de la ficha debe apilarse debajo, por eso `top-14` (no `top-0`).

**Skeleton:** un componente sencillo `FibraPageSkeleton` con divs animados usando las clases de Tailwind `animate-pulse bg-muted rounded`. No instalar librerías adicionales.

### Task 5: Actualizar `router.tsx`

```tsx
// Cambiar:
import { FichaPlaceholder } from '@/modules/ficha-publica/FichaPlaceholder'
// Por:
import { FibraPage } from '@/modules/ficha-publica/FibraPage'

// Y en la ruta:
{ path: '/fibras/:ticker', element: <FibraPage /> },
```

Después de verificar que el build pasa sin errores, **eliminar `FichaPlaceholder.tsx`** (ya no tiene ningún uso).

### Estructura de directorios — archivos de esta historia

```
src/Web/Main/src/
├── api/
│   └── fibrasApi.ts                              ← MODIFICAR: agregar fetchFibraByTicker
├── app/
│   └── router.tsx                                ← MODIFICAR: FibraPage en lugar de FichaPlaceholder
└── modules/
    └── ficha-publica/
        ├── FichaPlaceholder.tsx                  ← ELIMINAR (reemplazado por FibraPage)
        ├── FibraPage.tsx                         ← NUEVO (página principal)
        ├── FibraNotFound.tsx                     ← NUEVO (estado 404)
        └── sections/
            ├── PrecioSection.tsx                 ← NUEVO
            ├── MercadoSection.tsx                ← NUEVO
            ├── FundamentalesSection.tsx          ← NUEVO
            ├── DistribucionesSection.tsx         ← NUEVO
            ├── NoticiasSection.tsx               ← NUEVO
            └── ReportesSection.tsx               ← NUEVO
```

### Stack exacto — NO negociar versiones

| Componente | Versión |
|---|---|
| React Router | 7 (import de `react-router`, NO `react-router-dom`) |
| TanStack Query | v5 (`useQuery`, `queryKey`, `queryFn`, `enabled`) |
| openapi-fetch | ^0.14.0 (cliente ya en `fibrasApi.ts`) |
| shadcn | radix-nova style |
| Tailwind | v4 |

### Patrón de datos degradados — UX Spec PI-05

Ningún dato financiero aparece sin su estado de calidad. Para MVP, donde los datos aún no existen:
- Precio → `—` + mensaje explicativo (no simplemente vacío)
- Gráfica → empty state descriptivo con qué épica lo poblará
- Fundamentales → empty state
- Distribuciones → empty state
- Nunca mostrar `0` para datos nulos — siempre `—`

### Convenciones de código a respetar

- Componentes en PascalCase, archivos `.tsx`
- Imports absolutos con alias `@/`
- `noUnusedLocals` activo en tsconfig — no dejar imports sin usar
- No añadir dependencias npm nuevas — todo está disponible
- No crear nuevos componentes shadcn (`npx shadcn@latest add`) — los existentes son suficientes: `button.tsx`, `command.tsx`, `popover.tsx`, `input.tsx`

### Qué NO debe hacer esta historia

- **No crear ni modificar endpoints backend**
- **No implementar precio real** — placeholder explícito
- **No implementar gráfica real** — selectores + empty state es suficiente
- **No implementar fundamentales reales** — solo estructura + empty state
- **No implementar distribuciones reales** — solo estructura + empty state
- **No implementar noticias** — placeholder
- **No añadir favoritos** — Épica 7
- **No añadir FreshnessBADGE al precio** — Épica 3
- **No agregar SEO/meta tags** — Historia 2.4
- **No instalar Chart.js, Recharts ni ninguna librería de gráficas** — Épica 3 elegirá la librería

### Aprendizajes de historias anteriores aplicados

1. **`top-14` para el sticky de ficha:** el header de PublicLayout es `h-14 sticky top-0`. El header de la ficha debe ser `sticky top-14` para apilarse debajo, no encima.
2. **`encodeURIComponent` en navegación:** cualquier link a `/fibras/${ticker}` debe usar `encodeURIComponent(ticker)`.
3. **Null guard en items de API:** siempre usar `data?.field ?? defaultValue` — los campos nullable del contrato (`siteUrl`, `investorUrl`, `reportsUrl`) vienen como `null` desde C#.
4. **openapi-fetch 404:** usar `response.status === 404` para detectar not-found; no lanzar error en ese caso, retornar `null`.
5. **`noUnusedLocals`:** cada import y variable DEBE usarse. No dejar imports de secciones "para después".
6. **CPM obligatorio en backend:** si por algún motivo se toca el backend (no debería), NO añadir versiones en `.csproj`.
7. **`react-router` no `react-router-dom`:** en v7 el paquete es `react-router`.

### Verificación final antes de `review`

1. `npm run build --workspace=src/Web/Main` — exit code 0, 0 errores TypeScript, 0 warnings
2. Dev server en localhost:5173 — navegar a `/fibras/FUNO11` → ficha carga con todas las secciones (CA-1)
3. Navegar a `/fibras/FAKE99` → "FIBRA no encontrada" con enlace a Home (CA-4)
4. Simular red lenta (DevTools → Network → Slow 3G) → skeleton visible mientras carga (CA-5)
5. Revisar en 360px, 768px, 1280px — sin overflow horizontal (NFR-15)
6. Verificar sección Reportes con FUNO11 — si tiene URLs muestra links; si null muestra `—`

### Referencias

- Historia 2.1: [_bmad-output/implementation-artifacts/2-1-catalogo-maestro-de-fibras-con-datos-semilla-iniciales.md]
- Historia 2.2: [_bmad-output/implementation-artifacts/2-2-home-publica-con-busqueda-global-y-layout.md]
- FR-06: Ficha pública consolidada (precio, gráfica, fundamentales, distribuciones, noticias, reportes)
- FR-07: Período de origen y advertencia de antigüedad en fundamentales
- UX Spec S-05: Ficha pública — layout, anclas, sticky header — [_bmad-output/planning-artifacts/ux-design-specification.md#s-05]
- UX Spec PI-05: Estado de datos degradados — `—` para faltantes, nunca `0`
- NFR-15: Responsive en 360px/768px/1280px sin overflow horizontal
- `src/Server/Api/Endpoints/Public/CatalogEndpoints.cs` — endpoint GET /{ticker} ya implementado
- `src/Web/SharedApiClient/schema.d.ts` — tipos `FibraDetail`, `ProblemDetails`

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6 (create-story, 2026-05-18)

### Debug Log References

### Completion Notes List

- Implementada `fetchFibraByTicker` en `fibrasApi.ts` usando openapi-fetch con manejo correcto de 404 (retorna `null`) vs error real (lanza excepción).
- Creado `FibraNotFound.tsx` con mensaje claro y link de regreso a Home usando `react-router` v7.
- Creadas las 6 secciones en `modules/ficha-publica/sections/`: PrecioSection (placeholder MVP), MercadoSection (selectores + empty state), FundamentalesSection (preparada para Épica 5 con lógica de advertencia y `—` para nulos), DistribucionesSection, NoticiasSection, ReportesSection (único con datos reales: links o `—` si todos null).
- `FibraPage.tsx` orquesta todos los estados: skeleton (isLoading), error genérico (isError), 404 (fibra === null), y layout completo con header sticky en `top-14`.
- `router.tsx` actualizado; `FichaPlaceholder.tsx` eliminado.
- Build: `npm run build --workspace=src/Web/Main` → exit code 0, 0 errores TypeScript, 0 warnings.

### File List

- src/Web/Main/src/api/fibrasApi.ts (modificado)
- src/Web/Main/src/app/router.tsx (modificado)
- src/Web/Main/src/modules/ficha-publica/FichaPlaceholder.tsx (eliminado)
- src/Web/Main/src/modules/ficha-publica/FibraPage.tsx (nuevo)
- src/Web/Main/src/modules/ficha-publica/FibraNotFound.tsx (nuevo)
- src/Web/Main/src/modules/ficha-publica/sections/PrecioSection.tsx (nuevo)
- src/Web/Main/src/modules/ficha-publica/sections/MercadoSection.tsx (nuevo)
- src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx (nuevo)
- src/Web/Main/src/modules/ficha-publica/sections/DistribucionesSection.tsx (nuevo)
- src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx (nuevo)
- src/Web/Main/src/modules/ficha-publica/sections/ReportesSection.tsx (nuevo)

### Change Log

- 2026-05-18: Historia 2.3 implementada — Ficha Pública de FIBRA con 6 secciones, skeleton, manejo de 404, header sticky anclado. Build limpio. (claude-sonnet-4-6)
