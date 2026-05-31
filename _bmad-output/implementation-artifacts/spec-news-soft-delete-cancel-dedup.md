---
title: 'News: soft-delete, cancelar edición, deduplicación inteligente'
type: 'feature'
created: '2026-05-30'
status: 'done'
baseline_commit: '9f9dfa312a6867ee45f5487a8ddc16052e0309c6'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** En ops existe un botón "Limpiar (null)" innecesario; no hay forma de eliminar noticias del sistema; las eliminadas siguen visibles en ops y en main; y los duplicados ingeridos (mismo tema, diferente fuente) se descartan silenciosamente en lugar de quedar registrados.

**Approach:** Agregar `DeletedAt` para soft-delete. Reemplazar "Limpiar (null)" con "Cancelar" en el panel de edición. Agregar botón de eliminar por fila con confirmación inline. Filtrar eliminadas de todas las queries (ops + main). Bloquear generación IA sobre noticias eliminadas. En el pipeline de ingesta, guardar duplicados por título como eliminados en lugar de descartarlos.

## Boundaries & Constraints

**Always:**
- Soft delete únicamente — nunca borrar filas de `NewsArticle`.
- Noticias con `DeletedAt != null` deben ser invisibles en todas las queries: ops listing, main listing, detalle público.
- AI generation endpoints (`/ai-analysis`, `/ai-summary`) deben retornar 409 Conflict si el artículo está eliminado.
- Duplicados por título en ingesta: guardar con `DeletedAt` ya seteado, sin scraping ni IA. Duplicados por URL siguen descartándose sin guardar.
- La eliminación requiere confirmación explícita antes de persistir.

**Ask First:**
- Ninguna.

**Never:**
- Hard delete de filas.
- Cambiar el comportamiento de duplicados por URL (siguen siendo ignorados completamente).
- Mostrar noticias eliminadas en ningún listado o detalle.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eliminar artículo | Admin hace clic en "Eliminar", confirma | Artículo soft-deleted, desaparece del listado ops inmediatamente | Toast de error si la API falla |
| Eliminar cancelado | Admin hace clic en "Eliminar", luego "No" | Sin cambios | — |
| IA en eliminado (manual) | `POST /ai-analysis` para artículo con `DeletedAt != null` | 409 Conflict, mensaje "Artículo eliminado" | Frontend muestra el mensaje de error |
| Cancelar edición | Admin hace clic en "Cancelar" dentro del panel de edición | Panel se cierra, sin petición de red | — |
| Artículo eliminado en main | `GET /api/v1/news` o `GET /api/v1/news/{id}` | No aparece / retorna 404 | — |
| Duplicado por título en ingesta | Artículo con título normalizado igual a uno reciente, URL nueva | Guardado con `DeletedAt = UtcNow`, sin scraping ni IA, no visible | — |
| Duplicado por URL en ingesta | Artículo con URL ya existente en DB | Ignorado completamente, no se guarda | — |

</frozen-after-approval>

## Code Map

- `src/Server/Domain/News/NewsArticle.cs` — entidad: agregar `DateTimeOffset? DeletedAt`
- `src/Server/Application/News/INewsRepository.cs` — agregar `SoftDeleteAsync`
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs` — implementar `SoftDeleteAsync`; agregar filtro `DeletedAt == null` en `GetLatestAsync`, `GetLatestForFibraAsync`, `GetPagedForOpsAsync`, `GetNullBodyTextArticlesAsync`
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — agregar `DELETE /{articleId}`; en handlers de `/ai-analysis` y `/ai-summary` verificar `DeletedAt != null` → 409
- `src/Server/Api/Endpoints/Public/NewsEndpoints.cs` — en `GET /{id}` retornar 404 si `DeletedAt != null`
- `src/Server/Application/News/NewsDeduplicator.cs` — agregar overload `FilterSeparatingDuplicates()` que retorna `(Fresh, TitleDuplicates)`
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — usar nuevo overload; guardar `TitleDuplicates` como eliminados
- `src/Web/Ops/src/api/newsApi.ts` — agregar `deleteNewsArticle(id)`
- `src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx` — quitar `handleClear` + botón "Limpiar (null)"; agregar "Cancelar" en el panel de edición; agregar botón "Eliminar" por fila con confirmación inline

## Tasks & Acceptance

**Execution:**
- [x] `src/Server/Domain/News/NewsArticle.cs` — Agregar `public DateTimeOffset? DeletedAt { get; set; }` — habilita soft-delete
- [x] Ejecutar `dotnet ef migrations add AddDeletedAtToNewsArticle --project src/Server/Infrastructure --startup-project src/Server/Api` y aplicar con `dotnet ef database update` — genera y aplica la migración
- [x] `src/Server/Application/News/INewsRepository.cs` — Agregar `Task SoftDeleteAsync(Guid id, CancellationToken ct = default)` — contrato
- [x] `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs` — Implementar `SoftDeleteAsync` con `ExecuteUpdateAsync` seteando `DeletedAt = DateTimeOffset.UtcNow`; agregar `.Where(n => n.DeletedAt == null)` en `GetLatestAsync`, `GetLatestForFibraAsync`, `GetPagedForOpsAsync` y `GetNullBodyTextArticlesAsync` — filtra eliminadas de todas las queries
- [x] `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — Agregar `DELETE /{articleId:guid}` que llama `SoftDeleteAsync`; en los handlers de `/ai-analysis` y `/ai-summary`, después de cargar el artículo, retornar `Results.Conflict("Artículo eliminado.")` si `article.DeletedAt != null` — bloquea IA en eliminados
- [x] `src/Server/Api/Endpoints/Public/NewsEndpoints.cs` — En `GET /{id}`, retornar `Results.NotFound()` si `article.DeletedAt != null` — oculta eliminadas del público
- [x] `src/Server/Application/News/NewsDeduplicator.cs` — Agregar método estático `FilterSeparatingDuplicates(items, existingUrls, recentTitles, blocklistTerms)` que retorna `(IReadOnlyList<RssItem> Fresh, IReadOnlyList<RssItem> TitleDuplicates)`; URL-dup → skip en ambas listas; title-dup (con URL nueva) → en `TitleDuplicates`; blocklist-match → skip en ambas — preserva registro de duplicados semánticos
- [x] `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — Reemplazar llamada a `NewsDeduplicator.Filter()` por `FilterSeparatingDuplicates()`; para cada `TitleDuplicate` crear `NewsArticle` con `DeletedAt = DateTimeOffset.UtcNow` y `Status = Processed`, ejecutar `NewsAssociator.Associate()`, guardar con `AddWithLinksAsync` sin scraping ni IA — registra duplicados sin exponer contenido
- [x] `src/Web/Ops/src/api/newsApi.ts` — Agregar `deleteNewsArticle(id: string): Promise<void>` que llama `DELETE /api/v1/ops/news/{articleId}` — contrato de UI
- [x] `src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx` — Eliminar función `handleClear` y el botón "Limpiar (null)"; agregar botón "Cancelar" (estilo slate, llama `handleCancelEdit`) en la fila de botones del panel expandido junto a "Guardar"; agregar estado `pendingDeleteId: string | null` para confirmación inline; cuando `pendingDeleteId === article.id` mostrar "¿Confirmar?" + botones "Sí" + "No" en la columna de acción, en lugar del botón "Eliminar"; al confirmar, llamar `deleteNewsArticle` e invalidar `['ops-news-list']`
- [x] `npm run codegen:api` — Regenerar cliente tipado con el nuevo endpoint DELETE

**Acceptance Criteria:**
- Dado un artículo en el listado ops, cuando se hace clic en "Eliminar" y se confirma, entonces el artículo desaparece del listado ops inmediatamente.
- Dado un artículo eliminado, cuando se carga el feed público de noticias (`/api/v1/news`), entonces el artículo no aparece.
- Dado el GUID de un artículo eliminado, cuando se dispara "Generar análisis" en ops, entonces se muestra un mensaje de error.
- Dado un artículo en modo edición, cuando se hace clic en "Cancelar" dentro del panel expandido, entonces el panel se cierra sin hacer ninguna petición de red.
- Dado que el pipeline corre y encuentra un artículo con título igual a uno reciente pero con URL distinta, entonces el artículo se guarda en DB con `DeletedAt` seteado y no es visible en ningún listado.
- Dado que el pipeline corre y encuentra un artículo con URL ya existente en DB, entonces el artículo no se guarda en ningún caso.
- Dado que el pipeline guarda un duplicado semántico como eliminado, entonces no se hace scraping ni llamada a IA para ese artículo.

## Design Notes

**`FilterSeparatingDuplicates` — lógica:**
- Blocklist match → skip (igual que antes)
- URL en `existingUrls` O URL repetida dentro del batch → skip en ambas listas
- Título normalizado en `recentTitleSet` O repetido dentro del batch → `TitleDuplicates` (solo si la URL es nueva)
- Resto → `Fresh`

**Confirmación inline en ops:** Usar `pendingDeleteId: string | null` (estado en el componente) en lugar de `window.confirm()`. Cuando `pendingDeleteId === article.id`, la columna "Acción" muestra "¿Confirmar? [Sí] [No]" en lugar de los botones habituales.

## Suggested Review Order

**Domain & Persistence**

- Campo que habilita todo el feature: nullable DateTimeOffset
  [`NewsArticle.cs:19`](../../src/Server/Domain/News/NewsArticle.cs#L19)

- Mapping de columna `deleted_at` en la configuración EF
  [`NewsArticleConfiguration.cs:28`](../../src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleConfiguration.cs#L28)

- Soft-delete atómico con guard idempotente (`DeletedAt == null`)
  [`NewsRepository.cs:155`](../../src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs#L155)

- Filtro `DeletedAt == null` en queries de listado ops, main y retry job
  [`NewsRepository.cs:83`](../../src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs#L83)

- Filtro `DeletedAt == null` en dedup de títulos para evitar feedback loop
  [`NewsRepository.cs:30`](../../src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs#L30)

**Backend API**

- Nuevo endpoint DELETE: carga artículo, verifica existencia, llama SoftDeleteAsync
  [`AiModeEndpoints.cs:173`](../../src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs#L173)

- Guards 409 en `/ai-analysis` y `/ai-summary` para artículos eliminados
  [`AiModeEndpoints.cs:221`](../../src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs#L221)

- Guards 404 en detail ops y PUT body-text para artículos eliminados
  [`AiModeEndpoints.cs:163`](../../src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs#L163)

- 404 en detalle público si artículo tiene DeletedAt
  [`NewsEndpoints.cs:46`](../../src/Server/Api/Endpoints/Public/NewsEndpoints.cs#L46)

**Deduplicación inteligente**

- `FilterSeparatingDuplicates`: URL-dup → skip; title-dup con URL nueva → `TitleDuplicates`
  [`NewsDeduplicator.cs:50`](../../src/Server/Application/News/NewsDeduplicator.cs#L50)

- Pipeline guarda title-dups con `DeletedAt = UtcNow`, sin scraping ni IA
  [`NewsPipelineJob.cs:103`](../../src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs#L103)

**Frontend Ops**

- `deleteNewsArticle`: fetch directo al endpoint DELETE con error handling
  [`newsApi.ts:113`](../../src/Web/Ops/src/api/newsApi.ts#L113)

- Estado `pendingDeleteId` + `deleteMutation` con error inline visible
  [`NewsBodyTextSection.tsx:67`](../../src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx#L67)

- Botón "Eliminar" con confirmación inline (¿Confirmar? Sí/No) y columna separada
  [`NewsBodyTextSection.tsx:359`](../../src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx#L359)

- "Limpiar (null)" eliminado; "Cancelar" en panel de edición sin petición de red
  [`NewsBodyTextSection.tsx:486`](../../src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx#L486)

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` — expected: Build succeeded, 0 errores
- `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api` — expected: migración aplicada
- `npm run codegen:api` — expected: cliente regenerado con endpoint DELETE
