---
title: 'SEO #10 — NewsArticle JSON-LD siempre inyectado en páginas de noticia'
type: 'bugfix'
created: '2026-06-18'
status: 'in-progress'
baseline_commit: '84d40d9b805f2e66560d7a6488230d47fb0a234c'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Artículos de noticia que tienen una fila `SeoMetadata` activa en BD con `JsonLd = null` y `JsonLdIsOverridden = false` no reciben el bloque `<script type="application/ld+json">` NewsArticle. El middleware resuelve el row de BD y lo usa tal cual, sin el fallback "regenera si no está overridden" que sí tiene `FibraProfileMetadataMiddleware`. El schema correcto ya existe en `SeoDefaultsBuilder.BuildNews()` — simplemente no se aplica en este caso.

**Approach:** Añadir un guard post-resolución en `NewsMetadataMiddleware` que rellena `seoMetadata.JsonLd` desde `BuildNews()` cuando el campo es null/vacío y `!JsonLdIsOverridden`. Eliminar el overload `BuildMetaBlock(NewsArticle, string)` que es dead code (nunca llamado).

## Boundaries & Constraints

**Always:**
- El guard solo actúa cuando `!seoMetadata.JsonLdIsOverridden && string.IsNullOrEmpty(seoMetadata.JsonLd)`.
- La mutación es solo en memoria — el middleware nunca llama `SaveChanges`; no se persiste nada en BD.
- El schema emitido es idéntico al que ya genera `BuildNews()`: `@type=NewsArticle`, `headline`, `datePublished`, `author` (Organization con el `Source` del artículo), `publisher` (Fibras Inmobiliarias), `url`, `description`.

**Ask First:**
- Si al inspeccionar los datos reales, el campo `Source` está vacío en una gran parte de los artículos, preguntar si se debe usar un fallback (e.g. "Fuente no disponible") o simplemente omitir `author`.

**Never:**
- No cambiar la estructura del JSON-LD (campos, tipos, encoding).
- No persistir en BD — este middleware es read-only respecto a la BD.
- No tocar la lógica del overload `BuildMetaBlock(SeoMetadata, ...)` que es la ruta activa.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Artículo con row BD, JsonLd null, no override | `seoMetadata.JsonLd = null`, `JsonLdIsOverridden = false` | HTML incluye `<script type="application/ld+json">{"@type":"NewsArticle"...}</script>` | — |
| Artículo con row BD, JsonLd ya poblado | `seoMetadata.JsonLd = "{...}"`, `JsonLdIsOverridden = false` | HTML usa el JsonLd existente sin regenerar | — |
| Artículo con row BD, JsonLd overridden por Ops | `seoMetadata.JsonLdIsOverridden = true` | HTML usa exactamente el JsonLd guardado en BD (puede ser cualquier valor, incluido vacío) | — |
| Artículo sin row BD | `GetAsync` retorna null → `BuildNews()` genera SeoMetadata | JsonLd siempre poblado (camino existente, sin cambio) | — |

</frozen-after-approval>

## Code Map

- `src/Server/Api/Middleware/NewsMetadataMiddleware.cs:163-166` — resolución de `seoMetadata`; añadir guard de JsonLd inmediatamente después
- `src/Server/Api/Middleware/NewsMetadataMiddleware.cs:240-313` — overload `BuildMetaBlock(NewsArticle article, string baseUrl)` dead code; eliminar
- `src/Server/Infrastructure/Seo/SeoDefaultsBuilder.cs:131-182` — `BuildNews()` ya genera el JsonLd correcto; no se modifica

## Tasks & Acceptance

**Execution:**

- [x] `src/Server/Api/Middleware/NewsMetadataMiddleware.cs` -- Después del bloque `if (seoMetadata is null || !seoMetadata.IsActive)` (línea ~166), añadir: `if (!seoMetadata.JsonLdIsOverridden && string.IsNullOrEmpty(seoMetadata.JsonLd)) seoMetadata.JsonLd = seoDefaultsBuilder.BuildNews(article, _baseUrl, DateTimeOffset.UtcNow, "system").JsonLd;` -- garantiza que artículos con row BD sin JsonLd reciban el schema
- [x] `src/Server/Api/Middleware/NewsMetadataMiddleware.cs` -- Eliminar el overload `private static string BuildMetaBlock(NewsArticle article, string baseUrl)` completo (líneas ~240-313) -- dead code nunca llamado; su lógica está cubierta por `SeoDefaultsBuilder.BuildNews()`

**Acceptance Criteria:**

- Dado un artículo con `SeoMetadata` activa en BD y `JsonLd = null`, cuando se hace GET a `/noticias/{slug}`, entonces el HTML contiene `<script type="application/ld+json">` con `"@type":"NewsArticle"` y los campos `headline`, `datePublished`, `author`, `publisher`
- Dado un artículo con `SeoMetadata` activa y `JsonLdIsOverridden = true`, cuando se hace GET a `/noticias/{slug}`, entonces el HTML usa exactamente el `JsonLd` guardado en BD (el guard no actúa)
- Dado el build del backend, cuando compila, entonces 0 errores y 0 referencias a `BuildMetaBlock(NewsArticle`

## Spec Change Log

## Design Notes

**¿Por qué mutar `seoMetadata.JsonLd` en memoria?** El middleware abre un scope DI (`using var scope = scopeFactory.CreateScope()`) y nunca llama `SaveChanges`. La mutación es invisible para EF Core — no existe tracking entre el `GetAsync` de solo lectura y el disposal del scope. Es el patrón correcto para enriquecer datos leídos sin persistirlos.

**¿Por qué no agregar `BuildNewsJsonLd(NewsArticle, string)` a `ISeoDefaultsBuilder`?** Reutilizar `BuildNews()` y extraer solo `.JsonLd` evita duplicar lógica. El coste de crear el `SeoMetadata` completo (título, descripción, OG tags) es insignificante dado que la ruta ya invoca lógica equivalente. Una interfaz nueva sería over-engineering para dos líneas.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: 0 errores, 0 warnings nuevos, sin referencias a `BuildMetaBlock(NewsArticle`
