# Story 12.10: Redirects 301/302 administrables desde Ops

Status: done

<!-- Independiente de 12-1 (reusa su patrón Ops CRUD + schema seo). -->

## Story

As an **AdminOps de FIBRADIS**,
I want **administrar reglas de redirección (301/302) desde Ops en vez de hardcodearlas en C#**,
so that **podamos cambiar URLs, retirar páginas o preparar el blog sin redeploy, preservando link equity y evitando 404s**.

## Dependencias y contexto
- **Estado actual:** redirects **hardcodeados** en [FibraSlugRedirectMiddleware.cs](src/Server/Api/Middleware/FibraSlugRedirectMiddleware.cs): `/blog → /noticias` (:48-53), `/catalogo → /fibras` (:55), `/aviso-de-privacidad → /privacidad` (:62), además del 301 a slug canónico de fibras (lógica dinámica, **se queda en código**). También `WwwToNonWwwMiddleware` (www→non-www) — se queda.
- Patrón Ops CRUD + auditoría + schema `seo`: ya establecido por 12-1.

## Acceptance Criteria

**AC-1 — Entidad Redirect.** `seo.UrlRedirect`: `Id`, `FromPath` (único, normalizado lowercase/sin trailing slash), `ToPath`, `StatusCode` (301|302), `IsActive`, `Notes?`, `CreatedAt`, `CreatedBy`, `UpdatedAt`. Migración EF en schema `seo`.

**AC-2 — Middleware lee de BD.** Un middleware de redirects (nuevo, o extensión de `FibraSlugRedirectMiddleware`) consulta las reglas activas y emite el redirect con el `StatusCode` configurado, **preservando query string** (como hoy). Debe correr **antes** de servir HTML/metadata (mismo lugar temprano del pipeline que los redirects actuales). Cache en memoria de las reglas (TTL corto, invalidado al editar) para no consultar BD en cada request.

**AC-3 — CRUD desde Ops.** Módulo/sub-módulo en Ops: listar/crear/editar/activar/desactivar reglas. Endpoints `/api/v1/ops/seo/redirects`, `RequireAuthorization("AdminOps")`, auditados (`GetActor`). Validaciones: `FromPath`/`ToPath` empiezan con `/`, no iguales, sin loops obvios (A→B y B→A), `StatusCode ∈ {301,302}`, `FromPath` no colisiona con rutas dinámicas críticas (p.ej. `/fibras/`, `/api/`, `/ops/`).

**AC-4 — Seed de los hardcodeados.** Las 3 reglas actuales (`/blog`, `/catalogo`, `/aviso-de-privacidad`) se siembran en BD y **se retiran del código** del middleware (o el middleware deja de evaluarlas hardcodeadas y las toma de BD). La lógica de slug canónico de fibras y www→non-www **NO** se migra (siguen en código).

**AC-5 — Anti-loop y prioridad.** El middleware no entra en bucle (un `ToPath` que también es `FromPath` activo se detecta/limita a 1 salto). Las reglas de BD no interfieren con el 301 de slug canónico de fibra ni con assets/`/api`/`/ops`.

**AC-6 — Tests.** Unit repo (upsert único, normalización path). Integration: `/blog`→301→`/noticias` (vía BD), preserva query, 302 cuando se configura, regla inactiva no redirige, anti-loop. Endpoints Ops 200 + 401/403. Verdes antes de `done`.

## Tasks / Subtasks

- [x] **T1 — Dominio + EF (AC-1)**: `Domain/Seo/UrlRedirect.cs`; config schema `seo` (índice único `FromPath` normalizado); `DbSet`; migración `AddUrlRedirects` (`--project src/Server/Infrastructure --startup-project src/Server/Api`).
- [x] **T2 — Repo (AC-1, AC-2)**: `Application/Seo/IRedirectRepository.cs` + impl; `GetActiveAsync` (para cache). Registrar en `ApiServiceExtensions.cs`.
- [x] **T3 — Middleware (AC-2, AC-4, AC-5)**: leer reglas (cacheadas), aplicar redirect con StatusCode + query string. Ubicar en el pipeline donde hoy están los redirects ([Program.cs:52,59]). Migrar las 3 reglas hardcodeadas a seed y quitarlas del código. Conservar slug canónico de fibra + www→non-www.
- [x] **T4 — Endpoints Ops + auditoría (AC-3)**: `OpsSeoRedirectsEndpoints` (`/api/v1/ops/seo/redirects`), `RequireAuthorization("AdminOps")`, `GetActor`, validaciones (paths, statusCode, anti-loop, colisión con prefijos reservados).
- [x] **T5 — Codegen + SPA Ops (AC-3)**: `dotnet build` → `npm run codegen:api`; sub-módulo "Redirects" en la página SEO de Ops (tabla + form). TanStack Query + React Hook Form. `noUnusedLocals`.
- [x] **T6 — Invalidación de cache (AC-2)**: al crear/editar/eliminar regla, invalidar la cache en memoria del middleware.
- [x] **T7 — Tests (AC-6)**: unit + integration (redirect vía BD, query string, 302, inactiva, anti-loop, 401/403). `dotnet test tests/Unit/`, `dotnet test tests/Integration/ -m:1`.

## Dev Notes
- **Stack real = SQL Server**. Schema `seo` ya creado por 12-1.
- **Valor real hoy es bajo** (solo 3 reglas estáticas) — esta historia cobra sentido al reestructurar URLs o llegar el blog. Está bien dejarla en backlog hasta que se necesite; documentado en la discusión con el usuario.
- **NO migrar lógica dinámica**: el 301 a slug canónico de fibra y www→non-www se quedan en código (no son "reglas" administrables).
- **Pipeline order crítico**: el redirect debe ir antes de los metadata middlewares y del fallback SPA (como los redirects actuales en [Program.cs:52,59]). No romper el orden establecido en 12-1.
- **Cache obligatorio**: no consultar BD por cada request; cache en memoria con invalidación al editar.
- **Anti-loop y prefijos reservados**: nunca redirigir `/api/`, `/ops/`, `/hangfire/`, assets, ni interferir con `/fibras/{slug}`.

### Security Checklist — antes del primer commit
- [x] **TOCTOU**: upsert con índice único `FromPath` → capturar `DbUpdateException`.
- [x] **Auth-gating UI**: módulo redirects solo en Ops; endpoints `.RequireAuthorization("AdminOps")`; verificar 401/403.
- [x] **Open redirect**: validar que `ToPath` sea **ruta interna** (empieza con `/`, no `//` ni `http(s)://` externo) para evitar open-redirect abuse. Guard explícito + test.
- [x] **Denominador cero**: N/A.

### References
- [FibraSlugRedirectMiddleware.cs:48-62](src/Server/Api/Middleware/FibraSlugRedirectMiddleware.cs) (redirects hardcodeados actuales)
- [Program.cs:52,59](src/Server/Api/Program.cs) (orden de middlewares de redirect)
- Patrón Ops CRUD + auditoría: [OpsCatalogEndpoints.cs](src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs), [OpsConfigEndpoints.cs](src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs)
- Story 12-1: [12-1-modulo-seo-administrable.md](_bmad-output/implementation-artifacts/12-1-modulo-seo-administrable.md)
- 2026: [SEO Best Practices 2026 — ALM](https://almcorp.com/blog/seo-best-practices-complete-guide-2026/)

## Dev Agent Record
### Agent Model Used
GPT-5 / Codex
### Debug Log References
- `dotnet build FIBRADIS.slnx`
- `dotnet ef migrations add AddUrlRedirects --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release --output-dir Migrations/SqlServer`
- `dotnet ef migrations list --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`
- `npm run codegen:api`
- `npm run build --workspace=src/Web/Ops`
- `npm run build --workspace=src/Web/Main`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~RedirectRepositoryTests|FullyQualifiedName~UrlRedirectMiddlewareTests|FullyQualifiedName~FibraSlugRedirectMiddlewareTests"`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~RedirectsEndpointTests|FullyQualifiedName~OpenApiEndpointTests"`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj` (fallos preexistentes y no relacionados: `CalculadoraEndpointTests.GetCalculadora_ReturnsOk_WithExpectedDistributionTotals`, `PortfolioEndpointTests.DeletePosition_ExistingPosition_Returns204`, `Ops.DashboardEndpointTests.GetDashboard_WithAdminOpsToken_ReturnsPipelineDashboardDto`)
### Completion Notes List
- Implementado `seo.UrlRedirect` con normalización de path y restricciones `301|302`, más configuración EF en `schema seo`.
- Añadido `IRedirectRepository` y `RedirectRepository` con lectura activa, cacheable por middleware.
- Creado `UrlRedirectMiddleware` con cache en memoria, preservación de query string y bypass para `api/`, `ops/`, `hangfire/`, assets y `fibras/`.
- Eliminados los redirects hardcodeados de `FibraSlugRedirectMiddleware` y registrado el middleware nuevo antes del resto del pipeline.
- Agregadas las rutas y el módulo de Ops para CRUD de redirects, con auditoría por actor y invalidación de cache.
- Generada la migración `20260614023840_AddUrlRedirects` y sembradas las 3 reglas legacy: `/blog`, `/catalogo`, `/aviso-de-privacidad`.
- Regenerado OpenAPI y el cliente tipado compartido para exponer `UrlRedirectDto` y `UpsertUrlRedirectRequest` en Ops.
- Verificados build, codegen, unit tests y la integración enfocada del módulo; el suite completo de `Api.Tests` mantiene 3 fallos preexistentes ajenos a esta historia.
### File List
- `src/Server/Domain/Seo/UrlRedirect.cs`
- `src/Server/Application/Seo/IRedirectRepository.cs`
- `src/Server/Application/Seo/UrlRedirectPath.cs`
- `src/Server/SharedApiContracts/Seo/UrlRedirectDto.cs`
- `src/Server/SharedApiContracts/Seo/UpsertUrlRedirectRequest.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Seo/UrlRedirectConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Seo/RedirectRepository.cs`
- `src/Server/Api/Middleware/UrlRedirectMiddleware.cs`
- `src/Server/Api/Endpoints/Ops/OpsSeoRedirectsEndpoints.cs`
- `src/Server/Api/Middleware/FibraSlugRedirectMiddleware.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260614023840_AddUrlRedirects.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260614023840_AddUrlRedirects.Designer.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/AppDbContextModelSnapshot.cs`
- `src/Web/Ops/src/api/redirectsApi.ts`
- `src/Web/Ops/src/pages/SeoRedirectsPage.tsx`
- `src/Web/Ops/src/components/OpsShell.tsx`
- `src/Web/Ops/src/main.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `scripts/codegen/Api.json`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/Seo/RedirectRepositoryTests.cs`
- `tests/Unit/Infrastructure.Tests/Middleware/UrlRedirectMiddlewareTests.cs`
- `tests/Unit/Infrastructure.Tests/Middleware/FibraSlugRedirectMiddlewareTests.cs`
- `tests/Integration/Api.Tests/RedirectsEndpointTests.cs`
- `tests/Integration/Api.Tests/OpenApiEndpointTests.cs`
### Change Log
- 2026-06-14: `seo.UrlRedirect` pasó de hardcodeado en middleware a reglas administrables desde Ops con cache en memoria e invalidación.
- 2026-06-14: Se añadió seed de las 3 redirecciones legacy y se regeneró el contrato OpenAPI/cliente tipado para el módulo de redirects.

## Senior Developer Review (AI)

**Fecha:** 2026-06-13 · **Modo:** full (spec + diff) · **Capas:** Blind Hunter + Edge Case Hunter + Acceptance Auditor
**Resultado:** 0 decision-needed, 3 patch, 5 defer, 3 dismissed. ACs: AC-1..AC-5 PASS, AC-6 PARTIAL (gaps de cobertura).

### Review Findings

#### Patches (acción requerida)

- [x] [Review][Patch] Open-redirect: `IsInternalPath` se evade con backslash/control chars (`/\evil.com`, `/%5Cevil.com`) [src/Server/Application/Seo/UrlRedirectPath.cs:37-50] — HIGH. **RESUELTO**: guard endurecido (rechaza backslashes, control chars y 2.º char `/`o`\`) + tests. El guard solo bloquea `//` literal; `Uri.TryCreate(Absolute)` devuelve false para `/\evil.com`, así que se acepta como interno y el middleware emite `Location: /\evil.com`, que los navegadores normalizan a `//evil.com` (host externo). El Security Checklist marca este ítem `[x]` con "Guard explícito + test" pero el guard es evadible. Fix: rechazar cualquier `value` cuyo 2.º char sea `/` o `\`, rechazar backslashes y caracteres de control, antes del check de `Uri.TryCreate`.
- [x] [Review][Patch] **RESUELTO** — Tests obligatorios faltantes del Security Checklist y AC-6 [tests/Integration/Api.Tests/RedirectsEndpointTests.cs + nuevo tests/Unit/.../Seo/UrlRedirectPathTests.cs] — MEDIUM/HIGH. Faltan: (a) test que envíe `ToPath` externo/`//`/`/\` y asserte 400 (exigido por Security Checklist "Open redirect → test"); (b) re-request público que assert emisión HTTP **302** desde una regla 302 en BD — hoy `OpsRedirectCrud_AndPublicRedirect_WorkEndToEnd` actualiza a 302 vía PUT pero nunca vuelve a pedir `/blog` para confirmar el 302 (AC-6 lista "302 cuando se configura" como escenario requerido); (c) unit tests directos de `UrlRedirectPath.Normalize`/`IsReservedSource`/`IsInternalPath` (funciones puras branch-heavy sin cobertura directa).
- [x] [Review][Patch] **RESUELTO** — Middleware duplica la lista de prefijos reservados en vez de usar `UrlRedirectPath.IsReservedSource` (drift de fuente única) [src/Server/Api/Middleware/UrlRedirectMiddleware.cs:25-39] — LOW/MEDIUM. Ahora delega en `IsReservedSource(normalizedPath)`. La cadena inline de `StartsWith("/api/")…/ops//fibras//hangfire//assets` repite literalmente `ReservedExactPaths`/`ReservedPrefixes`. Si se agrega un prefijo a `UrlRedirectPath` el middleware no lo honra. Fix: delegar el bypass de prefijos a `UrlRedirectPath.IsReservedSource(normalizedPath)` conservando el check de extensión de archivo. Alineado con la deuda A2 de retro 11 (consolidar lógica duplicada de rutas).

#### Diferidos (deuda registrada, no bloquean)

- [x] [Review][Defer] Anti-loop solo detecta el par directo A↔B; cadenas multi-hop (A→B, B→C, C→A) y `ToPath` que es `FromPath` activo no se rechazan en escritura [src/Server/Api/Endpoints/Ops/OpsSeoRedirectsEndpoints.cs:322-334] — el middleware sí limita a 1 salto (un solo `FirstOrDefault`, sin chain-following), por lo que AC-5 "limita a 1 salto" se cumple y no hay loop de servidor. Detección de cadenas en creación es mejora más allá del spec.
- [x] [Review][Defer] `Normalize` pasa `ToPath` a minúsculas — corrompería destinos case-sensitive [src/Server/Application/Seo/UrlRedirectPath.cs:24-31] — sin impacto hoy (todos los slugs del proyecto son lowercase); reconsiderar si se introducen destinos sensibles a mayúsculas.
- [x] [Review][Defer] `Normalize` no decodifica `%xx` ni colapsa `//`/slash inicial → una regla creada con esos casos podría no matchear el path entrante decodificado por Kestrel [src/Server/Application/Seo/UrlRedirectPath.cs:24-31] — borde de baja probabilidad (admin escribe paths ASCII planos); canonicalización idéntica write/emit como hardening futuro.
- [x] [Review][Defer] `ToPath` puede apuntar a prefijos reservados (`/api/...`, `/ops/...`); solo `FromPath` corre por `IsReservedSource` [src/Server/Api/Endpoints/Ops/OpsSeoRedirectsEndpoints.cs:295] — AdminOps es confiable y el spec solo exige no-colisión en `FromPath`; evaluar validar `ToPath` también.
- [x] [Review][Defer] `ValidateRequest` sobrescribe mensajes del mismo campo (`errors[field] = [...]` en vez de acumular) → con dos violaciones del mismo campo solo se reporta la última [src/Server/Api/Endpoints/Ops/OpsSeoRedirectsEndpoints.cs:267-302] — la validación sigue rechazando correctamente; solo afecta el detalle del mensaje (UX baja).

#### Dismissed (ruido / por diseño / patrón establecido)

- Cache `IMemoryCache` per-process / staleness multi-instancia (TTL 5 min) — por diseño del spec (cache en memoria con invalidación), FIBRADIS es single-instance.
- `GetActor` llama `emailEncryptor.Decrypt` sobre el claim — patrón idéntico y establecido en `AiModeEndpoints`/`OpsBanxicoEndpoints`; no introducido por esta historia, funciona en tests 401/403.
- Escaneo lineal `FirstOrDefault` por request sin tope de reglas — fuera de alcance; 3 reglas hoy, bajo valor documentado en Dev Notes.
