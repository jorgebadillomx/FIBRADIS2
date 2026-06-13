# Story 12.11: Editor de directivas robots por página + presets

Status: ready-for-dev

<!-- Depende de 12-1 (campo RobotsDirectives en SeoMetadata) y coordina con 12-6 (exclusión noindex del sitemap). -->

## Story

As an **AdminOps de FIBRADIS**,
I want **editar las directivas `robots` (index/noindex, follow/nofollow, max-snippet, max-image-preview, max-video-preview) por página desde Ops, con presets**,
so that **podamos controlar la indexación de páginas individuales sin redeploy (despublicar, limitar snippet, etc.) y mantener consistencia con el sitemap automáticamente**.

## Dependencias y contexto
- **12-1 ya crea el campo `SeoMetadata.RobotsDirectives`** y los middlewares lo inyectan como `<meta name="robots">`. **12-6 ya excluye `noindex` del sitemap.** Esta historia es principalmente la **UI/UX de Ops + presets + validación + coordinación** — el almacenamiento y la emisión ya existen.
- Directivas 2026 relevantes: `index,follow` (default), `noindex`, `nofollow`, `max-snippet:-1`, `max-image-preview:large`, `max-video-preview:-1`.
- Regla de convenciones (coordinación SEO↔auth): cambiar una página a `noindex` debe sincronizar con su salida del sitemap **en el mismo deploy** — aquí es automático porque ambos leen `SeoMetadata`.

## Acceptance Criteria

**AC-1 — Editor de robots en Ops.** En el módulo SEO de Ops, cada fila `SeoMetadata` permite editar `RobotsDirectives` con una UI clara: toggles para `index/noindex` y `follow/nofollow`, y opciones para `max-snippet`/`max-image-preview`/`max-video-preview`. No texto libre crudo propenso a typos (o texto libre + validación estricta).

**AC-2 — Presets.** Presets de un clic: **"Indexable (recomendado)"** = `index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1`; **"No indexar"** = `noindex,nofollow`; **"Indexar sin snippet"** = `index,follow,max-snippet:0`. Aplicar un preset rellena las directivas; el AdminOps puede ajustar.

**AC-3 — Validación.** El backend valida que `RobotsDirectives` contenga solo tokens válidos (lista blanca); rechaza combinaciones contradictorias (p.ej. `index` + `noindex`). Marca el campo como override (regla 12-1) al editar.

**AC-4 — Emisión y sitemap consistentes.** La directiva editada se refleja en el `<meta name="robots">` que inyectan los middlewares (12-1) y la página con `noindex` desaparece del sitemap (12-6) sin acción adicional. Verificable end-to-end.

**AC-5 — Default seguro.** Páginas sin `RobotsDirectives` explícito emiten el default indexable (o ningún `<meta robots>`, equivalente a `index,follow`) — nunca `noindex` accidental.

**AC-6 — Tests.** Unit de validación (tokens válidos, contradicción `index`+`noindex` rechazada, presets producen la cadena esperada). Integration: editar a `noindex` → `<meta robots>` correcto + ausente del sitemap; preset indexable → meta correcto. Frontend: toggles/presets producen la cadena correcta. Verdes antes de `done`.

## Tasks / Subtasks

- [ ] **T1 — Validación backend (AC-3, AC-5)**: helper que valida/normaliza `RobotsDirectives` (lista blanca de tokens, detecta contradicciones). Usar en el `PUT` de `SeoMetadata` (endpoints de 12-1). Default seguro cuando vacío.
- [ ] **T2 — Presets (AC-2)**: definir los presets (compartidos front/back o solo front). Documentar las cadenas exactas.
- [ ] **T3 — UI Ops (AC-1, AC-2)**: en `SeoForm` (de 12-1) agregar controles de robots: toggles + selector de presets + preview de la cadena resultante. Marcar override al cambiar. Accesible. `noUnusedLocals`.
- [ ] **T4 — Verificación end-to-end (AC-4)**: confirmar que editar robots se refleja en el meta inyectado y en la exclusión del sitemap (coord. 12-1/12-6). Si 12-6 no está done, dejar documentado que la exclusión depende de él.
- [ ] **T5 — Tests (AC-6)**: unit validación + presets (valores exactos), integration meta+sitemap, frontend. `dotnet test tests/Unit/`, `dotnet test tests/Integration/ -m:1`, `npm run build`.

## Dev Notes
- **Stack real = SQL Server**. **No crea tablas** — usa el campo `RobotsDirectives` de `SeoMetadata` (12-1). Historia ligera, mayormente UI + validación.
- **Alternativa considerada**: fusionar esto en 12-1. Decisión del usuario: mantenerla como historia propia (12-11) para no inflar la fundación.
- **No reinventar la inyección ni la exclusión del sitemap**: ya están en 12-1 (meta) y 12-6 (sitemap). Aquí solo se administra el valor + presets + validación.
- **Lista blanca de tokens 2026**: `index, noindex, follow, nofollow, none, all, noarchive, nosnippet, max-snippet:N, max-image-preview:none|standard|large, max-video-preview:N, noimageindex`. Rechazar tokens fuera de lista y combinaciones imposibles.
- **Default seguro es crítico**: un bug que ponga `noindex` por defecto desindexaría el sitio. El default sin valor = indexable.
- **Coordinación SEO↔auth (convenciones)**: cuando una ruta pasa a privada, `noindex` + salida del sitemap deben ir juntos — aquí es automático al leer ambos de `SeoMetadata`.

### Security Checklist — antes del primer commit
- [ ] **TOCTOU**: edición sobre fila existente de `SeoMetadata` (PUT idempotente de 12-1).
- [ ] **Auth-gating UI**: editor solo en Ops; endpoints `.RequireAuthorization("AdminOps")` (heredado de 12-1); verificar 401/403.
- [ ] **Default inseguro**: test explícito de que vacío ⇒ indexable, nunca `noindex`.
- [ ] **Denominador cero**: N/A.

### References
- Story 12-1 (campo RobotsDirectives + PUT SeoMetadata): [12-1-modulo-seo-administrable.md](_bmad-output/implementation-artifacts/12-1-modulo-seo-administrable.md)
- Story 12-6 (exclusión noindex del sitemap): [12-6-rastreo-indexacion-sitemap-index-llms-txt.md](_bmad-output/implementation-artifacts/12-6-rastreo-indexacion-sitemap-index-llms-txt.md)
- [convenciones-fibradis.md §Coordinación SEO↔auth](_bmad-output/planning-artifacts/convenciones-fibradis.md)
- 2026: [15 Essential SEO Tags 2026](https://www.link-assistant.com/news/html-tags-for-seo.html) (robots directives) · [Meta Tags 2026 — webspidersolutions](https://webspidersolutions.com/what-are-meta-tags-seo-guide-marketers/)

## Dev Agent Record
### Agent Model Used
### Debug Log References
### Completion Notes List
### File List
