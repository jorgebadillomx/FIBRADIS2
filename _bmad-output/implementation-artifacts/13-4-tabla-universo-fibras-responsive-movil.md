# Story 13.4: Tabla "Universo FIBRAS" responsiva en móvil

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **usuario de FIBRADIS en móvil**,
I want **que la tabla "Universo FIBRAS" de la home se vea bien en pantallas pequeñas (sin scroll horizontal extenso que oculte columnas clave)**,
so that **pueda consultar precio, variación y datos esenciales de cada FIBRA desde el celular sin pelear con el scroll**.

## Contexto del problema

Derivada de la revisión de UI del 2026-06-13. Hoy `FibraUniverseTable` (home `/`) es una **rejilla de 13 columnas** dentro de `min-w-[60rem]` (960px) con `overflow-x-auto`: en 375px exige scroll horizontal extenso y deja fuera de vista columnas clave (Volumen, Rango 52S, Yield). Hay que adoptar un **layout responsivo** en móvil. La tabla de `/fundamentales` (`min-w-[640px]`, 8 columnas) tiene el mismo problema en menor grado — **evaluar** el mismo patrón. **Sin cambio de datos ni rutas.**

**Patrón reutilizable ya existente en el repo:** filas/secciones **expandibles** con `useState<Set<string>>` + toggle + detalle (`PositionsTable`, `OportunidadesPage`/RankingTable, `DistribucionesSection`). El detalle usa `grid-cols-1 md:grid-cols-2` (ver `PositionExpandedDetail`). No hay hook `useMediaQuery`; se usan breakpoints Tailwind por defecto (`sm` 640 / `md` 768 / `lg` 1024).

## Acceptance Criteria

### A. `FibraUniverseTable` responsiva (foco principal)

1. En **<768px** (`md`), `FibraUniverseTable` **no** obliga a scroll horizontal extenso. Se adopta uno de estos layouts (decisión del dev, justificada en Dev Agent Record):
   - **(a) Tarjeta por emisora**: cada FIBRA como card apilada con sus datos clave; **o**
   - **(b) Columnas prioritarias + detalle expandible**: mostrar **Emisora + Precio + Var%** siempre, y el resto (Volumen, Rango 52S, Máx/Mín 52S, Yield, Último Rep., Estado) en un **detalle expandible** por fila (reusar el patrón `Set<string>` existente).
2. **Ningún dato se pierde en móvil:** todas las columnas actuales (Emisora, Precio, Var $, Var %, Volumen, Rango 52S, Máx 52S, Mín 52S, Yield, Último Rep., Estado/FreshnessBadge) siguen **alcanzables** (visibles directamente o tras expandir/en la card).
3. En **≥768px** se conserva la tabla/rejilla actual **sin cambios** (incluido el `FreshnessBadge`, la barra visual de Rango 52S y el `FibraLogo`).
4. **Sin scroll horizontal** en 375px (ni 320px si es viable). La fila/card sigue siendo clickable hacia la ficha (`/fibras/:slug`) como hoy.

### B. Tabla de `/fundamentales` (evaluar mismo patrón)

5. Evaluar `FundamentalesPage` (tabla HTML `min-w-[640px]`, 8 columnas): si en <768px también fuerza scroll horizontal incómodo, aplicar el **mismo patrón** elegido en A (columnas prioritarias FIBRA + 1–2 KPIs + detalle expandible, o card). Si se decide **no** tocarla en esta historia, **justificarlo** en el Dev Agent Record (no dejarlo implícito).

### C. Transversal + tests

6. Cumple `design-system/fibradis/MASTER.md`: iconos `lucide-react` (chevron de expandir, no emojis), `cursor-pointer` en filas/cards clickables y en el toggle, transiciones 150–300ms, `prefers-reduced-motion`, foco visible, target táctil ≥44px en el control de expandir. Estados de carga/vacío/error de la tabla se conservan.
7. **Reutilizar, no reinventar:** usar el patrón de expandible existente (`Set<string>` + toggle + fila/bloque detalle) en vez de crear uno nuevo. Si se necesita decidir layout por ancho en JS, crear un `useMediaQuery`/`useBreakpoint` mínimo y reutilizable (no inline disperso); preferir breakpoints CSS (`md:`) cuando baste.
8. **Tests/verificación (obligatorio antes de `review`):** verificación manual responsive con chrome-devtools (MCP) en 375/768/1024/1440 (sin scroll horizontal en móvil, todos los datos alcanzables, expandir/colapsar OK). Si se extrae lógica pura (p. ej. partición de columnas prioritarias vs. detalle, o el hook de breakpoint), test `node:test`. Build Main verde. Documentar evidencia en el Dev Agent Record.

## Tasks / Subtasks

- [ ] **T1 — Layout responsivo de `FibraUniverseTable`** (AC: 1, 2, 3, 4)
  - [ ] Elegir (a) card o (b) columnas prioritarias + detalle expandible (justificar). Reusar patrón `Set<string>` de `PositionsTable`/`RankingTable`.
  - [ ] `<md`: layout móvil; `≥md`: rejilla actual intacta. Conservar click a ficha, `FreshnessBadge`, barra Rango 52S, `FibraLogo`.
- [ ] **T2 — Evaluar/ajustar tabla `/fundamentales`** (AC: 5) — aplicar mismo patrón o justificar no hacerlo.
- [ ] **T3 — a11y/diseño** (AC: 6, 7) — chevron lucide, target ≥44px, transiciones, prefers-reduced-motion; hook `useMediaQuery` mínimo solo si hace falta.
- [ ] **T4 — Verificación + build** (AC: 8) — chrome-devtools 375/768/1024/1440; test de lógica pura si aplica; build Main verde; documentar.

## Dev Notes

### Estado actual

**`src/Web/Main/src/modules/home/FibraUniverseTable.tsx`** — **UPDATE (foco principal)**:
- Contenedor: `<div className="overflow-x-auto"><div className="min-w-[60rem]">` (≈ l.95-96).
- Rejilla: `grid grid-cols-[2.5rem_minmax(5rem,1fr)_auto_auto_auto_auto_6rem_auto_auto_auto_auto_auto] gap-3` (≈ l.97) — 13 columnas.
- Columnas: Emisora (logo+ticker), Precio, Var $, Var %, Volumen, Rango 52S (barra visual), Máx 52S, Mín 52S, Yield, Último Rep., Estado (`FreshnessBadge`).
- Datos: `useQuery` con `fetchMarketSnapshots()`, `fetchAllFibras()`, `fetchFundamentalesSummary()` (≈ l.32-52). **No tocar la capa de datos.**
- **Sin** responsive hoy (sin `hidden`/`md:` en columnas).

**`src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx`** — **UPDATE condicional (AC-5)**:
- `<table className="w-full min-w-[640px]">` dentro de `overflow-x-auto` (≈ l.119-120). 8 columnas: FIBRA, Período, Cap Rate, NAV/CBFI, LTV, Margen NOI, Margen FFO, Distribución Trimestral. Filas: `FundamentalesRow` (≈ l.187-224).

**Patrón expandible reutilizable (referencia):**
- `modules/portafolio/PositionsTable.tsx` — `expandedRows: Set<string>`, `toggleRow`, fila detalle con `colSpan` + `PositionExpandedDetail` (`grid-cols-1 md:grid-cols-2`).
- `modules/oportunidades/OportunidadesPage.tsx` — `expanded: Set<string>`, chevron con `rotate-90`.
- `modules/ficha-publica/sections/DistribucionesSection.tsx` — grupos expandibles.

**Breakpoints:** Tailwind por defecto (`sm` 640 / `md` 768 / `lg` 1024). **No** existe `useMediaQuery` — crear uno mínimo solo si se necesita decidir layout en JS; preferir CSS (`hidden md:grid` / `md:hidden`).

### Guardrails técnicos

- 🚫 **NO `npx shadcn@latest add` sin aprobación.**
- **No cambiar datos ni rutas** (la fila sigue enlazando a `/fibras/:slug`).
- **Reutilizar** el patrón de expandible existente (no crear uno nuevo).
- **≥md intacto:** el desktop no debe cambiar visualmente.
- **Iconos lucide-react** (chevron), nada de emojis.

### Security Checklist — completar antes del primer commit

- [ ] **TOCTOU doble-request:** N/A.
- [ ] **Auth-gating de componentes UI:** N/A (datos públicos).
- [ ] **Denominador cero:** N/A.

### Project Structure Notes

- Archivos: `modules/home/FibraUniverseTable.tsx` (principal), `modules/fundamentales/FundamentalesPage.tsx` (condicional), posible `shared/hooks/useMediaQuery.ts` (solo si necesario). Sin backend.

### Limitación de toolchain de tests

Main `node:test` sin DOM → verificación responsive **manual** (chrome-devtools MCP). Tests automatizados solo para lógica pura extraída (partición de columnas, hook de breakpoint). Documentar evidencia en Dev Agent Record.

### References

- [Source: src/Web/Main/src/modules/home/FibraUniverseTable.tsx] — tabla Universo (min-w-[60rem])
- [Source: src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx] — tabla /fundamentales (min-w-[640px])
- [Source: src/Web/Main/src/modules/portafolio/PositionsTable.tsx] — patrón expandible `Set<string>`
- [Source: src/Web/Main/src/modules/portafolio/PositionExpandedDetail.tsx] — detalle `grid-cols-1 md:grid-cols-2`
- [Source: src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx] — RankingTable expandible
- [Source: design-system/fibradis/MASTER.md#Pre-Delivery Checklist] — a11y, responsive

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
