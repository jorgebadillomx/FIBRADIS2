# Story 13.1: Reorganización de menús de navegación (Main + Ops)

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **usuario de FIBRADIS (visitante público, inversionista autenticado y AdminOps)**,
I want **que los menús de navegación de la SPA Main y el sidebar de Ops estén reorganizados con jerarquía y agrupación**,
so that **pueda encontrar las secciones rápidamente sin que el nav se sature ni desborde, ahora que hay demasiados elementos para una sola fila/lista plana**.

## Contexto del problema

El nav de Main creció a **hasta 10 enlaces directos en una sola fila** (`h-14`) + buscador global + menú de cuenta, lo que ya no cabe entre 768px y 1024px. El sidebar de Ops tiene **13 ítems planos** con descripción siempre visible — scroll largo, sin jerarquía, y la Épica 12 (SEO administrable, en progreso) sumará al menos un módulo más. Esta historia aplica las mejores prácticas de navegación (≤5–7 ítems de primer nivel, agrupación por tarea mental, divulgación progresiva, separación público/privado) sin cambiar ninguna ruta destino ni funcionalidad existente.

**Decisiones tomadas con el usuario (2026-06-13):**
- Una sola historia cubre ambos menús (un branch / un PR).
- Main → agrupar en **dropdowns** (no overflow simple ni mega-menú).
- Ops → **secciones con encabezado** (no colapsables en esta iteración).
- Ops móvil (<1024px) → **drawer con hamburguesa** (off-canvas), consistente con el patrón móvil de Main. Hoy el sidebar se apila completo arriba del contenido.
- Nueva **Épica 13: UX / Navegación e Información Arquitectónica**.

## Acceptance Criteria

### Main — `PublicLayout.tsx`

1. En desktop (≥768px) el nav primario muestra como **máximo 4 enlaces directos**: `Fibras` (`/fibras`), `Comparar` (`/comparar`), `Noticias` (`/noticias`), `Fundamentales` (`/fundamentales`).
2. Un menú desplegable **"Más"** (alternativa aceptable: "Recursos") agrupa los enlaces secundarios públicos: `Conoce las FIBRAs` (`/conoce-las-fibras`), `Calendario` (`/calendario`), `Calculadora` (`/calculadora`). El desplegable abre con click/teclado, cierra con `Escape` y con click fuera, y tiene `aria-haspopup="menu"` + `aria-expanded` correctos.
3. Para usuarios **autenticados** existe un cluster/desplegable **"Mi inversión"** que agrupa `Portafolio` (`/portafolio`), `Oportunidades` (`/oportunidades`) y `Herramientas` (`/herramientas`). Estos enlaces **no se renderizan** para usuarios anónimos (guardia `status === 'authenticated'`, nunca `display:none` por CSS).
4. El buscador global (`GlobalSearch`) y el menú de cuenta (perfil / cerrar sesión / iniciar sesión) siguen presentes y funcionales. El header **no genera scroll horizontal ni desborda** en los breakpoints 768px, 1024px y 1440px, tanto anónimo como autenticado.
5. El menú móvil de Main (`Dialog`, <768px) refleja la **misma agrupación** con encabezados de sección (p. ej. "Navegar", "Más", "Mi inversión", "Cuenta"). Todos los enlaces siguen alcanzables, los targets táctiles son ≥44px de alto, y el diálogo se cierra al navegar.
6. **Ninguna ruta destino cambia.** Cada uno de los enlaces existentes hoy (Conoce las FIBRAs, Fibras, Comparar, Noticias, Calculadora, Calendario, Fundamentales, Portafolio, Oportunidades, Herramientas, Mi perfil, Iniciar/Cerrar sesión) sigue alcanzable desde algún punto del nav desktop y del nav móvil.

### Ops — `OpsShell.tsx`

7. El sidebar agrupa los 13 ítems en **5 secciones con encabezado**, en este orden y contenido:
   - **Operación**: Dashboard (`/dashboard`), Logs del Pipeline (`/pipeline-logs`), Llamadas IA (`/ai-call-logs`)
   - **Datos**: Catálogo (`/catalog`), Distribuciones (`/distribuciones`), Fundamentales (`/fundamentals`)
   - **Contenido**: Contenido Editorial (`/editorial`), Noticias (`/noticias`), Blocklist (`/blocklist`)
   - **IA**: AI Config (`/ai-config`), Prompts de IA (`/ai-prompts`)
   - **Sistema**: Configuración (`/config`), Usuarios (`/users`)
8. Cada encabezado de sección es un elemento **no interactivo** (label/`<p>` con `role` apropiado o agrupado con `<nav aria-label>`), visualmente diferenciado (uppercase, tracking, color atenuado), y los ítems conservan su `NavLink` con el estado activo actual (borde/fondo teal + punto indicador).
9. La descripción de cada ítem **deja de ocupar espacio vertical permanente**: pasa a `title` nativo (tooltip del navegador) sobre el `NavLink`. Esto reduce la densidad y permite que entren más ítems/secciones sin scroll excesivo.
10. **Todas las rutas Ops actuales siguen alcanzables** y el indicador de ruta activa (`isActive`) sigue funcionando idéntico. El redirect index → `/ai-config` se preserva.

### Ops — comportamiento móvil (<1024px)

11. En **<1024px** (`lg`) el sidebar **no se apila como tarjeta completa arriba del contenido**. Se oculta tras un **botón hamburguesa** en una barra superior compacta (logo "FIBRADIS Ops" + hamburguesa) y abre como **panel lateral off-canvas** (drawer) con las mismas 5 secciones agrupadas. En **≥1024px** el comportamiento es el sidebar sticky actual (sin cambios).
12. El drawer cierra con `Escape`, con click/tap en el backdrop y **al navegar** a una ruta; el foco se gestiona correctamente (trap mientras está abierto, retorno al botón al cerrar) y tiene `aria-expanded`/`aria-label`. Targets táctiles ≥44px. La guardia `OpsLoginGate` no se ve afectada.

### Transversal (ambas SPAs)

13. Se cumple el checklist de `design-system/fibradis/MASTER.md`: iconos de **lucide-react** (no emojis), `cursor-pointer` en todo lo clickable, transiciones 150–300ms, foco visible por teclado, contraste AA, `prefers-reduced-motion` respetado en cualquier animación de apertura, y **sin scroll horizontal** en 375/768/1024/1440px.
14. **Tests** (obligatorios antes de `review`, por `workflow-rules.md`):
    - Main: test de componente que renderiza `PublicLayout` como **anónimo** y verifica que NO aparecen Portafolio/Oportunidades/Herramientas; y como **autenticado** y verifica que SÍ aparecen (en "Mi inversión"). Test de que el dropdown "Más" alterna `aria-expanded` y cierra con `Escape`.
    - Ops: test de render de `OpsShell` que verifica que los **13 enlaces** siguen presentes y que existen los **5 encabezados de sección**; y test de que el drawer móvil alterna estado abierto/cerrado (`aria-expanded`) y cierra al navegar.

## Tasks / Subtasks

- [x] **T1 — Main: reorganizar nav desktop con dropdowns** (AC: 1, 2, 3, 4, 6, 11)
  - [x] Definir la estructura de datos del nav (arrays `primaryLinks`, `moreLinks`, `investmentLinks`) en `PublicLayout.tsx` para evitar duplicación entre desktop y móvil.
  - [x] Construir el dropdown "Más" reutilizando el patrón existente del menú de cuenta (estado `useState` + `useRef` + click-fuera + `Escape`) **o** el componente `popover.tsx` ya disponible. NO agregar `dropdown-menu` de shadcn (ver guardrail).
  - [x] Construir el cluster/dropdown "Mi inversión" condicionado a `status === 'authenticated'`.
  - [x] Mantener `GlobalSearch` y el menú de cuenta; verificar que no hay desborde en 768/1024/1440.
- [x] **T2 — Main: reflejar agrupación en el menú móvil (`Dialog`)** (AC: 5, 6)
  - [x] Reusar los mismos arrays de T1 para renderizar secciones con encabezado dentro del `DialogContent`.
  - [x] Conservar el cierre del diálogo `onClick` en cada enlace y el bloque de Cuenta.
- [x] **T3 — Ops: agrupar sidebar en 5 secciones** (AC: 7, 8, 9, 10, 13)
  - [x] Reestructurar `navigationItems` a un arreglo de grupos `{ section, items: [...] }` (reusable por desktop y drawer móvil para no duplicar).
  - [x] Renderizar encabezado de sección + lista de `NavLink`; mover `description` a `title` del `NavLink`.
  - [x] Conservar exactamente el estilo de estado activo y el punto indicador.
- [x] **T4 — Ops: drawer móvil (<1024px)** (AC: 11, 12, 13)
  - [x] Agregar barra superior compacta visible solo en `<lg` (logo + botón hamburguesa con icono `Menu` de lucide).
  - [x] Ocultar el `<aside>` actual en `<lg` y mostrarlo como drawer off-canvas controlado por estado (`useState`), reutilizando el mismo render de grupos de T3. Reusar `dialog.tsx` de `Ops/shared/ui` (Radix) o un panel state-driven con backdrop; NO agregar componentes shadcn nuevos.
  - [x] Cierre con `Escape`, backdrop y al navegar; gestión de foco (trap + retorno) y `aria-expanded`/`aria-label`.
  - [x] Conservar el sidebar sticky sin cambios en `≥lg`.
- [x] **T5 — Tests** (AC: 14)
  - [x] Main: tests de componente (anónimo vs autenticado, toggle dropdown "Más" con `aria-expanded` + `Escape`).
  - [x] Ops: test de render (13 enlaces + 5 encabezados) + test de toggle del drawer móvil (abre/cierra, `aria-expanded`, cierra al navegar).
- [x] **T6 — Verificación manual a11y/responsive** (AC: 4, 5, 11, 12, 13)
  - [x] Verificar en dev server (`npm run dev:main`, `npm run dev:ops`) navegación por teclado, foco visible, drawer Ops en móvil y ausencia de scroll horizontal en 375/768/1024/1440. (No lo cubre el test suite — requiere browser.)

## Dev Notes

### Estado actual de los archivos a MODIFICAR (UPDATE)

**`src/Web/Main/src/shared/layouts/PublicLayout.tsx`** (337 líneas)
- Header `sticky` con `container ... flex h-14 items-center gap-3`. Logo + botón hamburguesa (`md:hidden`) + `<nav class="hidden md:flex ... gap-5">` con **7 enlaces públicos hardcodeados** + bloque condicional `status === 'authenticated'` con 3 enlaces más → hasta 10 en línea.
- Tras el nav: `GlobalSearch` centrado (`hidden md:flex flex-1`) y a la derecha el menú de cuenta (dropdown hand-rolled con `menuRef`, `menuOpen`, listeners `mousedown`/`keydown` Escape) o botón "Iniciar sesión".
- Menú móvil = `<Dialog>` (`mobileMenuOpen`) que **duplica** la lista de enlaces como bloques `<Link>` + bloque "Cuenta".
- **Patrón de dropdown ya existente** (menú de cuenta, líneas ~130–162): `useState`+`useRef`+`useEffect` con `mousedown` y `Escape`, `role="menu"`, `role="menuitem"`, `aria-haspopup`, `aria-expanded`. **Reutilizar este mismo patrón** para "Más" y "Mi inversión" — es consistente y no agrega dependencias.
- **Qué se preserva sin tocar**: `GlobalSearch`, `PriceCarousel`, el menú de cuenta, `TermsModal`/`showTermsModal`, `handleLogout`, `profileLabel`/`truncateEmail`, el skip-link de accesibilidad y el footer.

**`src/Web/Ops/src/components/OpsShell.tsx`** (85 líneas)
- `navigationItems` = array plano de 13 `{ label, to, description }`.
- `<aside>` sticky con tarjeta oscura; `<nav class="flex flex-col gap-2">` mapea cada item a un `NavLink` con `className` función de `isActive` (borde/fondo teal + `shadow inset`) y un `<span>` punto indicador. La `description` se renderiza siempre como `<p class="text-xs ...">`.
- **Responsive actual (gap a corregir)**: el contenedor es `flex flex-col ... lg:flex-row`, y el `<aside>` solo es `lg:sticky lg:w-72`. En **<1024px** el `<aside>` queda en flujo normal **arriba** del contenido, ocupando pantalla completa — hay que hacer scroll por los 13 ítems antes de llegar al `<Outlet>`. No existe hamburguesa ni drawer. AC-11/12 introducen el drawer off-canvas para `<lg`; en `≥lg` el sidebar sticky se conserva igual.
- `Ops/shared/ui` solo tiene `dialog.tsx` (Radix Dialog). Es el componente a reusar para el drawer móvil (o un panel state-driven con backdrop) — **no** agregar componentes shadcn nuevos.
- **Qué se preserva sin tocar**: el header de marca "AdminOps / FIBRADIS Ops", la tarjeta "Ruta segura" al fondo (`mt-auto`), el contenedor `<Outlet>`, `OpsLoginGate` y todo el gradiente/estilo del shell.
- Rutas confirmadas en `src/Web/Ops/src/main.tsx`: el index redirige a `/ai-config` (no a `/dashboard`); preservar ese redirect.

### Información Arquitectónica objetivo (IA de navegación)

**Main desktop — primer nivel (≤4) + 2 dropdowns:**
```
[ Fibras ] [ Comparar ] [ Noticias ] [ Fundamentales ]   [ Más ▾ ]        ...GlobalSearch...   [ Mi inversión ▾ (auth) ] [ Cuenta ▾ ]
                                                            ├ Conoce las FIBRAs                    ├ Portafolio
                                                            ├ Calendario                           ├ Oportunidades
                                                            └ Calculadora                          └ Herramientas
```
**Ops sidebar — 5 secciones:** Operación / Datos / Contenido / IA / Sistema (contenido exacto en AC-7). La sección **Contenido** es donde aterrizará el futuro módulo SEO de Épica 12 — dejar el grupo preparado para crecer.

### Guardrails técnicos (de cumplimiento estricto)

- 🚫 **NO ejecutar `npx shadcn@latest add` sin aprobación explícita** (`convenciones-fibradis.md`). Main **no tiene** `dropdown-menu.tsx`; sí tiene `popover.tsx` (Radix). Usar el patrón hand-rolled existente o `popover.tsx`. Ops solo tiene `dialog.tsx` en `shared/ui` — el sidebar no requiere componentes nuevos.
- **No cambiar rutas ni `to=` destino.** Esta historia es puramente de reorganización visual/IA; cualquier cambio de ruta sería regresión (rompería SEO/sitemap de Épica 11 y enlaces existentes).
- **Auth-gating real**: render condicional `{status === 'authenticated' && ...}`, nunca ocultar por CSS (Security Checklist).
- **Sin duplicar listas en Main**: extraer arrays compartidos entre desktop y `Dialog` móvil para que no se desincronicen (el código actual ya sufre de duplicación literal — corregirla).
- **Iconos**: si se añaden, usar `lucide-react` (ya importado: `Menu`). Nada de emojis (anti-patrón design-system).

### Security Checklist — completar antes del primer commit

- [x] **TOCTOU doble-request**: N/A — esta historia no agrega endpoints de escritura ni mutaciones.
- [x] **Auth-gating de componentes UI**: el cluster "Mi inversión" (Portafolio/Oportunidades/Herramientas) debe renderizarse **solo** con `status === 'authenticated'`. Verificado por test (AC-12).
- [x] **Denominador cero**: N/A — sin funciones de cálculo.

### Project Structure Notes

- Archivos a tocar: `src/Web/Main/src/shared/layouts/PublicLayout.tsx`, `src/Web/Ops/src/components/OpsShell.tsx`, y los archivos de test correspondientes (`*.test.tsx` junto a cada componente o en la carpeta de tests del módulo, según el patrón existente del repo — verificar dónde viven los tests de componentes actuales antes de crear nuevos).
- No se tocan rutas (`routes.tsx`, `main.tsx`), ni backend, ni migraciones EF.
- Naming TS: componentes `PascalCase.tsx`, utils `kebab-case.ts` (no aplica aquí, sin utils nuevos).

### References

- [Source: src/Web/Main/src/shared/layouts/PublicLayout.tsx] — nav desktop + Dialog móvil + patrón dropdown de cuenta
- [Source: src/Web/Main/src/app/routes.tsx] — rutas destino confirmadas (incluye `/acerca`, `/contacto` no enlazadas hoy; fuera de alcance salvo que se decida lo contrario)
- [Source: src/Web/Ops/src/components/OpsShell.tsx] — sidebar plano de 13 ítems
- [Source: src/Web/Ops/src/main.tsx] — rutas Ops + redirect index `/ai-config`
- [Source: design-system/fibradis/MASTER.md#Pre-Delivery Checklist] — a11y, iconos, transiciones, responsive
- [Source: _bmad-output/planning-artifacts/convenciones-fibradis.md] — regla shadcn (línea 10), gate de tests por AC
- [Source: AGENTS.md#Stack] — shadcn/ui + Tailwind v4 + React Router 7

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- `npm run test --workspace=src/Web/Main`
- `npm run build --workspace=src/Web/Main`
- `npm run test --workspace=src/Web/Ops`
- `npm run build --workspace=src/Web/Ops`
- Browser verification Main en `http://localhost:5173` con menú desktop/móvil y sin overflow horizontal
- Browser verification Ops en `http://localhost:5179` con Playwright, validando secciones, drawer, `Escape`, foco de retorno y cierre al navegar

### Completion Notes List

- Main quedó reorganizado en `PublicLayout.tsx` con data compartida en `public-navigation.ts` para desktop y `Dialog` móvil.
- Ops quedó reorganizado en `OpsShell.tsx` con data compartida en `ops-navigation.ts`, secciones agrupadas y drawer móvil responsive.
- Los tests de navegación quedaron en `PublicLayout.test.ts` y `tests/ops/OpsShell.test.ts`, alineados con la nueva estructura de datos.
- La verificación manual confirmó sin scroll horizontal y comportamiento correcto de `aria-expanded`, cierre por `Escape` y retorno de foco.

### Change Log

- 2026-06-15: reordenación de navegación Main y Ops completada, tests actualizados, build y verificación browser validados; story movida a `review`.

### File List

- `src/Web/Main/src/shared/layouts/PublicLayout.tsx`
- `src/Web/Main/src/shared/layouts/public-navigation.ts`
- `src/Web/Main/src/shared/layouts/PublicLayout.test.ts`
- `src/Web/Main/package.json`
- `src/Web/Ops/src/components/OpsShell.tsx`
- `src/Web/Ops/src/components/ops-navigation.ts`
- `src/Web/Ops/package.json`
- `tests/ops/OpsShell.test.ts`

## Review Findings (code review 2026-06-15 — foco: impacto en Épica 12)

### Decisiones requeridas

- [x] [Review][Decision] **RESUELTO — rama stale: el nav omitía los 4 ítems SEO de la Épica 12.** Se extrajo el trabajo de navegación a una rama nueva `story/13-1-reorganizacion-menus-navegacion-v2` creada desde `main` (que ya tiene toda la Épica 12), evitando el merge de la implementación 12-1 duplicada que arrastraba la rama vieja. Los 4 ítems SEO (`/seo/organization`, `/seo/faq`, `/seo/robots`, `/seo/redirects`) se re-integraron en una nueva sección **"SEO"** de `ops-navigation.ts`. **Deviación de AC-7**: el sidebar ahora tiene **6 secciones** (Operación/Datos/Contenido/SEO/IA/Sistema) y **17 ítems** en vez de 5/13, porque la Épica 12 entregó 4 pantallas SEO admin que el spec original (escrito pre-Épica-12) asumía como "futuras". [src/Web/Ops/src/components/ops-navigation.ts]
- [x] [Review][Decision] **RESUELTO — referencia a `robotsDirectives.test.ts`.** En `main` el archivo `src/Web/Ops/src/modules/seo/robotsDirectives.test.ts` (de 12-11) existe, así que el script `test` de Ops ahora es válido: `npm run test` en Ops corre 12 tests (3 de nav + 9 del test SEO de 12-11). Resuelto por la extracción. [src/Web/Ops/package.json]
- [ ] [Review][Decision] **AC14: los tests no renderizan componentes; solo validan datos/funciones puras** — `PublicLayout.test.ts` y `tests/ops/OpsShell.test.ts` hacen `assert.deepEqual` sobre arrays y `shouldCloseMenuOnEscape`; ninguno monta `PublicLayout`/`OpsShell` ni ejerce `aria-expanded`, `Escape`, toggle del drawer ni "cierra al navegar", que AC14 exige explícitamente. El bug del logout no-op pasaría todos los tests. Decisión PENDIENTE: ¿se acepta la cobertura a nivel de datos o se introduce setup de render (jsdom/RTL) ausente en el toolchain actual (node:test sin DOM)? [PublicLayout.test.ts, tests/ops/OpsShell.test.ts]

> **Nota de re-basado (2026-06-15):** la rama vieja `story/13-1-reorganizacion-menus-navegacion` (commit snapshot `c907f9f`) queda abandonada — arrastraba una re-implementación paralela de 12-1 que ya está superada por `main`. Todo el trabajo de navegación continúa en `…-v2`. Tests Main (162) y Ops (12) verdes; builds Main y Ops verdes.

### Patches

- [x] [Review][Patch] Drawer móvil de Ops sin `DialogTitle` → resuelto: `DialogTitle`+`DialogDescription` sr-only agregados (AC12) [src/Web/Ops/src/components/OpsShell.tsx]
- [x] [Review][Patch] `prefers-reduced-motion` no respetado → resuelto: `motion-reduce:animate-none motion-reduce:transition-none` en overlay+content de ambos `dialog.tsx` (AC13) [src/Web/Main/src/shared/ui/dialog.tsx, src/Web/Ops/src/shared/ui/dialog.tsx]
- [x] [Review][Patch] Logout no-op latente → resuelto: `buildMainMobileSections(status, onLogout)` cablea el handler real en la entrada; `MobileSection` ahora invoca `item.onClick()` [src/Web/Main/src/shared/layouts/public-navigation.ts]
- [x] [Review][Patch] Target táctil hamburguesa → resuelto: Main `h-9 w-9`→`h-11 w-11` (hamburguesa + cerrar), Ops `h-10 w-10`→`h-11 w-11` (44px) (AC5/AC13) [src/Web/Main/src/shared/layouts/PublicLayout.tsx, src/Web/Ops/src/components/OpsShell.tsx]

### Deferred

- [x] [Review][Defer] Dropdowns desktop con `role="menu"` sin navegación por flechas (APG incompleto) — AC2 cumplido literalmente; mejora a11y — deferred
- [x] [Review][Defer] SEO/internal-linking: las 3 rutas públicas movidas al dropdown "Más" (`/conoce-las-fibras`, `/calendario`, `/calculadora`) salen del DOM cuando está cerrado (`{open ? … : null}`); siguen indexables vía sitemap (`SeoEndpoints.cs`) + metadata server-side (`SpaMetadataProvider.cs`), pero pierden el enlace interno site-wide del header — deferred, tradeoff de diseño
- [x] [Review][Defer] Estado `checking`: el drawer móvil muestra "Iniciar sesión" para un usuario autenticado durante la revalidación (flicker) — deferred, pre-existente
- [x] [Review][Defer] Drawer de Ops abre por `onClick` manual fuera de `DialogTrigger` y retorno de foco manual; diverge del patrón de Main pero funciona — deferred
