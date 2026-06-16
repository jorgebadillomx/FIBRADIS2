# Story 13.3: Accesibilidad de formularios + contraste AA (transversal)

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **usuario de FIBRADIS (incluidos usuarios de lectores de pantalla y con baja visión)**,
I want **que los campos de formulario tengan identificación/etiqueta accesibles y que los textos y badges cumplan el contraste AA (4.5:1)**,
so that **la app sea usable con teclado y lector de pantalla, no genere warnings de accesibilidad en consola, y el texto sea legible para todos**.

## Contexto del problema

Derivada de la revisión de UI del 2026-06-13 (localhost:5173, MCP chrome-devtools + `design-system/fibradis/MASTER.md`). Tres frentes, todos **transversales** (no cambian rutas ni lógica de negocio):

1. **Inputs sin `id`/`name`** → warning de consola *"A form field element should have an id or name attribute"*.
2. **Inputs numéricos sin etiqueta asociada** (label sin `htmlFor`/`id`, o sin `aria-label`) → inaccesibles para lector de pantalla.
3. **Contraste AA insuficiente** en texto `muted` sobre fondos claros y en algunos badges.
4. Corregir una **contradicción en `MASTER.md`**: lista "Light mode default" como anti-patrón, pese a que la app **es light por diseño**.

## Acceptance Criteria

### A. Inputs con `id`/`name` (elimina warning de consola)

1. Todos los `<input>`/`<select>` de formulario tienen `id` **y** `name` (o `aria-label` cuando no hay label visible). Casos confirmados a corregir:
   - `modules/auth/LoginPage.tsx` — input email (≈ l.66-74) y password (≈ l.79-87): añadir `id`/`name` y asociar los `<label>` con `htmlFor`. **Coordinar con 13-6** (que extrae el formulario a `LoginForm`): el fix debe quedar en `LoginForm` si 13-6 va primero; si no, aplicarlo aquí y 13-6 lo preserva.
   - `modules/calculadora/CalculadoraPage.tsx` — input de búsqueda "Buscar fibra..." (≈ l.141-147): `id`/`name` + `aria-label`.
2. Verificar que **no quedan warnings** "form field should have an id or name attribute" en la consola del navegador en las páginas afectadas (Login, Calculadora, y cualquier otra que aparezca al auditar).

### B. Inputs numéricos con etiqueta accesible

3. Cada `<input type="number">` queda asociado a su `<label>` (vía `htmlFor`/`id`) **o** tiene `aria-label` descriptivo. Casos confirmados:
   - `modules/ficha-publica/IsrCalculatorWidget.tsx` — distribución por CBFI (≈ l.73-80) y CBFIs (≈ l.87-95).
   - `modules/herramientas/HerramientasPage.tsx` — `NumberField` (≈ l.661-669): el `<input>` interno necesita `id` ligado al `<label>` del wrapper (o `aria-label`).
   - `modules/oportunidades/PromediarTab.tsx` — "Títulos a comprar" (≈ l.367-375) y "Renta mensual objetivo" (≈ l.416-424).
4. Auditar el resto de inputs numéricos del Main; cualquier otro sin etiqueta se corrige igual. Si `NumberField` es un wrapper reutilizable, corregir **en el wrapper** (genera `id` y lo liga) para cubrir todos sus usos de una vez.

### C. Contraste AA (4.5:1)

5. **Token `--muted-foreground`**: hoy `#64748B` (slate-500) — falla AA sobre `--muted`/`--background` (≈3.3–3.5:1). Subir a un slate más oscuro (p. ej. `#475569` slate-600 ó `#334155` slate-700) hasta lograr ≥4.5:1 sobre `--card` (#FFFFFF), `--muted` (#F1F5F9) y `--background` (#F4F6F8). Ajustar en `src/Web/Main/src/index.css` (`:root`). Verificar que no rompe la jerarquía visual (el texto muted sigue diferenciándose del foreground).
6. **Badges**: corregir combinaciones bajo 4.5:1:
   - `modules/portafolio/SignalBadge.tsx` — variante `gris` (`bg-muted` + `text-muted-foreground` ≈3.3:1): subir el texto o el fondo hasta AA.
   - `modules/portafolio/ScoreBadge.tsx` — combinaciones `bg-*-50` + `text-*-700` (~3:1): usar `bg-*-100`/`text-*-800` u otra combinación que cumpla AA.
   - Si existe un **badge de yield** con tono violeta/púrpura de bajo contraste, corregirlo igual (verificar durante la auditoría).
7. **Verificación de contraste**: comprobar (DevTools/axe o medición manual) que los textos `muted` y los badges corregidos alcanzan ≥4.5:1 en los fondos donde se usan. Sin regresión visual evidente.

### D. Corregir `MASTER.md`

8. En `design-system/fibradis/MASTER.md` (≈ l.197, sección "Anti-Patterns (Do NOT Use)"): la entrada **"❌ Light mode default"** es incorrecta (la app es light por diseño; la paleta `:root` light es la intención). Corregirla (p. ej. a "❌ Dark mode default" si aplica) o eliminarla, y dejar una nota de que el tema base es light.

### E. Transversal + verificación

9. Cumple `design-system/fibradis/MASTER.md`: foco visible por teclado, `cursor-pointer` en clickables, sin emojis, sin scroll horizontal en 375/768/1024/1440. **No** se cambian rutas, endpoints ni lógica de negocio.
10. **Verificación (obligatoria antes de `review`):** auditoría a11y con chrome-devtools (MCP) o axe en las páginas tocadas — 0 warnings de "id/name", inputs numéricos con nombre accesible, y contraste AA confirmado en los elementos corregidos. Build Main verde. (Tests automatizados: si se extrae lógica pura no hay; el grueso es verificación manual a11y — documentarla en el Dev Agent Record con capturas/resultados de axe.)

## Tasks / Subtasks

- [ ] **T1 — Inputs `id`/`name` + labels** (AC: 1, 2)
  - [ ] LoginPage (email/password) — coordinar con 13-6/`LoginForm`; Calculadora (búsqueda).
- [ ] **T2 — Inputs numéricos con etiqueta** (AC: 3, 4)
  - [ ] IsrCalculatorWidget (×2), HerramientasPage `NumberField` (en el wrapper), PromediarTab (×2). Auditar resto.
- [ ] **T3 — Contraste: token muted + badges** (AC: 5, 6, 7)
  - [ ] `index.css`: subir `--muted-foreground` hasta AA. `SignalBadge`/`ScoreBadge`: combinaciones que cumplan 4.5:1. Verificar contraste.
- [ ] **T4 — MASTER.md** (AC: 8) — corregir/eliminar "Light mode default" como anti-patrón.
- [ ] **T5 — Auditoría a11y + build** (AC: 9, 10) — chrome-devtools/axe en páginas tocadas, sin warnings; build Main verde; documentar evidencia.

## Dev Notes

### Hallazgos concretos (de la auditoría)

- **Inputs sin id/name:** `LoginPage.tsx` (l.66-74 email, l.79-87 password), `CalculadoraPage.tsx` (l.141-147 búsqueda).
- **Inputs numéricos sin aria-label/label:** `IsrCalculatorWidget.tsx` (l.73-80, l.87-95), `HerramientasPage.tsx` `NumberField` (l.661-669), `PromediarTab.tsx` (l.367-375, l.416-424).
- **Tokens de color** en `src/Web/Main/src/index.css` `:root`: `--muted-foreground: #64748B`, `--muted: #F1F5F9`, `--background: #F4F6F8`, `--card: #FFFFFF`, `--border: #E2E8F0`, `--foreground: #1E293B`, `--primary: #1A4A3A`. El cambio de contraste se hace aquí (CSS variables), no en clases sueltas.
- **Badges:** `modules/portafolio/SignalBadge.tsx` (variante `gris`), `modules/portafolio/ScoreBadge.tsx` (bg-50/text-700).
- **MASTER.md** ≈ l.197: "❌ Light mode default" bajo "Anti-Patterns (Do NOT Use)".

### Guardrails técnicos

- 🚫 **NO `npx shadcn@latest add` sin aprobación.** Cambios mínimos sobre componentes/tokens existentes.
- **Contraste en tokens, no parcheando clases una a una:** ajustar `--muted-foreground` cubre la mayoría de los casos de texto muted de golpe. Verificar que no degrada placeholders ni estados disabled.
- **No cambiar rutas/endpoints/lógica.** Es puramente a11y/estilo.
- **Coordinar con 13-6** el fix de los inputs de Login (el formulario se extrae a `LoginForm`).
- Si `NumberField` (Herramientas) es wrapper reutilizable, corregir ahí (un solo punto).

### Security Checklist — completar antes del primer commit

- [ ] **TOCTOU doble-request:** N/A.
- [ ] **Auth-gating de componentes UI:** N/A (solo a11y/estilo).
- [ ] **Denominador cero:** N/A.

### Project Structure Notes

- Archivos a tocar: `index.css`, `LoginPage.tsx` (o `LoginForm.tsx` si 13-6 va antes), `CalculadoraPage.tsx`, `IsrCalculatorWidget.tsx`, `HerramientasPage.tsx`, `PromediarTab.tsx`, `SignalBadge.tsx`, `ScoreBadge.tsx`, `design-system/fibradis/MASTER.md`. Sin backend, sin migraciones.

### Limitación de toolchain de tests

Main usa `node:test` sin DOM → la verificación a11y/contraste es **manual** (chrome-devtools MCP / axe). Documentar resultados en el Dev Agent Record (regla de evidencia de `workflow-rules.md`).

### References

- [Source: src/Web/Main/src/index.css] — tokens de color (`:root`)
- [Source: src/Web/Main/src/modules/auth/LoginPage.tsx]
- [Source: src/Web/Main/src/modules/calculadora/CalculadoraPage.tsx]
- [Source: src/Web/Main/src/modules/ficha-publica/IsrCalculatorWidget.tsx]
- [Source: src/Web/Main/src/modules/herramientas/HerramientasPage.tsx] — `NumberField`
- [Source: src/Web/Main/src/modules/oportunidades/PromediarTab.tsx]
- [Source: src/Web/Main/src/modules/portafolio/SignalBadge.tsx]
- [Source: src/Web/Main/src/modules/portafolio/ScoreBadge.tsx]
- [Source: design-system/fibradis/MASTER.md#Anti-Patterns] — "Light mode default" a corregir
- [Source: _bmad-output/implementation-artifacts/13-6-portafolio-landing-publico.md] — coordinación `LoginForm`

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
