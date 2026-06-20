# Story 14.11: Microsoft Clarity — tracking de comportamiento

Status: done

## Story

Como propietario del producto,
quiero que Microsoft Clarity se inyecte en el SPA Main de forma dinámica y sin dependencia del estado de autenticación,
para que pueda analizar el comportamiento real de todos los visitantes (anónimos y autenticados) sin afectar la experiencia de usuario.

## Acceptance Criteria

1. **Dado que** cualquier visitante (anónimo o autenticado) carga cualquier página del SPA Main,
   **Entonces** el script `https://www.clarity.ms/tag/hd9ip85air` se inyecta dinámicamente en el DOM exactamente una vez — sin importar cuántas veces el componente se re-renderice.

2. **Dado que** el script ya fue inyectado y el componente se re-monta o el estado de auth cambia,
   **Entonces** no se añade una segunda copia del script (la inyección es idempotente).

3. **Dado que** corro `dotnet build FIBRADIS.slnx` y `npm run build --workspace=src/Web/Main`,
   **Entonces** 0 errores TypeScript y 0 warnings de compilación.

4. **Dado que** corro `npm test --workspace=src/Web/Main`,
   **Entonces** los tests nuevos de `clarity.test.ts` pasan correctamente junto con todos los tests existentes.

## Tasks / Subtasks

- [x] T1: Crear `src/Web/Main/src/shared/ui/clarity.ts` (AC: 1, 2)
  - [x] T1.1: Exportar constante `CLARITY_SCRIPT_URL = 'https://www.clarity.ms/tag/hd9ip85air'`
  - [x] T1.2: Exportar interfaz `ClarityDocumentLike` con `querySelector`, `createElement` y `head.appendChild` — misma forma que `AdSenseDocumentLike` en `adsense.ts`
  - [x] T1.3: Exportar función `injectClarityScript(doc: ClarityDocumentLike = document): void`:
    - Si ya existe un `<script>` con `src === CLARITY_SCRIPT_URL` → retornar sin hacer nada (idempotente)
    - Crear el elemento `<script>`, asignar `async = true`, `src = CLARITY_SCRIPT_URL`
    - Añadirlo con `doc.head.appendChild(script)` (equivalente al `insertBefore` del snippet nativo)

- [x] T2: Crear `src/Web/Main/src/shared/ui/ClarityLoader.tsx` (AC: 1, 2)
  - [x] T2.1: Componente funcional que en un `useEffect` con array vacío (`[]`) llama a `injectClarityScript()` una vez al montar
  - [x] T2.2: No importar `useAuth` — Clarity carga independientemente del estado de auth
  - [x] T2.3: Retornar `null`

- [x] T3: Montar `<ClarityLoader />` en `PublicLayout.tsx` (AC: 1)
  - [x] T3.1: Importar `ClarityLoader` desde `@/shared/ui/ClarityLoader`
  - [x] T3.2: Añadir `<ClarityLoader />` junto a `<AdSenseLoader />` al inicio del JSX del layout (línea 218 aprox., dentro del `<div className="min-h-screen ...">`)

- [x] T4: Crear `src/Web/Main/src/shared/ui/clarity.test.ts` (AC: 4)
  - [x] T4.1: Test `injectClarityScript inyecta el script exactamente una vez` — verificar que doble llamada solo produce un script en el DOM stubbeado
  - [x] T4.2: Test `injectClarityScript configura async=true y src correcto` — verificar atributos del script inyectado

- [x] T5: Build y lint (AC: 3, 4)
  - [x] T5.1: `npm run build --workspace=src/Web/Main` — 0 errores TypeScript
  - [x] T5.2: `npm test --workspace=src/Web/Main` — tests nuevos pasan

## Dev Notes

### Patrón a seguir: AdSense (historia 14-7)

La implementación replica exactamente la arquitectura de AdSense:

| Archivo AdSense | Archivo Clarity equivalente |
|---|---|
| `src/Web/Main/src/shared/ui/adsense.ts` | `src/Web/Main/src/shared/ui/clarity.ts` |
| `src/Web/Main/src/shared/ui/AdSenseLoader.tsx` | `src/Web/Main/src/shared/ui/ClarityLoader.tsx` |
| `src/Web/Main/src/shared/ui/adsense.test.ts` | `src/Web/Main/src/shared/ui/clarity.test.ts` |
| `<AdSenseLoader />` en `PublicLayout.tsx:218` | `<ClarityLoader />` junto a `<AdSenseLoader />` |

**Diferencia clave respecto a AdSense:** Clarity no tiene dependencia de auth. `ClarityLoader` **no importa `useAuth`**. El `useEffect` tiene dependencias `[]` (mount-only). No existe una función `shouldLoadClarity` ni lógica de remoción.

### Script original proporcionado (referencia)

```html
<script type="text/javascript">
    (function(c,l,a,r,i,t,y){
        c[a]=c[a]||function(){(c[a].q=c[a].q||[]).push(arguments)};
        t=l.createElement(r);t.async=1;t.src="https://www.clarity.ms/tag/"+i;
        y=l.getElementsByTagName(r)[0];y.parentNode.insertBefore(t,y);
    })(window, document, "clarity", "script", "hd9ip85air");
</script>
```

La función auto-invocada originaria usa `insertBefore` antes del primer `<script>` del documento. En nuestra implementación usamos `appendChild` en `document.head`, que es equivalente para el propósito de carga asíncrona y es más simple de testear (mismo patrón que `syncAdSenseScript`).

### Implementación exacta de `clarity.ts`

```typescript
export const CLARITY_SCRIPT_URL = 'https://www.clarity.ms/tag/hd9ip85air'

export interface ClarityDocumentLike {
  querySelector(selectors: string): Element | null
  createElement(tagName: 'script'): HTMLScriptElement
  head: Pick<HTMLHeadElement, 'appendChild'>
}

export function injectClarityScript(doc: ClarityDocumentLike = document): void {
  const selector = `script[src="${CLARITY_SCRIPT_URL}"]`
  if (doc.querySelector(selector)) return

  const script = doc.createElement('script')
  script.async = true
  script.src = CLARITY_SCRIPT_URL
  doc.head.appendChild(script)
}
```

### Implementación exacta de `ClarityLoader.tsx`

```tsx
import { useEffect } from 'react'
import { injectClarityScript } from './clarity'

export function ClarityLoader() {
  useEffect(() => {
    injectClarityScript()
  }, [])

  return null
}
```

### Cambio en `PublicLayout.tsx`

```tsx
// ANTES (línea 218 aprox.)
<div className="min-h-screen flex flex-col bg-background text-foreground">
  <AdSenseLoader />

// DESPUÉS
<div className="min-h-screen flex flex-col bg-background text-foreground">
  <AdSenseLoader />
  <ClarityLoader />
```

### Implementación exacta de `clarity.test.ts`

```typescript
import assert from 'node:assert/strict'
import test from 'node:test'
import {
  CLARITY_SCRIPT_URL,
  injectClarityScript,
  type ClarityDocumentLike,
} from './clarity.ts'

function createDocumentStub() {
  const appendedScripts: Array<{ async: boolean; src: string }> = []

  const doc: ClarityDocumentLike = {
    querySelector(selector: string) {
      if (selector !== `script[src="${CLARITY_SCRIPT_URL}"]`) return null
      return (appendedScripts[0] ?? null) as unknown as Element | null
    },
    createElement() {
      return { async: false, src: '' } as unknown as HTMLScriptElement
    },
    head: {
      appendChild(node) {
        appendedScripts.push(node as unknown as { async: boolean; src: string })
      },
    },
  }

  return { doc, appendedScripts }
}

test('injectClarityScript inyecta el script exactamente una vez (idempotente)', () => {
  const { doc, appendedScripts } = createDocumentStub()

  injectClarityScript(doc)
  injectClarityScript(doc)

  assert.equal(appendedScripts.length, 1)
})

test('injectClarityScript configura async=true y src correcto', () => {
  const { doc, appendedScripts } = createDocumentStub()

  injectClarityScript(doc)

  assert.equal(appendedScripts[0]?.src, CLARITY_SCRIPT_URL)
  assert.equal(appendedScripts[0]?.async, true)
})
```

### Estado actual del código

| Archivo | Relevancia |
|---|---|
| `src/Web/Main/src/shared/layouts/PublicLayout.tsx` línea 11 | Ya importa `AdSenseLoader`. Añadir import de `ClarityLoader` al lado. |
| `src/Web/Main/src/shared/layouts/PublicLayout.tsx` línea 218 | `<AdSenseLoader />` ya montado. Añadir `<ClarityLoader />` inmediatamente después. |
| `src/Web/Main/src/shared/ui/adsense.ts` | Referencia de patrón a replicar (interfaz injectable, función pura testable). |
| `src/Web/Main/src/shared/ui/AdSenseLoader.tsx` | Referencia de patrón a replicar (useEffect + retorna null). |
| `src/Web/Main/src/shared/ui/adsense.test.ts` | Referencia del patrón de test con `createDocumentStub`. |

### No hacer

- No importar `useAuth` en `ClarityLoader` — Clarity se carga para todos, sin distinción de estado de auth.
- No crear función `shouldLoadClarity` — no hay condición condicional de carga.
- No añadir lógica de remoción — Clarity no tiene caso de "si el usuario se autentica, quitar el script".
- No añadir `crossOrigin` al script de Clarity — el script oficial no lo usa (a diferencia de AdSense que requiere `crossorigin="anonymous"`).
- No tocar `AdBanner.tsx` ni `adsense.ts` — son archivos distintos e independientes.
- No agregar AdBanner ni slots de Clarity — Clarity no tiene un equivalente de slots publicitarios; el script hace todo el tracking automáticamente.

### Security Checklist

- [x] **TOCTOU inyección de script**: el guard `doc.querySelector(selector)` previene doble-inyección.
- [x] **Datos de usuario**: Clarity recopila comportamiento de navegación (heatmaps, session recordings). Al ser un script de terceros, sigue la política de privacidad declarada en `/privacidad`. No se envían credenciales ni datos sensibles de la app.
- [x] **CSP/CORS**: Clarity no requiere `crossOrigin` en el tag. No bloquea la app.

### References

- Historia de referencia (patrón AdSense): `_bmad-output/implementation-artifacts/14-7-cta-registro-plataforma.md`
- Archivo de patrón: `src/Web/Main/src/shared/ui/adsense.ts`
- Archivo de patrón: `src/Web/Main/src/shared/ui/AdSenseLoader.tsx`
- Archivo de patrón: `src/Web/Main/src/shared/ui/adsense.test.ts`
- Layout donde montar: `src/Web/Main/src/shared/layouts/PublicLayout.tsx:218`

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_ninguno_

### Completion Notes List

- Implementados 3 archivos nuevos siguiendo el patrón exacto de AdSense (clarity.ts, ClarityLoader.tsx, clarity.test.ts).
- `injectClarityScript` es idempotente: guard con `querySelector` previene doble-inyección.
- `ClarityLoader` usa `useEffect([], [])` sin `useAuth` — carga para todos los visitantes.
- `<ClarityLoader />` montado en `PublicLayout.tsx` junto a `<AdSenseLoader />`.
- `clarity.test.ts` añadido a la lista explícita de `package.json` test script.
- 205/205 tests verdes (2 nuevos de Clarity + 203 existentes sin regresiones). Build 0 errores TypeScript.

### File List

- `_bmad-output/implementation-artifacts/14-11-microsoft-clarity.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Web/Main/src/shared/ui/clarity.ts`
- `src/Web/Main/src/shared/ui/ClarityLoader.tsx`
- `src/Web/Main/src/shared/ui/clarity.test.ts`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx`

## Senior Developer Review (AI)

### Review Findings

- [x] Review/Defer — Sin guard de entorno: Clarity ID de producción `hd9ip85air` se inyecta también en dev/staging, mezclando tráfico de desarrollo con analytics reales (`ClarityLoader.tsx`) — patrón consistente con AdSense; fuera del scope del spec
- [x] Review/Defer — Sin consent gate (GDPR/LGPD): Clarity registra grabaciones de sesión sin comprobación de consentimiento previo (`ClarityLoader.tsx`) — decisión product/legal consciente; cubierta por /privacidad (Security Checklist del story file); patrón igual a AdSense
- [x] Review/Defer — Sin CSP para scripts de terceros: ni Clarity ni AdSense están en allowlist de `script-src`; CDN comprometida no tendría barrera (`Program.cs`) — brecha pre-existente; no introducida por esta historia
- [x] Review/Defer — Footgun SSR: `= document` como parámetro por defecto en `injectClarityScript` lanzaría `ReferenceError` si se invoca fuera de `useEffect` en contexto Node.js (`clarity.ts:9`) — `useEffect` no corre en SSR, no hay crash hoy; patrón idéntico a AdSense

## Change Log

- 2026-06-20: Historia creada.
- 2026-06-20: Implementación completa — clarity.ts + ClarityLoader.tsx + clarity.test.ts + PublicLayout.tsx + package.json. 205/205 tests verdes.
