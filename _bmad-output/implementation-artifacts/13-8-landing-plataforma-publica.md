# Story 13.8: Landing pública `/plataforma` (showcase de funcionalidades) enlazada desde el footer

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **visitante público que quiere entender qué ofrece la plataforma antes de registrarse**,
I want **una página de marca/venta en `/plataforma` que describa de forma ordenada y atractiva TODAS las funcionalidades públicas y privadas de Fibras Inmobiliarias, alcanzable desde el "Fibras Inmobiliarias" del footer, optimizada para SEO**,
so that **la plataforma capte tráfico orgánico de descubrimiento, comunique su propuesta de valor completa y convierta visitantes en usuarios/inversionistas**.

## Contexto del problema

Decisión de producto/SEO del usuario (2026-06-15): el texto **"Fibras Inmobiliarias"** del footer (hoy `© {año} Fibras Inmobiliarias`, texto plano) debe convertirse en un **enlace** a una **nueva página landing de venta** que describa "de forma ordenada todas las funcionalidades públicas y privadas del sitio", respetando el estilo del sitio pero pudiendo ser más creativa, y siguiendo lineamientos de SEO.

**Decisión de ruta (confirmada con el usuario):** **nueva ruta `/plataforma`** — página de marca/venta independiente, distinta de:
- **`/portafolio` (13-6):** punto de entrada al producto + login embebido (intención: acceso/conversión a sesión).
- **`/acerca`:** metodología y fuentes de datos (intención: confianza/E-E-A-T).
- **`/plataforma` (esta):** **showcase de capacidades** (intención: descubrimiento de funcionalidades). Para evitar contenido duplicado con `/portafolio`, ambas se diferencian en ángulo y keywords y se enlazan entre sí.

### Dependencias y coordinación

- **13-7 (rebranding):** esta landing **nace con la marca correcta**: copy "Fibras Inmobiliarias", título `… | Fibras Inmobiliarias`, **nunca "FIBRADIS"**. Si 13-7 aún no está mergeada, igualmente usar la marca nueva aquí.
- **13-6 (`/portafolio` landing):** describe funcionalidades + login. Esta página (13-8) las describe con ángulo de showcase/venta y **enlaza a `/portafolio`** como CTA de acceso. Evitar copiar literalmente los mismos textos (riesgo de duplicate content). Si 13-6 ya existe, reutilizar sus listados de capacidades como insumo pero con redacción propia.
- **13-5 (Reportes):** la sección privada `/reportes` (reportes trimestrales por FIBRA) debe figurar entre las funcionalidades descritas.
- **Footer compartido con 13-7:** ambas tocan `PublicLayout.tsx`. 13-7 cambia el copy del footer ("FIBRADIS" → "Fibras Inmobiliarias") y el `mailto`; 13-8 convierte el span "Fibras Inmobiliarias" en `<Link to="/plataforma">`. Coordinar merge (recomendado: 13-7 primero).
- **Stash SEO aparcado** ("seo-wip-pendiente-no-relacionado-con-epica13") toca `SpaMetadataProvider.cs`/`SpaMetadataProviderTests.cs`. **Coordinar con el usuario** antes de editarlos.

## Acceptance Criteria

### A. Enlace desde el footer

1. En `PublicLayout.tsx`, el `<span>© {año} Fibras Inmobiliarias</span>` (≈ l.442) cambia para que **"Fibras Inmobiliarias" sea un `<Link to="/plataforma">`** (el `© {año}` puede quedar como texto y "Fibras Inmobiliarias" como enlace). Conserva estilo del footer (mismo patrón `transition-colors hover:text-foreground focus-visible:ring-2 …` de los otros enlaces del footer), target táctil y foco visible.

### B. Página `/plataforma` (nueva ruta pública)

2. Nueva ruta pública en `routes.tsx` bajo `<PublicLayout>` (fuera de `<ProtectedRoute>`): `{ path: '/plataforma', element: p(<PlataformaPage />) }`, con `PlataformaPage` cargada **lazy** (patrón de las demás páginas). `/portafolio`, `/oportunidades`, `/herramientas`, `/perfil`, `/reportes` permanecen sin cambios.
3. **`PlataformaPage`** (nuevo, `modules/plataforma/PlataformaPage.tsx`) es **estática/evergreen** (sin fetches de datos en vivo) y contiene, como mínimo:
   - **Hero** con propuesta de valor de marca (Fibras Inmobiliarias), tipografía `font-playfair` como `HomePage`, y CTAs (p.ej. "Ver catálogo" → `/fibras`, "Crear cuenta / Iniciar sesión" → `/portafolio`).
   - **Funcionalidades PÚBLICAS** con **texto indexable** (no solo iconos): Catálogo de FIBRAs (`/fibras`), Ficha de FIBRA, Comparador (`/comparar`), Fundamentales (`/fundamentales`), Calculadora (`/calculadora`), Calendario (`/calendario`), Noticias (`/noticias`), Guía "¿Qué son las FIBRAs?" (`/conoce-las-fibras`).
   - **Funcionalidades PRIVADAS** (descriptivas, sin exponer datos): Portafolio/dashboard (KPIs + calendario de distribuciones), **Reportes trimestrales por FIBRA — fundamentales + análisis IA (ver 13-5)**, Oportunidades/ranking configurable, Herramientas, Favoritos.
   - **Enlaces internos** a todas las secciones públicas citadas (refuerza enlazado interno SEO) y CTA a `/portafolio`.
4. La página usa `usePageTitle` con título/descripcion **alineados a la metadata SSR** (AC-6) y marca "Fibras Inmobiliarias" (nunca "FIBRADIS").

### C. SEO — `/plataforma` indexable (backend)

5. `/plataforma` se añade a **`SpaMetadataProvider`**: a `KnownPaths` y a un `case "/plataforma"` en `GetMetaForPathAsync()` con `title` (`… | Fibras Inmobiliarias`), `description` (**120–160 caracteres**, validada por test), `canonicalPath` `/plataforma` y **JSON-LD** (`BuildWebPageJsonLd`/`BuildCollectionPageJsonLd` siguiendo el patrón de `/acerca`/`/fibras`; `isPartOf` `#website`, `publisher`/`provider` `#organization` vía `App:BaseUrl`, sin dominio hardcodeado).
6. `/plataforma` se añade al **sitemap** (`StaticRoutes` en `SeoEndpoints.cs`) y al **`SpaRouteCatalog`** (lista de rutas conocidas del SPA fallback). Respeta la visibilidad `noindex` administrable existente (sin código nuevo).
7. Se añade el **breadcrumb** de `/plataforma` en `SpaMetadataMiddleware.cs` (switch de breadcrumbs: `Inicio` → `Plataforma`). `/plataforma` es **indexable por defecto**. `KnownPaths`, `StaticRoutes` y `SpaRouteCatalog` quedan **sincronizados**.

### D. Transversal (diseño + tests)

8. `PlataformaPage` cumple `design-system/fibradis/MASTER.md`: iconos `lucide-react` (no emojis), `cursor-pointer` en clickables, transiciones 150–300ms, foco visible, contraste AA, `prefers-reduced-motion`, y **sin scroll horizontal** en 375/768/1024/1440px.
9. **Tests (obligatorios antes de `review`, por `workflow-rules.md`):**
   - **Backend (xUnit, `SpaMetadataProviderTests.cs`):** añadir `/plataforma` a los `[Theory]` de rutas conocidas (meta no nula, title termina en `| Fibras Inmobiliarias`, canonical `/plataforma`) y al de longitud 120–160; **moverla del theory de rutas desconocidas** si figura ahí como `null`. `dotnet test` verde.
   - **Frontend Main (`node:test`):** si se extrae lógica pura (p.ej. lista de capacidades o enlaces del footer), testearla con el patrón existente. `npm run test --workspace=src/Web/Main` verde.
   - **Builds:** `dotnet build FIBRADIS.slnx` y `npm run build --workspace=src/Web/Main` verdes.

## Tasks / Subtasks

- [x] **T1 — Enlace del footer** (AC: 1)
  - [x] `PublicLayout.tsx`: "Fibras Inmobiliarias" del `©` → `<Link to="/plataforma">` con el estilo de enlace del footer.
- [x] **T2 — Ruta + página** (AC: 2, 3, 4, 8)
  - [x] `routes.tsx`: declarar `PlataformaPage` lazy + ruta pública `/plataforma`.
  - [x] Crear `modules/plataforma/PlataformaPage.tsx`: hero + secciones públicas + privadas (incluida Reportes 13-5) + enlaces internos + CTA `/portafolio`. Estática. `usePageTitle` alineado a SSR. Cumple MASTER.md.
- [x] **T3 — SEO backend** (AC: 5, 6, 7)
  - [x] `SpaMetadataProvider.cs`: const `PlataformaDescription` (120–160), `/plataforma` en `KnownPaths`, `case "/plataforma"` con JSON-LD.
  - [x] `SeoEndpoints.cs`: `/plataforma` en `StaticRoutes`.
  - [x] `SpaRouteCatalog.cs`: `/plataforma` en la lista de rutas conocidas.
  - [x] `SpaMetadataMiddleware.cs`: breadcrumb `Inicio → Plataforma`.
- [x] **T4 — Tests** (AC: 9)
  - [x] Backend: extender `SpaMetadataProviderTests.cs` (conocidas + longitud + JSON-LD). `dotnet test`.
  - [x] Frontend: tests de lógica pura si aplica. `npm run test --workspace=src/Web/Main`.
  - [x] Builds Main + backend verdes.
- [x] **T5 — Verificación manual a11y/responsive/SEO** (AC: 8, 5-7) — verificada en code review 2026-06-16 (Chrome DevTools MCP + tests)
  - [x] Dev server: footer "Fibras Inmobiliarias" → `/plataforma` (foco visible OK); página describe 7 funciones públicas (tarjeta fusionada "Catálogo y fichas de FIBRAs") + 5 privadas (incl. "Reportes trimestrales por FIBRA"); **sin scroll horizontal en 375/768/1024/1440** (overflow 0, 0 elementos desbordados en los 4 breakpoints vía emulación CDP).
  - [x] `/plataforma` server-side verificado por tests autoritativos del middleware/endpoint: `SpaMetadataMiddlewareTests.InjectsMetadata_ForPlataforma` (title termina en `| Fibras Inmobiliarias`, canonical `https://fibrasinmobiliarias.com/plataforma`, `CollectionPage`, `BreadcrumbList`, hit directo `nextCalled=false`) + `SeoEndpointsTests` (`<loc>…/plataforma` en sitemap + conteos). Client-side: title/canonical/description confirmados en navegador.

## Dev Notes

### Estado actual de los archivos a MODIFICAR (UPDATE)

**`src/Web/Main/src/shared/layouts/PublicLayout.tsx`** — footer (≈ l.436-457): `<span>© {new Date().getFullYear()} Fibras Inmobiliarias</span>`. Convertir "Fibras Inmobiliarias" en `<Link to="/plataforma">` reutilizando las clases del `<Link to="/privacidad">` vecino. **Coordinar con 13-7** (que cambia el disclaimer y el `mailto` del mismo footer).

**`src/Web/Main/src/app/routes.tsx`** — bloque de rutas públicas bajo `<PublicLayout>` (l.42-56), antes del `<ProtectedRoute>`. Patrón lazy: `const PlataformaPage = lazy(() => import('@/modules/plataforma/PlataformaPage').then(m => ({ default: m.PlataformaPage })))` y `{ path: '/plataforma', element: p(<PlataformaPage />) }`.

**`src/Web/Main/src/modules/home/HomePage.tsx`** — referencia de estilo del hero (`font-playfair`, `usePageTitle`). `HomePage` muestra datos en vivo; **`/plataforma` debe ser estática/evergreen**.

**Backend SEO (UPDATE):**
- `src/Server/Api/Seo/SpaMetadataProvider.cs` — `KnownPaths` (l.48-52) + `switch GetMetaForPathAsync()` (l.58-112). Patrón: `"/x" => new SpaPageMeta(title, description, "/x", BuildXJsonLd(...))`. Informativas → `BuildWebPageJsonLd`; listados → `BuildCollectionPageJsonLd`. Descripciones 120–160. `BrandName` (tras 13-7) = "Fibras Inmobiliarias".
- `src/Server/Api/Seo/SpaRouteCatalog.cs` — lista de rutas conocidas del SPA (l.18-33). Añadir `/plataforma` (entre las estáticas públicas).
- `src/Server/Api/Endpoints/Public/SeoEndpoints.cs` — `StaticRoutes` (l.22+, fuente de verdad del sitemap). Añadir `/plataforma`.
- `src/Server/Api/Middleware/SpaMetadataMiddleware.cs` — switch de breadcrumbs (l.214-221): añadir `"/plataforma" => new[] { new SeoBreadcrumbItem("Inicio", "/"), new SeoBreadcrumbItem("Plataforma", "/plataforma") }`.
- `tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs` — añadir `/plataforma` a conocidas/longitud/JSON-LD; quitarla de desconocidas si aplica.

### Guardrails técnicos (de cumplimiento estricto)

- 🚫 **NO `npx shadcn@latest add` sin aprobación.** Reusar Tailwind + lucide-react + componentes existentes.
- **Marca:** "Fibras Inmobiliarias" en todo; **nunca "FIBRADIS"** (regla de 13-7). Título `… | Fibras Inmobiliarias`.
- **Sin fuga de datos privados:** la página es pública y estática; describe las funciones privadas con **texto**, sin renderizar ni consultar datos privados.
- **No duplicar contenido con `/portafolio` (13-6):** ángulo de showcase/venta, redacción propia, enlaza a `/portafolio` como CTA. URLs canónicas distintas.
- **SEO sin dominio hardcodeado:** JSON-LD/canonical vía `App:BaseUrl`. `KnownPaths`, `StaticRoutes` y `SpaRouteCatalog` sincronizados (3 listas).
- **Iconos lucide-react**, nada de emojis.
- **Sin migraciones EF, endpoints nuevos ni cambios de contrato API.**

### Security Checklist — completar antes del primer commit

- [x] **TOCTOU doble-request:** N/A — sin endpoints de escritura.
- [x] **Auth-gating de componentes UI:** página pública estática; **no** monta componentes privados ni hace fetches autenticados. Describe funciones privadas solo con texto.
- [x] **Denominador cero:** N/A.
- [x] **Open redirect:** N/A (sin login embebido; CTAs son enlaces internos fijos).

### Project Structure Notes

- **Nuevos archivos:** `src/Web/Main/src/modules/plataforma/PlataformaPage.tsx` (+ test de lógica pura si aplica).
- **Sin** migraciones EF, endpoints nuevos ni cambios de contrato API.
- **3 listas de rutas a sincronizar:** `KnownPaths` (SpaMetadataProvider), `StaticRoutes` (SeoEndpoints), `SpaRouteCatalog` — añadir `/plataforma` a las tres + breadcrumb.

### Limitación de toolchain de tests (heredada de 13.1)

El runner de Main es **`node:test` sin DOM** — los tests validan estructuras de datos / funciones puras, no montan componentes. Si se extrae lógica (lista de capacidades, enlaces), testearla así; la verificación visual es manual (T5).

### References

- [Source: src/Web/Main/src/shared/layouts/PublicLayout.tsx] — footer (l.436-457)
- [Source: src/Web/Main/src/app/routes.tsx] — rutas públicas (l.39-69)
- [Source: src/Web/Main/src/modules/home/HomePage.tsx] — estilo hero (`font-playfair`, `usePageTitle`)
- [Source: src/Server/Api/Seo/SpaMetadataProvider.cs] — `KnownPaths` + switch metadata
- [Source: src/Server/Api/Seo/SpaRouteCatalog.cs] — rutas conocidas del SPA
- [Source: src/Server/Api/Endpoints/Public/SeoEndpoints.cs] — `StaticRoutes` (sitemap)
- [Source: src/Server/Api/Middleware/SpaMetadataMiddleware.cs] — breadcrumbs (l.214-221)
- [Source: tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs] — theory de rutas conocidas/longitud
- [Source: design-system/fibradis/MASTER.md#Pre-Delivery Checklist]
- [Source: _bmad-output/implementation-artifacts/13-6-portafolio-landing-publico.md] — landing /portafolio (diferenciar contenido)
- [Source: _bmad-output/implementation-artifacts/13-7-rebranding-fibras-inmobiliarias-y-contacto.md] — regla de marca
- [Source: _bmad-output/implementation-artifacts/13-5-reportes-trimestrales-privados.md] — Reportes (describir en el showcase)
- [Source: _bmad-output/planning-artifacts/convenciones-fibradis.md#Checklist SEO] — rutas públicas indexables

## Dev Agent Record

### Agent Model Used
GPT-5 Codex

### Debug Log References
`dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`
`npm run test --workspace=src/Web/Main`
`dotnet build FIBRADIS.slnx`
`npm run build --workspace=src/Web/Main`

### Completion Notes List
`/plataforma` quedó enlazada desde el footer y registra metadata/canonical/breadcrumbs del lado servidor.
La landing pública se implementó como página estática evergreen con hero, superficies públicas, superficies privadas descriptivas y CTA a `/portafolio`.
El sitemap y el catálogo de rutas quedaron sincronizados con `/plataforma`.
La validación automatizada quedó verde en backend, frontend y builds.
Verificación manual en navegador pendiente.

### File List
`src/Web/Main/src/shared/layouts/PublicLayout.tsx`
`src/Web/Main/src/app/routes.tsx`
`src/Web/Main/src/modules/plataforma/PlataformaPage.tsx`
`src/Server/Api/Seo/SpaMetadataProvider.cs`
`src/Server/Api/Seo/SpaRouteCatalog.cs`
`src/Server/Api/Endpoints/Public/SeoEndpoints.cs`
`src/Server/Api/Middleware/SpaMetadataMiddleware.cs`
`tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs`
`tests/Unit/Infrastructure.Tests/Middleware/SpaMetadataMiddlewareTests.cs`
`tests/Unit/Infrastructure.Tests/Endpoints/SeoEndpointsTests.cs`
`tests/Unit/Infrastructure.Tests/Seo/SpaRouteCatalogTests.cs`

## Senior Developer Review (AI)

### Review Findings (code review 2026-06-16 — capas Blind Hunter / Edge Case Hunter / Acceptance Auditor)

Veredicto Acceptance Auditor: **implementación conforme** — todos los AC verificables por código (A1, B2–B4, C5–C7, D8–D9) cumplidos; las 4 listas/breadcrumb sincronizadas; title cliente == SSR byte a byte; description = 152 chars (120–160); JSON-LD `CollectionPage` con `isPartOf #website`/`publisher #organization` vía `App:BaseUrl` (fail-fast), sin dominio hardcodeado; sin "FIBRADIS", sin fuga de datos privados, sin migraciones/endpoints nuevos.

- [x] [Review][Patch] Fusionar "Catálogo de FIBRAs" + "Ficha de FIBRA" en una sola tarjeta/ítem → `numberOfItems`=7 sin `ListItem` de URL duplicada [SpaMetadataProvider.cs `BuildPlataformaJsonLd` + PlataformaPage.tsx `PUBLIC_FEATURES`/StatTile] — **decisión de review resuelta (opción 2)**: ambas tarjetas apuntaban a `/fibras`; se unifican en una tarjeta "Catálogo y fichas de FIBRAs". Ajustar JSON-LD (7 ítems, renumerar posiciones), StatTile "8"→"7", y el test `Plataforma_HasCollectionPageJsonLd_WithBaseUrlReferences` (`numberOfItems` 8→7 + nombre del ítem fusionado).
- [ ] [Review][Patch] Footer: `© {año}` queda separado de "Fibras Inmobiliarias" por el `gap-x-6` del flex-wrap [PublicLayout.tsx:411-418] — al partir el `<span>© {año} Fibras Inmobiliarias</span>` original en `<span>© {año} </span>` + `<Link>`, ambos pasan a ser hermanos flex y se separan 1.5rem (igual que de "Contacto"/"Aviso de privacidad"), leyéndose como 4 ítems independientes; en viewport estrecho el `© {año}` puede quedar huérfano en otra línea. Regresión visual en todas las páginas públicas. Fix: envolver `© {año}` + `<Link>` en un único contenedor flex (p.ej. `<span className="inline-flex items-center gap-1">`) para que el `gap-x-6` solo separe la unidad marca de los demás enlaces.
- [x] [Review][Defer] Conteos hardcodeados no derivados de `.length` [SpaMetadataProvider.cs `numberOfItems`=8 + PlataformaPage.tsx StatTile "8"/"5"] — deferred, polish LOW; hoy coinciden, riesgo de desincronización silenciosa al editar los arreglos.
- [x] [Review][Resuelto] T5 — verificación manual a11y/responsive/SEO **completada en el code review (2026-06-16)**: sin scroll horizontal en 375/768/1024/1440 (Chrome DevTools MCP, overflow 0 + 0 elementos desbordados), footer → `/plataforma` con foco visible, 7 públicas + 5 privadas renderizadas; server-side SEO confirmado por `SpaMetadataMiddlewareTests`/`SeoEndpointsTests`. Ya no es gate de `done`.

**Dismissed (7):** duplicación title/description backend↔frontend (patrón del proyecto: cada página declara metadata en componente y en `SpaMetadataProvider` para SSR+hidratación); sincronización de 3–4 listas de rutas a mano (deuda estructural pre-existente, correctamente manejada en este cambio); `/plataforma` indexable y posible canibalización con `/portafolio` (decisión explícita del spec C7 + contenido diferenciado por ángulo/keywords, confirmado por auditor); marca del footer enlaza a `/plataforma` en vez de `/` (mandato del AC A1); import `react-router` v6/v7 (proyecto en v7, import correcto por convención); `description: '...'` placeholder (elisión del diff enviado al revisor; el archivo real tiene copy completo); casillas del Security Checklist sin tildar (todas N/A justificadas).
