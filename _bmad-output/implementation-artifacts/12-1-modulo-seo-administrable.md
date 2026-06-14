# Story 12.1: Módulo SEO administrable desde Ops (fundación)

Status: in-progress

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an **AdminOps de FIBRADIS**,
I want **administrar desde Ops todos los metadatos SEO de cada tipo de página (home, páginas fijas, fibras, noticias y, a futuro, blog) desde una sola tabla en base de datos, con valores que se auto-llenan al crear contenido**,
so that **el equipo controle el SEO sin redeploy, cada página nueva nazca con metadatos correctos y el sitio cumpla las reglas SEO/GEO 2026 sin depender de valores hardcodeados en C#**.

## Contexto de Épica 12 — SEO Administrable

Épica 11 (SEO Foundation, **cerrada con retro**) dejó el SEO funcionando pero **hardcodeado en C#**: tres middlewares (`SpaMetadataMiddleware`, `FibraProfileMetadataMiddleware`, `NewsMetadataMiddleware`) y un diccionario estático (`SpaMetadataProvider`). No hay forma de editar un title, una description o un JSON-LD sin recompilar y redeployar.

**Épica 12** convierte el SEO en datos administrables. Esta historia **12-1 es la fundación**: introduce el schema `seo`, la entidad `SeoMetadata`, el módulo Ops CRUD, y conecta los tres middlewares existentes para que **lean de la BD con fallback al código actual**. Auto-llena los valores al crear noticias y fibras, hace backfill del contenido existente (últimas 100 noticias por fecha de captura + todas las fibras + seed de páginas fijas) y respeta ediciones manuales.

**Fuera de alcance de 12-1 (historias futuras de Épica 12):**
- 12-2: editor de directivas `robots` por página (noindex/nofollow/max-snippet), `FAQPage` schema editable, hreflang multi-región. **NO incluir `HowTo` schema** — Google lo deprecó en sept-2023 (ya no produce rich results); no invertir en él.
- 12-3: `llms.txt`, soporte completo de **blog** (entidad nueva), panel de previsualización de SERP/social card.

> El blog **no existe** como contenido hoy (solo el redirect 301 `/blog → /noticias` en `FibraSlugRedirectMiddleware.cs:48-53`). El modelo de datos de esta historia debe quedar **listo para blog** (el `PageType` lo contempla) pero NO se implementa ingesta/entidad de blog aquí.

## Acceptance Criteria

**AC-1 — Schema y entidad.** Existe un schema `seo` en SQL Server con la tabla `seo.SeoMetadata`. Cada fila representa los metadatos SEO de **una página** identificada por `(PageType, EntityKey)`:
- `PageType`: enum `Home | StaticPage | Fibra | News | Blog`.
- `EntityKey`: la ruta canónica para páginas fijas (`/`, `/fibras`, …) o el identificador del contenido para dinámicas (ticker de fibra, slug de noticia). Único por `(PageType, EntityKey)`.
- La migración EF crea el schema y la tabla; `dotnet ef migrations list` la muestra y `database update` aplica sin error.

**AC-2 — Campos SEO 2026 administrables.** La fila persiste, como mínimo, los siguientes campos editables (ver Dev Notes §"Modelo de datos 2026" para tipos exactos): `Title`, `MetaDescription`, `CanonicalPath`, `OgTitle`, `OgDescription`, `OgType`, `OgImageUrl`, `OgLocale`, `TwitterCard`, `RobotsDirectives`, `JsonLd` (texto JSON-LD crudo validado), más metadatos de control (`IsActive`, `UpdatedAt`, `UpdatedBy`). Cada campo de contenido tiene un flag de override (ver AC-6).

**AC-3 — Módulo Ops CRUD.** En la SPA Ops aparece un módulo "SEO" (nav + página) que permite: listar todas las filas con filtro por `PageType` y búsqueda por `EntityKey`/título; ver/editar una fila; y editar metadatos manualmente. Los endpoints `GET/PUT` viven en `/api/v1/ops/seo`, protegidos con `RequireAuthorization("AdminOps")`. Toda escritura queda auditada (actor + timestamp; ver Dev Notes §Auditoría).

**AC-4 — Los 3 middlewares leen de la BD con fallback.** `SpaMetadataMiddleware`, `FibraProfileMetadataMiddleware` y `NewsMetadataMiddleware` resuelven sus metadatos consultando `seo.SeoMetadata` primero. Si **no hay fila** (o `IsActive=false`), caen al comportamiento actual hardcodeado (diccionario / `BuildMetaBlock`). El HTML servido es idéntico al actual cuando no hay fila, y refleja los valores de BD cuando sí la hay. **Todas las reglas de la sección "Middleware de Metadata SEO" de `convenciones-fibradis.md` se mantienen** (description 120-160, og:title == title, strip Markdown, soft-404, encoding HTML vs JSON-LD, Cache-Control no-cache, solo GET/HEAD, guard `<!-- prerender-meta -->`, guard longitud ≤256).

**AC-5 — Auto-llenado al crear.** Al crear una **noticia** (`NewsRepository.AddWithLinksAsync`, después de resolver el slug) y al crear una **fibra** (`FibraRepository.AddAsync`) se inserta automáticamente la fila `SeoMetadata` correspondiente, generada por un **servicio compartido** de defaults que produce exactamente los mismos valores que hoy generan `NewsMetadataMiddleware.BuildMetaBlock` y `FibraProfileMetadataMiddleware.BuildMetaBlock` (title/description/canonical/og/json-ld). Crear una fibra o noticia nueva ⇒ existe su fila SEO sin intervención manual.

**AC-6 — Override: la edición manual gana.** Cada campo de contenido tiene un flag `*_IsOverridden`. La regeneración automática (re-proceso IA de noticia, edición de FullName/Sector de fibra) **solo rellena los campos NO marcados como override**; nunca pisa lo editado por un AdminOps. Editar un campo en Ops marca su flag de override = true.

**AC-7 — Backfill de contenido existente.** Existe un endpoint Ops idempotente (modelado sobre `POST /api/v1/ops/news/backfill-slugs`) que crea filas `SeoMetadata` faltantes para: (a) las **últimas 100 noticias por `CapturedAt`** (fecha de guardado), (b) **todas las fibras activas**, (c) **las páginas fijas** del diccionario actual (seed con los valores hardcodeados vigentes). Re-ejecutarlo no duplica filas ni pisa overrides; devuelve un DTO con conteos por tipo.

**AC-8 — Sitemap y consistencia.** `SeoEndpoints` (sitemap.xml/robots.txt) sigue funcionando. Las páginas con `RobotsDirectives` que incluyan `noindex` **no** se emiten en el sitemap (regla de coordinación SEO↔auth de convenciones). El sitemap sigue listando fibras activas y últimas noticias como hoy.

**AC-9 — Tests.** Unit tests del repositorio (`SeoMetadataRepository`) y del servicio de defaults (valores exactos), tests por-middleware de que el fallback produce el HTML actual y que la fila de BD lo sobreescribe, test `Descriptions_AreBetween120And160Chars` para los defaults generados, e integration tests de `/api/v1/ops/seo` (200 happy path + 401/403 auth). Todos verdes antes de `done`.

## Tasks / Subtasks

- [ ] **T1 — Dominio + EF + migración (AC-1, AC-2)**
  - [ ] Crear `src/Server/Domain/Seo/SeoMetadata.cs` (POCO, sin atributos EF) con los campos de §"Modelo de datos 2026" y `src/Server/Domain/Seo/SeoPageType.cs` (enum).
  - [ ] Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Seo/SeoMetadataConfiguration.cs` (`IEntityTypeConfiguration`, `ToTable("SeoMetadata", schema: "seo")`, columnas `snake_case` vía `HasColumnName`, índice único `(PageType, EntityKey)`, `PageType` como `string`/`int` consistente con convenciones de otros enums del repo — verificar cómo mapea `FibraState`/`NewsArticleStatus`).
  - [ ] Agregar `DbSet<SeoMetadata>` en `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs` (la config se auto-descubre vía `ApplyConfigurationsFromAssembly`, no registrar manualmente).
  - [ ] `dotnet ef migrations add AddSeoModule --project src/Server/Infrastructure --startup-project src/Server/Api` (si los DLLs están bloqueados por la API corriendo, agregar `--configuration Release`). Verificar que emite `EnsureSchema("seo")`. Correr `dotnet ef migrations list` y `database update`.
- [ ] **T2 — Repositorio (AC-1, AC-6, AC-7)**
  - [ ] `src/Server/Application/Seo/ISeoMetadataRepository.cs` (namespace `Application.Seo`): `GetAsync(PageType, entityKey, ct)`, `GetAllAsync(filtros, ct)`, `UpsertAsync(...)` o `AddAsync`+`UpdateAsync`, `ExistsAsync`. Métodos para backfill batch.
  - [ ] `src/Server/Infrastructure/Persistence/Repositories/Seo/SeoMetadataRepository.cs` (primary-ctor `AppDbContext db`). Queries **secuenciales** (regla: EF Core nunca `Task.WhenAll`). En `Upsert`, respetar flags de override: solo escribir campos cuyo `*_IsOverridden == false` en la ruta de auto-regeneración (parámetro explícito `overrideMode`).
  - [ ] Registrar `AddScoped<ISeoMetadataRepository, SeoMetadataRepository>()` en `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`.
- [ ] **T3 — Servicio compartido de defaults (AC-5, AC-9)**
  - [ ] Crear `src/Server/Application/Seo/ISeoDefaultsBuilder.cs` + impl (capa Application o Infrastructure según dónde viva la lógica de `BuildMetaBlock`). Debe producir `SeoMetadata` para `News`, `Fibra` y `StaticPage` reusando **la misma lógica** que hoy tienen `NewsMetadataMiddleware.BuildMetaBlock` (NewsMetadataMiddleware.cs:163-236) y `FibraProfileMetadataMiddleware.BuildMetaBlock` (FibraProfileMetadataMiddleware.cs:143-211): mismos title/description/canonical/og/json-ld, mismo strip Markdown, mismo piso 120 / techo 160.
  - [ ] **Refactor crítico (anti-divergencia):** extraer la generación de meta a este servicio y hacer que **los middlewares lo consuman** (vía el repo: fila de BD si existe, defaults del builder si no). Evitar tres copias de `BuildMetaBlock` divergentes (deuda ya señalada en el análisis de Épica 11).
- [ ] **T4 — Conectar middlewares a la BD con fallback (AC-4)**
  - [ ] `SpaMetadataMiddleware`: resolver `GetMetaForPath` → primero `ISeoMetadataRepository.GetAsync(StaticPage, normalizedPath)`; si null/IsActive=false → `ISpaMetadataProvider` (diccionario actual). Mantener normalización de path (`TrimEnd('/').ToLowerInvariant()`, vacío→`/`).
  - [ ] `FibraProfileMetadataMiddleware`: tras resolver `fibra` por ticker → `GetAsync(Fibra, fibra.Ticker)`; si null → `BuildMetaBlock(fibra)` actual.
  - [ ] `NewsMetadataMiddleware`: tras resolver `article` → `GetAsync(News, article.Slug ?? article.Id)`; si null → `BuildMetaBlock(article)` actual. **Mantener el soft-404** cuando el artículo no existe o `DeletedAt != null`.
  - [ ] Conservar TODAS las reglas de `convenciones-fibradis.md §Middleware`: GET/HEAD, guard `<!-- prerender-meta -->`, guard ≤256 chars, encoding `HtmlEncoder` (meta) vs `JavaScriptEncoder`/`JsonSerializer` (JSON-LD), `Cache-Control: no-cache`, eliminación del `<title>` estático vía `[GeneratedRegex]`.
  - [ ] El middleware es Singleton y el repo es Scoped → resolver vía `IServiceScopeFactory.CreateScope()` (patrón ya usado en `NewsMetadataMiddleware.cs:105`).
- [ ] **T5 — DTOs + endpoints Ops + auditoría (AC-3)**
  - [ ] DTOs en `src/Server/SharedApiContracts/Seo/` (`sealed record`): `SeoMetadataDto`, `UpdateSeoMetadataRequest`, `SeoBackfillResultDto`.
  - [ ] `src/Server/Api/Endpoints/Ops/OpsSeoEndpoints.cs`: `MapOpsSeo` con grupo `/api/v1/ops/seo` `.RequireAuthorization("AdminOps")`. `GET /` (lista + filtros), `GET /{pageType}/{entityKey}`, `PUT /{pageType}/{entityKey}` (marca overrides de campos cambiados), `POST /backfill` (AC-7).
  - [ ] Auditoría: usar el helper `GetActor(HttpContext, IEmailEncryptor, ILogger)` (copiar verbatim de `OpsCatalogEndpoints.cs:400-413`) y registrar la acción. **Decisión de auditoría:** usar el patrón de log estructurado (catalog) salvo que se requiera recuperabilidad campo-a-campo; documentar la elección en Dev Agent Record. (El modelo OperationalConfig + ConfigAuditLog está disponible si se quiere tabla de auditoría.)
  - [ ] Validación manual `Dictionary<string,string[]>` → `Results.ValidationProblem` (400). Validar `JsonLd` parseable (si viene no vacío), URLs http/https-only (guard SSRF como `AddOptionalUrl`). **Description: techo 160 = rechazo duro (400); piso 120 = *warning* no bloqueante** en la edición manual de Ops (páginas simples como `/contacto` no deben forzarse a rellenar). El piso 120 sigue siendo objetivo duro solo para los **defaults auto-generados** (AC-9, test `Descriptions_AreBetween120And160Chars`), no para el override manual.
  - [ ] `app.MapOpsSeo();` en `src/Server/Api/Program.cs` (junto a `MapOpsCatalog`/`MapOpsConfig`).
- [ ] **T6 — Auto-llenado al crear (AC-5, AC-6)**
  - [ ] En `NewsRepository.AddWithLinksAsync` (NewsRepository.cs:36, tras `article.Slug ??= …` línea 38): construir y persistir la fila SEO vía `ISeoDefaultsBuilder` + repo. **Mismo `SaveChangesAsync`/transacción** que el artículo cuando sea posible; manejar colisión idempotente.
  - [ ] En `FibraRepository.AddAsync` (FibraRepository.cs:11): ídem para fibra.
  - [ ] Re-proceso/edición: en los puntos donde la IA actualiza `AiAnalysisJson`/`AiSummary` de una noticia o donde el PUT de fibra cambia `FullName`/`Sector`, invocar el upsert en modo `overrideMode = regenerate` (solo campos sin override). Identificar esos puntos y documentarlos (no introducir regeneración silenciosa que pise overrides).
- [ ] **T7 — Backfill endpoint (AC-7)**
  - [ ] Implementar `POST /api/v1/ops/seo/backfill` idempotente: últimas 100 noticias por `CapturedAt` (usar/añadir query en `INewsRepository` análoga a `GetLatestAsync` pero ordenada por `CapturedAt`), todas las fibras activas (`IFibraRepository.GetAllActiveForSitemapAsync` o equivalente), y seed de las 10 páginas fijas desde `SpaMetadataProvider.Routes`. Saltar las que ya tienen fila. Devolver `SeoBackfillResultDto { staticPages, fibras, news }`.
  - [ ] Patrón a copiar: `OpsNewsManagementEndpoints.cs:15-62` (backfill-slugs idempotente con DTO de conteo).
- [ ] **T8 — Sitemap respeta noindex (AC-8)**
  - [ ] En `SeoEndpoints.BuildSitemapXml`, excluir URLs cuya fila `SeoMetadata.RobotsDirectives` contenga `noindex`. Si no hay fila → comportamiento actual (incluir). Mantener TTL 1h del `IMemoryCache`.
- [ ] **T9 — OpenAPI + cliente tipado + SPA Ops (AC-3)**
  - [ ] `dotnet build FIBRADIS.slnx` (emite `scripts/codegen/Api.json`) → `npm run codegen:api` (regenera `src/Web/SharedApiClient/schema.d.ts`).
  - [ ] `src/Web/Ops/src/api/seoApi.ts` (patrón `configApi.ts`/`catalogApi.ts`: `createPathBasedClient<paths>`, `assertOpsAccessToken()`, `getOpsAuthHeaders()`, `getOpsApiErrorMessage`).
  - [ ] `src/Web/Ops/src/pages/SeoPage.tsx` + `src/Web/Ops/src/modules/seo/SeoTable.tsx` + `SeoForm.tsx` (TanStack Query: `useQuery(['ops-seo'])`, `useMutation` + `invalidateQueries`; React Hook Form sin resolvers). Filtro por `PageType`, búsqueda, edición con indicador visual de campos override. Botón "Backfill" que llama el endpoint y muestra los conteos.
  - [ ] Ruta `{ path: 'seo', element: <SeoPage /> }` en `src/Web/Ops/src/main.tsx` (dentro de children de `OpsShell`) y entrada en `navigationItems` de `src/Web/Ops/src/components/OpsShell.tsx`. Cuidar `noUnusedLocals` (todo import usado).
- [ ] **T10 — Tests (AC-9)**
  - [ ] Unit: `tests/Unit/Infrastructure.Tests/Persistence/Repositories/Seo/SeoMetadataRepositoryTests.cs` (upsert respeta overrides; get por clave; backfill no duplica). Unit del `SeoDefaultsBuilder` con **valores exactos** esperados (title/description para una noticia y una fibra de ejemplo) — gate "tests con valores exactos en Dev Notes".
  - [ ] Por-middleware: `Descriptions_AreBetween120And160Chars` para defaults; test de fallback (sin fila → HTML actual) y override (con fila → valores BD).
  - [ ] Integration: `tests/Integration/Api.Tests/Ops/SeoEndpointTests.cs` (200 con `adminops@test.com`; 401 sin token; 403 con usuario Main). Correr con `-m:1` (serial).
  - [ ] Comandos: `dotnet test tests/Unit/`, `dotnet test tests/Integration/ -m:1`, `npm run build` (Ops, 0 errores TS). Registrar resultados en Dev Agent Record.

## Dev Notes

### ⚠️ Corrección de stack (NO confiar en AGENTS.md aquí)
`AGENTS.md` dice "PostgreSQL 16 + Npgsql" pero **el stack real es SQL Server**: `UseSqlServer` (`Program.cs:37-38`), `DesignTimeDbContextFactory.cs:12` usa `Server=LAPBADIS;Database=FIBRADIS_Dev;Trusted_Connection=True`, Hangfire `UseSqlServerStorage`. SQL Server soporta schemas igual que Postgres. Usar el patrón EF de SQL Server del repo, **no** Npgsql. (Confirmado por memoria del proyecto: "SQL Server LAPBADIS, base FIBRADIS_Dev".)

### Arquitectura: nuevo schema `seo` (decisión explícita)
`docs/req/architecture.md` fija 7 schemas (`catalog|market|news|fundamentals|portfolio|ai|jobs`) y no contempla SEO. Esta historia **introduce un 8º schema `seo`** como extensión arquitectónica deliberada (SEO es un concern transversal con su propio dueño de datos). Documentar esta decisión en Dev Agent Record. Alternativa descartada: meter SEO en un schema existente (violaría "ningún módulo accede directo a la persistencia de otro").

### Regla crítica anti-divergencia (la trampa #1 de esta historia)
Hoy hay **tres** `BuildMetaBlock` casi idénticos (uno por middleware) + un diccionario estático. Si esta historia agrega una 4ª fuente (la BD) sin consolidar, habrá 4 lugares que pueden divergir. **Mandato:** extraer la generación de defaults a `ISeoDefaultsBuilder` y que tanto el auto-llenado (create-time) como el fallback de los middlewares (request-time) usen el mismo builder. Create-time y request-time DEBEN producir el mismo HTML para el mismo contenido.

### Prep-sprint gate A2 — triple slugify (BLOQUEANTE si se tocan slugs)
La retro de Épica 11 marcó como **bloqueante** que existen 3 implementaciones de slugify con semántica divergente: `FibraSlug.cs` (C#), `fibra-slug.ts` (TS) y `SlugGenerator.cs` (C#). Esta historia **no debe generar slugs nuevos** — reusa los slugs ya existentes (`fibra.Ticker` / `article.Slug`). Si el dev detecta necesidad de slugificar, DETENERSE y consolidar primero (o confirmar con el usuario). El `EntityKey` de fibras usa el **ticker**, no un slug nuevo; el de noticias usa el `Slug` ya persistido.

### Modelo de datos 2026 (qué campos administrar y por qué)
Basado en investigación SEO/GEO 2026 (ver §Referencias web) + lo que ya emite el sitio. Campos de `seo.SeoMetadata`:

| Campo | Tipo | Regla / nota 2026 |
|---|---|---|
| `Id` | Guid | PK |
| `PageType` | enum (`Home/StaticPage/Fibra/News/Blog`) | parte de la clave lógica |
| `EntityKey` | string(256) | ruta canónica (fijas) o ticker/slug (dinámicas). Índice único con PageType |
| `Title` | string(70) | 50-60 chars ideal; techo 70 |
| `MetaDescription` | string(160) | **techo 160 duro; piso 120 = objetivo para defaults, *warning* (no 400) en edición manual** (ver T5). El piso 120 es convención del proyecto, no regla SEO de Google (que solo impone un máximo ~155-160) |
| `CanonicalPath` | string | path relativo; el dominio lo antepone `App:BaseUrl` (nunca hardcodear dominio) |
| `OgTitle` | string | **debe == Title** (regla convenciones) |
| `OgDescription` | string | igual/similar a MetaDescription |
| `OgType` | string | `website` / `article` (news) / `article` (blog) |
| `OgImageUrl` | string? | 1200×630; fallback `/og-image.png` |
| `OgLocale` | string | `es_MX` |
| `TwitterCard` | string | `summary_large_image` |
| `RobotsDirectives` | string? | 2026: `index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1`. `noindex` excluye del sitemap (AC-8) |
| `JsonLd` | string? (nvarchar(max)) | JSON-LD crudo validado. Tipos: Home=Organization+WebSite+FinancialService; News=NewsArticle; Fibra=FinancialProduct+BreadcrumbList; StaticPage según corresponda |
| `IsActive` | bool | false ⇒ middleware usa fallback |
| `*_IsOverridden` | bool por campo de contenido | true ⇒ regeneración no lo toca (AC-6) |
| `UpdatedAt` | DateTimeOffset | auditoría |
| `UpdatedBy` | string | actor (email desencriptado) |

> Mantener simple: los `*_IsOverridden` pueden ser columnas booleanas por campo editable (Title, MetaDescription, OgImageUrl, JsonLd, RobotsDirectives) o un único `OverriddenFields` (CSV/JSON). Elegir y documentar; recomendado columnas booleanas explícitas por claridad y testeo.

### Reglas de Middleware de Metadata SEO (de `convenciones-fibradis.md` — NO negociables)
- **Solo GET/HEAD**; POST/PUT/DELETE pasan a `_next`.
- **Guard `<!-- prerender-meta -->`**: sin el comentario → pass-through sin modificar.
- **Guard longitud identificador ≤256** antes de consultar el repo; si excede → 404 sin hit a BD.
- **Soft-404 obligatorio**: recurso inexistente / `DeletedAt != null` / id muy largo ⇒ escribir shell SPA con `StatusCode = 404` y `return`. **Nunca** `await _next` en ese caso (`MapFallbackToFile` devolvería 200 y ocultaría el 404). Ya implementado en `NewsMetadataMiddleware.cs:134-144`.
- **Encoding**: meta tags con `HtmlEncoder.Create(UnicodeRanges.All)`; JSON-LD con `JsonSerializer` + `JavaScriptEncoder.Create(UnicodeRanges.All)`. **No** usar HtmlEncoder para JSON-LD ni `.Replace("<","\\u003c")` manual.
- **Description**: strip de Markdown antes de inyectar; piso 120, techo 160. Test `Descriptions_AreBetween120And160Chars` por middleware (no compartido).
- **og:title == title** exacto.
- **Canonical** con dominio de `App:BaseUrl` (`.TrimEnd('/')`, fail-fast si null).
- **Cache-Control: no-cache** en la respuesta inyectada.
- **Sustitución del `<title>` estático** vía `[GeneratedRegex("<title>.*?</title>", RegexOptions.Singleline)]` `.Replace(..., count:1)`.
- **Coordinación SEO↔auth**: si `RobotsDirectives` = noindex ⇒ fuera del sitemap (mismo deploy).

### Checklist de cierre SSR/SEO (de convenciones — verificar en browser/curl, no lo cubre el test suite)
Ruta responde 200 en hit directo; `<title>` y `<meta description>` 120-160 (contar chars); og:title==title; og:type correcto; sin flash de hidratación; SPA fallback cubre la ruta; WCAG 2.1 AA en componentes Ops nuevos; `npm run build` 0 errores TS.

### Source tree — archivos a tocar / crear

**Backend (crear):**
- `src/Server/Domain/Seo/SeoMetadata.cs`, `src/Server/Domain/Seo/SeoPageType.cs`
- `src/Server/Application/Seo/ISeoMetadataRepository.cs`, `ISeoDefaultsBuilder.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Seo/SeoMetadataConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Seo/SeoMetadataRepository.cs`
- `src/Server/Infrastructure/.../SeoDefaultsBuilder.cs` (o en Application si no necesita EF)
- `src/Server/Api/Endpoints/Ops/OpsSeoEndpoints.cs`
- `src/Server/SharedApiContracts/Seo/SeoMetadataDto.cs`, `UpdateSeoMetadataRequest.cs`, `SeoBackfillResultDto.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/<timestamp>_AddSeoModule.cs` (generado)

**Backend (UPDATE — leer completo antes de tocar):**
- `src/Server/Api/Middleware/SpaMetadataMiddleware.cs` — hoy: solo diccionario vía `ISpaMetadataProvider`. Cambio: anteponer lookup BD. Preservar: normalización de path, todas las reglas de encoding/cache.
- `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs` — hoy: `BuildMetaBlock(fibra)` (líneas 143-211). Cambio: lookup BD por ticker, fallback al builder. Preservar: resolución de ticker desde slug, JSON-LD FinancialProduct+BreadcrumbList.
- `src/Server/Api/Middleware/NewsMetadataMiddleware.cs` — hoy: `BuildMetaBlock(article)` (163-236) + soft-404 (134-144). Cambio: lookup BD por slug/id, fallback al builder. **Preservar el soft-404 intacto.**
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs:36` (`AddWithLinksAsync`) — insertar fila SEO tras slug (línea 38).
- `src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs:11` (`AddAsync`) y `:24` (`UpdateAsync`).
- `src/Server/Api/Endpoints/Public/SeoEndpoints.cs` — excluir noindex del sitemap (`BuildSitemapXml`, líneas 70-93).
- `src/Server/Api/Program.cs` — `app.MapOpsSeo();` (~línea 99-110).
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — `AddScoped` repo + builder.
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs` — `DbSet<SeoMetadata>`.

**Frontend Ops (crear/UPDATE):**
- `src/Web/Ops/src/api/seoApi.ts` (crear)
- `src/Web/Ops/src/pages/SeoPage.tsx` (crear), `src/Web/Ops/src/modules/seo/SeoTable.tsx` + `SeoForm.tsx` (crear)
- `src/Web/Ops/src/main.tsx` (UPDATE — ruta), `src/Web/Ops/src/components/OpsShell.tsx` (UPDATE — nav)
- `src/Web/SharedApiClient/schema.d.ts` (regenerado por codegen)

**NO tocar** (mundo público Main): los middlewares ya cubren `/fibras/{slug}` y `/noticias/{slug}`; `usePageTitle.ts` del cliente sigue igual (no maneja JSON-LD). No re-implementar prerender (`scripts/prerender.mjs` es LEGACY).

### Patrón de módulo Ops CRUD (referencia end-to-end verificada)
Plantilla más cercana = **catalog CRUD (5-3)** para multi-fila + **OperationalConfig (5-4)** para auditoría.
- Endpoints: `MapGroup("/api/v1/ops/seo").RequireAuthorization("AdminOps")` — policy en `AddAuthorizationExtensions.cs:14-15` (`RequireClaim(ClaimTypes.Role,"AdminOps")`).
- Actor: helper `GetActor(...)` (`OpsCatalogEndpoints.cs:400-413`) — lee `User.Identity?.Name ?? email ?? nameId`, fallback `"unknown"`, `emailEncryptor.Decrypt(actor)` (emails cifrados; `IEmailEncryptor` en `Application.Auth`). **Copiar verbatim.**
- Validación: `Dictionary<string,string[]>` → `Results.ValidationProblem`. URL guard http/https-only como `AddOptionalUrl` (`OpsCatalogEndpoints.cs:244-371`).
- Migración: `dotnet ef migrations add … --project src/Server/Infrastructure --startup-project src/Server/Api` (+ `--configuration Release` si DLLs bloqueados). Migraciones en `src/Server/Infrastructure/Migrations/SqlServer/`.
- Codegen: build `.slnx` → `npm run codegen:api` → `src/Web/SharedApiClient/schema.d.ts`. DTOs como `sealed record` en `SharedApiContracts/Seo/`.
- Ops SPA: `seoApi.ts` con `createPathBasedClient<paths>` + helpers de `opsAuth.ts`; page con TanStack Query; ruta en `main.tsx`; nav en `OpsShell.tsx`. `noUnusedLocals: true`.

### Backfill — patrón idempotente
Copiar `OpsNewsManagementEndpoints.cs:15-62` (`POST /api/v1/ops/news/backfill-slugs`): batch, skip de los ya procesados, DTO con conteo. Para "últimas 100 noticias por fecha de guardado" usar `CapturedAt` (no `PublishedAt`) — patrón de orden en `NewsRepository.cs:219` (`GetNullBodyTextArticlesAsync` ordena por `CapturedAt`). `GetLatestAsync` (`NewsRepository.cs:103`) ordena por `PublishedAt` y filtra `AiAnalysisJson != null`; para el backfill se necesita una query por `CapturedAt` desc, Take(100), sin exigir `AiAnalysisJson`.

### Datos de páginas fijas para el seed (valores actuales verbatim)
Fuente: `src/Server/Api/Seo/SpaMetadataProvider.cs:86-136` — 10 rutas: `/`, `/calculadora`, `/comparar`, `/fibras`, `/noticias`, `/conoce-las-fibras`, `/calendario`, `/fundamentales`, `/privacidad`, `/acerca`, `/contacto`. JSON-LD presente solo en `/` (Organization+WebSite+FinancialService), `/conoce-las-fibras` (Article), `/calculadora` (SoftwareApplication). El seed del backfill debe leer este diccionario como fuente de verdad inicial (no re-teclear los textos). **Nota:** `/herramientas` NO está en el diccionario hoy (ruta privada tras 11-6) — no seedearla salvo confirmación.

### Pipeline de middlewares (orden actual — no romper)
`Program.cs:42-79`: ForwardedHeaders → security headers → `WwwToNonWwwMiddleware` (301) → HSTS/HttpsRedirect → `FibraSlugRedirectMiddleware` (301 canónico, **antes de servir HTML**) → `FibraProfileMetadataMiddleware` → `NewsMetadataMiddleware` → `SpaMetadataMiddleware` → DefaultFiles/StaticFiles/Routing/endpoints (`MapSeo()` en :109) → fallbacks. Los 3 metadata middlewares son mutuamente excluyentes por prefijo de path; conservar el orden.

### Security Checklist — completar antes del primer commit

Para cada endpoint de escritura nuevo (`POST`/`PUT`) y componente interactivo:

- [ ] **TOCTOU doble-request**: `PUT`/`POST backfill` y el auto-llenado en create → ¿dos requests en paralelo? Para el upsert con índice único `(PageType,EntityKey)`: capturar `DbUpdateException` y resolver idempotente (no 500). El auto-llenado dentro de `AddWithLinksAsync`/`AddAsync` debe tolerar fila ya existente.
- [ ] **Auth-gating de componentes UI**: la página SEO y el botón "Backfill" viven en Ops (ya tras `OpsLoginGate`); endpoints `.RequireAuthorization("AdminOps")`. Verificar 401/403 con test de integración.
- [ ] **SSRF en URLs**: `OgImageUrl`/`CanonicalPath`/`JsonLd` editables ⇒ validar esquema http/https-only y no permitir inyección en el HTML (encoding ya cubre meta; JSON-LD validado como JSON parseable).
- [ ] **Denominador cero**: N/A (sin funciones de cálculo financiero en esta historia).

### Project Structure Notes
- Sigue la convención: Domain (POCO) → Application (interfaces) → Infrastructure (EF+repos) → Api (endpoints) → SharedApiContracts (DTOs). Schema nuevo `seo` (8º). Tablas `PascalCase`, columnas `snake_case`, schema `lowercase`. Rutas API `/api/v1/ops/seo` (kebab/plural). JSON `camelCase`.
- Conflicto conocido: AGENTS.md (PostgreSQL) vs realidad (SQL Server) — resuelto arriba. Arquitectura fija 7 schemas — esta historia extiende a 8 deliberadamente.

### References
- [Source: src/Server/Api/Seo/SpaMetadataProvider.cs:86-136] — diccionario estático de páginas fijas (fuente del seed).
- [Source: src/Server/Api/Seo/SpaPageMeta.cs / ISpaMetadataProvider.cs] — record `SpaPageMeta(Title, Description, CanonicalPath, JsonLd?)` y seam de inyección.
- [Source: src/Server/Api/Middleware/SpaMetadataMiddleware.cs] — middleware de páginas fijas.
- [Source: src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs:143-211] — BuildMetaBlock fibra (FinancialProduct+BreadcrumbList).
- [Source: src/Server/Api/Middleware/NewsMetadataMiddleware.cs:163-236, :134-144] — BuildMetaBlock noticia + soft-404.
- [Source: src/Server/Api/Endpoints/Public/SeoEndpoints.cs:15-125] — sitemap.xml/robots.txt; `MapSeo()` en Program.cs:109.
- [Source: src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs:36,103,219,311] — AddWithLinksAsync, GetLatestAsync, orden por CapturedAt, GenerateUniqueSlugAsync.
- [Source: src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs:11,24] — AddAsync/UpdateAsync.
- [Source: src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs:21,400-413] — patrón CRUD Ops + GetActor.
- [Source: src/Server/Api/Endpoints/Ops/OpsNewsManagementEndpoints.cs:15-62] — backfill idempotente con DTO de conteo.
- [Source: src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs:11] — ToTable schema + snake_case + HasData.
- [Source: src/Server/Api/Program.cs:37-38,42-79,99-110,130-214] — UseSqlServer, pipeline, mapeos Ops, Hangfire.
- [Source: src/Server/Infrastructure/DesignTimeDbContextFactory.cs:12] — connection string SQL Server (LAPBADIS/FIBRADIS_Dev).
- [Source: _bmad-output/planning-artifacts/convenciones-fibradis.md §"Checklist SSR/SEO" (65-82) y §"Middleware de Metadata SEO" (84-116)] — reglas no negociables.
- [Source: docs/req/architecture.md:192,291-292,309,365] — 7 schemas, decisión de SEO rendering (ASP.NET-hosted prerender).
- [Source: _bmad-output/implementation-artifacts/epic-11-retro-2026-06-11.md] — A1 checklist (done), A2 triple slugify (gate), audit SEO junio 2026 (score 41/100).
- [Source: src/Web/Ops/src/api/configApi.ts / dashboardApi.ts / components/OpsShell.tsx / main.tsx] — patrón SPA Ops.

### Latest Tech / Investigación SEO-GEO 2026
- **Robots avanzado 2026:** `max-image-preview:large`, `max-snippet:-1`, `max-video-preview:-1` para maximizar presencia en rich results y AI Overviews. `noindex` debe sincronizar con sitemap (regla de convenciones). [Source: link-assistant.com "15 Essential SEO Tags 2026", clickrank.ai HTML tags 2026 guide]
- **Open Graph:** imagen 1200×630 (≤5MB); og:title idéntico a title; og:type `article` para noticias/blog, `website` para fijas. [Source: webspidersolutions.com SEO Meta Tags 2026]
- **GEO (Generative Engine Optimization):** para ser citado por ChatGPT/Perplexity/AI Overviews importan más la **claridad de entidad** y el **structured data** que la densidad de keywords. `FAQPage` JSON-LD es el de mayor impacto para citabilidad (diferido a 12-2). `NewsArticle`/`Article`/`Organization` ya presentes son base correcta. robots.txt ya permite GPTBot/ClaudeBot/Google-Extended (verificado en `SeoEndpoints.BuildRobotsTxt`). [Source: frase.io GEO 2026, enrichlabs.ai GEO guide 2026]
- **Title/description:** title 50-60 (techo 70); description útil 120-160 (regla del proyecto, alineada con práctica 2026). [Source: sink-or-swim-marketing 2026 guide]
- Fuentes: [15 Essential SEO Tags 2026](https://www.link-assistant.com/news/html-tags-for-seo.html) · [HTML Tags for SEO 2026](https://www.clickrank.ai/html-tags-for-seo/) · [Meta Tags 2026](https://webspidersolutions.com/what-are-meta-tags-seo-guide-marketers/) · [GEO 2026 — Frase](https://www.frase.io/blog/what-is-generative-engine-optimization-geo) · [GEO 2026 — Enrich Labs](https://www.enrichlabs.ai/blog/generative-engine-optimization-geo-complete-guide-2026)

## Hallazgos de auditoría SEO (2026-06-13)

> Auditoría completa del sitio en producción (score 84/100). Artefactos: [seo-audit/FULL-AUDIT-REPORT.md](../../seo-audit/FULL-AUDIT-REPORT.md), [seo-audit/ACTION-PLAN.md](../../seo-audit/ACTION-PLAN.md). Estos hallazgos **amendan/aclaran** ACs de esta historia — leer antes de implementar T3.

### 🔴 C1 + C2 — AMIENDA CRÍTICA a AC-5 / T3: el builder debe LIMPIAR, no replicar el bug
La auditoría detectó que **el `BuildMetaBlock` actual de fibras produce una meta description defectuosa**. Para `/fibras/fibra-uno-funo11` el HTML servido hoy emite:

```
<meta name="description" content="  ?? Fibra Uno | FUNO11 Ticker: FUNO11 Fecha de constitución: 10 de enero de 2011 Inicio de operaciones: 18 de marzo de 2011 (debut en BMV) | Campo | Detalle ...">
```

Dos defectos confirmados (mismo origen: la description se genera volcando el markdown crudo de la ficha):
1. **Volcado de markdown sin limpiar**: incluye el heading, pipes de tabla `| Campo | Detalle |`, y se trunca con `...`. Se propaga a `<meta description>`, `<meta twitter:description>` **y** al campo `description` del JSON-LD `FinancialProduct` (verificado en los tres).
2. **Emoji corrupto `??`**: el emoji al inicio del markdown se sirve como **dos bytes literales `0x3F`** (pérdida de encoding, no UTF-8) — confirmado a nivel de bytes.

**Implicación para esta historia:** AC-5 y T3 dicen *"producir exactamente los mismos valores que hoy generan los `BuildMetaBlock`"* y *"mismo strip Markdown"*. **Esto se amenda:** el `ISeoDefaultsBuilder` para `Fibra` **NO debe replicar la description actual** — debe generar una description **limpia**:
- Strip real de markdown (`#`, `|`, `*`, `>`, emojis sueltos) y colapso de espacios — el strip actual claramente no cubre tablas ni el heading.
- Generar desde una fuente limpia: primera(s) frase(s) de la prosa "Descripción" larga, o plantilla determinista (p.ej. *"Análisis de {FullName} ({Ticker}): precio, yield, fundamentales (Cap Rate, NAV, LTV) y distribuciones. Sector {Sector} en la BMV."*). Clamp a ~155 chars en frontera de palabra (techo 160 duro).
- UTF-8 end-to-end (o quitar el emoji del origen) — eliminar el `??`.
- El test de T10 `SeoDefaultsBuilder` con **valores exactos** debe aserir que la description de fibra **no contiene** `|`, `#`, `??`, ni termina en `...`, y que es una frase legible. Esto convierte el bug en un gate de regresión.

> Como el `BuildMetaBlock` de noticias y páginas fijas **no** mostró este defecto en la auditoría (sus descriptions están bien redactadas), la consolidación anti-divergencia de T3 sigue válida para esos dos; solo la rama **Fibra** requiere el fix de limpieza descrito.

### L4 — Inconsistencia de `<title>` servidor vs. cliente (menor)
El servidor sirve `Fibra Uno (FUNO11) | FIBRADIS — Fibras Inmobiliarias`, pero el cliente (`usePageTitle`) reescribe el `<title>` renderizado a `FUNO11 — Fibra Uno | Fibras Inmobiliarias`. Google usa el valor renderizado; no es dañino, pero **alinear ambos formatos** evita señales mixtas. Al migrar los titles a `SeoMetadata`, definir un único formato canónico y que `usePageTitle.ts` no lo pise (o lo reproduzca idéntico).

## Dev Agent Record

### Agent Model Used
GPT-5 Codex

### Debug Log References
- `dotnet ef migrations add AddSeoModule --project src/Server/Infrastructure --startup-project src/Server/Api`
- `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter SeoMetadataModelTests`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter SeoMetadataRepositoryTests`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter SeoDefaultsBuilderTests`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter Seo`

### Completion Notes List
- Se implementó el núcleo del módulo SEO administrable: `seo.SeoMetadata`, `SeoPageType`, configuración EF, migración `AddSeoModule` y aplicación a SQL Server.
- Se agregó `ISeoMetadataRepository` y `SeoMetadataRepository` con upsert idempotente y respeto de flags de override.
- Se agregó `ISeoDefaultsBuilder` / `SeoDefaultsBuilder` y se conectaron `SpaMetadataMiddleware`, `FibraProfileMetadataMiddleware` y `NewsMetadataMiddleware` a lookup en BD con fallback al builder.
- Se añadieron pruebas unitarias para modelo, repositorio, builder y cobertura de override/fallback en middlewares.
- Queda pendiente el alcance Ops/UI/backfill/sitemap de la historia.

### File List
- `src/Server/Application/Seo/ISeoDefaultsBuilder.cs`
- `src/Server/Application/Seo/ISeoMetadataRepository.cs`
- `src/Server/Application/Seo/SeoMetadataQuery.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs`
- `src/Server/Api/Middleware/NewsMetadataMiddleware.cs`
- `src/Server/Api/Middleware/SpaMetadataMiddleware.cs`
- `src/Server/Domain/Seo/SeoMetadata.cs`
- `src/Server/Domain/Seo/SeoPageType.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260613222344_AddSeoModule.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260613222344_AddSeoModule.Designer.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Seo/SeoMetadataRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Seo/SeoMetadataConfiguration.cs`
- `src/Server/Infrastructure/Seo/SeoDefaultsBuilder.cs`
- `tests/Unit/Infrastructure.Tests/Middleware/FibraProfileMetadataMiddlewareTests.cs`
- `tests/Unit/Infrastructure.Tests/Middleware/NewsMetadataMiddlewareTests.cs`
- `tests/Unit/Infrastructure.Tests/Middleware/SpaMetadataMiddlewareTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/Seo/SeoMetadataRepositoryTests.cs`
- `tests/Unit/Infrastructure.Tests/Seo/SeoDefaultsBuilderTests.cs`
- `tests/Unit/Infrastructure.Tests/Seo/SeoMetadataModelTests.cs`

## Senior Developer Review (AI)

**Fecha:** 2026-06-13 · **Modo:** full (spec + convenciones) · **Target:** `git diff main` + untracked (módulo SEO 12-1)
**Reviewers:** Blind Hunter · Edge Case Hunter · Acceptance Auditor (3 capas paralelas)

> **Estado del alcance:** la historia está `in-progress`. El diff cubre **T1–T4** (dominio, EF/migración, repositorio, builder de defaults, conexión de los 3 middlewares con fallback + unit tests). **Ausentes:** AC-3 (endpoints + UI Ops + auditoría), AC-5 (auto-llenado en `NewsRepository`/`FibraRepository`), AC-7 (backfill), AC-8 (sitemap noindex) y parte de AC-9 (integration tests). La base de datos (AC-1/AC-2) y el lookup→fallback request-time (AC-4) están sólidos y bien testeados, incluyendo el fix C1/C2 de description limpia de fibra.

### Veredicto por AC

| AC | Veredicto |
|---|---|
| AC-1 Schema + entidad + migración | ✅ cumplido |
| AC-2 Campos SEO 2026 + flags override | ✅ cumplido |
| AC-3 Módulo Ops CRUD | ❌ ausente (T5/T9) |
| AC-4 3 middlewares leen BD con fallback | ⚠️ parcial (ver P1/P2/P5) |
| AC-5 Auto-llenado al crear | ❌ ausente (T6) |
| AC-6 Override: edición manual gana | ⚠️ parcial (mecánica lista; sin flujo de escritura) |
| AC-7 Backfill idempotente | ❌ ausente (T7) |
| AC-8 Sitemap respeta noindex | ❌ ausente (T8) |
| AC-9 Tests | ⚠️ parcial (falta `Descriptions_AreBetween120And160Chars` + integration) |

### Review Findings

- [x] [Review][Patch] Title/OgTitle del builder no caben en `nvarchar(70)` — `SeoDefaultsBuilder.BuildFibra` arma `"{FullName} ({Ticker}) | FIBRADIS — Fibras Inmobiliarias"` (~36 chars de sufijo) sin tope; un `FullName` largo supera 70 y, al persistir vía backfill/upsert (T7), SQL Server lanzará truncation error. **Decisión tomada (2026-06-13): ampliar la columna** — subir `Title`/`OgTitle` a `nvarchar(120)` en `SeoMetadataConfiguration` + ajustar la migración `AddSeoModule` (aún no commiteada) y el `AppDbContextModelSnapshot`. El meta title largo lo trunca Google en SERP de todas formas. [src/Server/Infrastructure/Persistence/SqlServer/Configurations/Seo/SeoMetadataConfiguration.cs]

- [x] [Review][Patch] JSON-LD de fila de BD se inyecta crudo en los middlewares de Fibra y News — `BuildMetaBlock(SeoMetadata)` hace `.Append($"<script ...>{metadata.JsonLd}</script>")` sin re-encodar, mientras `SpaMetadataMiddleware` sí re-serializa con `JsonDocument.Parse` + `JavaScriptEncoder` (escapa `<`→`<`). Viola la regla "JSON-LD encoding" de convenciones. Hoy inocuo (el JsonLd viene del builder, ya seguro), pero stored-XSS en cuanto AC-3 habilite edición desde Ops. Replicar el patrón de SPA. [src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs:186] [src/Server/Api/Middleware/NewsMetadataMiddleware.cs:210]
- [x] [Review][Patch] `JsonDocument.Parse(metadata.JsonLd)` sin try/catch en SPA — un `JsonLd` no parseable en BD lanza `JsonException` → 500 en home/páginas estáticas. Envolver en try/catch y, ante fallo, omitir el bloque JSON-LD (no romper la página). Aplicar el mismo guard al patch P1 de Fibra/News. [src/Server/Api/Middleware/SpaMetadataMiddleware.cs:169]
- [x] [Review][Patch] Falta el test obligatorio `Descriptions_AreBetween120And160Chars` para los defaults de `SeoDefaultsBuilder` — exigido por AC-9 y por convenciones (§Middleware). `SeoDefaultsBuilderTests` asevera valores exactos pero ningún test cubre el invariante piso 120 / techo 160 sobre las descriptions generadas (Fibra y News). [tests/Unit/Infrastructure.Tests/Seo/SeoDefaultsBuilderTests.cs]
- [x] [Review][Patch] `UpsertAsync` no hace `Detach` de la entidad tras `catch (DbUpdateException)` — `metadata` queda en estado `Added`; el `SaveChangesAsync` final (tras `ApplyMetadata(existing)`) reintenta el insert junto al update → re-dispara la violación de índice único. Detach `metadata` antes de aplicar a `existing`. No cubierto por test (repo usa InMemory, que ignora índices únicos). [src/Server/Infrastructure/Persistence/Repositories/Seo/SeoMetadataRepository.cs:66-78]
- [x] [Review][Patch] Mismatch de casing en EntityKey de Fibra — `NormalizeEntityKey` solo hace `Trim()`+`TrimEnd('/')` (no uppercasea), pero `BuildFibra` guarda `EntityKey = ticker.ToUpperInvariant()` y `FibraProfileMetadataMiddleware` consulta con `fibra.Ticker` crudo. Con collation case-sensitive la fila administrada nunca se encuentra → siempre cae al fallback, ignorando overrides. Normalizar el ticker de forma idéntica en lookup y almacenamiento. [src/Server/Infrastructure/Persistence/Repositories/Seo/SeoMetadataRepository.cs:146] [src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs:144]
- [x] [Review][Patch] `og:image:width/height/alt` en News condicionado a `OgImageUrl.EndsWith("/og-image.png")` — falso negativo (imagen propia 1200×630 con otro nombre pierde las anotaciones) y falso positivo (URL arbitraria que termine en ese nombre las recibe). Fibra las emite siempre. Unificar: emitir dimensiones reales o de forma consistente. [src/Server/Api/Middleware/NewsMetadataMiddleware.cs:197]

- [x] [Review][Defer] Cambio de slug de noticia deja huérfano el override SEO (clave por slug vs id) — deferred, depende de auto-llenado/backfill (T6/T7) aún no implementados; resolver el esquema de claves estables al implementarlos.
- [x] [Review][Defer] Sobrecargas antiguas `BuildMetaBlock(Fibra,…)` y `BuildMetaBlock(NewsArticle,…)` quedan sin invocar tras el refactor — deferred; eliminar tras verificar que ningún test las usa (deuda anti-divergencia: lógica de generación duplicada). [src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs:190] [src/Server/Api/Middleware/NewsMetadataMiddleware.cs:215]
- [x] [Review][Defer] Tests del repositorio usan InMemory — no validan índice único, longitudes `nvarchar` ni collation, por lo que P4/P5 y el truncado de Title quedan sin cobertura. Añadir cobertura con provider relacional/Testcontainers al implementar la persistencia (T6/T7). [tests/Unit/Infrastructure.Tests/Persistence/Repositories/Seo/SeoMetadataRepositoryTests.cs]
- [x] [Review][Defer] `TruncateWithEllipsis` accede `text[cut-1]` sin guard `cut>=1` — deferred, no alcanzable con las constantes actuales (solo se invoca con maxLength=160); añadir guard si la función se reutiliza. [src/Server/Infrastructure/Seo/SeoDefaultsBuilder.cs]
- [x] [Review][Defer] Caracteres de control (` `–``) en `Title`/`OgImageUrl`/`CanonicalPath` no se sanitizan — deferred; `HtmlEncoder` escapa `<>&"'` pero no neutraliza whitespace de control en campos que el builder no limpia. Relevante cuando AC-3 permita edición libre.
- [x] [Review][Defer] `UpsertAsync` en modo no-override no reactiva `IsActive=false` — deferred; probablemente intencional (preservar la desactivación manual), pero no hay ruta de regen que reactive. Confirmar comportamiento deseado al implementar el flujo de escritura.

**Descartados (ruido / falso positivo):** (1) `if (seoMetadata is null)` en `SpaMetadataMiddleware.cs:93` reportado como código muerto — **falso**: es alcanzable cuando el path no tiene entrada en el provider ni fila en BD (`GetMetaForPath` devuelve null). (2) `SeoDefaultsBuilder` como Singleton — correcto, es stateless.

**Resolución (2026-06-13):** los 7 patches (P1–P6 + D1) se aplicaron. Detalle:
- **P1/P2** — helper compartido `Api/Middleware/SeoJsonLd.cs`: re-serializa el JSON-LD con `JavaScriptEncoder` (escapa `<`→`<`) y omite el bloque ante JSON inválido (try/catch). Lo consumen los 3 middlewares → encoding unificado, sin stored-XSS ni 500.
- **P3** — además del test `Descriptions_AreBetween120And160Chars` (Theory para Fibra y News), se añadió el **piso 120** a `BuildFibraDescription` (antes solo truncaba el techo 155; un nombre corto sin sector caía bajo 120, violando la convención).
- **P4** — `Detach` de la entidad `Added` tras `DbUpdateException` en `UpsertAsync`.
- **P5** — lookup de Fibra normaliza el ticker a MAYÚSCULAS para coincidir con el `EntityKey` almacenado.
- **P6** — dimensiones `og:image` en News por comparación exacta contra el OG default (no por sufijo de nombre).
- **D1** — `Title`/`OgTitle` ampliados a `nvarchar(120)` (config + migración `AddSeoModule` + snapshot + Designer); test del modelo actualizado.

**Build verde (0 advertencias) · 540/540 Infrastructure.Tests verdes.** La historia permanece `in-progress`: faltan AC-3/5/7/8 y los integration tests de AC-9 (tasks T5–T9).

