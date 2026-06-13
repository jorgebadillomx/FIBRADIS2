# Story 12.5: JSON-LD en comparador y fundamentales + breadcrumbs universales

Status: ready-for-dev

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

- [ ] **T1 — Helper BreadcrumbList compartido (AC-3)**: extraer la construcción de `BreadcrumbList` (hoy inline en `FibraProfileMetadataMiddleware`) a un helper reutilizable en el `ISeoDefaultsBuilder` de 12-1, parametrizado por la jerarquía de cada ruta. Aplicarlo a todas las páginas.
- [ ] **T2 — JSON-LD `/fundamentales` Dataset (AC-2)**: en el builder, resolver `IFundamentalRepository.GetSummaryLatestAsync`, componer `Dataset` (variableMeasured, conteo, dateModified = MAX CapturedAt). BreadcrumbList.
- [ ] **T3 — JSON-LD `/comparar` (AC-1)**: componer `SoftwareApplication`/`WebApplication` + opcional `ItemList` de fibras activas (`GetAllActiveForSitemapAsync` + `FibraSlug.Build`). BreadcrumbList.
- [ ] **T4 — Wiring DB-driven + override (AC-4)**: estos JSON-LD se sirven por el `SpaMetadataMiddleware` ya conectado a BD (12-1). Si el campo está override → stored; si no → recomponer (estático o con datos vivos según la página). Caché corto opcional (documentar).
- [ ] **T5 — Seed (AC-1, AC-2, AC-3)**: ajustar el seed de páginas fijas de 12-1 para incluir el JSON-LD base de `/comparar` y `/fundamentales` y los breadcrumbs de todas las rutas.
- [ ] **T6 — Tests (AC-6)**: unit builders (valores exactos), por-middleware con/sin datos. `dotnet test tests/Unit/`, `dotnet test tests/Integration/ -m:1`.
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
### Debug Log References
### Completion Notes List
### File List
