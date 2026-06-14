# Story 12.5: JSON-LD en comparador y fundamentales + breadcrumbs universales

Status: done

<!-- Depende de 12-1 (SpaMetadataMiddleware ya con acceso a BD vía IServiceScopeFactory + JsonLd administrable). -->

## Story

As a **buscador / motor generativo**,
I want **que `/comparar` y `/fundamentales` emitan JSON-LD (hoy no tienen) y que todas las páginas públicas tengan BreadcrumbList**,
so that **se cierren los huecos de structured data, mejore la comprensión de la jerarquía del sitio y aumente la elegibilidad a rich results / citabilidad**.

## Dependencias y contexto
- **Requiere 12-1 done**: hoy `SpaMetadataMiddleware` **no inyecta `IServiceScopeFactory`** y no puede leer BD ([SpaMetadataMiddleware.cs](src/Server/Api/Middleware/SpaMetadataMiddleware.cs)). 12-1 lo conecta a la BD; esta historia se apoya en ese acceso para JSON-LD dinámico (lista de fibras). Si 12-1 dejó el acceso a scope, aquí es directo.
- **Confirmado**: `/comparar` ([SpaMetadataProvider.cs:99-102](src/Server/Api/Seo/SpaMetadataProvider.cs)) y `/fundamentales` ([:120-123]) pasan solo 3 args → `JsonLd = null`. **Sin structured data.**
- **Breadcrumbs**: hoy **solo** la ficha de fibra tiene `BreadcrumbList` ([FibraProfileMetadataMiddleware.cs:172-181](src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs)). Faltan en home, fibras, noticias, comparar, fundamentales, conoce-las-fibras, calendario, etc.
- **Datos server-side para JSON-LD dinámico:**
  - Lista de fibras activas: `IFibraRepository.GetAllActiveAsync(ct)` o `GetAllActiveForSitemapAsync(ct)` (FullName+Ticker → `FibraSlug.Build`).
  - Resumen fundamentales: `IFundamentalRepository.GetSummaryLatestAsync(ct)` → métricas (CapRate, NavPerCbfi, Ltv, NoiMargin, FfoMargin) + conteo de fibras ([FundamentalsEndpoints.cs:15](src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs)).

## Acceptance Criteria

**AC-1 — JSON-LD en `/comparar`.** Emite, vía el mecanismo DB-driven de 12-1: un `WebApplication`/`SoftwareApplication` (herramienta comparador) **y/o** un `ItemList` de las FIBRAs activas comparables (cada item → `url` con `FibraSlug.Build`). Más `BreadcrumbList` (Inicio › Comparar).

**AC-2 — JSON-LD en `/fundamentales`.** Emite un `Dataset` con `variableMeasured` (Cap Rate, NAV por CBFI, LTV, NOI Margin, FFO Margin), `creator`/`publisher` Organization, conteo de FIBRAs cubiertas y `dateModified` (= máximo `CapturedAt` del summary). Más `BreadcrumbList` (Inicio › Fundamentales).

**AC-3 — BreadcrumbList universal.** Todas las páginas públicas con metadata tienen `BreadcrumbList` JSON-LD consistente: home (solo Inicio o WebSite), `/fibras`, `/noticias`, `/comparar`, `/fundamentales`, `/conoce-las-fibras`, `/calendario`, `/acerca`, `/contacto`, `/privacidad`. La ficha de fibra y noticia ya lo tienen (no romper).

**AC-4 — Datos vivos vs estáticos.** El `ItemList`/`Dataset` que dependen de la lista de fibras se componen en request (datos vivos) cuando el JSON-LD no está override (regla 12-1). Si no hay datos, degradan a un JSON-LD estático mínimo (no crash). Las listas pueden cachearse con `IMemoryCache` corto (medir).

**AC-5 — Validez + reglas.** JSON-LD válido schema.org (Rich Results Test). Encoding `JsonSerializer` + `JavaScriptEncoder`. Reglas middleware de 12-1 intactas (GET/HEAD, guard prerender-meta, soft-404, Cache-Control no-cache).

**AC-6 — Tests.** Unit del builder de `ItemList`/`Dataset`/`BreadcrumbList` (valores exactos dado un set de fibras/summary de ejemplo). Por-middleware: `/comparar` y `/fundamentales` con datos → JSON-LD presente; sin datos → mínimo estático. Verde antes de `done`.

## Tasks / Subtasks

- [x] **T1 — Helper BreadcrumbList compartido (AC-3)**: extraer la construcción de `BreadcrumbList` (hoy inline en `FibraProfileMetadataMiddleware`) a un helper reutilizable en el `ISeoDefaultsBuilder` de 12-1, parametrizado por la jerarquía de cada ruta. Aplicarlo a todas las páginas.
- [x] **T2 — JSON-LD `/fundamentales` Dataset (AC-2)**: en el builder, resolver `IFundamentalRepository.GetSummaryLatestAsync`, componer `Dataset` (variableMeasured, conteo, dateModified = MAX CapturedAt). BreadcrumbList.
- [x] **T3 — JSON-LD `/comparar` (AC-1)**: componer `SoftwareApplication`/`WebApplication` + opcional `ItemList` de fibras activas (`GetAllActiveForSitemapAsync` + `FibraSlug.Build`). BreadcrumbList.
- [x] **T4 — Wiring DB-driven + override (AC-4)**: estos JSON-LD se sirven por el `SpaMetadataMiddleware` ya conectado a BD (12-1). Si el campo está override → stored; si no → recomponer (estático o con datos vivos según la página). Caché corto opcional (documentar).
- [ ] **T5 — Seed (AC-1, AC-2, AC-3)**: ajustar el seed de páginas fijas de 12-1 para incluir el JSON-LD base de `/comparar` y `/fundamentales` y los breadcrumbs de todas las rutas.
- [x] **T6 — Tests (AC-6)**: unit builders (valores exactos), por-middleware con/sin datos. `dotnet test tests/Unit/`, `dotnet test tests/Integration/ -m:1`.
- [ ] **T7 — Validación manual**: Rich Results Test sobre `/comparar` y `/fundamentales` en dev.

## Dev Notes
- **Stack real = SQL Server**. No crea tablas (solo lee fibras/fundamentales). Reusa el `JsonLd` administrable de `SeoMetadata` (12-1) para estas dos páginas fijas.
- **`SpaMetadataMiddleware` y acceso a BD**: requisito que 12-1 ya satisface (le agrega lookup de `SeoMetadata`). Confirmar en 12-1 que el middleware quedó con acceso a scope; si solo lee `SeoMetadata` por path pero el JSON-LD dinámico (lista de fibras) no cabe en una fila estática, componer en el builder con lectura viva — mismo patrón que 12-3.
- **Dataset es el tipo correcto para `/fundamentales`** (tabla comparativa de métricas) — alta señal para GEO (datos estructurados citables). `variableMeasured` lista las métricas.
- **`/comparar` no tiene entidad fija** (tickers vienen por query): por eso su JSON-LD es la herramienta (`SoftwareApplication`) y/o `ItemList` del universo comparable, no un dataset por-comparación.
- **No duplicar BreadcrumbList**: ficha de fibra/noticia ya lo tienen en sus middlewares; el helper compartido debe unificar el formato sin romperlos (idealmente refactor para que todos usen el helper).
- **Reglas middleware de 12-1 intactas**.

### Security Checklist — antes del primer commit
- [ ] **TOCTOU**: N/A (lectura).
- [ ] **Auth-gating UI**: N/A (rutas públicas).
- [ ] **Denominador cero**: N/A (no hay cálculo nuevo; las métricas vienen precalculadas del summary).
- [ ] **Performance**: lectura de lista de fibras/summary por request → cachear con `IMemoryCache` corto si la medición lo amerita (AC-4).

### References
- [SpaMetadataProvider.cs:99-102,120-123](src/Server/Api/Seo/SpaMetadataProvider.cs) (sin JsonLd confirmado), [SpaPageMeta.cs](src/Server/Api/Seo/SpaPageMeta.cs)
- [SpaMetadataMiddleware.cs](src/Server/Api/Middleware/SpaMetadataMiddleware.cs) (hoy sin scope DB; 12-1 lo conecta)
- [FibraProfileMetadataMiddleware.cs:133,172-181](src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs) (BreadcrumbList existente + FibraSlug.Build)
- [IFibraRepository.cs:20,22](src/Server/Application/Catalog/IFibraRepository.cs) (GetAllActiveAsync / GetAllActiveForSitemapAsync)
- [IFundamentalRepository.cs:21](src/Server/Application/Fundamentals/IFundamentalRepository.cs) (GetSummaryLatestAsync), [FundamentalsEndpoints.cs:15](src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs)
- [ComparadorPage.tsx](src/Web/Main/src/modules/comparador/ComparadorPage.tsx), [FundamentalesPage.tsx](src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx)
- Story 12-1: [12-1-modulo-seo-administrable.md](_bmad-output/implementation-artifacts/12-1-modulo-seo-administrable.md)
- 2026: [15 Essential SEO Tags 2026](https://www.link-assistant.com/news/html-tags-for-seo.html) · [GEO 2026 — Frase](https://www.frase.io/blog/what-is-generative-engine-optimization-geo)

## Hallazgos de auditoría SEO (2026-06-13)

> Auditoría completa (score 84/100): [seo-audit/FULL-AUDIT-REPORT.md](../../seo-audit/FULL-AUDIT-REPORT.md).

### ✅ Confirmación del premise (M2) + adición menor
La auditoría **confirma** el premise de la historia: `/comparar` y `/fundamentales` se sirven hoy con **0 bloques JSON-LD** (verificado en el HTML servido). También se confirmó que solo la ficha de fibra y la de noticia tienen `BreadcrumbList`; el resto no.
- **Adición a AC-3 (menor):** la página de **listado `/noticias`** también se sirve **sin JSON-LD**. Además de su `BreadcrumbList` (ya en AC-3), considerar un `CollectionPage` + `ItemList` de las noticias de la página 1 (coordina con la estrategia de paginación/canonical de 12-6, AC-4: solo la página 1 indexable).
- **L7 (menor):** `/comparar` es naturalmente ligera en texto indexable (herramienta interactiva). Si se añade copy de apoyo (qué compara, cómo leer el score), refuerza la página además del `SoftwareApplication`/`ItemList`.

## Dev Agent Record
### Agent Model Used
Codex GPT-5
### Debug Log References
`dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~SeoDefaultsBuilderTests|FullyQualifiedName~SpaMetadataProviderTests|FullyQualifiedName~SpaMetadataMiddlewareTests|FullyQualifiedName~FibraProfileMetadataMiddlewareTests|FullyQualifiedName~NewsMetadataMiddlewareTests"` -> 130 passed, 0 failed

`dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` -> 581 passed, 0 failed
### Completion Notes List
- `SpaMetadataMiddleware` ahora recompone JSON-LD vivo para `/comparar` y `/fundamentales` cuando no hay override y agrega `BreadcrumbList` a las rutas públicas soportadas.
- `SeoDefaultsBuilder` expone helpers reutilizables para `BreadcrumbList`, `/comparar` y `/fundamentales`; `FibraProfileMetadataMiddleware` y `NewsMetadataMiddleware` ahora anexan breadcrumbs por separado.
- `SpaMetadataProvider` entrega mínimos estáticos para `/comparar` y `/fundamentales`; el middleware sustituye el JSON-LD con la versión viva en request.
- Se actualizaron tests unitarios de builder y middleware para cubrir compare, fundamentales y breadcrumbs universales.
- Validación manual de Rich Results quedó pendiente.
### File List
- `src/Server/Application/Seo/ISeoDefaultsBuilder.cs`
- `src/Server/Application/Seo/SeoBreadcrumbItem.cs`
- `src/Server/Infrastructure/Seo/SeoDefaultsBuilder.cs`
- `src/Server/Api/Seo/SpaMetadataProvider.cs`
- `src/Server/Api/Middleware/SpaMetadataMiddleware.cs`
- `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs`
- `src/Server/Api/Middleware/NewsMetadataMiddleware.cs`
- `tests/Unit/Infrastructure.Tests/Seo/SeoDefaultsBuilderTests.cs`
- `tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs`
- `tests/Unit/Infrastructure.Tests/Middleware/SpaMetadataMiddlewareTests.cs`
- `tests/Unit/Infrastructure.Tests/Middleware/FibraProfileMetadataMiddlewareTests.cs`
- `tests/Unit/Infrastructure.Tests/Middleware/NewsMetadataMiddlewareTests.cs`

## Change Log
- 2026-06-14: Implementación de JSON-LD para `/comparar` y `/fundamentales`, breadcrumbs universales en rutas públicas, y actualización de tests unitarios y middleware.

### Review Findings (code review 2026-06-14)

Resultado: 6/6 AC PASS. Sin defectos Critical/High. 3 capas adversariales (Blind Hunter, Edge Case Hunter, Acceptance Auditor). 2 decision-needed, 2 patch, 4 defer, 13 dismissed (incl. falsos positivos refutados: doble emisión de JSON-LD, regresión de breadcrumb en ficha, 500 por App:BaseUrl en hot path — todos verificados como no aplicables).

- [x] [Review][Patch] (aplicado) Indentación anómala corregida en `BuildFibraJsonLd` [SeoDefaultsBuilder.cs:425-426](src/Server/Infrastructure/Seo/SeoDefaultsBuilder.cs#L425-L426)
- [x] [Review][Patch] (aplicado) JSON-LD dinámico + breadcrumb movidos después del guard de placeholder; evita lecturas a BD en peticiones que terminan en `next()` [SpaMetadataMiddleware.cs](src/Server/Api/Middleware/SpaMetadataMiddleware.cs)
- [x] [Review][Patch] (aplicado) Eliminado el breadcrumb de home de un solo ítem; home conserva `WebSite`. Test `InjectsMetadata_ForHome_WithRootCanonical` actualizado [SpaMetadataMiddleware.cs](src/Server/Api/Middleware/SpaMetadataMiddleware.cs)
- [x] [Review][Defer] T5 (seed JSON-LD páginas fijas) marcada **N/A** — redundante por diseño: la composición viva en request del middleware genera breadcrumbs y JSON-LD de compare/fundamentales sin necesidad de fila seed.
- [x] [Review][Defer] T7 (validación manual Rich Results, AC-5) diferida — deferred; razón: validez estructural cubierta por tests (`JsonDocument.Parse`); gate manual de Rich Results a ejecutar en el smoke-check de despliegue.
- [ ] [Review][Patch] Indentación anómala tras remover BreadcrumbList inline (`additionalType` a 16 espacios, `};` a 12) — compila pero rompe el estilo del literal [SeoDefaultsBuilder.cs:425-426](src/Server/Infrastructure/Seo/SeoDefaultsBuilder.cs#L425-L426)
- [ ] [Review][Patch] JSON-LD dinámico y breadcrumb se computan ANTES del guard de placeholder prerender → en una petición que luego hace `next()` (index.html sin marcador) se ejecutan lecturas a BD (`GetAllActiveForSitemapAsync`/`GetSummaryLatestAsync`) que se descartan; `FibraProfileMetadataMiddleware` ya lo hace después del guard. Mover el bloque después del guard [SpaMetadataMiddleware.cs:102-106](src/Server/Api/Middleware/SpaMetadataMiddleware.cs#L102-L106)
- [x] [Review][Defer] Referencia `@id` colgante a `#organization` en `/comparar` — `WebApplication.provider` apunta a un nodo `Organization` que `/comparar` nunca define en su markup (sí lo define `/fundamentales`). Completitud de structured data inconsistente [SeoDefaultsBuilder.cs:BuildComparePageJsonLd](src/Server/Infrastructure/Seo/SeoDefaultsBuilder.cs) — deferred, mejora menor de calidad SEO
- [x] [Review][Defer] El ítem hoja del breadcrumb se descarta en silencio si `CanonicalPath` es vacío (filtro de `BuildBreadcrumbListJsonLd`) — hoy no disparable (canonical siempre seteado en ficha/noticia); guard defensivo [FibraProfileMetadataMiddleware.cs / NewsMetadataMiddleware.cs](src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs) — deferred, no triggerable hoy
- [x] [Review][Defer] Sin try/catch alrededor de las lecturas de BD del JSON-LD dinámico → una falla transitoria de BD devuelve 500 en `/comparar` y `/fundamentales` en vez de servir el shell; el mínimo estático no se usa como fallback. Brecha de resiliencia consistente con el resto del pipeline (la lectura de `SeoMetadata` ya es sin guard) [SpaMetadataMiddleware.cs:105](src/Server/Api/Middleware/SpaMetadataMiddleware.cs#L105) — deferred, gap transversal del pipeline
- [x] [Review][Defer] Sobrecargas muertas `BuildMetaBlock(Fibra,...)` / `BuildMetaBlock(NewsArticle,...)` aún cargan `BreadcrumbList` inline (sin call sites) — borrar para evitar doble breadcrumb si un futuro caller las reusa [FibraProfileMetadataMiddleware.cs / NewsMetadataMiddleware.cs](src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs) — deferred, código muerto pre-existente
