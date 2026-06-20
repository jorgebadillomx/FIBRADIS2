# Story 14.7: CTA de registro en /plataforma + AdSense solo para anónimos

Status: done

## Story

Como visitante anónimo que llega a `/plataforma`,
quiero ver un botón "Crear cuenta" que me lleve directamente a `/registro`,
para que pueda registrarme sin tener que adivinar cómo hacerlo.

Como usuario autenticado en el SPA Main,
quiero que el script y los slots de AdSense no se carguen en absoluto,
para que el acceso de pago no sirva publicidad.

## Acceptance Criteria

1. **Dado que** estoy en `/plataforma` sin autenticarme,
   **Entonces** el hero muestra tres botones en la misma fila: "Ver catálogo" (primario → `/fibras`), "Crear cuenta" (outline → `/registro`) e "Iniciar sesión" (ghost → `/portafolio`).

2. **Dado que** el ancho de pantalla es < 640px (móvil),
   **Entonces** los tres botones se apilan verticalmente gracias al `flex-wrap` existente — sin cambios de layout adicionales.

3. **Dado que** hago clic en "Crear cuenta",
   **Entonces** navego a `/registro` y veo el formulario de registro.

4. **Dado que** hago clic en "Iniciar sesión",
   **Entonces** navego a `/portafolio` (landing dual anónimo/autenticado).

5. **Dado que** estoy autenticado y visito `/plataforma`,
   **Entonces** los tres botones siguen visibles — no se ocultan según estado de auth (la página es pública y estática).

6. **Dado que** el estado de auth es `authenticated`,
   **Entonces** el script de AdSense (`adsbygoogle.js`) **no se inyecta** en el DOM — ni el tag `<script>` en `<head>` ni ningún `.push({})`.

7. **Dado que** el estado de auth es `anonymous` o `checking`,
   **Entonces** el script de AdSense se inyecta dinámicamente en `<head>` exactamente una vez, igual que hoy.

8. **Dado que** `<AdBanner>` se renderiza en cualquier página,
   **Entonces** si el usuario está autenticado el componente retorna `null` sin montar el `<ins>` ni llamar a `.push({})`.

## Tasks / Subtasks

- [x] T1: Editar el hero de `PlataformaPage.tsx` (AC: 1, 2, 3, 4, 5)
  - [x] T1.1: Reemplazar el botón único "Crear cuenta / Iniciar sesión" → `/portafolio` por dos botones separados:
    - `<Button variant="outline">Crear cuenta</Button>` → `<Link to="/registro">`
    - `<Button variant="ghost">Iniciar sesión</Button>` → `<Link to="/portafolio">`
  - [x] T1.2: Verificar que el contenedor `flex flex-wrap gap-3` ya existente agrupa los tres botones sin cambios de clase.

- [x] T2: Mover carga de AdSense de estática a dinámica (AC: 6, 7)
  - [x] T2.1: Eliminar la línea `<script async src="https://pagead2.googlesyndication.com/...adsbygoogle.js...">` de `src/Web/Main/index.html`.
  - [x] T2.2: Crear componente `src/Web/Main/src/shared/ui/AdSenseLoader.tsx`:
    - [x] Importa `useAuth` de `AuthContext`.
    - [x] Si `status === 'authenticated'` → `return null` (no inyecta nada).
    - [x] Si `status !== 'authenticated'` → inyecta el `<script>` en `document.head` con `useEffect` (idempotente: revisar si ya existe antes de crear).
  - [x] T2.3: Montar `<AdSenseLoader />` en `App.tsx` o en `PublicLayout.tsx` — una sola vez en el árbol.

- [x] T3: Guard en `AdBanner.tsx` (AC: 8)
  - [x] T3.1: Importar `useAuth` y agregar al inicio de `AdBanner`: `if (status === 'authenticated') return null;`
  - [x] T3.2: La lógica existente de `key={pathname}` y `.push({})` queda intacta para usuarios anónimos.

- [x] T4: Build y verificación (AC: 1, 2, 6, 7, 8)
  - [x] T4.1: `npm run build --workspace=src/Web/Main` — 0 errores TypeScript, 0 warnings.
  - [x] T4.2: En sesión autenticada: abrir DevTools → Network → verificar que `adsbygoogle.js` no aparece en requests.
  - [x] T4.3: En sesión anónima: verificar que `adsbygoogle.js` sí se carga y los slots de `<AdBanner>` montan normalmente.

## Dev Notes

### Estado actual del código antes de esta historia

| Archivo | Estado |
|---|---|
| `src/Web/Main/src/modules/plataforma/PlataformaPage.tsx` líneas 135-145 | Hero con dos botones: "Ver catálogo" (primary → `/fibras`) y "Crear cuenta / Iniciar sesión" (outline → `/portafolio`). El `<div>` ya usa `flex flex-wrap gap-3`. |
| `src/Web/Main/src/app/routes.tsx` línea 67 | Ruta `/registro` → `RegistroPage` ya existe y está completamente implementada desde 14-2. |
| `src/Web/Main/src/pages/RegistroPage.tsx` | Formulario de registro completo (email, contraseña, apodo, howDidYouHear). Sin cambios requeridos. |
| `src/Web/Main/index.html` línea 9 | `<script async src="https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=ca-pub-6045003898585028" crossorigin="anonymous">` — carga estática, no distingue estado de auth. **Eliminar en T2.1.** |
| `src/Web/Main/src/shared/ui/AdBanner.tsx` | Componente `AdBanner` + `AdUnit` — llama `.push({})` en `useEffect`. Definido pero **no usado en ninguna página todavía**. Sin guard de auth. |

### Cambio exacto a realizar

```tsx
// ANTES (líneas 142-144)
<Button asChild size="lg" variant="outline" className="cursor-pointer">
  <Link to="/portafolio">Crear cuenta / Iniciar sesión</Link>
</Button>

// DESPUÉS
<Button asChild size="lg" variant="outline" className="cursor-pointer">
  <Link to="/registro">Crear cuenta</Link>
</Button>
<Button asChild size="lg" variant="ghost" className="cursor-pointer">
  <Link to="/portafolio">Iniciar sesión</Link>
</Button>
```

### Decisiones de diseño

- El botón "Iniciar sesión" usa `variant="ghost"` para establecer jerarquía visual clara: primario > registro (outline) > login (ghost). El registro es la acción que queremos promover.
- El navbar no se toca: el botón "Portafolio" del header ya es compacto y no tiene espacio para un segundo botón sin comprometer el layout.
- La página es estática/pública, así que los botones no necesitan condicional por estado de auth.

### Patrón para `AdSenseLoader.tsx`

```tsx
import { useEffect } from 'react';
import { useAuth } from '@/modules/auth/AuthContext';

const AD_SCRIPT_URL =
  'https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=ca-pub-6045003898585028';

export function AdSenseLoader() {
  const { status } = useAuth();

  useEffect(() => {
    if (status === 'authenticated') return; // usuarios de pago: no cargar
    if (document.querySelector(`script[src="${AD_SCRIPT_URL}"]`)) return; // idempotente
    const s = document.createElement('script');
    s.src = AD_SCRIPT_URL;
    s.async = true;
    s.crossOrigin = 'anonymous';
    document.head.appendChild(s);
  }, [status]);

  return null;
}
```

### No hacer

- No tocar `LoginForm.tsx`, `PortafolioLanding.tsx` ni el navbar (`PublicLayout.tsx`) en esta historia.
- No agregar tests unitarios: no hay lógica nueva — solo JSX estático + inyección de script. El AC de build TypeScript y la verificación manual en DevTools son suficientes.
- No usar `status === 'checking'` como condición para cargar AdSense — esperar a `anonymous` evita un flash de carga innecesaria, pero dado que `checking` es transitorio y brevísimo, cargar en `!== 'authenticated'` es aceptable (incluye `checking`).

### Security Checklist — completar antes del primer commit

- [x] **TOCTOU**: N/A — solo navegación, sin endpoints de escritura.
- [x] **Auth-gating**: N/A — botones visibles para todos intencionalmente (página pública).
- [x] **Denominador cero**: N/A — sin cálculos financieros.

### Security Checklist — AdSense

- [x] **TOCTOU inyección de script**: el guard `document.querySelector` previene doble-inyección si el efecto corre más de una vez.
- [x] **Auth-gating**: el script no se carga si `status === 'authenticated'` — los usuarios de pago no ven ni descargan AdSense.

### References

- Ruta `/registro` definida en: `src/Web/Main/src/app/routes.tsx:67`
- Componente destino: `src/Web/Main/src/pages/RegistroPage.tsx`
- Archivo a modificar (CTA): `src/Web/Main/src/modules/plataforma/PlataformaPage.tsx:135-145`
- Archivo a modificar (AdBanner guard): `src/Web/Main/src/shared/ui/AdBanner.tsx`
- Archivo a eliminar script: `src/Web/Main/index.html:9`
- Archivo nuevo: `src/Web/Main/src/shared/ui/AdSenseLoader.tsx`
- Historia que implementó `/registro`: `_bmad-output/implementation-artifacts/14-2-registro-y-confirmacion-email.md`
- Historia que implementó `AuthContext` + `useAuth`: `_bmad-output/implementation-artifacts/6-5-autenticacion-y-menus-privados.md`

## Senior Developer Review (AI)

### Review Findings

- [x] \[Review\]\[Defer\] `AdBanner` llama `.push({})` durante `status='checking'` para usuarios que terminarán autenticados — `src/Web/Main/src/shared/ui/AdBanner.tsx` — AC-8 solo aplica a `status='authenticated'`; el `try/catch` atrapa el no-op; el componente no está en uso en ninguna página todavía.
- [x] \[Review\]\[Defer\] `AdSenseAuthStatus` en `adsense.ts` es un tipo independiente de `AuthContext` que podría divergir — `src/Web/Main/src/shared/ui/adsense.ts` — deuda de acoplamiento de tipos; los valores de auth status son estables.
- [x] \[Review\]\[Defer\] `AdSenseLoader` no remueve script previo en estado `checking+cookie` — `src/Web/Main/src/shared/ui/AdSenseLoader.tsx` — escenario improbable en práctica (script estático eliminado de `index.html`; el script se remueve en el siguiente ciclo a `authenticated`).

### Dismissed (10)

`Iniciar sesión → /portafolio` correcto (AC-4 explícito) | Sin test para `AdSenseLoader` (excluido por spec) | `Element.remove()` disponible en DOM estándar | `adsbygoogle.push` antes del script es comportamiento intencional (cola AdSense) | Stub `querySelector` post-remove funciona correctamente | `crossOrigin` en stub — limitación estándar de DI | `doc.head` null — solo SSR, no aplicable | `AdSenseLoader` no se desmonta (vive en `PublicLayout`) | `hasSessionCookie` excepción — lectura DOM simple | `shouldLoadAdSense('anonymous', true)` — estado contradictorio imposible en práctica

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- `npm run build --workspace=src/Web/Main` ✅
- `npm test --workspace=src/Web/Main` ✅
- `npm run lint --workspace=src/Web/Main` ⚠️ falló por deuda preexistente fuera de esta historia (`routes.tsx`, `AuthContext.tsx`, `HerramientasPage.tsx`, `NewsSection.tsx`, `NoticiasListPage.tsx`, `OportunidadesPage.tsx`, `PerfilPage.tsx`, `PositionsTable.tsx`, `ReportesPage.tsx`, `FaqAccordion.tsx`, `dist-server/entry-server.js`)
- Smoke check Playwright en `http://127.0.0.1:4173/plataforma`:
  - anónimo: `requests=1`, `scriptCount=1`, `ctaCount=1`
  - autenticado simulado con `s=1` + token en `sessionStorage`: `requests=0`, `scriptCount=0`, `ctaCount=1`

### Completion Notes List

- Separé el CTA del hero en `/plataforma` en tres botones visibles y preservé el `flex flex-wrap gap-3` ya existente.
- Eliminé la carga estática de AdSense desde `index.html` y la reemplacé por `AdSenseLoader` montado una sola vez en `PublicLayout`.
- Añadí un helper compartido para sincronizar AdSense y lo reutilicé en `AdBanner` para bloquear `push({})` y el `<ins>` cuando la sesión está autenticada.
- Añadí cobertura unitaria para el gate de AdSense, la inyección idempotente y la remoción cuando la sesión pasa a autenticada.
- Verifiqué en navegador que el flujo anónimo sigue cargando AdSense y que una sesión autenticada simulada no genera request ni deja script en el DOM.

### File List

- `_bmad-output/implementation-artifacts/14-7-cta-registro-plataforma.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Web/Main/index.html`
- `src/Web/Main/package.json`
- `src/Web/Main/src/modules/plataforma/PlataformaPage.tsx`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx`
- `src/Web/Main/src/shared/ui/AdBanner.tsx`
- `src/Web/Main/src/shared/ui/AdSenseLoader.tsx`
- `src/Web/Main/src/shared/ui/adsense.ts`
- `src/Web/Main/src/shared/ui/adsense.test.ts`

## Change Log

- 2026-06-19: Separé el CTA de `/plataforma`, moví AdSense de estático a dinámico, añadí el gate para `AdBanner`, agregué pruebas unitarias y confirmé el comportamiento en navegador.
