---
title: 'SEO Backend Quick Fixes — lastmod, Google News sitemap, llms.txt, footer nav'
type: 'chore'
created: '2026-06-18'
status: 'draft'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Cinco defectos SEO de bajo esfuerzo degradan el crawl budget, la indexación de noticias en Google News, la accesibilidad de llms.txt para crawlers de IA, y el PageRank interno del footer. Todos se concentran en `SeoEndpoints.cs` y `PublicLayout.tsx`.

**Approach:** Corregir cada defecto quirúrgicamente sin refactoring — cambios en firmas de métodos existentes, XML builder, constante de caché, y un link en JSX.

## Boundaries & Constraints

**Always:**
- Mantener la firma externa de `BuildSitemapIndexXml()` y `BuildUrlSetXml()` — son helpers reutilizados.
- El namespace XML de Google News (`xmlns:news`) solo va en `sitemap-noticias-*.xml`, no en otros sitemaps.
- `GetVisibleStaticRoutes` sigue recibiendo `SitemapVisibility` — solo cambia su tipo de retorno.
- En PublicLayout.tsx el nuevo link `Plataforma` sigue el mismo patrón CSS de los links existentes.

**Ask First:**
- Si `SeoMetadata` no expone `UpdatedAt` en el objeto `SitemapVisibility`, preguntar antes de añadir un campo nuevo al DTO.
- Si al expandir llms.txt se detecta que `GetMetaForPathAsync` falla en alguna ruta nueva (retorna null/vacío), preguntar qué fallback usar.

**Never:**
- No cambiar la lógica de filtrado de rutas noindex (`LoadSitemapVisibilityAsync`).
- No agregar nuevas rutas a `StaticRoutes[]` — eso es scope separado.
- No tocar `sitemap-fibras.xml` ni `sitemap-static.xml` más allá del lastmod.
- No modificar el disclaimer legal del footer.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Ruta estática con `UpdatedAt` en DB | `SeoMetadata.UpdatedAt = 2025-03-15` | lastmod=`2025-03-15` en sitemap-static.xml | — |
| Ruta estática sin `UpdatedAt` (null) | `SeoMetadata.UpdatedAt = null` | lastmod=`2024-01-01` (fecha de lanzamiento del sitio como fallback) | — |
| FIBRAs en sitemap-fibras.xml | Precio actualizado diariamente | lastmod = hoy (FIBRAs cambian daily, es correcto) | — |
| Noticia en sitemap-noticias con extensión news | `article.Slug`, `article.PublishedAt` | `<news:news><news:publication>...</news:publication><news:publication_date>ISO8601</news:publication_date><news:title>...</news:title></news:news>` | Si título vacío, omitir `<news:title>` |
| llms.txt cacheo | Cualquier request | `Cache-Control: public, max-age=86400` | — |
| Footer nav | Usuario en cualquier página pública | Muestra: © [año] Fibras Inmobiliarias (→ /) · Plataforma (→ /plataforma) · Contacto · Aviso de privacidad | — |

</frozen-after-approval>

## Code Map

- `src/Server/Api/Endpoints/SeoEndpoints.cs:386` — `GetGeneratedLastMod()` → devuelve hoy siempre; cambiar para aceptar `DateTimeOffset?`
- `src/Server/Api/Endpoints/SeoEndpoints.cs:333` — `GetVisibleStaticRoutes(SitemapVisibility)` → cambiar retorno a `IReadOnlyList<(string path, string lastMod)>`
- `src/Server/Api/Endpoints/SeoEndpoints.cs:114` — endpoint `sitemap-noticias-{page}.xml` → añadir namespace `xmlns:news` y campos `<news:news>`
- `src/Server/Api/Endpoints/SeoEndpoints.cs:157` — endpoint `llms.txt` → cambiar `max-age=3600` a `max-age=86400`, añadir rutas faltantes
- `src/Server/Api/Endpoints/SeoEndpoints.cs:201` — método `BuildSitemapXml()` → eliminar (dead code sin binding HTTP)
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx:414` — copyright Link `to="/plataforma"` → cambiar a `to="/"`, añadir nuevo Link `to="/plataforma"` con texto "Plataforma"

## Tasks & Acceptance

**Execution:**

- [ ] `src/Server/Api/Endpoints/SeoEndpoints.cs` -- Cambiar `GetGeneratedLastMod()` para aceptar `DateTimeOffset? updatedAt` y devolver `updatedAt?.ToString("yyyy-MM-dd") ?? "2024-01-01"` -- las rutas estáticas que raramente cambian deben tener lastmod real, no hoy
- [ ] `src/Server/Api/Endpoints/SeoEndpoints.cs` -- Cambiar `GetVisibleStaticRoutes` para retornar `IReadOnlyList<(string path, string lastMod)>` usando `SeoMetadata.UpdatedAt` si está disponible en `SitemapVisibility`; actualizar todos los call sites del retorno -- eliminar el lastmod falso en sitemap-static.xml
- [ ] `src/Server/Api/Endpoints/SeoEndpoints.cs` -- En el endpoint `sitemap-noticias-{page}.xml`: añadir `xmlns:news="http://www.google.com/schemas/sitemap-news/0.9"` al `<urlset>` y para cada artículo añadir `<news:news><news:publication><news:name>Fibras Inmobiliarias</news:name><news:language>es</news:language></news:publication><news:publication_date>{article.PublishedAt:O}</news:publication_date><news:title>{article.Title}</news:title></news:news>` -- habilita elegibilidad en Google News
- [ ] `src/Server/Api/Endpoints/SeoEndpoints.cs` -- En endpoint `llms.txt`: cambiar `max-age=3600` a `max-age=86400` en el header `Cache-Control`; añadir a las rutas hardcodeadas: `/acerca`, `/portafolio`, `/calculadora`, `/calendario` -- mejora caché y superficie indexable por LLM crawlers
- [ ] `src/Server/Api/Endpoints/SeoEndpoints.cs` -- Eliminar el método `BuildSitemapXml()` (línea ~201) -- dead code sin binding HTTP, genera confusión sobre la arquitectura de sitemaps
- [ ] `src/Web/Main/src/shared/layouts/PublicLayout.tsx` -- En el copyright: cambiar `to="/plataforma"` a `to="/"` en el Link de "Fibras Inmobiliarias"; añadir un nuevo `<Link to="/plataforma">Plataforma</Link>` en la misma fila de links del footer, usando el mismo className de los otros links -- copyright debe apuntar al home; /plataforma conserva su único enlace interno

**Acceptance Criteria:**

- Dado que `sitemap-static.xml` se genera, cuando una ruta tiene `SeoMetadata.UpdatedAt = 2025-03-15`, entonces el XML contiene `<lastmod>2025-03-15</lastmod>` (no la fecha de hoy)
- Dado que `sitemap-noticias-1.xml` se genera, cuando el XML se valida, entonces contiene el namespace `xmlns:news` y cada `<url>` incluye el bloque `<news:news>` con `<news:publication_date>` e `<news:title>`
- Dado que se solicita `/llms.txt`, cuando se inspecciona la respuesta, entonces el header `Cache-Control` contiene `max-age=86400` y el body incluye `/acerca`, `/portafolio`, `/calculadora`, `/calendario`
- Dado que el footer se renderiza, cuando el usuario hace clic en "Fibras Inmobiliarias" del copyright, entonces navega a `/`; cuando hace clic en "Plataforma", entonces navega a `/plataforma`
- Dado el build del backend, cuando compila sin warnings de referencia a `BuildSitemapXml`, entonces el método ha sido eliminado correctamente

## Design Notes

**lastmod en rutas estáticas:** `SitemapVisibility` puede no exponer `UpdatedAt` por ruta hoy. Si no está disponible, la solución mínima es pasar `null` → fallback `"2024-01-01"`. Esto sigue siendo correcto: una fecha fija y pasada señala a Google que la página es estable, sin engañarlo con "modificada hoy".

**Google News namespace:** El campo `<news:title>` debe escapar XML (usar `SecurityElement.Escape()` o el equivalente de `XElement`). Si el artículo no tiene título, omitir `<news:title>` antes que emitir vacío.

**BuildSitemapXml dead code:** Verificar antes de eliminar que no haya referencias en tests. Si hay tests que lo usan directamente, eliminarlos también.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: 0 errors, 0 warnings nuevos
- `dotnet test` -- expected: suite verde

**Manual checks:**
- GET `/sitemap-static.xml` → `<lastmod>` no es la fecha de hoy en rutas estables como `/privacidad`
- GET `/sitemap-noticias-1.xml` → XML contiene `xmlns:news` y bloque `<news:news>` en cada `<url>`
- GET `/llms.txt` → response header `Cache-Control: public, max-age=86400`; body incluye `/acerca` y `/calculadora`
- Frontend: footer muestra "Plataforma" como link separado; click en "Fibras Inmobiliarias" del copyright va a `/`
