---
title: 'Video de ayuda del portafolio (modal YouTube)'
type: 'feature'
created: '2026-06-23'
status: 'done'
context: []
baseline_commit: '05e0463bbb6a5d02bf75f137f141426d3a45d8c9'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Los usuarios nuevos no entienden cómo funciona la sección privada de portafolio (cargar posiciones, KPIs, calendario). No hay material guía y el momento de mayor confusión es antes de cargar el primer archivo y antes de suscribirse.

**Approach:** Un modal reutilizable que reproduce un video corto de YouTube embebido (ID `_nArOCSpPz4`). El video solo se carga al abrir el modal (clic explícito, sin autoplay). El disparador aparece en dos superficies: el estado vacío del portafolio privado (sin posiciones) y la landing pública `/portafolio`.

## Boundaries & Constraints

**Always:**
- Reutilizar el componente `Dialog` existente (`@/shared/ui/dialog`) y `Button` (`@/shared/ui/button`).
- El `<iframe>` de YouTube se monta SOLO cuando el modal está abierto (no en el render inicial de la página) para no cargar recursos de YouTube ni cookies de terceros al entrar.
- Usar el dominio `youtube-nocookie.com` (modo privacidad) y `rel=0`. Sin `autoplay`.
- Iframe responsivo con relación de aspecto 16:9 que no rompa el layout en móvil.
- Accesibilidad: `DialogTitle` y `DialogDescription` presentes; `<iframe>` con `title`; respetar `prefers-reduced-motion` (lo gestiona el Dialog).
- Toda la copy en español. La marca visible es "Fibras Inmobiliarias", nunca "FIBRADIS".

**Ask First:**
- Cambiar el ID del video, agregar autoplay, o agregar el disparador a una tercera superficie (ej. header del portafolio ya poblado).

**Never:**
- No auto-abrir el modal en la primera visita (queda bajo demanda).
- No tocar los archivos `.xlsx` sin commitear en `docs/req/`.
- No introducir dependencias nuevas (sin librerías de video/player externas).
- No modificar backend, CSP del servidor ni configuración de despliegue.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Construir URL embed | videoId `_nArOCSpPz4` | `https://www.youtube-nocookie.com/embed/_nArOCSpPz4?rel=0` | N/A |
| videoId con espacios | `'  _nArOCSpPz4  '` | se recorta (trim) antes de interpolar | N/A |
| Modal cerrado (estado inicial) | página renderizada | NO existe `<iframe>` en el DOM | N/A |
| Clic en disparador | usuario hace clic | modal abre y monta el `<iframe>` con la URL embed | N/A |
| Cerrar modal | clic en X / overlay / Esc | modal cierra y desmonta el `<iframe>` (detiene reproducción) | N/A |

</frozen-after-approval>

## Code Map

- `src/Web/Main/src/modules/portafolio/youtube-embed.ts` -- NUEVO. Helper puro `youtubeEmbedUrl(videoId)` + constante `PORTAFOLIO_VIDEO_ID`.
- `src/Web/Main/src/modules/portafolio/youtube-embed.test.ts` -- NUEVO. Test unitario del helper (node --test).
- `src/Web/Main/src/modules/portafolio/VideoAyudaDialog.tsx` -- NUEVO. Componente trigger (Button) + Dialog con iframe lazy.
- `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx` -- EDIT. Renderizar el disparador en el bloque de estado vacío (`!hasPositions`), líneas ~268-283.
- `src/Web/Main/src/modules/portafolio/PortafolioLanding.tsx` -- EDIT. Renderizar el disparador en la sección hero, debajo del párrafo introductorio (~líneas 122-124).
- `src/Web/Main/package.json` -- EDIT. Agregar `youtube-embed.test.ts` a la lista del script `test`.
- `src/Web/Main/src/shared/ui/dialog.tsx` -- referencia (no editar). Patrón de modal.

## Tasks & Acceptance

**Execution:**
- [x] `youtube-embed.ts` -- exportar `PORTAFOLIO_VIDEO_ID = '_nArOCSpPz4'` y `youtubeEmbedUrl(videoId: string)` que retorna `https://www.youtube-nocookie.com/embed/${videoId.trim()}?rel=0` -- centraliza la URL y la hace testeable.
- [x] `youtube-embed.test.ts` -- cubrir los escenarios de la matriz (URL correcta, trim) usando `node:test`/`node:assert` como los demás tests del módulo -- garantizar construcción de URL.
- [x] `VideoAyudaDialog.tsx` -- componente con estado `open` propio; props `triggerLabel`, `triggerVariant?`, `triggerClassName?`; `DialogTrigger asChild` con `Button` (ícono `PlayCircle` de lucide-react); dentro del `DialogContent` un contenedor `aspect-video` con el `<iframe>` montado solo cuando `open === true` (`allowFullScreen`, `title`, `allow="accelerated-display; fullscreen; picture-in-picture"`); ampliar `DialogContent` (`sm:max-w-2xl`) para que el video se vea bien -- modal reutilizable.
- [x] `PortafolioPage.tsx` -- importar y renderizar `<VideoAyudaDialog>` dentro del bloque `!hasPositions`, junto al texto de instrucciones del Excel, como botón destacado ("Ver video: cómo usar tu portafolio") -- ayuda en onboarding.
- [x] `PortafolioLanding.tsx` -- renderizar `<VideoAyudaDialog>` en la sección hero (debajo del párrafo introductorio), como botón secundario -- ayuda pre-suscripción.
- [x] `package.json` -- añadir la ruta del nuevo test al script `test` -- que corra en CI/local.

**Acceptance Criteria:**
- Given un usuario suscrito sin posiciones, when entra al portafolio, then ve un botón para ver el video y al hacer clic se abre el modal con el video reproducible.
- Given un visitante no autenticado en `/portafolio`, when ve la landing, then encuentra el botón del video en el hero y puede reproducirlo sin iniciar sesión.
- Given la página recién cargada con el modal cerrado, when se inspecciona el DOM, then no existe ningún `<iframe>` de YouTube (se monta al abrir).
- Given el modal abierto, when el usuario lo cierra (X, Esc u overlay), then el iframe se desmonta y la reproducción se detiene.

## Design Notes

Lazy-mount del iframe (clave para perf y privacidad):

```tsx
const [open, setOpen] = useState(false)
// ...
<DialogContent className="sm:max-w-2xl">
  <DialogHeader>
    <DialogTitle>Cómo usar tu portafolio</DialogTitle>
    <DialogDescription>Recorrido rápido de la sección privada.</DialogDescription>
  </DialogHeader>
  <div className="aspect-video w-full overflow-hidden rounded-lg">
    {open && (
      <iframe
        className="h-full w-full"
        src={youtubeEmbedUrl(PORTAFOLIO_VIDEO_ID)}
        title="Cómo usar tu portafolio | Fibras Inmobiliarias"
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; fullscreen; gyroscope; picture-in-picture; web-share"
        referrerPolicy="strict-origin-when-cross-origin"
        allowFullScreen
      />
    )}
  </div>
</DialogContent>
```

`{open && <iframe/>}` dentro del Dialog (que ya desmonta su contenido al cerrar) garantiza que el video no cargue hasta el clic y se detenga al cerrar.

## Verification

**Commands:**
- `cd src/Web/Main && npm test` -- expected: pasa incluyendo `youtube-embed.test.ts`.
- `cd src/Web/Main && npx tsc --noEmit` -- expected: sin errores de tipos.
- `cd src/Web/Main && npm run build` -- expected: build exitoso.

**Manual checks:**
- `npm run dev:main`: en `/portafolio` sin sesión, abrir el modal del hero y verificar reproducción. Con sesión y portafolio vacío, abrir desde el estado vacío. Confirmar en DevTools (Network) que no hay request a youtube hasta abrir el modal, y que el layout no se rompe en viewport móvil.
- Si el hosting aplica una CSP, confirmar que `frame-src`/`child-src` permita `https://www.youtube-nocookie.com` (no hay CSP en el código del repo).

## Suggested Review Order

**Componente del modal (núcleo)**

- Punto de entrada: API de props y valores por defecto del componente reutilizable.
  [`VideoAyudaDialog.tsx:34`](../../src/Web/Main/src/modules/portafolio/VideoAyudaDialog.tsx#L34)

- Decisión clave: el `<iframe>` se monta solo con `open === true` → sin carga ni cookies de YouTube hasta el clic, y se detiene al cerrar.
  [`VideoAyudaDialog.tsx:59`](../../src/Web/Main/src/modules/portafolio/VideoAyudaDialog.tsx#L59)

- Estado único como fuente de verdad para el Dialog y el gate del iframe.
  [`VideoAyudaDialog.tsx:43`](../../src/Web/Main/src/modules/portafolio/VideoAyudaDialog.tsx#L43)

**Construcción de la URL embed**

- Helper puro con dominio nocookie, `rel=0` y `encodeURIComponent` defensivo.
  [`youtube-embed.ts:12`](../../src/Web/Main/src/modules/portafolio/youtube-embed.ts#L12)

**Integración en superficies**

- Disparador en el estado vacío del portafolio privado (onboarding).
  [`PortafolioPage.tsx:279`](../../src/Web/Main/src/modules/portafolio/PortafolioPage.tsx#L279)

- Disparador en el hero de la landing pública (pre-suscripción).
  [`PortafolioLanding.tsx:126`](../../src/Web/Main/src/modules/portafolio/PortafolioLanding.tsx#L126)

**Periféricos**

- Test unitario del helper (3 casos).
  [`youtube-embed.test.ts:5`](../../src/Web/Main/src/modules/portafolio/youtube-embed.test.ts#L5)

- Registro del test en el script `test`.
  [`package.json:11`](../../src/Web/Main/package.json#L11)
