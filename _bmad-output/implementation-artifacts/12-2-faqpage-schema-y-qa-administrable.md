# Story 12.2: FAQPage schema + contenido Q&A administrable (GEO)

Status: ready-for-dev

<!-- Depende de 12-1 (módulo SEO administrable: schema seo, SeoMetadata, inyección DB-driven en middlewares). -->

## Story

As an **AdminOps de FIBRADIS**,
I want **administrar preguntas y respuestas (FAQ) por página desde Ops y que se publiquen como FAQPage JSON-LD + acordeón visible en el sitio**,
so that **ChatGPT, Perplexity y Google AI Overviews citen a FIBRADIS como fuente (el lever #1 de GEO 2026) y los usuarios encuentren respuestas directas**.

## Dependencias y contexto
- **Requiere 12-1 done**: reusa el módulo SEO (schema `seo`, inyección DB-driven en los middlewares, patrón Ops CRUD). El FAQPage JSON-LD se inyecta por el mismo mecanismo de 12-1.
- **Contenido fuente ya existente** (no reinventar):
  - `EditorialPage` (story 8-1): 5 secciones markdown editables desde Ops (`que-son-las-fibras`, `historia`, `como-se-estructuran`, `por-que-invertir`, `regimen-fiscal`) — ya contienen encabezados con forma de pregunta (`¿Qué son?`, `¿Cómo se estructuran?`). Entidad en [EditorialPage.cs](src/Server/Domain/Ops/EditorialPage.cs), editor Ops en [EditorialPage.tsx](src/Web/Ops/src/pages/EditorialPage.tsx), página pública [ConoceLasFibrasPage.tsx](src/Web/Main/src/modules/conoce-las-fibras/ConoceLasFibrasPage.tsx).
  - `KPI_DEFINITIONS` (static TS, 6 KPIs con label+formula+description) en [kpi-definitions.ts](src/Web/Main/src/shared/lib/kpi-definitions.ts) — fuente natural de "¿Qué es Cap Rate?".
- **Hoy NO existe** ningún componente FAQ, ruta ni FAQPage JSON-LD (confirmado).

> **Nuance GEO/SEO 2026:** Google restringió los *rich results* de FAQ a sitios gubernamentales/salud en 2023, pero FAQPage sigue siendo de **alto valor para citabilidad en motores generativos (GEO)** y para entity clarity. El contenido FAQ **debe ser visible en la página** (acordeón), no solo en el JSON-LD — Google exige paridad markup↔contenido visible.

## Acceptance Criteria

**AC-1 — Entidad FAQ administrable.** Existe `seo.FaqItem` (o `FaqEntry`): `Id`, `PageType` + `EntityKey` (misma clave que `SeoMetadata` de 12-1, para asociar la FAQ a una página: home, conoce-las-fibras, fundamentales, o una fibra específica), `Question`, `Answer` (markdown/texto), `Order`, `IsActive`, `UpdatedAt`, `UpdatedBy`. Migración EF en schema `seo`.

**AC-2 — CRUD desde Ops.** En el módulo SEO de Ops se pueden listar/crear/editar/reordenar/desactivar FAQ items por página. Endpoints bajo `/api/v1/ops/seo/faq`, `RequireAuthorization("AdminOps")`, auditados (helper `GetActor`).

**AC-3 — FAQPage JSON-LD server-side.** Cuando una página tiene FAQ items activos, los middlewares de 12-1 inyectan un bloque `FAQPage` JSON-LD (vía `@graph` junto al JSON-LD principal de la página, o como segundo `<script type="application/ld+json">`). Encoding JSON-LD con `JsonSerializer` + `JavaScriptEncoder` (regla de convenciones). Cada `Question`/`Answer` → `Question`/`acceptedAnswer.Answer`.

**AC-4 — Acordeón visible en página.** Las páginas con FAQ renderizan un acordeón accesible (WCAG 2.1 AA: teclado + aria-expanded) con las mismas Q&A del JSON-LD. Mínimo: `/conoce-las-fibras` y `/fundamentales`. El texto visible debe coincidir con el markup (paridad).

**AC-5 — Seed inicial.** Backfill/seed que crea FAQ items iniciales: (a) desde los encabezados `¿...?` de las 5 secciones de `EditorialPage`, (b) desde `KPI_DEFINITIONS` (6 entradas "¿Qué es X?" con su descripción) asociadas a `/fundamentales`. Idempotente (no duplica si ya existen).

**AC-6 — Sin romper 12-1.** El FAQ JSON-LD es aditivo; no altera title/description/canonical/og existentes. Si no hay FAQ items → comportamiento idéntico a 12-1.

**AC-7 — Tests.** Unit del repo FAQ (orden, filtro por página, no duplica en seed). Test de composición del FAQPage JSON-LD (estructura schema.org válida, Q&A correctas, encoding). Integration `/api/v1/ops/seo/faq` (200 + 401/403). Frontend: render del acordeón. Verdes antes de `done`.

## Tasks / Subtasks

- [ ] **T1 — Dominio + EF (AC-1)**: `src/Server/Domain/Seo/FaqItem.cs`; config `Configurations/Seo/FaqItemConfiguration.cs` (schema `seo`, índice `(PageType, EntityKey, Order)`); `DbSet` en `AppDbContext`; migración `AddSeoFaq` (`--project src/Server/Infrastructure --startup-project src/Server/Api`, `--configuration Release` si DLLs bloqueados).
- [ ] **T2 — Repo (AC-1, AC-5)**: `Application/Seo/IFaqRepository.cs` + `Infrastructure/.../Seo/FaqRepository.cs` (queries secuenciales). Registrar en `ApiServiceExtensions.cs`.
- [ ] **T3 — Composición FAQPage JSON-LD (AC-3, AC-6)**: extender el `ISeoDefaultsBuilder`/inyección de 12-1 para componer `FAQPage` cuando hay items. Decidir: `@graph` combinado con el JSON-LD principal vs. segundo `<script>`. Reusar el patrón de serialización de [NewsMetadataMiddleware.cs:179-201](src/Server/Api/Middleware/NewsMetadataMiddleware.cs).
- [ ] **T4 — DTOs + endpoints Ops (AC-2)**: `SharedApiContracts/Seo/FaqItemDto.cs`, `UpsertFaqItemRequest.cs`. `OpsSeoEndpoints` (de 12-1) o nuevo `OpsSeoFaqEndpoints`: `GET/POST/PUT/DELETE /api/v1/ops/seo/faq`. Auditoría + validación (Question/Answer no vacíos, longitudes).
- [ ] **T5 — Seed/backfill inicial (AC-5)**: endpoint `POST /api/v1/ops/seo/faq/seed` idempotente. Parsear encabezados `¿...?` de `EditorialPage` (vía `IEditorialPageRepository`) y materializar `KPI_DEFINITIONS` (replicar las 6 definiciones en C# o leerlas de un seed compartido) para `/fundamentales`. Patrón idempotente de [OpsNewsManagementEndpoints.cs:15-62](src/Server/Api/Endpoints/Ops/OpsNewsManagementEndpoints.cs).
- [ ] **T6 — Codegen + SPA Ops (AC-2)**: `dotnet build FIBRADIS.slnx` → `npm run codegen:api`. Sub-módulo FAQ en la página SEO de Ops (lista por página, editor con reorder). TanStack Query + React Hook Form. `noUnusedLocals`.
- [ ] **T7 — Acordeón público (AC-4)**: componente `FaqAccordion` accesible en `src/Web/Main/src/shared/ui/`; consumir un endpoint público `GET /api/v1/faq?pageType=&entityKey=` (anónimo). Integrar en `ConoceLasFibrasPage` y `FundamentalesPage`. Cumplir WCAG 2.1 AA.
- [ ] **T8 — Tests (AC-7)**: unit repo + composición JSON-LD (valores exactos), integration endpoints (401/403), frontend acordeón. `dotnet test tests/Unit/`, `dotnet test tests/Integration/ -m:1`, `npm run build`.

## Dev Notes
- **Stack real = SQL Server** (no PostgreSQL pese a AGENTS.md). Schema `seo` ya creado por 12-1.
- **Reusar, no reinventar**: el contenido Q&A ya existe en `EditorialPage` (DB, editable) y `kpi-definitions.ts` (static). El seed materializa estos en `FaqItem` para que sean administrables y emitibles como JSON-LD.
- **`dateModified` de FAQ/Article**: `EditorialPage.UpdatedAt` (seed hardcodeado a `2026-01-01` hasta primera edición). Útil también para 12-4.
- **Paridad visible↔markup obligatoria**: si el JSON-LD declara una Q&A que no está visible en la página, es spam estructurado. El acordeón (T7) es requisito, no opcional.
- **Reglas middleware de 12-1 intactas**: GET/HEAD, guard `<!-- prerender-meta -->`, soft-404, encoding HTML vs JSON-LD, Cache-Control no-cache.

### Security Checklist — antes del primer commit
- [ ] **TOCTOU**: upsert FAQ con índice único → capturar `DbUpdateException`. Seed idempotente tolera items existentes.
- [ ] **Auth-gating UI**: editor FAQ solo en Ops (tras `OpsLoginGate`); endpoints `.RequireAuthorization("AdminOps")`; verificar 401/403.
- [ ] **XSS/inyección**: `Answer` markdown renderizado con `ReactMarkdown` (sanitizado) en el acordeón; JSON-LD vía `JsonSerializer` (no concatenación manual).
- [ ] **Denominador cero**: N/A.

### References
- [EditorialPage.cs](src/Server/Domain/Ops/EditorialPage.cs), [EditorialPageConfiguration.cs](src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/EditorialPageConfiguration.cs) (seed markdown 5 secciones), [EditorialEndpoints.cs](src/Server/Api/Endpoints/Public/EditorialEndpoints.cs) (`GET /api/v1/pages`)
- [ConoceLasFibrasPage.tsx](src/Web/Main/src/modules/conoce-las-fibras/ConoceLasFibrasPage.tsx), [EditorialPage.tsx (Ops)](src/Web/Ops/src/pages/EditorialPage.tsx)
- [kpi-definitions.ts](src/Web/Main/src/shared/lib/kpi-definitions.ts), [KpiLabel.tsx](src/Web/Main/src/shared/ui/KpiLabel.tsx)
- [NewsMetadataMiddleware.cs](src/Server/Api/Middleware/NewsMetadataMiddleware.cs) (patrón JSON-LD serialización/encoding)
- Story 12-1: [12-1-modulo-seo-administrable.md](_bmad-output/implementation-artifacts/12-1-modulo-seo-administrable.md)
- [convenciones-fibradis.md §Middleware de Metadata SEO](_bmad-output/planning-artifacts/convenciones-fibradis.md)
- GEO 2026: [Frase](https://www.frase.io/blog/what-is-generative-engine-optimization-geo) · [Enrich Labs](https://www.enrichlabs.ai/blog/generative-engine-optimization-geo-complete-guide-2026)

## Hallazgos de auditoría SEO (2026-06-13)

> Auditoría completa (score 84/100): [seo-audit/FULL-AUDIT-REPORT.md](../../seo-audit/FULL-AUDIT-REPORT.md).

### ✅ Confirmación (L5)
La auditoría **confirma** que hoy no existe `FAQPage` en ningún lado y lo identifica como la mayor oportunidad de rich-result/citabilidad GEO pendiente — exactamente el objetivo de esta historia. Sin cambios de alcance; solo refuerza la prioridad. El contenido del sitio (pasajes factuales cortos, métricas etiquetadas, fundamentales con fuente) ya es muy citable, así que el `FAQPage` visible (acordeón, AC-4) capitaliza una base sólida.

## Dev Agent Record
### Agent Model Used
### Debug Log References
### Completion Notes List
### File List
