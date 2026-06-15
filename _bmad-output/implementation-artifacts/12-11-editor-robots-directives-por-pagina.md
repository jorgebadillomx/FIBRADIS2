# Story 12.11: Editor de directivas robots por página + presets

Status: done

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

- [x] **T1 — Validación backend (AC-3, AC-5)**: helper que valida/normaliza `RobotsDirectives` (lista blanca de tokens, detecta contradicciones). Usar en el `PUT` de `SeoMetadata` (endpoints de 12-1). Default seguro cuando vacío.
- [x] **T2 — Presets (AC-2)**: definir los presets (compartidos front/back o solo front). Documentar las cadenas exactas.
- [x] **T3 — UI Ops (AC-1, AC-2)**: en `SeoForm` (de 12-1) agregar controles de robots: toggles + selector de presets + preview de la cadena resultante. Marcar override al cambiar. Accesible. `noUnusedLocals`.
- [x] **T4 — Verificación end-to-end (AC-4)**: confirmar que editar robots se refleja en el meta inyectado y en la exclusión del sitemap (coord. 12-1/12-6). Si 12-6 no está done, dejar documentado que la exclusión depende de él.
- [x] **T5 — Tests (AC-6)**: unit validación + presets (valores exactos), integration meta+sitemap, frontend. `dotnet test tests/Unit/`, `dotnet test tests/Integration/ -m:1`, `npm run build`.

### Review Findings (code review 2026-06-15)

- [x] [Review][Decision→Patch] Filas con `IsActive=false` son editables pero su `noindex` se ignora — **RESUELTO** (opción 1): el GET `/api/v1/ops/seo` ahora filtra `row.IsActive`, el editor solo lista filas activas. — el editor lista y guarda robots sobre filas inactivas (`GetByIdAsync` no filtra `IsActive`) y reporta "Guardado", pero los tres middlewares descartan la fila inactiva y reconstruyen desde `seoDefaultsBuilder` (indexable), y `LoadSitemapVisibilityAsync` solo excluye filas `IsActive && noindex`. El operador cree que despublicó la página pero se sigue sirviendo indexable y permanece en el sitemap. Requiere intención: (a) ocultar/inhabilitar filas inactivas en el editor, o (b) honrar el override de robots independientemente de `IsActive`. [SpaMetadataMiddleware.cs:79-98] [SeoEndpoints.cs:299]

- [x] [Review][Patch] GET `/api/v1/ops/seo` sin `pageType` devuelve 400 y rompe el estado inicial "Todos" de la página [src/Server/Api/Endpoints/Ops/OpsSeoEndpoints.cs:19-35] — `useState('all')` envía `pageType: undefined` (SeoPage.tsx:48,58) → el endpoint llama `TryParsePageType(null)` que retorna `false` → `ValidationProblem` 400. El repositorio y `SeoMetadataQuery.PageType` ya soportan null (SeoMetadataRepository.cs:29). Fix: tratar null/vacío como "sin filtro" y solo devolver 400 cuando un valor no vacío no parsea.
- [x] [Review][Patch] `saveMutation` y `refreshRowMutation` sin `onError`: fallos del backend se tragan sin feedback [src/Web/Ops/src/pages/SeoPage.tsx:90-115] — solo se renderiza `seoQuery.isError`. Un 400 de validación (p.ej. `max-snippet:abc` desde los inputs de texto libre) deja el botón volver a "Guardar robots" sin mensaje; el operador cree que guardó. Fix: `onError` + render del error de mutación.
- [x] [Review][Patch] `useEffect([selectedRow])` pisa el draft en edición al refetch/cambio de filtro [src/Web/Ops/src/pages/SeoPage.tsx:78-88] — `selectedRow` se recomputa con referencia nueva al invalidar la query o filtrar; el effect resetea `draft` con el valor del servidor descartando ediciones no guardadas sin aviso.
- [x] [Review][Patch] Round-trip lossy: el editor descarta silenciosamente `noarchive`/`nosnippet`/`noimageindex`/`all`/`none` [src/Web/Ops/src/modules/seo/robotsDirectives.ts:45-130] — el backend acepta y persiste esos tokens, pero `parseRobotsDirectives` los ignora y `buildRobotsDirectives` nunca los re-emite; abrir y guardar una fila que los contenga los elimina. Fix: preservar tokens no reconocidos en el round-trip.
- [x] [Review][Patch] Tests AC-6 faltantes [tests/Unit/Infrastructure.Tests/Seo/SeoRobotsDirectivesTests.cs] — sin cobertura para: contradicción `follow`+`nofollow` (AC-3 pide "combinaciones" en plural), `max-image-preview` inválido, `max-snippet`/`max-video-preview` no numérico o < -1, y mapeos `all`/`none`. Las ramas de error del parser no se ejercitan.
- [x] [Review][Patch] `parseRobotsDirectives('')` rellena `large/-1/-1` pero `parseRobotsDirectives('index,follow')` no → inconsistencia + override silencioso [src/Web/Ops/src/modules/seo/robotsDirectives.ts:48-56] — guardar una fila con default vacío sin tocar nada persiste `index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1` y marca `RobotsDirectivesIsOverridden=true`, rompiendo la herencia futura desde `SeoDefaultsBuilder` (intención AC-5). Fix: el caso vacío no debe auto-rellenar, o no marcar override si el valor no cambió.

- [x] [Review][Defer] TOCTOU / last-write-wins en el PUT sin concurrencia optimista [src/Server/Api/Endpoints/Ops/OpsSeoEndpoints.cs:72-81] — deferred, aceptable en Ops single-writer; el diseño viene de 12-1.
- [x] [Review][Defer] Rutas estáticas sin fila en BD no son editables desde el editor [src/Web/Ops/src/pages/SeoPage.tsx] — deferred, cobertura/scope; depende del seed de filas de 12-1.
- [x] [Review][Defer] Contradicciones semánticas menores no detectadas (`nosnippet`+`max-snippet`, `noindex`+`max-image-preview`) [src/Server/Application/Seo/SeoRobotsDirectives.cs] — deferred, no requerido por AC-3 (solo index/noindex y follow/nofollow).

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
GPT-5 / Codex
### Debug Log References
`dotnet build FIBRADIS.slnx`
`npm run codegen:api`
`npm run build --workspace=src/Web/Ops`
`node --experimental-strip-types --test src/modules/seo/robotsDirectives.test.ts`
`dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter SeoRobotsDirectivesTests`
`dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~SeoRobotsEndpointTests" -m:1`
`dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~OpenApiEndpointTests" -m:1`
`dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~SeoEndpointTests" -m:1`
### Completion Notes List
 - Implemented backend normalization for `RobotsDirectives` with whitelist validation, contradiction detection, and canonical preset ordering.
 - Added `GET /api/v1/ops/seo`, `GET /api/v1/ops/seo/{id}`, and `PUT /api/v1/ops/seo/{id}` plus sitemap cache invalidation on save.
 - Wired `SpaMetadataMiddleware`, `FibraProfileMetadataMiddleware`, and `NewsMetadataMiddleware` to emit `<meta name="robots">` from the stored SEO row.
 - Added the Ops SEO robots editor page with presets, toggles, preview, route, and sidebar entry.
 - Regenerated the shared OpenAPI client from the backend document so the new SEO contract is typed for the SPAs.
 - Validation passed: backend build, frontend build, helper unit test, story-specific C# unit test, new SEO integration tests, and OpenAPI smoke test.
 - The full `tests/Unit/Infrastructure.Tests` suite is blocked in this environment by unrelated SQL Server-backed tests; the story-specific validator subset passed.
### File List
 - `scripts/codegen/Api.json`
 - `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
 - `src/Server/Api/Endpoints/Ops/OpsSeoEndpoints.cs`
 - `src/Server/Api/Endpoints/Public/SeoEndpoints.cs`
 - `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs`
 - `src/Server/Api/Middleware/NewsMetadataMiddleware.cs`
 - `src/Server/Api/Middleware/SpaMetadataMiddleware.cs`
 - `src/Server/Api/Program.cs`
 - `src/Server/Api/Seo/SeoSitemapCacheState.cs`
 - `src/Server/Application/Seo/ISeoMetadataRepository.cs`
 - `src/Server/Application/Seo/SeoRobotsDirectives.cs`
 - `src/Server/Infrastructure/Persistence/Repositories/Seo/SeoMetadataRepository.cs`
 - `src/Server/SharedApiContracts/Seo/SeoMetadataDto.cs`
 - `src/Server/SharedApiContracts/Seo/UpdateSeoMetadataRequest.cs`
 - `src/Web/Ops/src/api/seoApi.ts`
 - `src/Web/Ops/src/components/OpsShell.tsx`
 - `src/Web/Ops/src/main.tsx`
 - `src/Web/Ops/src/modules/seo/robotsDirectives.test.ts`
 - `src/Web/Ops/src/modules/seo/robotsDirectives.ts`
 - `src/Web/Ops/src/pages/SeoPage.tsx`
 - `src/Web/Ops/tsconfig.app.json`
 - `src/Web/SharedApiClient/schema.d.ts`
 - `tests/Integration/Api.Tests/OpenApiEndpointTests.cs`
 - `tests/Integration/Api.Tests/Ops/SeoRobotsEndpointTests.cs`
 - `tests/Unit/Infrastructure.Tests/Middleware/FibraProfileMetadataMiddlewareTests.cs`
 - `tests/Unit/Infrastructure.Tests/Middleware/NewsMetadataMiddlewareTests.cs`
 - `tests/Unit/Infrastructure.Tests/Middleware/SpaMetadataMiddlewareTests.cs`
 - `tests/Unit/Infrastructure.Tests/Seo/SeoRobotsDirectivesTests.cs`
## Change Log
- 2026-06-14: Added per-page robots editor for Ops, backend normalization/validation, sitemap cache invalidation, shared API contracts, and targeted unit/integration coverage.
