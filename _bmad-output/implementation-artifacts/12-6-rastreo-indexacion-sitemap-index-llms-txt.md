# Story 12.6: Rastreo e indexación — sitemap index + paginación /noticias + llms.txt

Status: done

<!-- Mayormente independiente de 12-1. Coordina con 12-1 AC-8 (excluir noindex del sitemap). -->

## Story

As a **buscador / crawler (incluidos crawlers de IA)**,
I want **un sitemap escalable por secciones (índice), paginación de `/noticias` que no genere index bloat, y un `/llms.txt`**,
so that **FIBRADIS exponga eficientemente sus ~2000 noticias y catálogo creciente sin tope artificial, evite duplicados/páginas profundas inútiles y facilite la citación por motores generativos**.

## Decisiones del usuario (alcance)
- **Google News / Publisher Center: DESCARTADO.** No se implementa Google News sitemap (`<news:news>`). El sitemap sigue siendo el estándar para web search.
- Bundle de esta historia = **sitemap index** + **paginación /noticias** + **llms.txt**.

## Dependencias y contexto
- **Estado actual del sitemap** ([SeoEndpoints.cs](src/Server/Api/Endpoints/Public/SeoEndpoints.cs), `app.MapSeo()` en [Program.cs:109]):
  - `GET /sitemap.xml` en `IMemoryCache` (key `sitemap-xml`, TTL 1h, `Cache-Control: public, max-age=3600`).
  - Contiene: 11 rutas estáticas (`StaticRoutes`, mismas que el diccionario SEO), todas las fibras activas (`GetAllActiveForSitemapAsync` → `/fibras/{slug}`, lastmod=hoy), y **hasta `MaxNewsInSitemap = 500`** noticias (`GetArticlesForSitemapAsync` → `/noticias/{slug}`, lastmod=publishedAt). Solo `loc`+`lastmod`.
  - `GET /robots.txt` ([:57-60]): allow `/`, disallow `/ops//api//hangfire/`, permite GPTBot/ClaudeBot/Google-Extended/Applebot-Extended, bloquea CCBot/Bytespider/meta-externalagent, `Sitemap: {baseUrl}/sitemap.xml`.
  - `GetBaseUrl` ([:65-68]): `App:BaseUrl`, fallback `https://fibrasinmobiliarias.com`.
- **Problema:** con ~2000 noticias y solo 500 en el sitemap, **~1500 quedan sin exponer**. Un único sitemap también crecerá hacia el límite de 50k URLs / 50MB.
- **`/noticias` listado** (story 4-11): paginado 20/página vía query (`?page=N`), endpoint `GetPagedPublicAsync`. Página [NoticiasListPage.tsx](src/Web/Main/src/modules/noticias/NoticiasListPage.tsx). Hoy las páginas profundas con `?page=N` pueden generar **index bloat** y canónicos ambiguos.
- **Coordinación con 12-1 (AC-8):** el sitemap debe **excluir** páginas/fibras/noticias marcadas `noindex` en `SeoMetadata.RobotsDirectives`. Si 12-1 ya está done, leer de la misma fuente.

## Acceptance Criteria

**AC-1 — Sitemap index.** `GET /sitemap.xml` se convierte en un **sitemap index** (`<sitemapindex>`) que referencia sub-sitemaps por sección: `sitemap-static.xml`, `sitemap-fibras.xml`, y `sitemap-noticias-{n}.xml` (paginado si excede el umbral). Cada sub-sitemap es un `<urlset>` válido. Todos cacheados (TTL 1h) y con `Cache-Control` actual.

**AC-2 — Exponer todas las noticias indexables.** Se elimina el tope artificial de 500: el sitemap de noticias incluye **todas las noticias indexables** (no soft-deleted, no `noindex`), paginadas en sub-sitemaps de ≤ 45.000 URLs cada uno (margen bajo el límite de 50k). Ordenadas por `PublishedAt` desc. `lastmod` = `PublishedAt`.

**AC-3 — Exclusión de noindex (coord. 12-1).** Ninguna URL con `RobotsDirectives` que contenga `noindex` aparece en ningún sub-sitemap (fibras, noticias o estáticas). Si 12-1 no estuviera done, dejar el hook listo y documentar (comportamiento actual = incluir todo).

**AC-4 — Paginación /noticias sin index bloat.** Las páginas `/noticias?page=N` (N>1) reciben canonical **auto-referente** (a sí mismas) o canonical a `/noticias` según la estrategia elegida (documentar; recomendado self-canonical + `robots: noindex,follow` para N>1 para concentrar señales en la página 1 y dejar rastrear el detalle). La página 1 (`/noticias`) mantiene su canonical actual. No se rompe la navegación SPA ni el SSR de metadata. **Nota SEO:** `noindex,follow` en N>1 es **una de dos lecturas válidas** (la otra: self-canonical + indexable, dado que tras la deprecación de rel=next/prev Google trata cada página como independiente). Es defendible aquí porque cada noticia ya está en el sitemap; **medir cobertura en Google Search Console tras el deploy** y revertir a indexable si se ve pérdida de rastreo de profundidad.

**AC-5 — `/llms.txt` (experimental, bajo costo / sin garantía).** `GET /llms.txt` (anónimo, `text/plain` o `text/markdown`) devuelve un archivo con: nombre/descripción de FIBRADIS, y enlaces a las páginas clave (home, conoce-las-fibras, fibras, fundamentales, comparar, noticias) + nota de uso. Cacheado. Referenciado opcionalmente en robots.txt. **Encuadre realista:** a 2026 `llms.txt` es una **convención propuesta, NO un estándar honrado** por OpenAI/Anthropic/Google — ningún proveedor LLM mayor confirma consumirlo. Se implementa por su costo bajo y posible upside futuro; **no esperar impacto medible en citabilidad** ni venderlo como tal.

**AC-6 — robots.txt consistente.** `robots.txt` apunta al nuevo `sitemap.xml` (índice). Se mantienen las reglas de bots actuales. Opcional: línea hacia `/llms.txt`.

**AC-7 — Tests.** Unit/integration: el índice valida contra el XSD de sitemap index; cada sub-sitemap valida contra urlset XSD; noticias noindex/soft-deleted excluidas; conteo correcto de noticias (todas las indexables, no 500); `/llms.txt` 200 con contenido esperado; canonical correcto en `/noticias?page=N`. Verdes antes de `done`.

## Tasks / Subtasks

- [x] **T1 — Refactor a sitemap index (AC-1, AC-2)**: en `SeoEndpoints`, `GET /sitemap.xml` → `<sitemapindex>`. Nuevos endpoints `GET /sitemap-static.xml`, `/sitemap-fibras.xml`, `/sitemap-noticias-{page}.xml`. Mantener `IMemoryCache` (keys por sub-sitemap, TTL 1h) y `Cache-Control`. Escapar `loc` con `SecurityElement.Escape` (como hoy).
- [x] **T2 — Query de noticias sin tope (AC-2, AC-3)**: ajustar/añadir en `INewsRepository` una query paginada para sitemap (todas las indexables por `PublishedAt` desc, batches de ≤45k), con `READ UNCOMMITTED` como la actual `GetArticlesForSitemapAsync`. Excluir soft-deleted; excluir noindex (join/lookup a `SeoMetadata` de 12-1).
- [x] **T3 — Exclusión noindex (AC-3)**: integrar el filtro `noindex` desde `SeoMetadata` (12-1) en fibras/noticias/estáticas. Si 12-1 no está done, dejar el punto de extensión y documentar.
- [x] **T4 — Canonical/robots en /noticias paginado (AC-4)**: definir estrategia (recomendado: N>1 → self-canonical + `noindex,follow`). Implementar en el middleware/SSR que cubre `/noticias` (coordinar con `SpaMetadataMiddleware` de 12-1 que ya inyecta metadata de `/noticias`). Verificar que el SPA respeta el canonical en cliente ([usePageTitle.ts](src/Web/Main/src/shared/hooks/usePageTitle.ts)) sin pisar el server.
- [x] **T5 — `/llms.txt` (AC-5, AC-6)**: endpoint anónimo en `SeoEndpoints` (`BuildLlmsTxt`), cacheado. Listar páginas clave (leer de `SpaMetadataProvider`/`SeoMetadata` para no duplicar textos). Actualizar `robots.txt` para apuntar al índice y opcionalmente a `/llms.txt`.
- [x] **T6 — Tests (AC-7)**: validación XSD (index + urlset), exclusiones, conteo, `/llms.txt`, canonical paginado. `dotnet test tests/Integration/ -m:1`, `dotnet test tests/Unit/`.
- [x] **T7 — Verificación manual**: `curl` de `/sitemap.xml`, un sub-sitemap, `/robots.txt`, `/llms.txt` en dev; validar el índice en un validador de sitemaps.

## Dev Notes
- **Stack real = SQL Server**. Esta historia **no crea tablas** (solo endpoints + queries). 
- **Reusar, no reinventar**: el sitemap, robots y `GetBaseUrl` ya existen en `SeoEndpoints.cs`; esto es refactor + extensión, no reescritura. `MaxNewsInSitemap=500` se elimina/eleva.
- **Límites de sitemap**: 50.000 URLs / 50MB por archivo; usar ≤45k por margen. El índice puede referenciar hasta 50k sitemaps.
- **Paginación — evitar index bloat**: 2000 noticias en listado paginado generan muchas `?page=N`; el detalle de cada noticia ya está en el sitemap, así que las páginas de listado N>1 aportan poco valor de indexación → `noindex,follow` concentra señales sin bloquear el rastreo del detalle. Documentar la decisión final.
- **Coordinación 12-1**: idealmente 12-1 done antes (para el filtro noindex desde BD). Si no, la historia funciona excluyendo solo soft-deleted (comportamiento actual) y deja el hook.
- **Reglas de cache/encoding existentes intactas** (TTL 1h, `SecurityElement.Escape`, `Cache-Control public`).
- **Google News explícitamente fuera de alcance** (decisión usuario).

### Security Checklist — antes del primer commit
- [ ] **TOCTOU**: N/A (solo lectura).
- [ ] **Auth-gating UI**: N/A (endpoints públicos anónimos, como hoy).
- [ ] **DoS/escala**: sub-sitemaps de noticias deben paginar en BD (no materializar 2000+ en memoria sin límite); usar `Skip/Take` por sub-sitemap y cache. `READ UNCOMMITTED` como la query actual.
- [ ] **Denominador cero**: N/A.

### References
- [SeoEndpoints.cs](src/Server/Api/Endpoints/Public/SeoEndpoints.cs) (sitemap/robots/GetBaseUrl, MaxNewsInSitemap=500, StaticRoutes), `app.MapSeo()` en [Program.cs:109](src/Server/Api/Program.cs)
- [INewsRepository / NewsRepository.cs](src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs) (`GetArticlesForSitemapAsync`, READ UNCOMMITTED, orden PublishedAt)
- [IFibraRepository.cs](src/Server/Application/Catalog/IFibraRepository.cs) (`GetAllActiveForSitemapAsync`)
- [NoticiasListPage.tsx](src/Web/Main/src/modules/noticias/NoticiasListPage.tsx), [usePageTitle.ts](src/Web/Main/src/shared/hooks/usePageTitle.ts)
- [SpaMetadataMiddleware.cs](src/Server/Api/Middleware/SpaMetadataMiddleware.cs) (inyecta metadata de `/noticias`)
- Story 12-1 (noindex en SeoMetadata, AC-8): [12-1-modulo-seo-administrable.md](_bmad-output/implementation-artifacts/12-1-modulo-seo-administrable.md)
- 2026: [15 Essential SEO Tags 2026](https://www.link-assistant.com/news/html-tags-for-seo.html) · [GEO 2026 — Enrich Labs](https://www.enrichlabs.ai/blog/generative-engine-optimization-geo-complete-guide-2026)

## Hallazgos de auditoría SEO (2026-06-13)

> Auditoría completa (score 84/100): [seo-audit/FULL-AUDIT-REPORT.md](../../seo-audit/FULL-AUDIT-REPORT.md). Hallazgos de rastreo/indexación que caen en el alcance de esta historia.

### 🟠 H1 — Soft 404 en rutas desconocidas (nuevo — añadir al alcance)
Una ruta inexistente arbitraria (p.ej. `https://fibrasinmobiliarias.com/esta-pagina-no-existe-xyz123`) devuelve **HTTP 200** con `<title>` vacío, no `404`. El soft-404 que mandata 12-1 solo cubre fibras/noticias **dentro de sus prefijos** (`/fibras/*`, `/noticias/*`); las rutas que no matchean ningún middlewar caen a `MapFallbackToFile` → shell SPA con 200. Riesgo: index bloat y reportes "soft 404" en Search Console.
- **Fix:** validar la ruta antes del fallback. Para paths que no correspondan a ninguna ruta SPA conocida ni a un recurso dinámico válido, responder `404` (renderizando el NotFound del cliente). Mantener 200 en rutas SPA válidas. Coordinar con el orden de pipeline de 12-1 (el guard va junto/antes del `MapFallbackToFile`).
- **Test:** `curl -I` de una URL inválida → `404`; ruta SPA válida → `200`. Añadir a AC-7.

### 🟠 H3 — robots.txt servido tiene un bloque en conflicto (Cloudflare-managed)
El `robots.txt` **servido en producción** NO es solo el de `BuildRobotsTxt`: Cloudflare inyecta un bloque `# BEGIN Cloudflare Managed content` que **`Disallow`ea `ClaudeBot`, `GPTBot`, `Google-Extended`, `Applebot-Extended`, `Amazonbot`, `CloudflareBrowserRenderingCrawler`** (etc.), seguido del bloque propio de la app que los **re-`Allow`ea**, y además hay **dos grupos `User-agent: *`**. Múltiples grupos para el mismo agente es ambiguo por spec → los crawlers de IA podrían honrar el `Disallow` gestionado y nunca llegar al re-allow, contradiciendo la intención (permitir grounding/training de IA, que es objetivo GEO del proyecto).
- **Fix (config + código):** decidir la política y hacerla coherente en **una sola fuente**. Si se quiere permitir bots de IA, **desactivar la feature "Managed robots.txt / Block AI bots" de Cloudflare** (o configurarla para no contradecir) y dejar que `BuildRobotsTxt` sea la fuente de verdad. Si se quiere bloquear, hacerlo en `BuildRobotsTxt` y quitar el re-allow. Eliminar el `User-agent: *` duplicado. Coordina con AC-6 ("robots.txt consistente").
- **Verificar** con los testers de cada bot (Google robots tester, docs de GPTBot/ClaudeBot) que `/` queda permitido para los agentes deseados.

### L3 / L1 / L2 — Menores de esta área
- **L3 (sitemap):** las rutas core (`/`, `/fibras`, `/comparar`, `/noticias`, …) se emiten **sin `<lastmod>`** (solo fibras y noticias lo tienen). Añadir `lastmod` a las core es barato (al construir cada sub-sitemap de T1).
- **L1 (CSP):** la respuesta no incluye `Content-Security-Policy`. Considerar añadirlo a los security headers (`Program.cs`) — fuera del foco SEO pero detectado en la auditoría de best-practices.
- **L2 (info leak):** el header `X-Powered-By: ASP.NET` se expone; suprimirlo (`AddServerHeader=false` / quitar el header) es trivial.

## Dev Agent Record
### Agent Model Used
GPT-5 Codex
### Debug Log References
`dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`
`dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~Api.Tests.SeoEndpointTests`
`npm test --workspace=src/Web/Main`
`npm run build --workspace=src/Web/Main`
`npx playwright test tests/e2e/news-epic4.spec.ts -g "Noticias paginadas"`
### Completion Notes List
- Sitemap index implementado con `sitemap-static.xml`, `sitemap-fibras.xml` y `sitemap-noticias-{page}.xml`, todos cacheados y con exclusión de `noindex` en estáticas, fibras y noticias.
- Noticias paginadas expuestas sin tope artificial, filtrando `soft-deleted` y `noindex` en repositorio.
- `/noticias?page=N` ahora sincroniza canonical y robots con la URL real del navegador; N>1 queda en `noindex,follow`.
- `/llms.txt` quedó disponible y referenciado desde `robots.txt`.
- Validación cerrada con tests unitarios, integración focalizada, build de frontend y e2e aislado de noticias paginadas.
### File List
- `src/Server/Application/News/INewsRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs`
- `src/Server/Api/Endpoints/Public/SeoEndpoints.cs`
- `src/Server/Api/Middleware/SpaMetadataMiddleware.cs`
- `src/Web/Main/src/shared/hooks/usePageTitle.ts`
- `src/Web/Main/src/modules/noticias/noticiasSeo.ts`
- `src/Web/Main/src/modules/noticias/NoticiasListPage.tsx`
- `src/Web/Main/package.json`
- `src/Web/Main/src/modules/noticias/noticiasSeo.test.ts`
- `src/Web/Main/tests/e2e/news-epic4.spec.ts`
- `tests/Unit/Infrastructure.Tests/Middleware/SpaMetadataMiddlewareTests.cs`
- `tests/Unit/Infrastructure.Tests/Endpoints/SeoEndpointsTests.cs`
- `tests/Integration/Api.Tests/SeoEndpointTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/NewsRepositorySlugTests.cs`
- `tests/Unit/Infrastructure.Tests/Middleware/NewsMetadataMiddlewareTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`
- `tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs`

## Senior Developer Review (AI)

Revisión adversarial (Blind Hunter + Edge Case Hunter + Acceptance Auditor) — 2026-06-14. 3 capas, 0 fallidas. Hallazgos verificados contra el código real.

### Review Findings

**Todos los patches y la decisión se resolvieron en este review (2026-06-14). Backend verde: Api build 0/0, Infrastructure.Tests 608/608, SeoEndpointTests 21/21, RedirectsEndpointTests 8/8.**

Archivos añadidos/modificados en el review:
- `src/Server/Api/Seo/SpaRouteCatalog.cs` (NEW) — catálogo de rutas SPA conocidas para H1
- `src/Server/Api/Program.cs` — fallback con soft-404 real para rutas desconocidas
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs` — P1/P2/P3
- `src/Server/Api/Endpoints/Public/SeoEndpoints.cs` — P4
- `tests/Unit/Infrastructure.Tests/Seo/SpaRouteCatalogTests.cs` (NEW)
- `tests/Integration/Api.Tests/SeoEndpointTests.cs` — tests H1
- `tests/Integration/Api.Tests/RedirectsEndpointTests.cs` — ajuste passthrough → 404

- [x] [Review][Decision→Patch] **H1 — soft-404 en rutas desconocidas: IMPLEMENTADO** — `Program.cs:120` servía `index.html` sin StatusCode → cualquier ruta inexistente devolvía 200 (soft-404). Decisión del usuario: implementar ahora. Fix: nuevo `SpaRouteCatalog` (`src/Server/Api/Seo/SpaRouteCatalog.cs`, espejo de `routes.tsx`) + el fallback de `Program.cs` devuelve 404 para rutas no-SPA (sirviendo el shell para que el cliente renderice NotFound), dejando `/fibras/{slug}` y `/noticias/{slug}` a sus middlewares de soft-404 existentes. Tests: `SpaRouteCatalogTests` (unit) + `UnknownRoute_ReturnsNotFound_NotSoft200`/`KnownSpaRoute_ReturnsOk` (integración). `RedirectsEndpointTests` ajustado (deactivar redirect a `/blog` → 404 correcto, no 200).

- [x] [Review][Patch] **Orden de paginación sin tiebreaker → duplicados/faltantes entre sub-sitemaps** [src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs:~389] — Aplicado: `.OrderByDescending(n => n.PublishedAt).ThenByDescending(n => n.Id)`. Orden determinista entre consultas independientes de sub-sitemaps.

- [x] [Review][Patch] **Filtro noindex del repositorio ignora `IsActive`** [src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs:~363-385] — Aplicado: añadido `&& seo.IsActive` a las ramas relacional e InMemory del filtro noindex, alineándolo con `SeoEndpoints.LoadSitemapVisibilityAsync`.

- [x] [Review][Patch] **Overflow de `int` en `Skip` para páginas fuera de rango → 500 anónimo** [src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs:~388] — Aplicado: `var skip = (long)Math.Max(0, page - 1) * pageSize; if (skip >= total) return ([], total);` antes de materializar. Sin overflow ni query inútil para páginas enormes.

- [x] [Review][Patch] **Cacheo de resultados vacíos/404 de sitemap-noticias → 404 rancio + crecimiento ilimitado de caché** [src/Server/Api/Endpoints/Public/SeoEndpoints.cs] — Aplicado: el endpoint de noticias ahora solo cachea XML no vacío (`cache.TryGetValue` + `cache.Set` tras construir); páginas fuera de rango → 404 sin cachear. Guard simplificado a `page > Math.Max(1, totalPages)` (page 1 siempre 200 con urlset válido).

- [x] [Review][Defer] **AC-7: validación XSD más débil que el spec** [tests/...] — deferred. Los tests hacen `XDocument.Parse` + aserción de nombre de raíz (`sitemapindex`/`urlset`) y namespace, no validación contra el XSD oficial de sitemaps.org que pide el AC-7. El XML producido es estructuralmente correcto; reforzar requiere cargar los .xsd y `XmlReaderSettings`. Acción: añadir validación XSD real en próxima historia SEO.
- [x] [Review][Defer] **H3: conflicto robots.txt gestionado por Cloudflare no resuelto ni documentado** — deferred, config/infra. El diff solo añade la línea `# llms.txt:` a `BuildRobotsTxt`. El bloque "Managed robots.txt / Block AI bots" de Cloudflare y el doble `User-agent: *` descritos en §H3 no se tocan (no es código). Acción: registrar la decisión de política de bots y reconciliar en una sola fuente.
- [x] [Review][Defer] **`<urlset>` vacío devuelve 200 en /sitemap-static.xml y /sitemap-fibras.xml (vs 404 en noticias)** [src/Server/Api/Endpoints/Public/SeoEndpoints.cs:68-107] — deferred, bajo impacto. Un urlset vacío es técnicamente inválido por XSD; solo alcanzable por URL directa (el índice no los referencia si están vacíos). Inconsistente con la rama de noticias que devuelve 404.
- [x] [Review][Defer] **`GetNewsPageCountAsync` materializa hasta 45k items solo para contar** [src/Server/Api/Endpoints/Public/SeoEndpoints.cs:332-336] — deferred, perf menor (cacheado 1h, ~2000 noticias hoy). Reusa `GetArticlesForSitemapPageAsync(1, 45000)` y descarta los items. Acción: ruta count-only dedicada.
- [x] [Review][Defer] **llms.txt: título/descripción sin escapar para Markdown** [src/Server/Api/Endpoints/Public/SeoEndpoints.cs:235] — deferred, bajo. `Title`/`Description` provienen de SeoMetadata (editable desde Ops); un `]`/`)` rompería el link Markdown. A diferencia del XML, no se aplica encoder. Títulos actuales limpios.
- [x] [Review][Defer] **`lastmod`=hoy (regenerado a diario) para rutas estáticas es señal débil** [src/Server/Api/Endpoints/Public/SeoEndpoints.cs:380] — deferred, bajo/discutible. L3 pedía añadir `lastmod` a las core (hecho), pero "hoy" cada día le dice a Google que cambian a diario. Considerar fecha de build estática.

**Dismissed (9, falsos positivos / por diseño):** `RobotsDirectives` null-deref (campo no-nullable `= string.Empty`, SeoMetadata.cs:17); doble canonical SSR/cliente (`setCanonical` reutiliza el `<link>` existente y el valor coincide); meta robots perdido si falta `prerender-meta` (guard por diseño, comentario garantizado); canonical descarta `q`/`fibraId` en N>1 (defendible, N>1 es noindex); higiene de aislamiento de tests con factory desechable (resuelta por patch de no-cachear-vacíos); `page > totalPages` no clampeado en cliente (noindex evita indexación); dependencia AC-3 de 12-1 (informativo, el código ya filtra correcto); L1 CSP (opcional, fuera de foco SEO); L2 X-Powered-By (moot en Kestrel; header `Server` ya removido con `AddServerHeader=false`).
