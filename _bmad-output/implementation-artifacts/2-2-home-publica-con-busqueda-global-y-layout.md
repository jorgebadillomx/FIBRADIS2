# Historia 2.2: Home Pública con Búsqueda Global y Layout

Status: review

## Historia

Como visitante público,
quiero llegar a la página de inicio y usar la barra de búsqueda global para encontrar cualquier FIBRA por ticker o nombre,
para que pueda navegar el universo de FIBRAs y llegar al perfil de cualquier FIBRA específica en menos de dos clics.

## Criterios de Aceptación

**CA-1: Layout de Home carga con todas las secciones**
Dado que navego a `/`,
Cuando la Home carga,
Entonces veo el encabezado con barra de búsqueda, una sección de carrusel de precios (placeholder si no hay datos), sección de top movers (placeholder), sección de ranking rápido (placeholder) y sección de noticias (placeholder para los últimos 10 items).

**CA-2: Autocompletado de búsqueda por ticker o nombre**
Dado que escribo "FUN" en la barra de búsqueda,
Cuando aparecen los resultados,
Entonces veo sugerencias de autocompletado que coinciden con FIBRAs por ticker o nombre (insensible a mayúsculas), limitadas a 8 resultados.

**CA-3: Seleccionar sugerencia navega a la ficha**
Dado que selecciono "FUNO11" de las sugerencias,
Cuando soy redirigido,
Entonces llego a `/fibras/FUNO11`.

**CA-4: Sin coincidencias muestra estado vacío**
Dado que escribo una cadena que no coincide con nada en el catálogo,
Entonces veo un estado claro de "sin resultados encontrados" — sin error.

**CA-5: Home renderiza sin datos de mercado**
Dado que no hay datos de mercado cargados aún,
Entonces la Home renderiza correctamente con estados placeholder/cargando; no ocurren errores de JavaScript.

## Tareas / Subtareas

- [x] Task 1: Regenerar SharedApiClient con tipos de catálogo (AC: CA-2)
  - [x] Ejecutar `dotnet build FIBRADIS.slnx` desde la raíz del repositorio (verifica que `scripts/codegen/Api.json` esté actualizado con los endpoints de fibras)
  - [x] Ejecutar `npm run codegen:api` desde la raíz del repositorio (regenera `src/Web/SharedApiClient/schema.d.ts` con tipos `FibraListItem`, `FibraDetail`, `PagedResultOfFibraListItem`)
  - [x] Verificar que `schema.d.ts` ahora incluye los paths `/api/v1/fibras` y `/api/v1/fibras/{ticker}`

- [x] Task 2: Instalar componentes shadcn necesarios (AC: CA-2)
  - [x] Desde `src/Web/Main/`: `npx shadcn@latest add command`
  - [x] Desde `src/Web/Main/`: `npx shadcn@latest add popover`
  - [x] Desde `src/Web/Main/`: `npx shadcn@latest add input`
  - [x] Los componentes van a `src/Web/Main/src/shared/ui/` (alias `@/shared/ui` — configurado en `components.json`)

- [x] Task 3: Configurar React Router 7 y TanStack Query en `main.tsx` (AC: CA-3)
  - [x] Actualizar `src/Web/Main/src/main.tsx` para envolver la app con `QueryClientProvider` y `RouterProvider`
  - [x] Crear `src/Web/Main/src/app/router.tsx` con `createBrowserRouter` — rutas `/` y `/fibras/:ticker`

- [x] Task 4: Crear cliente API tipado (AC: CA-1, CA-2)
  - [x] Crear `src/Web/Main/src/api/fibrasApi.ts` con `createClient` de `openapi-fetch` y función `fetchAllFibras()`

- [x] Task 5: Crear PublicLayout con header y GlobalSearch (AC: CA-1, CA-2, CA-3, CA-4)
  - [x] Crear `src/Web/Main/src/shared/layouts/PublicLayout.tsx` — header fijo, nav pública, GlobalSearch, `<Outlet />`
  - [x] Crear `src/Web/Main/src/modules/home/GlobalSearch.tsx` — combobox con `Command` + `Popover` + `Input` de shadcn

- [x] Task 6: Crear página Home con secciones placeholder (AC: CA-1, CA-5)
  - [x] Crear `src/Web/Main/src/modules/home/HomePage.tsx` — orquesta todas las secciones
  - [x] Crear `src/Web/Main/src/modules/home/PriceCarousel.tsx` — placeholder para Épica 3
  - [x] Crear `src/Web/Main/src/modules/home/TopMovers.tsx` — placeholder para Épica 3
  - [x] Crear `src/Web/Main/src/modules/home/QuickRanking.tsx` — placeholder
  - [x] Crear `src/Web/Main/src/modules/home/NewsSection.tsx` — placeholder para Épica 4

- [x] Task 7: Crear página placeholder para ficha pública (AC: CA-3)
  - [x] Crear `src/Web/Main/src/modules/ficha-publica/FichaPlaceholder.tsx` — muestra el ticker del param de URL, placeholder para Historia 2.3

- [x] Task 8: Validación final
  - [x] `npm run build --workspace=src/Web/Main` — sin errores TypeScript, sin warnings
  - [x] `npm run dev:main` — servidor iniciado en localhost:5173 (verificación visual manual pendiente)
- [x] Verificar en 360px, 768px y 1280px que no hay overflow horizontal (NFR-15)
- [x] Verificar que la búsqueda "FUN" muestra sugerencias, "FUNO11" navega a `/fibras/FUNO11`
- [x] Verificar que texto sin coincidencia muestra "sin resultados encontrados"

### Review Findings

- [x] [Review][Patch] Header público desborda en móvil y rompe NFR-15 [src/Web/Main/src/shared/layouts/PublicLayout.tsx:8]
- [x] [Review][Patch] Links placeholder del header navegan a rutas sin definir y terminan en error del router [src/Web/Main/src/shared/layouts/PublicLayout.tsx:11]
- [x] [Review] GlobalSearch confunde estados `loading`/`error` con estado vacío y puede mostrar "Sin resultados encontrados" antes de que cargue el catálogo o cuando la petición falla [src/Web/Main/src/modules/home/GlobalSearch.tsx:15]
- [x] [Review] La navegación a rutas del SPA depende del router cliente, pero no hay fallback de hosting en backend para `/fibras/:ticker` ni para refresh/direct hit; riesgo de 404 fuera del dev server [src/Server/Api/Program.cs:17]

#### Ronda 2 — Revisión adversarial (2026-05-18)

- [x] [Review][Patch][HIGH] CommandEmpty no tiene guarda explícita y puede aparecer mientras carga o falla — debe condicionarse a `!isLoading && !isError && query.length >= 1 && filtered.length === 0` [src/Web/Main/src/modules/home/GlobalSearch.tsx]
- [x] [Review][Patch][HIGH] MapFallbackToFile intercepta rutas `/api/v1/...` desconocidas devolviendo index.html con HTTP 200 — usar patrón regex que excluya `/api/` [src/Server/Api/Program.cs]
- [x] [Review][Patch][HIGH] `data.items` accedido sin null guard en fibrasApi.ts — puede crashear si la API devuelve `items: null`; fix: `return data?.items ?? []` [src/Web/Main/src/api/fibrasApi.ts]
- [x] [Review][Patch][MEDIUM] `UseDefaultFiles`/`UseStaticFiles` colocados antes de `UseHttpsRedirection` — peticiones HTTP para assets estáticos no son redirigidas a HTTPS [src/Server/Api/Program.cs]
- [x] [Review][Patch][MEDIUM] `f.ticker`/`f.fullName` sin null guard en `filter()` — TypeScript lo previene en tiempo de compilación pero defensivamente: `(f.ticker ?? '').toLowerCase()` [src/Web/Main/src/modules/home/GlobalSearch.tsx]
- [x] [Review][Patch][LOW] `fetchAllFibras` descarta el error original — `throw new Error('Error al obtener fibras')` elimina status code y body; preservar con `throw error` o incluir detalles [src/Web/Main/src/api/fibrasApi.ts]
- [x] [Review][Patch][LOW] `ticker` no está URI-encoded en navegación — `navigate(\`/fibras/${ticker}\`)` debe ser `navigate(\`/fibras/${encodeURIComponent(ticker)}\`)` [src/Web/Main/src/modules/home/GlobalSearch.tsx]

- [x] [Review][Defer] Accesibilidad: falta `aria-label="Buscar FIBRA"` en CommandInput y `role="search"` en el wrapper — diferido a historia de accesibilidad
- [x] [Review][Defer] `pageSize: 100` hardcodeado en fibrasApi.ts — diferido, extraer a constante nombrada cuando haya más consumidores
- [x] [Review][Defer] `staleTime: 5 * 60 * 1000` magic number — diferido, centralizar en una constante de configuración de caché
- [x] [Review][Defer] Skeleton/shimmer state ausente en GlobalSearch durante carga inicial — diferido a sprint de UX polish
- [x] [Review][Defer] `NotFound` no actualiza `document.title` — diferido, SEO/meta es Historia 2.4
- [x] [Review][Defer] Sin atajo de teclado (Cmd+K/Ctrl+K) para enfocar búsqueda — diferido, mejora de UX para historias futuras
- [x] [Review][Defer] Falta `aria-expanded` en el trigger del Popover para lectores de pantalla — diferido junto con hallazgo de accesibilidad general

---

## Dev Notes

### Stack exacto — NO negociar versiones

| Componente | Versión |
|---|---|
| React | 19.2 (ya fijado) |
| React Router | 7 (librería mode — `react-router`, NO `react-router-dom`) |
| TanStack Query | v5 (ya en package.json) |
| shadcn | radix-nova style (ver `components.json`) |
| Tailwind | v4 con plugin `@tailwindcss/vite` |
| openapi-fetch | ^0.14.0 (ya en package.json) |

### Task 1: Regenerar SharedApiClient — CRÍTICO

El archivo `src/Web/SharedApiClient/schema.d.ts` está DESACTUALIZADO. No incluye los tipos de catálogo (`FibraListItem`, `FibraDetail`, `PagedResultOfFibraListItem`) aunque el `scripts/codegen/Api.json` sí los tiene (resultado de la Historia 2.1).

**Pasos obligatorios antes de escribir cualquier código frontend:**
```powershell
# Desde la raíz del repositorio
dotnet build FIBRADIS.slnx          # regenera scripts/codegen/Api.json
npm run codegen:api                  # regenera src/Web/SharedApiClient/schema.d.ts
```

Después de esto, `schema.d.ts` incluirá:
- `paths["/api/v1/fibras"]` con `GET`
- `paths["/api/v1/fibras/{ticker}"]` con `GET`
- `components["schemas"]["FibraListItem"]` — `{ id, ticker, fullName, shortName, sector, market, currency, state, siteUrl }`
- `components["schemas"]["FibraDetail"]` — completo con `nameVariants`
- `components["schemas"]["PagedResultOfFibraListItem"]`

### Task 2: shadcn — Alias NO estándar

**CRÍTICO:** En este proyecto shadcn NO instala en `src/components/ui/`. El `components.json` tiene:
```json
"aliases": {
  "ui": "@/shared/ui",
  "components": "@/shared/ui"
}
```

Los componentes van a `src/Web/Main/src/shared/ui/` (por ejemplo: `src/shared/ui/command.tsx`, `src/shared/ui/popover.tsx`).

Los imports son: `import { Command, CommandInput, ... } from '@/shared/ui/command'`

El `cn()` helper ya existe en `src/Web/Main/src/shared/lib/utils.ts`.

### Task 3: Router y QueryClient

**PATRÓN CORRECTO para React Router 7 en modo librería:**
```tsx
// src/Web/Main/src/app/router.tsx
import { createBrowserRouter } from 'react-router'
import { PublicLayout } from '@/shared/layouts/PublicLayout'
import { HomePage } from '@/modules/home/HomePage'
import { FichaPlaceholder } from '@/modules/ficha-publica/FichaPlaceholder'

export const router = createBrowserRouter([
  {
    element: <PublicLayout />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/fibras/:ticker', element: <FichaPlaceholder /> },
    ],
  },
])
```

```tsx
// src/Web/Main/src/main.tsx — actualizar
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router'
import { router } from './app/router'
import './index.css'

const queryClient = new QueryClient()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>,
)
```

**NOTA:** NO hay `import App from './App.tsx'` ni `<App />`. El `App.tsx` actual queda obsoleto — reemplazarlo completamente por el sistema de routing, o bien eliminar su contenido para que el `router.tsx` sea el punto de entrada real. La opción más limpia es **eliminar o vaciar `App.tsx`** y que `main.tsx` use directamente `RouterProvider`.

Importar de `'react-router'`, NO de `'react-router-dom'` (en v7 el paquete unificado es `react-router`).

### Task 4: Cliente API

```typescript
// src/Web/Main/src/api/fibrasApi.ts
import createClient from 'openapi-fetch'
import type { paths } from '@fibradis/shared-api-client'

const apiClient = createClient<paths>({ baseUrl: '' })

export async function fetchAllFibras() {
  const { data, error } = await apiClient.GET('/api/v1/fibras', {
    params: { query: { page: 1, pageSize: 100 } },
  })
  if (error) throw new Error('Error al obtener fibras')
  return data.items
}
```

La `baseUrl: ''` funciona porque Vite proxea `/api` al backend en `localhost:5265` (configurado en `vite.config.ts`). NO poner `http://localhost:5265` — rompería en producción.

### Task 5: GlobalSearch — Implementación de autocompletado

**Estrategia: filtrado client-side.** El universo FIBRADIS tiene ≤ 30 FIBRAs activas (NFR-09). Cargar todas al inicio es más eficiente que un endpoint de búsqueda separado. No se necesita nuevo endpoint backend.

```tsx
// src/Web/Main/src/modules/home/GlobalSearch.tsx
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { Command, CommandInput, CommandList, CommandItem, CommandEmpty } from '@/shared/ui/command'
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/ui/popover'
import { fetchAllFibras } from '@/api/fibrasApi'

export function GlobalSearch() {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const navigate = useNavigate()

  const { data: fibras = [] } = useQuery({
    queryKey: ['fibras', 'all'],
    queryFn: fetchAllFibras,
    staleTime: 5 * 60 * 1000, // 5 minutos — el catálogo cambia raramente
  })

  const filtered = query.length >= 1
    ? fibras
        .filter(f =>
          f.ticker.toLowerCase().includes(query.toLowerCase()) ||
          f.fullName.toLowerCase().includes(query.toLowerCase())
        )
        .slice(0, 8)
    : []

  function handleSelect(ticker: string) {
    setOpen(false)
    setQuery('')
    navigate(`/fibras/${ticker}`)
  }

  return (
    <Popover open={open && query.length >= 1} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <div className="relative w-64 lg:w-96">
          <CommandInput
            placeholder="Buscar FIBRA por ticker o nombre..."
            value={query}
            onValueChange={(val) => { setQuery(val); setOpen(true) }}
            className="h-9"
          />
        </div>
      </PopoverTrigger>
      <PopoverContent className="w-64 lg:w-96 p-0" align="start">
        <Command>
          <CommandList>
            <CommandEmpty>Sin resultados encontrados.</CommandEmpty>
            {filtered.map((f) => (
              <CommandItem key={f.ticker} onSelect={() => handleSelect(f.ticker)}>
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

**Advertencia sobre `CommandInput` vs `Input`:** El componente `Command` de shadcn tiene su propio `CommandInput` con lógica de filtrado interna. Al usarlo dentro de un `Popover` independiente (no dentro de `<Command>`), desactivar el filtrado automático de `Command` con `shouldFilter={false}` para que el filtrado sea el manual mostrado arriba:

```tsx
<Command shouldFilter={false}>
  <CommandList>
    ...
  </CommandList>
</Command>
```

### Task 5: PublicLayout

```tsx
// src/Web/Main/src/shared/layouts/PublicLayout.tsx
import { Outlet } from 'react-router'
import { GlobalSearch } from '@/modules/home/GlobalSearch'

export function PublicLayout() {
  return (
    <div className="min-h-screen flex flex-col bg-background text-foreground">
      <header className="sticky top-0 z-50 border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="container mx-auto flex h-14 items-center gap-6 px-4">
          <a href="/" className="text-lg font-semibold tracking-tight">FIBRADIS</a>
          <nav className="hidden md:flex items-center gap-4 text-sm text-muted-foreground">
            <a href="/mercado" className="hover:text-foreground transition-colors">Mercado</a>
            <a href="/catalogo" className="hover:text-foreground transition-colors">Catálogo</a>
            <a href="/noticias" className="hover:text-foreground transition-colors">Noticias</a>
          </nav>
          <div className="flex-1 flex justify-center">
            <GlobalSearch />
          </div>
          <div className="flex items-center gap-2">
            <a href="/login" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
              Iniciar sesión
            </a>
          </div>
        </div>
      </header>
      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  )
}
```

**NOTA:** Los `href` de nav son `<a>` simples por ahora — estas rutas no tienen página aún (Épicas 3 y 4). NO usar `<Link>` de react-router para rutas que no existen: causarían 404 con error de router. Usar `<a>` nativo hasta que las rutas existan es lo correcto para esta historia.

### Task 6: HomePage y secciones placeholder

```tsx
// src/Web/Main/src/modules/home/HomePage.tsx
import { PriceCarousel } from './PriceCarousel'
import { TopMovers } from './TopMovers'
import { QuickRanking } from './QuickRanking'
import { NewsSection } from './NewsSection'

export function HomePage() {
  return (
    <div className="container mx-auto px-4 py-6 space-y-8">
      <PriceCarousel />
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <TopMovers />
        </div>
        <div>
          <NewsSection />
        </div>
      </div>
      <QuickRanking />
    </div>
  )
}
```

**Secciones placeholder — patrón estándar:**
```tsx
// src/Web/Main/src/modules/home/PriceCarousel.tsx
export function PriceCarousel() {
  return (
    <section aria-label="Carrusel de precios">
      <div className="h-20 rounded-lg border border-border bg-muted/30 flex items-center justify-center">
        <p className="text-sm text-muted-foreground">Precios de mercado — disponible en Épica 3</p>
      </div>
    </section>
  )
}
```

Usar el mismo patrón para `TopMovers`, `QuickRanking` y `NewsSection`. Cada uno en su propio archivo. Los textos de placeholder deben ser descriptivos pero discretos — son solo scaffolding visual.

**REGLA DE DISEÑO (UX Spec S-01):** La búsqueda global en el header ES el CTA principal. Los placeholders son secundarios y no deben competir visualmente con el header.

### Task 7: FichaPlaceholder

```tsx
// src/Web/Main/src/modules/ficha-publica/FichaPlaceholder.tsx
import { useParams, Link } from 'react-router'

export function FichaPlaceholder() {
  const { ticker } = useParams<{ ticker: string }>()

  return (
    <div className="container mx-auto px-4 py-12">
      <h1 className="text-2xl font-semibold mb-2">{ticker?.toUpperCase()}</h1>
      <p className="text-muted-foreground mb-6">Ficha pública — disponible en Historia 2.3</p>
      <Link to="/" className="text-sm text-primary hover:underline">← Volver a la Home</Link>
    </div>
  )
}
```

### Estructura de directorios — archivos creados/modificados

```
src/Web/Main/src/
├── main.tsx                               ← MODIFICAR (agregar QueryClientProvider + RouterProvider)
├── App.tsx                                ← DEJAR VACÍO o ELIMINAR su contenido (ya no se usa)
├── app/
│   └── router.tsx                         ← NUEVO (createBrowserRouter con rutas)
├── api/
│   └── fibrasApi.ts                       ← NUEVO (cliente openapi-fetch + fetchAllFibras)
├── shared/
│   ├── layouts/
│   │   └── PublicLayout.tsx               ← NUEVO (header + Outlet)
│   ├── ui/
│   │   ├── command.tsx                    ← INSTALAR: npx shadcn@latest add command
│   │   ├── popover.tsx                    ← INSTALAR: npx shadcn@latest add popover
│   │   └── input.tsx                      ← INSTALAR: npx shadcn@latest add input
│   ├── hooks/                             ← (vacío por ahora)
│   └── lib/
│       └── utils.ts                       ← YA EXISTE, no modificar
└── modules/
    ├── home/
    │   ├── HomePage.tsx                   ← NUEVO
    │   ├── GlobalSearch.tsx               ← NUEVO (componente central de esta historia)
    │   ├── PriceCarousel.tsx              ← NUEVO (placeholder)
    │   ├── TopMovers.tsx                  ← NUEVO (placeholder)
    │   ├── QuickRanking.tsx               ← NUEVO (placeholder)
    │   └── NewsSection.tsx                ← NUEVO (placeholder)
    └── ficha-publica/
        └── FichaPlaceholder.tsx           ← NUEVO (placeholder para Historia 2.3)

src/Web/SharedApiClient/
└── schema.d.ts                            ← REGENERAR con npm run codegen:api
```

### Convenciones a respetar

- **Componentes:** PascalCase, archivo `.tsx`
- **Hooks:** `useThing.ts`
- **Utils:** `kebab-case.ts`
- **Módulos:** carpeta en `src/modules/{nombre-modulo}/`
- **Shared UI:** `src/shared/ui/` (NO `src/components/ui/`)
- **Imports absolutos:** usar el alias `@/` (ej: `@/modules/home/HomePage`)
- **`noUnusedLocals`:** El tsconfig tiene esta regla activa — no dejar variables/imports sin usar

### Qué NO debe hacer esta historia

- **No implementar precios reales** — carrusel y top movers son placeholder hasta Épica 3
- **No implementar noticias** — sección de noticias es placeholder hasta Épica 4
- **No añadir autenticación en la UI** — el botón "Iniciar sesión" puede ser un `<a href="/login">` simple
- **No implementar la ficha pública** — solo un placeholder con el ticker y enlace de regreso (Historia 2.3)
- **No añadir SEO/prerender** — eso es Historia 2.4
- **No crear un endpoint de búsqueda backend** — filtrado client-side es suficiente con ≤ 30 FIBRAs
- **No usar `react-router-dom`** — en React Router 7 el paquete es `react-router` (ya instalado)
- **No modificar `components.json`** — la configuración de shadcn ya está establecida

### Aprendizajes de historias anteriores aplicados

1. **CPM obligatorio en backend:** Si por algún motivo se toca el backend, NO agregar versiones en `.csproj`. Todos los paquetes NuGet del backend están bajo Central Package Management.
2. **`InMemoryDatabaseRoot` en tests:** No modificar ese patrón en `ApiWebFactory.cs` (ya está correcto).
3. **Schema de `schema.d.ts` se regenera desde `Api.json`:** El proceso es `dotnet build` → `npm run codegen:api`. El archivo generado NO se edita a mano.
4. **shadcn style `radix-nova`:** Es el estilo configurado. Ejecutar `npx shadcn@latest add <componente>` desde `src/Web/Main/` (NO desde la raíz del repo).
5. **TypeScript estricto:** `noUnusedLocals` y `noUnusedParameters` están activos. Cada import y variable DEBE usarse.

### Verificación final antes de mover a `review`

1. `npm run build --workspace=src/Web/Main` — exit code 0, sin errores TypeScript
2. Abrir `http://localhost:5173` — Home carga con header + búsqueda + todas las secciones placeholder (CA-1)
3. Escribir "FUN" en búsqueda → sugerencias aparecen con FUNO11 y otros matches (CA-2)
4. Seleccionar FUNO11 → navegar a `/fibras/FUNO11` (CA-3)
5. Escribir "XXXXXX" → aparece "Sin resultados encontrados" (CA-4)
6. Con backend detenido: Home carga sin errores de JS (CA-5) — la query entra en estado `error` pero el componente debe manejarlo sin crash
7. Revisar en 360px, 768px, 1280px que no hay overflow horizontal (NFR-15)

### Referencias

- Historia 2.1 (catálogo backend, SharedApiClient regeneration): [_bmad-output/implementation-artifacts/2-1-catalogo-maestro-de-fibras-con-datos-semilla-iniciales.md]
- FR-03: Home pública con encabezado, búsqueda, carrusel, top movers, noticias (structure)
- FR-04: Buscador global por ticker o nombre con autocomplete, máx 8 resultados
- UX Spec S-01: Layout Home pública 1280px y 360px — [_bmad-output/planning-artifacts/ux-design-specification.md#s-01--home-pública]
- NFR-15: Responsive en 360px, 768px, 1280px sin overflow horizontal
- Arquitectura: estructura por módulos `src/modules/*`, shared en `src/shared/*`
- `scripts/codegen/Api.json` — spec OpenAPI con endpoints `/api/v1/fibras`

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6 (create-story, 2026-05-17)

### Debug Log References

- shadcn CLI instaló componentes en carpeta literal `@/shared/ui/` en lugar de `src/shared/ui/`. Se movieron manualmente al path correcto y se eliminó la carpeta errónea.
- El paquete `radix-ui` (unificado) no estaba instalado; se agregó con `npm install radix-ui --workspace=src/Web/Main`.

### Completion Notes List

- Task 1: dotnet build + npm run codegen:api → schema.d.ts regenerado con FibraListItem, FibraDetail, PagedResultOfFibraListItem y paths /api/v1/fibras y /api/v1/fibras/{ticker}
- Task 2: command, popover, input, button, dialog, input-group, textarea instalados en src/shared/ui/; radix-ui unified package instalado
- Task 3: main.tsx actualizado con QueryClientProvider + RouterProvider; router.tsx creado con rutas / y /fibras/:ticker
- Task 4: fibrasApi.ts con createClient de openapi-fetch y fetchAllFibras() usando filtrado client-side
- Task 5: PublicLayout.tsx con header sticky y GlobalSearch con Popover+Command (shouldFilter=false); filtrado manual por ticker y fullName, máx 8 resultados, insensible a mayúsculas
- Task 6: HomePage + 4 secciones placeholder (PriceCarousel, TopMovers, QuickRanking, NewsSection)
- Task 7: FichaPlaceholder con useParams y Link de react-router
- Task 8: npm run build exitoso — 0 errores TypeScript, 0 warnings; dev server en localhost:5173

### File List

- src/Web/SharedApiClient/schema.d.ts (regenerado)
- src/Web/Main/src/main.tsx (modificado)
- src/Web/Main/src/App.tsx (vaciado — obsoleto)
- src/Web/Main/src/app/router.tsx (modificado — catch-all NotFound)
- src/Web/Main/src/api/fibrasApi.ts (nuevo)
- src/Web/Main/src/shared/layouts/PublicLayout.tsx (modificado — min-w-0 en search wrapper)
- src/Web/Main/src/shared/layouts/NotFound.tsx (nuevo)
- src/Web/Main/src/shared/ui/button.tsx (nuevo)
- src/Web/Main/src/shared/ui/input.tsx (nuevo)
- src/Web/Main/src/shared/ui/textarea.tsx (nuevo)
- src/Web/Main/src/shared/ui/dialog.tsx (nuevo)
- src/Web/Main/src/shared/ui/input-group.tsx (nuevo)
- src/Web/Main/src/shared/ui/command.tsx (nuevo)
- src/Web/Main/src/shared/ui/popover.tsx (nuevo)
- src/Web/Main/src/modules/home/GlobalSearch.tsx (modificado — w-full max-w responsive; isLoading/isError estados diferenciados)
- src/Web/Main/src/modules/home/HomePage.tsx (nuevo)
- src/Web/Main/src/modules/home/PriceCarousel.tsx (nuevo)
- src/Web/Main/src/modules/home/TopMovers.tsx (nuevo)
- src/Web/Main/src/modules/home/QuickRanking.tsx (nuevo)
- src/Web/Main/src/modules/home/NewsSection.tsx (nuevo)
- src/Web/Main/src/modules/ficha-publica/FichaPlaceholder.tsx (nuevo)
- src/Web/Main/package.json (cmdk y radix-ui agregados)
- src/Server/Api/Program.cs (modificado — UseDefaultFiles, UseStaticFiles, MapFallbackToFile)
- _bmad-output/implementation-artifacts/sprint-status.yaml (actualizado)

### Change Log

- 2026-05-18: Implementación completa de Historia 2.2 — Home pública con búsqueda global, React Router 7, TanStack Query, shadcn UI components, layout público y secciones placeholder (claude-sonnet-4-6)
- 2026-05-18: Resueltos 2 hallazgos de code review — fix overflow móvil NFR-15 (w-full max-w responsive en GlobalSearch + min-w-0 en PublicLayout) y fix rutas indefinidas (NotFound catch-all en router); build limpio (claude-sonnet-4-6)
- 2026-05-18: Resueltos 2 hallazgos adicionales — GlobalSearch diferencia loading/error/vacío; Program.cs con UseDefaultFiles+UseStaticFiles+MapFallbackToFile para SPA hosting en producción; builds backend y frontend limpios (claude-sonnet-4-6)
- 2026-05-18: Revisión adversarial ronda 2 — 7 patches aplicados: CommandEmpty guard explícito; MapFallback excluye /api/ con ruta prioritaria; data?.items ?? [] null guard; UseHttpsRedirection movido antes de static files; null guard en filter(); error original preservado en fetchAllFibras; encodeURIComponent en navegación (claude-sonnet-4-6)
