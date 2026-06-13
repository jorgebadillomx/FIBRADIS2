# Story 12.10: Redirects 301/302 administrables desde Ops

Status: ready-for-dev

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

- [ ] **T1 — Dominio + EF (AC-1)**: `Domain/Seo/UrlRedirect.cs`; config schema `seo` (índice único `FromPath` normalizado); `DbSet`; migración `AddUrlRedirects` (`--project src/Server/Infrastructure --startup-project src/Server/Api`).
- [ ] **T2 — Repo (AC-1, AC-2)**: `Application/Seo/IRedirectRepository.cs` + impl; `GetActiveAsync` (para cache). Registrar en `ApiServiceExtensions.cs`.
- [ ] **T3 — Middleware (AC-2, AC-4, AC-5)**: leer reglas (cacheadas), aplicar redirect con StatusCode + query string. Ubicar en el pipeline donde hoy están los redirects ([Program.cs:52,59]). Migrar las 3 reglas hardcodeadas a seed y quitarlas del código. Conservar slug canónico de fibra + www→non-www.
- [ ] **T4 — Endpoints Ops + auditoría (AC-3)**: `OpsSeoRedirectsEndpoints` (`/api/v1/ops/seo/redirects`), `RequireAuthorization("AdminOps")`, `GetActor`, validaciones (paths, statusCode, anti-loop, colisión con prefijos reservados).
- [ ] **T5 — Codegen + SPA Ops (AC-3)**: `dotnet build` → `npm run codegen:api`; sub-módulo "Redirects" en la página SEO de Ops (tabla + form). TanStack Query + React Hook Form. `noUnusedLocals`.
- [ ] **T6 — Invalidación de cache (AC-2)**: al crear/editar/eliminar regla, invalidar la cache en memoria del middleware.
- [ ] **T7 — Tests (AC-6)**: unit + integration (redirect vía BD, query string, 302, inactiva, anti-loop, 401/403). `dotnet test tests/Unit/`, `dotnet test tests/Integration/ -m:1`.

## Dev Notes
- **Stack real = SQL Server**. Schema `seo` ya creado por 12-1.
- **Valor real hoy es bajo** (solo 3 reglas estáticas) — esta historia cobra sentido al reestructurar URLs o llegar el blog. Está bien dejarla en backlog hasta que se necesite; documentado en la discusión con el usuario.
- **NO migrar lógica dinámica**: el 301 a slug canónico de fibra y www→non-www se quedan en código (no son "reglas" administrables).
- **Pipeline order crítico**: el redirect debe ir antes de los metadata middlewares y del fallback SPA (como los redirects actuales en [Program.cs:52,59]). No romper el orden establecido en 12-1.
- **Cache obligatorio**: no consultar BD por cada request; cache en memoria con invalidación al editar.
- **Anti-loop y prefijos reservados**: nunca redirigir `/api/`, `/ops/`, `/hangfire/`, assets, ni interferir con `/fibras/{slug}`.

### Security Checklist — antes del primer commit
- [ ] **TOCTOU**: upsert con índice único `FromPath` → capturar `DbUpdateException`.
- [ ] **Auth-gating UI**: módulo redirects solo en Ops; endpoints `.RequireAuthorization("AdminOps")`; verificar 401/403.
- [ ] **Open redirect**: validar que `ToPath` sea **ruta interna** (empieza con `/`, no `//` ni `http(s)://` externo) para evitar open-redirect abuse. Guard explícito + test.
- [ ] **Denominador cero**: N/A.

### References
- [FibraSlugRedirectMiddleware.cs:48-62](src/Server/Api/Middleware/FibraSlugRedirectMiddleware.cs) (redirects hardcodeados actuales)
- [Program.cs:52,59](src/Server/Api/Program.cs) (orden de middlewares de redirect)
- Patrón Ops CRUD + auditoría: [OpsCatalogEndpoints.cs](src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs), [OpsConfigEndpoints.cs](src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs)
- Story 12-1: [12-1-modulo-seo-administrable.md](_bmad-output/implementation-artifacts/12-1-modulo-seo-administrable.md)
- 2026: [SEO Best Practices 2026 — ALM](https://almcorp.com/blog/seo-best-practices-complete-guide-2026/)

## Dev Agent Record
### Agent Model Used
### Debug Log References
### Completion Notes List
### File List
