# Story 13.6: `/portafolio` público (landing SEO + login) y botón "Portafolio"

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **visitante público de FIBRADIS (anónimo) y como inversionista autenticado**,
I want **que `/portafolio` sea la puerta de entrada pública: una página estática indexable que explica todo lo que hace la plataforma e incluye el inicio de sesión cuando estoy anónimo, y mi panel privado cuando ya inicié sesión; y que el botón del nav que decía "Iniciar sesión" diga "Portafolio" y apunte a `/portafolio`**,
so that **`/portafolio` capte tráfico SEO como landing de producto y el login tenga un punto de entrada claro y memorable**.

## Contexto del problema

Follow-up de la Épica 13 (navegación). Decisión de producto/SEO sobre el punto de entrada a la sesión:

1. **Botón → "Portafolio":** el botón anónimo "Iniciar sesión" (que apunta a `/login`) debe decir **"Portafolio"** y apuntar a **`/portafolio`**.
2. **`/portafolio` como landing pública + SEO:** `http://sitio/portafolio` deja de ser una ruta puramente privada. Para anónimos es una **página estática** (evergreen → ideal para SEO) que (a) explica **todo lo que hace la plataforma** y (b) **contiene el login**. Para autenticados sigue siendo el **dashboard de portafolio actual, sin cambios**. `/portafolio` entra al **sitemap** con metadata server-side, como el resto de páginas públicas.

**Decisión arquitectónica (confirmada con el usuario):**

> `/portafolio` pasa de **ruta protegida** a **ruta pública dual**, resuelta por estado de auth en un solo componente de ruta:
> - `status === 'anonymous'` (o `'checking'` → loader) → renderiza **`PortafolioLanding`** (overview de producto + login embebido).
> - `status === 'authenticated'` → renderiza **`PortafolioPage`** (el dashboard actual, intacto).
>
> Una sola URL canónica (`/portafolio`) en lugar de redirect: (a) URL única indexable y memorable, (b) Google rastrea anónimo y ve siempre el landing estático, (c) tras login el mismo componente re-renderiza al dashboard sin navegación, (d) no rompe el enlace `Portafolio` de "Mi inversión" ni el `redirect=/portafolio` del login. **`/login` se conserva** (otras rutas protegidas siguen redirigiendo ahí); el formulario de login se extrae a un componente reutilizable y se usa en `/login` y embebido en el landing.

**Sin fuga de datos privados:** el dashboard se hidrata con fetches autenticados en cliente; el middleware server-side solo inyecta `<meta>`/JSON-LD en el shell `index.html`. Un crawler anónimo nunca recibe datos privados — siempre ve el landing.

### Dependencias y coordinación con otras historias

- **13-1 (reabierta):** revierte el desplegable público "Más" (nav plano). **Ambas historias tocan `public-navigation.ts`/`PublicLayout.tsx`** → coordinar orden de merge (recomendado: 13-1 primero, luego rebase de 13-6). Esta historia (13-6) **solo** cambia la entrada anónima a "Portafolio"/`/portafolio` (botón desktop + entrada móvil "Cuenta"); **no** toca el dropdown "Más" (eso es 13-1) ni "Mi inversión".
- **13-5 (Reportes):** crea la página privada `/reportes` (reportes trimestrales por FIBRA). El overview del landing de `/portafolio` (AC-6) **debe describir también Reportes** entre las capacidades, igual que el resto de funciones.

## Acceptance Criteria

### A. Botón "Portafolio" (reemplaza "Iniciar sesión") — `PublicLayout.tsx` + `public-navigation.ts`

1. En **desktop**, para `status === 'anonymous'`, el `<Link>` que hoy dice **"Iniciar sesión"** y apunta a `/login` cambia su **texto a "Portafolio"** y su destino a **`/portafolio`**. Conserva su estilo de botón (borde, target táctil, focus-visible) y su posición en el cluster derecho del header.
2. En **móvil**, la entrada anónima de la sección "Cuenta" (`buildMainMobileSections`) cambia de `{ label: 'Iniciar sesión', to: '/login' }` a `{ label: 'Portafolio', to: '/portafolio' }`. (El login sigue alcanzable: vive embebido en el landing de `/portafolio`.)
3. Para `status === 'authenticated'`, el cluster derecho **no cambia** (menú de cuenta: perfil / cerrar sesión). El usuario autenticado llega a su portafolio por "Mi inversión → Portafolio". **No** se tocan el dropdown "Más" ni "Mi inversión".

### B. `/portafolio` ruta pública dual (routing + landing + login reutilizable)

4. En `routes.tsx`, `/portafolio` **sale del bloque `<ProtectedRoute>`** y pasa a ser ruta pública bajo `<PublicLayout>`. `/oportunidades`, `/herramientas`, `/perfil` y `/reportes` (13-5) **permanecen protegidas** (sin cambios).
5. La ruta `/portafolio` renderiza un conmutador por estado de auth (`PortafolioRoute`):
   - `authenticated` → `PortafolioPage` (dashboard actual, **lazy**, sin modificar su lógica ni sus llamadas API).
   - `anonymous` → `PortafolioLanding`.
   - `checking` → loader/skeleton (sin parpadeo a landing ni a dashboard durante revalidación).
6. **`PortafolioLanding`** (nuevo componente público, estático, evergreen — sin fetches de datos en vivo) contiene, como mínimo:
   - **Hero** con propuesta de valor de la plataforma.
   - **Overview de "todo lo que hace"**: secciones/tarjetas que describen las capacidades clave con **texto indexable** (no solo iconos): Portafolio (KPIs + calendario de distribuciones), **Reportes (reportes trimestrales por FIBRA — fundamentales + análisis IA; ver 13-5)**, Oportunidades/ranking, Herramientas/Calculadora, Fundamentales comparativos, Noticias, Catálogo de FIBRAs.
   - **Login embebido**: el formulario de inicio de sesión funcional (correo + contraseña), reutilizando la lógica existente (AC-7). CTAs/enlaces internos a secciones públicas (`/fibras`, `/fundamentales`, etc.).
7. **Reutilizar el login, no duplicarlo:** extraer el formulario de `LoginPage.tsx` a un componente compartido (`LoginForm` en `modules/auth/`) que encapsule estado de campos, `login()`, error y lógica de `redirect`. `LoginPage` (`/login`) lo consume tal como hoy; `PortafolioLanding` lo embebe. Tras login exitoso desde el landing, el cambio de `status → authenticated` re-renderiza `/portafolio` al dashboard automáticamente. **Preservar** el comportamiento de `LoginPage`: redirect por `?redirect=` validado (interno, no `//`), default `/portafolio`, y el `useEffect` que redirige si ya está autenticado.
8. **Sin fuga de datos privados / sin regresión de auth:** `PortafolioPage` (dashboard) **solo** se monta con `status === 'authenticated'`; ningún dato privado se renderiza para anónimos ni se inyecta server-side. Las rutas protegidas restantes siguen redirigiendo a `/login?redirect=…`. El flujo `anónimo → /portafolio → login embebido → dashboard` funciona sin recargar.

### C. SEO — `/portafolio` indexable (backend)

9. `/portafolio` se añade a **`SpaMetadataProvider`**: a `KnownPaths` y a un `case` en `GetMetaForPathAsync()` con `title` (`… | FIBRADIS`), `description` (**120–160 caracteres**, validado por test), `canonicalPath` `/portafolio` y JSON-LD (`BuildCollectionPageJsonLd`/`BuildWebPageJsonLd` siguiendo el patrón de `/fibras`/`/privacidad`; `isPartOf` `#website` y `publisher` `#organization` vía `App:BaseUrl`, sin dominio hardcodeado).
10. `/portafolio` se añade al **sitemap**: al array `StaticRoutes` de `SeoEndpoints.cs`. Respeta la visibilidad `noindex` administrable existente (sin código nuevo).
11. Se añade el **breadcrumb** de `/portafolio` en `SpaMetadataMiddleware` (`Inicio` → `Portafolio`). `/portafolio` es **indexable por defecto**. `KnownPaths` y `StaticRoutes` quedan **sincronizados**.

### D. Transversal (diseño + tests)

12. `PortafolioLanding` cumple `design-system/fibradis/MASTER.md`: iconos `lucide-react` (no emojis), `cursor-pointer` en clickables, transiciones 150–300ms, foco visible, contraste AA, `prefers-reduced-motion`, y **sin scroll horizontal** en 375/768/1024/1440px. Usa `usePageTitle` y la tipografía `font-playfair` del hero (como `HomePage`).
13. **Tests (obligatorios antes de `review`, por `workflow-rules.md`):**
    - **Backend (xUnit, `SpaMetadataProviderTests.cs`):** añadir `/portafolio` a los `[Theory]` de rutas conocidas (meta no nula, title `… | FIBRADIS`, canonical `/portafolio`) y al de longitud 120–160; añadir caso de tipo JSON-LD esperado. **Mover `/portafolio` del theory de rutas desconocidas** (hoy figura ahí como `null`). `dotnet test` verde.
    - **Frontend Main (`node:test`, patrón existente `public-navigation.ts`/`PublicLayout.test.ts`):** verificar que la entrada anónima de `buildMainMobileSections` es `Portafolio → /portafolio`. Si se extrae lógica pura de conmutación de ruta, testearla. `npm run test --workspace=src/Web/Main` verde.
    - **Builds verdes:** Main + backend `dotnet build`.

## Tasks / Subtasks

- [x] **T1 — Botón "Portafolio"** (AC: 1, 2, 3)
  - [x] Desktop (`PublicLayout.tsx`): `<Link>` anónimo `Iniciar sesión`/`/login` → `Portafolio`/`/portafolio` (conservar clases/estilo).
  - [x] Móvil (`public-navigation.ts`, `buildMainMobileSections`): entrada anónima de "Cuenta" → `{ label: 'Portafolio', to: '/portafolio' }`.
- [x] **T2 — `/portafolio` ruta pública dual** (AC: 4, 5, 8)
  - [x] `routes.tsx`: mover `/portafolio` fuera de `<ProtectedRoute>` a rutas públicas. Dejar `/oportunidades`, `/herramientas`, `/perfil`, `/reportes` protegidas.
  - [x] Crear `PortafolioRoute` (conmutador por `status`): `authenticated` → `PortafolioPage` (lazy, intacto); `anonymous` → `PortafolioLanding`; `checking` → loader.
- [x] **T3 — Extraer `LoginForm` reutilizable** (AC: 7)
  - [x] Extraer formulario + lógica de `LoginPage.tsx` a `modules/auth/LoginForm.tsx`. `LoginPage` lo consume sin cambiar su comportamiento observable.
- [x] **T4 — `PortafolioLanding`** (AC: 6, 7, 12)
  - [x] Crear `modules/portafolio/PortafolioLanding.tsx`: hero + overview de capacidades (incluida **Reportes**, ver 13-5) + `LoginForm` embebido + enlaces internos. `usePageTitle` alineado a la metadata SSR. Estático, sin fetches en vivo.
- [x] **T5 — SEO backend** (AC: 9, 10, 11)
  - [x] `SpaMetadataProvider.cs`: const `PortafolioDescription` (120–160), `/portafolio` en `KnownPaths`, `case "/portafolio"` con JSON-LD.
  - [x] `SeoEndpoints.cs`: `/portafolio` en `StaticRoutes`.
  - [x] `SpaMetadataMiddleware.cs`: breadcrumb `Inicio → Portafolio`.
- [x] **T6 — Tests** (AC: 13)
  - [x] Backend: extender `SpaMetadataProviderTests.cs` (mover `/portafolio` a conocidas; longitud; tipo JSON-LD). `dotnet test`.
  - [x] Frontend: extender tests de nav (CTA móvil = Portafolio). `npm run test --workspace=src/Web/Main`.
  - [x] Builds Main + backend verdes.
- [x] **T7 — Verificación manual a11y/responsive/SEO** (AC: 8, 12)
  - [x] Dev server: botón "Portafolio", `/portafolio` anónimo (landing+login) vs autenticado (dashboard), login embebido → dashboard sin recargar, sin scroll horizontal en 375/768/1024/1440.
  - [x] Confirmar `/portafolio` en `sitemap.xml`/`sitemap-static.xml` y `<title>`/canonical/JSON-LD server-side en el HTML servido (curl al shell).

### Review Findings

> Code review 2026-06-16 (bmad-code-review). Capas: Blind Hunter + Edge Case Hunter + Acceptance Auditor. AC: 13/13 cumplidos a nivel de código; tests verdes (frontend 9/9, backend SEO 176/176). Triaje: 1 decision-needed, 2 patch, 3 defer, 3 descartados.

- [x] [Review][Patch] "FIBRADIS" en copy VISIBLE de usuario → cambiado a "Fibras Inmobiliarias" (H1 hero, `PortafolioDescription` 120–160 ✓154, `PAGE_DESCRIPTION` sincronizada, `name` del ItemList). Sufijo `… | FIBRADIS` del `<title>` conservado (AC-9/13-7). — El H1 del hero `Todo lo que hace FIBRADIS…` ([PortafolioLanding.tsx:96](../../src/Web/Main/src/modules/portafolio/PortafolioLanding.tsx#L96)), el `name` del ItemList `Capacidades de FIBRADIS` y la `PortafolioDescription` `Tu entrada pública a FIBRADIS` ([SpaMetadataProvider.cs:29](../../src/Server/Api/Seo/SpaMetadataProvider.cs#L29)) contradicen la regla de marca. Corregir el copy de cuerpo + meta description + name del ItemList a "Fibras Inmobiliarias"; **conservar** el sufijo `… | FIBRADIS` del `<title>` (lo manda AC-9 y lo barre 13-7). Mantener `PortafolioDescription` en 120–160 chars y sincronizar `PAGE_DESCRIPTION` del frontend.
- [x] [Review][Patch] `resolveLoginRedirect` endurecido: rechaza `/\evil` (protocol-relative con backslash) y caracteres de control; tests añadidos [login-redirect.ts:7](../../src/Web/Main/src/modules/auth/login-redirect.ts#L7) — la validación pasaba `/\evil.example` (algunos navegadores normalizan `\`→`/` → protocol-relative). Verificado: hoy NO explotable (el valor solo fluye a `navigate()` de react-router, nunca a `window.location`) y es validación pre-existente preservada por AC-7; pero es el control anti-open-redirect citado en el Security Checklist y el endurecimiento es barato (normalizar `\`→`/` o rechazar 2º char `/`|`\`).
- [x] [Review][Patch] Anclas `#login` ahora re-scrollean en re-clic vía `onClick={handleHashAnchorClick}` (`scrollIntoView`, honra `prefers-reduced-motion`) [PortafolioLanding.tsx:113](../../src/Web/Main/src/modules/portafolio/PortafolioLanding.tsx#L113) — el router no tiene manejo de hash/ScrollRestoration; antes el primer clic scrolleaba pero re-clics con la URL ya en `#login` no disparaban scroll.
- [x] [Review][Defer] JSON-LD ItemList apunta a rutas privadas `/reportes` y `/oportunidades` [SpaMetadataProvider.cs](../../src/Server/Api/Seo/SpaMetadataProvider.cs) — deferred: rutas no están en sitemap y el riesgo SEO es marginal; las tarjetas del landing ya usan `#login`.
- [x] [Review][Defer] JSON-LD `CollectionPage`/`BreadcrumbList` del shell persiste en DOM para usuario autenticado (dashboard) [PortafolioLanding.tsx](../../src/Web/Main/src/modules/portafolio/PortafolioLanding.tsx) — deferred: `usePageTitle` no gestiona `ld+json`; sin impacto SEO (crawler nunca autenticado).
- [x] [Review][Defer] Literales de título/description duplicados frontend (`PortafolioLanding`) vs backend (`SpaMetadataProvider`) — deferred: patrón pre-existente en todas las páginas; hoy coinciden carácter a carácter, riesgo solo de drift futuro.

## Dev Notes

### Estado actual de los archivos a MODIFICAR (UPDATE)

**`src/Web/Main/src/shared/layouts/public-navigation.ts`** — `buildMainMobileSections(status, onLogout)`: la sección `Cuenta` anónima usa `{ label: 'Iniciar sesión', to: '/login' }` → **cambiar a `Portafolio`/`/portafolio`**. **No tocar** `MAIN_MORE_LINKS` (lo gestiona 13-1) ni `MAIN_INVESTMENT_LINKS`.

**`src/Web/Main/src/shared/layouts/PublicLayout.tsx`** — `<Link>` anónimo "Iniciar sesión" → `/login` (≈ l.414-421) → **renombrar a "Portafolio"/`/portafolio`**. `useAuth()` da `status`. **No tocar** "Mi inversión", menú de cuenta, `GlobalSearch`, footer.

**`src/Web/Main/src/app/routes.tsx`** — `/portafolio` está hoy en `<ProtectedRoute>` (junto a `/oportunidades`, `/herramientas`, `/perfil`). **Mover `/portafolio`** a rutas públicas. `PortafolioPage` ya es `lazy`. `/login` se conserva. Nota: 13-5 añadirá `/reportes` como protegida.

**`src/Web/Main/src/modules/auth/LoginPage.tsx`** — formulario correo+contraseña, `login()`, error, redirect (valida `?redirect=` interno, default `/portafolio`), `useEffect` redirige si `authenticated`, `if (status === 'checking') return null`. **Extraer el formulario a `LoginForm` preservando este comportamiento.**

**`src/Web/Main/src/modules/auth/ProtectedRoute.tsx`** — guard (`checking`→skeleton, `anonymous`→`/login?redirect=…`, else `<Outlet/>`). **No cambia**.

**`src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`** — dashboard privado. **No modificar su lógica**; se monta condicionado a `authenticated` vía `PortafolioRoute`.

**`src/Web/Main/src/modules/home/HomePage.tsx`** — referencia de estilo del hero (`font-playfair`, `usePageTitle`). `HomePage` muestra datos en vivo; el landing de `/portafolio` debe ser **estático/evergreen**.

**Backend SEO (UPDATE):**
- `src/Server/Api/Seo/SpaMetadataProvider.cs` — `KnownPaths` + `switch GetMetaForPathAsync()`. Patrón: `"/x" => new SpaPageMeta(title, description, "/x", BuildXJsonLd(...))`. Listados: `BuildCollectionPageJsonLd`; informativas: `BuildWebPageJsonLd`. Descripciones 120–160.
- `src/Server/Api/Endpoints/Public/SeoEndpoints.cs` — `StaticRoutes` = fuente de verdad del sitemap; añadir `/portafolio`.
- `src/Server/Api/Middleware/SpaMetadataMiddleware.cs` — `BuildBreadcrumbJsonLdBlock` switch: añadir `"/portafolio"`.
- `tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs` — `/portafolio` figura HOY en el theory de **rutas desconocidas**: **moverlo** a conocidas/descripciones/JSON-LD.

> ⚠️ **OJO `SpaMetadataProvider.cs` / `SpaMetadataProviderTests.cs`:** hay trabajo SEO sin commitear **aparcado en un git stash** ("seo-wip-pendiente-no-relacionado-con-epica13"). Antes de tocar estos archivos, coordinar con el usuario si ese stash debe aplicarse/commitearse primero, para evitar conflictos.

### Guardrails técnicos (de cumplimiento estricto)

- 🚫 **NO `npx shadcn@latest add` sin aprobación.** Reusar componentes/estilos existentes (`LoginForm` con los inputs/estilos de `LoginPage`; landing con Tailwind + lucide-react).
- **No cambiar rutas destino** salvo reclasificar `/portafolio` (protegida → pública). No alterar `/oportunidades`, `/herramientas`, `/perfil`, `/login`, `/reportes`.
- **Auth-gating real:** `PortafolioPage` se monta solo con `authenticated` (render condicional, nunca CSS). El landing nunca expone datos privados.
- **No duplicar** el formulario de login (extraer a `LoginForm`) ni las listas de nav.
- **SEO sin dominio hardcodeado:** JSON-LD/canonical vía `App:BaseUrl`. `KnownPaths` y `StaticRoutes` sincronizados.
- **Coordinación con 13-1:** ambas tocan `public-navigation.ts`/`PublicLayout.tsx`. Esta historia se limita a la entrada anónima → "Portafolio"; 13-1 maneja el dropdown "Más". Coordinar merge.
- **Iconos lucide-react**, nada de emojis.

### Security Checklist — completar antes del primer commit

- [x] **TOCTOU doble-request:** N/A — sin endpoints de escritura nuevos (reusa `login()`).
- [x] **Auth-gating de componentes UI:** `PortafolioPage` solo con `status === 'authenticated'` (vía `PortafolioRoute`); sin datos privados para anónimos ni server-side. Verificado por AC-8 y T7.
- [x] **Denominador cero:** N/A.
- [x] **Open redirect:** preservar la validación de `?redirect=` en `LoginForm` (solo rutas internas, rechazar `//`).

### Project Structure Notes

- **Nuevos archivos:** `modules/portafolio/PortafolioLanding.tsx`, `modules/auth/LoginForm.tsx`, conmutador `PortafolioRoute` (inline en `routes.tsx` o `modules/portafolio/PortafolioRoute.tsx`). Tests junto a `public-navigation.ts` y backend en `SpaMetadataProviderTests.cs`.
- **Sin** migraciones EF, endpoints nuevos, ni cambios de contrato API.

### Limitación de toolchain de tests (heredada de 13.1)

El runner de Main es **`node:test` sin DOM** — los tests validan estructuras de datos / funciones puras (`public-navigation.ts`), no montan componentes. Mantener esa estrategia.

### References

- [Source: src/Web/Main/src/shared/layouts/public-navigation.ts]
- [Source: src/Web/Main/src/shared/layouts/PublicLayout.tsx]
- [Source: src/Web/Main/src/app/routes.tsx]
- [Source: src/Web/Main/src/modules/auth/LoginPage.tsx]
- [Source: src/Web/Main/src/modules/auth/ProtectedRoute.tsx]
- [Source: src/Web/Main/src/modules/portafolio/PortafolioPage.tsx]
- [Source: src/Web/Main/src/modules/home/HomePage.tsx]
- [Source: src/Server/Api/Seo/SpaMetadataProvider.cs]
- [Source: src/Server/Api/Endpoints/Public/SeoEndpoints.cs]
- [Source: src/Server/Api/Middleware/SpaMetadataMiddleware.cs]
- [Source: tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs]
- [Source: _bmad-output/implementation-artifacts/13-1-reorganizacion-menus-navegacion.md] — nav 13.1 (revert "Más" reabierto ahí)
- [Source: _bmad-output/implementation-artifacts/13-5-reportes-trimestrales-privados.md] — Reportes (descrito en el landing)
- [Source: design-system/fibradis/MASTER.md#Pre-Delivery Checklist]
- [Source: AGENTS.md#Reglas Críticas] — "No existe `/dashboard`; unificado en `/portafolio`"

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` — passed (639 tests).
- `npm run test --workspace=src/Web/Main` — passed (174 tests).
- `dotnet build src/Server/Api/Api.csproj` — passed.
- `npm run build --workspace=src/Web/Main` — passed.
- `Invoke-WebRequest http://127.0.0.1:5265/portafolio` — passed (200; title/canonical/CollectionPage/BreadcrumbList present).
- `Invoke-WebRequest http://127.0.0.1:5265/sitemap-static.xml` — passed (200; `/portafolio` present).

### Completion Notes List

- 2026-06-16: Implemented public dual `/portafolio` flow with reusable login form, public landing, SEO metadata, breadcrumb and sitemap updates, plus nav CTA changes.
- 2026-06-16: Added manual HTTP verification for `/portafolio` shell metadata and `sitemap-static.xml` coverage.

### File List

- _bmad-output/implementation-artifacts/13-6-portafolio-landing-publico.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Server/Api/Endpoints/Public/SeoEndpoints.cs
- src/Server/Api/Middleware/SpaMetadataMiddleware.cs
- src/Server/Api/Seo/SpaMetadataProvider.cs
- src/Web/Main/package.json
- src/Web/Main/src/app/routes.tsx
- src/Web/Main/src/modules/auth/LoginForm.tsx
- src/Web/Main/src/modules/auth/LoginPage.tsx
- src/Web/Main/src/modules/auth/login-redirect.test.ts
- src/Web/Main/src/modules/auth/login-redirect.ts
- src/Web/Main/src/modules/portafolio/PortafolioLanding.tsx
- src/Web/Main/src/modules/portafolio/PortafolioRoute.tsx
- src/Web/Main/src/modules/portafolio/portafolio-route.test.ts
- src/Web/Main/src/modules/portafolio/portafolio-route.ts
- src/Web/Main/src/shared/layouts/PublicLayout.test.ts
- src/Web/Main/src/shared/layouts/PublicLayout.tsx
- src/Web/Main/src/shared/layouts/public-navigation.ts
- tests/Unit/Infrastructure.Tests/Endpoints/SeoEndpointsTests.cs
- tests/Unit/Infrastructure.Tests/Middleware/SpaMetadataMiddlewareTests.cs
- tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs

### Change Log

- 2026-06-16: Added backend SEO metadata, sitemap and breadcrumb support for `/portafolio`, and updated story tracking to review.
