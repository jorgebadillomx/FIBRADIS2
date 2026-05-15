---
stepsCompleted:
  - step-01-init
  - step-02-context
  - step-03-starter
  - step-04-decisions
  - step-05-patterns
  - step-06-structure
  - step-07-validation
lastStep: step-07-validation
inputDocuments:
  - C:\Users\jorge\source\repos\FIBRADIS\_bmad-output\planning-artifacts\prd.md
  - C:\Users\jorge\source\repos\FIBRADIS\_bmad-output\planning-artifacts\prd-validation-report.md
  - C:\Users\jorge\source\repos\FIBRADIS\docs\1 input_tecnico_consolidado_v1.md
  - C:\Users\jorge\source\repos\FIBRADIS\docs\2 sistema_principal_tecnico_v1.md
  - C:\Users\jorge\source\repos\FIBRADIS\docs\3 centro_de_procesos_tecnico_v1.md
workflowType: 'architecture'
project_name: 'FIBRADIS'
user_name: 'Jorge'
date: '2026-03-30'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**
El proyecto concentra mas de 50 FRs organizados en seis areas: Catalogo y Descubrimiento Publico, Mercado/Noticias/Fundamentales, Portafolio/Dashboard/Oportunidades, Favoritos (marcado personal integrado en M5, M8 y M9), Superficie Operativa Interna, y Acceso y Separacion. Arquitectonicamente, esto implica una plataforma con modulos de dominio bien delimitados pero altamente coordinados: catalogo maestro, mercado, distribuciones, noticias, fundamentales por periodo, portafolio, scoring, alertas y operaciones. La presencia de un Centro de Procesos interno no es accesoria; es una capacidad central para gobernar pipelines, reintentos, configuracion y modo IA.

**Non-Functional Requirements:**
Los 16 NFRs imponen decisiones arquitectonicas fuertes: tiempos de respuesta agresivos en Home y Dashboard, actualizacion de mercado cada 15 minutos, snapshots y retencion diferenciada, retencion documental de largo plazo para PDFs, degradacion segura ante datos faltantes, auditoria obligatoria para cambios operativos, observabilidad minima operativa y compatibilidad con un despliegue unico en hosting compartido. Tambien fijan soporte responsive, accesibilidad WCAG 2.1 AA, SEO basico en superficies publicas y contrato API documentado para frontend.

**Scale & Complexity:**
El sistema no es un CRUD convencional. Combina aplicacion web, pipelines programados, integraciones externas inestables, procesamiento documental, reglas de negocio configurables y una consola operativa interna. La necesidad de convivir con `AI_MODE` manual y API, junto con procesamiento parcial y estados visibles por item, incrementa la complejidad arquitectonica.

- Primary domain: full-stack web platform with ingestion and operational pipelines
- Complexity level: high
- Estimated architectural components: 10-12 major bounded modules plus cross-cutting infrastructure

### Technical Constraints & Dependencies

- Monolito modular con un solo deploy
- Backend ASP.NET Core en capas API/Application/Domain/Infrastructure
- SQL Server existente como almacenamiento principal
- Hosting en IIS compartido
- Hangfire in-app para jobs y scheduling
- Dos frontends React independientes: sistema principal y `/ops/*`
- Integracion con Yahoo Finance para mercado/distribuciones (best effort)
- Integracion con Google News RSS con filtrado estricto
- Discovery de PDFs config-driven por FIBRA
- Contratos estables para procesamiento IA manual y futuro modo API
- Cliente frontend tipado a partir de contrato API documentado

### Cross-Cutting Concerns Identified

- Autenticacion y autorizacion por superficie
- Idempotencia, exclusion logica y recuperacion segura tras recycle o overlap de jobs
- Persistencia de estado por corrida e item como base del modelo operativo
- Observabilidad, correlation IDs y health checks operativos
- Auditoria de acciones manuales y cambios de configuracion
- Tolerancia a datos faltantes y estados `partial`, `stale`, `error` y `null`
- Configuracion operativa editable sin redeploy y con trazabilidad
- Trazabilidad de fuente, periodo y calidad de datos
- Contrato API consistente para dos frontends desacoplados del backend interno
- Boundaries de modulo y ownership de datos para evitar acoplamiento interno

## Starter Template Evaluation

### Primary Technology Domain

Full-stack web platform based on a .NET backend plus two independent React frontends, driven by ingestion and operational pipelines.

### Starter Options Considered

1. **Official composite baseline: ASP.NET Core Web API + two Vite React TypeScript apps**
   - Uses officially maintained templates for the exact stack already chosen in project context.
   - Fits the required architecture: one backend, two frontends, IIS-friendly publish path, no hidden runtime assumptions.
   - Leaves module boundaries, schemas, jobs, and internal contracts under our control instead of forcing a framework opinion that conflicts with the PRD.

2. **Legacy ASP.NET Core React SPA template**
   - Rejected.
   - Microsoft documents the `react` SPA template as discontinued since .NET 8.
   - It would also push us toward an older integrated SPA model that does not fit the desired two-frontend structure.

3. **JavaScript full-stack starters (Next.js/T3/Redwood-style)**
   - Rejected.
   - They conflict with the explicit backend decision (`ASP.NET Core`), the hosting constraint (`IIS compartido`), and the requirement for Hangfire + SQL Server inside the .NET runtime boundary.

### Selected Starter: Custom Official Baseline

**Rationale for Selection:**
No single off-the-shelf starter matches the architecture already fixed by the project: modular monolith in ASP.NET Core, SQL Server, Hangfire in-app, one shared backend, and two React frontends (`main` and `ops`). The strongest option is therefore a custom baseline assembled from official, actively maintained templates instead of forcing a framework mismatch.

**Initialization Command:**

```bash
dotnet new sln -n FIBRADIS
dotnet new webapi -n Api -o src/Server/Api
npm create vite@latest src/Web/Main -- --template react-ts
npm create vite@latest src/Web/Ops -- --template react-ts
```

**Frontend Design-System Bootstrap:**

```bash
cd src/Web/Main
npx shadcn@latest init

cd ../Ops
npx shadcn@latest init
```

**Architectural Decisions Provided by Starter:**

**Language & Runtime:**
- Backend in C# on ASP.NET Core using the official `webapi` template.
- Frontends in React + TypeScript using the official Vite React TS template.
- Frontend toolchain requires a current Node runtime compatible with the latest Vite guidance.

**Styling Solution:**
- Vite starter keeps frontend setup minimal.
- shadcn CLI on Vite gives us a current path to Tailwind-based component scaffolding without forcing Next.js.
- Final design-system conventions remain under architecture control.

**Build Tooling:**
- `dotnet` build/publish for backend.
- Vite build pipeline for each frontend.
- Clear separation between backend artifact and frontend static bundles, which fits the IIS/shared-hosting constraint better than deprecated SPA templates.

**Testing Framework:**
- The selected baseline does not fully solve testing by itself.
- Backend and frontend test strategy should be added explicitly as architecture decisions rather than inherited accidentally from a community starter.

**Code Organization:**
- The official templates are intentionally light.
- This is a benefit here: we can impose modular-monolith boundaries, schema ownership, and feature-folder frontend organization without fighting a pre-opinionated scaffold.

**Development Experience:**
- Official templates reduce framework risk.
- Vite provides fast local feedback for both frontends.
- The setup remains compatible with typed API client generation from the backend contract in later steps.

**Decision Guardrail:**
No se adoptara un starter full-stack adicional ni una plantilla SPA integrada que sustituya el backend ASP.NET Core o fusione `Main` y `Ops` en una sola app, salvo una decision arquitectonica posterior documentada.

**Note:** Project initialization using this command set should be the first implementation story.

## Core Architectural Decisions

### Decision Principles

- The architecture must optimize for internal isolation, not distributed topology.
- Persisted SQL state is the sole source of truth; cache and client state are derivative views.
- Freshness, partiality, and recoverability are first-class product behaviors, not infrastructure afterthoughts.
- Operational control is part of the product architecture, not an external admin convenience.
- Every cross-module interaction must be explicit and testable.
- The API contract is the coordination boundary for both SPAs.

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**
- Runtime baseline: .NET 10 LTS + EF Core 10
- Data architecture: single SQL Server database with schema-per-module ownership
- API style: REST JSON with versioned route base and OpenAPI as source of truth
- Auth model: JWT bearer access tokens plus rotated refresh tokens with server-side revocation
- Frontend routing/state baseline for both SPAs
- Hosting model: single ASP.NET Core host serving API, jobs, Main SPA and Ops SPA

**Important Decisions (Shape Architecture):**
- In-process caching and output caching strategy for public read models
- Validation split between backend contracts and frontend forms
- Error contract and ProblemDetails mapping
- Module boundary rules and internal communication style
- Observability baseline and operational telemetry surface

**Deferred Decisions (Post-MVP):**
- Distributed cache or multi-node coordination
- External notification channels
- AI API budget orchestration hardening beyond MVP
- CI/CD platform vendor specifics
- CDN or edge caching strategy beyond standard hosting constraints

### Data Architecture

- Runtime and ORM baseline: `.NET 10 LTS` + `EF Core 10`
- Database: single `SQL Server` database
- Ownership model: schema per module (`catalog`, `market`, `news`, `fundamentals`, `portfolio`, `ai`, `jobs`). Favoritos se almacena en el schema `portfolio` como preferencia del usuario; no existe schema `alerts` en MVP.
- Persistence style:
  - aggregate-oriented writes inside module boundaries
  - read models allowed across schemas only through application queries, database views, or explicitly owned projection paths
- Migrations:
  - code-first migrations managed from Infrastructure
  - one migration stream for the deployable monolith, but with strict naming and folder separation by module
- Validation strategy:
  - backend contract validation at API boundary
  - domain invariants enforced inside Domain/Application
  - frontend validation with Zod + form-level validation for UX
- Caching strategy:
  - `IMemoryCache` for short-lived reference and computed read models
  - ASP.NET output caching for selected public GET endpoints
  - no Redis/distributed cache in MVP because hosting model is single deploy on shared IIS
- Data freshness strategy:
  - market last price every 15 minutes
  - slower snapshots persisted daily
  - partial/stale/error/null states modeled explicitly, not inferred ad hoc
- Cache entries are optimization artifacts only; SQL Server persisted state remains the sole source of truth.
- Domain and read models must preserve explicit data states such as `fresh`, `stale`, `partial`, `error`, and `null-equivalent` when applicable.

### Authentication & Security

- Authentication:
  - access token via JWT bearer
  - refresh token with rotation and revocation
- Refresh token persistence:
  - refresh tokens stored hashed server-side
  - rotation invalidates previous token chain on use or revocation event
- Authorization:
  - role-based policies for `User` and `AdminOps`
  - route-level and endpoint-level enforcement
- Surface model:
  - public routes anonymous
  - private product routes authenticated
  - `/ops/*` and ops APIs restricted to `AdminOps`
- API security:
  - no cookie-session web auth for product APIs
  - standard bearer flow for SPA requests
  - refresh endpoint hardened with secure `HttpOnly` cookie transport for refresh token; access token remains bearer-based for API calls
- Data protection:
  - secrets and connection strings outside source control
  - audit trail for all operational mutations
- Encryption:
  - HTTPS required in all non-local environments
  - at-rest DB encryption delegated to hosting/database posture when available
- The authentication design exists to enforce surface separation and revocation safety with minimum SPA exposure, not to maximize protocol flexibility.

### API & Communication Patterns

- API style: REST JSON
- Route base: `/api/v1`
- Contract source of truth: OpenAPI generated from backend
- Typed clients: generated for both frontends from the backend contract
- Error contract:
  - RFC-style `ProblemDetails` for errors
  - domain-specific codes layered on top for predictable UI handling
- Internal communication:
  - no internal HTTP between modules inside the monolith
  - module interaction through Application services, ports, domain events, or persistence-backed work items
- Module interaction inside the monolith must happen through Application-layer contracts, explicit queries/projections, or domain events; direct repository access across modules is forbidden.
- Long-running / operational commands:
  - ops-triggered commands return accepted/queued semantics when work continues asynchronously
- Rate limiting:
  - lightweight rate limiting for auth-sensitive and expensive public endpoints
  - stricter throttling for ops mutation endpoints if needed
- Idempotency:
  - required for manual reruns, retries, and selected write endpoints tied to operational actions
- The API contract is the primary integration boundary between backend, `Main`, and `Ops`.

### Frontend Architecture

- Frontend runtime:
  - `React 19.2`
  - `Vite 7`
  - `Node.js 20.19+ or 22.12+`
- Routing:
  - `React Router 7` in library mode for each SPA
  - separate route trees for `Main` and `Ops`
- Server-state management:
  - `TanStack Query v5`
- Client-state strategy:
  - local component state first
  - URL search params for navigable filter/sort state
  - lightweight shared client store only where cross-screen ephemeral state is justified
- Shared client store is restricted to shell/session UI concerns; remote data must remain under TanStack Query ownership.
- URL state is the primary persistence mechanism for navigable filters, sorting, and comparative context.
- Form strategy:
  - `React Hook Form` + `Zod`
- UI system:
  - `shadcn` on top of Tailwind-compatible Vite setup
  - shared primitives allowed, but `Main` and `Ops` can diverge in composition and UX language
- Frontend organization:
  - feature-folder structure by module
  - shared API client package/folder generated from OpenAPI
- Rendering model:
  - private product and `Ops` remain CSR for MVP
  - public indexable routes cannot rely on CSR-only metadata injection
  - full SSR is not required for MVP, but Home, ficha publica, comparador y rutas publicas documentales deben servir HTML inicial rastreable o prerender equivalente con `title`, `meta description`, canonical y semantica estructural
  - the chosen mechanism may be build-time prerender, publish-time prerender, or ASP.NET-hosted prerender pipeline compatible with the single-deploy model
- Performance:
  - route-level code splitting
  - query-level stale/fresh policies aligned to market/news/PDF cadence
  - avoid global client stores for server state

### Infrastructure & Deployment

- Hosting model:
  - one ASP.NET Core application as system host
  - backend APIs + Hangfire server + static serving for `Main` and `Ops`
- Deploy shape:
  - single deployable artifact
  - `Main` served from `/`
  - `Ops` served from `/ops`
- Background processing:
  - Hangfire in-app with persistent storage
  - all jobs designed for restart safety and overlap protection
- Environment configuration:
  - `appsettings.*` + environment variables + DB-backed operational config where runtime edits are needed
- Observability:
  - structured logging
  - correlation IDs
  - health checks for API, DB, and pipeline freshness
  - operational read models for `PipelineRun` and `WorkItem`
- CI/CD:
  - provider deferred
  - pipeline must build backend, both frontends, run tests, generate OpenAPI client, and gate DB migration execution
  - pipeline must validate public route metadata generation, sitemap output and any prerender artifact required by indexable routes
- Scaling strategy:
  - architect for single-node shared-hosting reality now
  - preserve seams for later external Hangfire worker, distributed cache, or split deploy if needed
- Job execution architecture prioritizes restart safety, overlap protection, idempotency, and operator-visible state over raw throughput optimization.

### Decision Impact Analysis

**Implementation Sequence:**
1. Initialize solution and starter baseline
2. Establish backend modular boundaries and schema ownership
3. Establish auth, API versioning, OpenAPI, and error contract
4. Stand up both SPAs with shared API client generation
5. Implement operational pipeline model (`PipelineRun`, `WorkItem`, idempotent jobs)
6. Add caching, observability, and deployment hardening

**Cross-Component Dependencies:**
- Auth model affects both SPAs, API filters, and ops boundaries
- OpenAPI contract governs frontend integration and DTO discipline
- Schema ownership shapes module boundaries and application query design
- Single-node hosting constraint drives caching, Hangfire topology, and runtime config choices
- Partial/stale/error state modeling affects Domain, API, UI, and ops diagnostics together

## Implementation Patterns & Consistency Rules

### Pattern Categories Defined

**Critical Conflict Points Identified:**
5 areas where AI agents could make different choices: naming, structure, API formats, module communication, and process behaviors.

Critical consistency rules in this section must be treated as normative requirements, not recommendations.

### Naming Patterns

**Database Naming Conventions:**
- Schemas use lowercase singular domain names: `catalog`, `market`, `news`, `fundamentals`, `portfolio`, `ai`, `jobs`
- Tables use singular PascalCase: `Fibra`, `PriceSnapshot`, `PipelineRun`, `WorkItem`
- Columns and foreign keys use snake_case: `fibra_id`, `captured_at`, `error_reason`
- Primary keys are named `id`
- Foreign keys are named `<entity>_id`
- Indexes are named `IX_<Table>_<ColumnList>`
- Unique constraints are named `UX_<Table>_<ColumnList>`
- Convention mapping happens only at persistence/API boundaries; domain models must not be shaped around transport or SQL naming

**API Naming Conventions:**
- Route segments use lowercase resource names, optionally kebab-case when a compound path is needed, for example `/api/v1/fibras`, `/api/v1/market/snapshots`
- Externally exposed resource collections are plural
- Route parameters use `{id}` style in specs and `:id` only inside frontend router code
- Query parameters use camelCase
- JSON payload fields use camelCase
- Standard HTTP headers are used as-is; custom headers are introduced only when required

**Code Naming Conventions:**
- C# types and public members use PascalCase
- C# private fields use `_camelCase`
- TypeScript React components use PascalCase and live in `PascalCase.tsx`
- Frontend hooks use `useThing.ts` naming consistently
- Non-hook utility files use `kebab-case.ts`
- One public type or primary component per file unless the contents are tightly coupled by design

### Structure Patterns

**Project Organization:**
- Backend is split by layer and by module boundary, not by technical artifact only
- Frontend is organized by feature/module first and shared concerns second
- Tests are separated by level and target component
- No module may access another module's repository or persistence layer directly
- Shared utilities live only in approved shared folders
- Copying helper logic across modules is forbidden
- Cross-module read projections must have an explicit owner module and a named use case that serves them
- A module may not write directly into another module's tables or owned projections

**File Structure Patterns:**
- Backend:
  - `src/Server/Api`
  - `src/Server/Application`
  - `src/Server/Domain`
  - `src/Server/Infrastructure`
- Frontend:
  - `src/Web/Main/src/modules/*`
  - `src/Web/Ops/src/modules/*`
  - `src/Web/*/src/shared/*`
  - generated API client in explicit `api` folder or package
- Configuration:
  - static configuration in code only when compile-time
  - runtime-editable operational configuration in DB and surfaced through Ops
- Backend tests use separate test projects by level, at minimum `Unit` and `Integration`
- Frontend unit/component tests are co-located; E2E tests live in a dedicated folder or project when introduced

### Format Patterns

**API Response Formats:**
- Single-resource success responses return the resource directly
- Collection responses use `{ items, page, pageSize, total }`
- Async operational commands may return `202 Accepted` with tracking payload
- Errors use `ProblemDetails` extended with `domainCode` and `correlationId`

**Data Exchange Formats:**
- Dates and timestamps use ISO 8601 strings in UTC
- Enums serialize as stable strings, not numeric ordinals
- `null` is allowed when domain semantics require missing or unavailable data
- Partial, stale, error, and unavailable states must be represented explicitly, not inferred from missing fields alone

### Communication Patterns

**Event System Patterns:**
- Internal domain or integration events use PascalCase past-tense names, for example `PdfReportDetected`, `MarketSnapshotStored`
- Event payloads include identifiers, timestamps, correlation ID, and only the minimal subscriber context required
- Application-layer direct calls are the default for synchronous required coordination
- Domain or integration events are used only for asynchronous, decoupled reactions
- Events must never hide mandatory synchronous dependencies
- Work-item creation is preferred over direct cross-module orchestration for long-running flows
- Cross-module read projections must live in explicit query/projection locations owned by the serving module and use stable names aligned with the use case they support

**State Management Patterns:**
- Server state belongs to TanStack Query
- Navigable UI state belongs in URL parameters
- Local interaction state belongs in local component or form state
- Shared client store is restricted to shell or session UI concerns
- Remote data must remain under TanStack Query ownership
- Duplication of server state into ad hoc client stores is forbidden

### Process Patterns

**Error Handling Patterns:**
- Distinguish domain errors, validation errors, provider or integration errors, and unexpected errors
- User-facing messages remain actionable and non-technical
- Technical diagnostics are logged with correlation ID and exposed to Ops where appropriate
- Retry and reprocess are separate behaviors with different semantics and audit requirements
- UI-safe messages and technical diagnostics must remain separated
- `correlationId` must be logged for unexpected and integration-facing failures and returned when appropriate

**Loading State Patterns:**
- Loading states use the consistent lifecycle `idle`, `loading`, `success`, `error`, with domain overlays such as `partial` or `stale`
- Public pages prefer skeleton or optimistic stale display over hard-blocking spinners when cached data exists
- Ops actions show queued, running, and result state rather than simulating synchronous completion

### Enforcement Guidelines

**All AI Agents MUST:**
- Respect module boundaries and never access another module's persistence layer directly
- Preserve SQL as source of truth and treat cache and client state as derived
- Use documented API, error, naming, and state conventions consistently

**Pattern Enforcement:**
- Architecture documentation is the source of truth for implementation conventions
- Pattern compliance must be enforced through CI checks where feasible: formatting, linting, OpenAPI drift detection, and test gates by level
- PR or code review should flag boundary violations and naming mismatches not covered automatically
- Any exception requires an explicit architecture update or ADR

### Pattern Examples

**Good Examples:**
- `POST /api/v1/ops/pipelines/news/run` returns `202 Accepted` with `pipelineRunId`
- `GET /api/v1/fibras/{id}` returns a direct resource payload in camelCase
- `PdfReportDetected` creates a downstream work item instead of calling another module repository directly

**Anti-Patterns:**
- A frontend store duplicating query cache as a second source of truth
- The `market` module reading `fundamentals` tables through its own repository shortcut
- API endpoints returning ad hoc error shapes per endpoint
- Manual ops actions implemented as synchronous long-running HTTP requests

## Project Structure & Boundaries

### Complete Project Directory Structure

```text
FIBRADIS/
├── README.md
├── FIBRADIS.sln
├── .gitignore
├── .editorconfig
├── .gitattributes
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── package.json
├── package-lock.json
├── .env.example
├── docs/
├── _bmad/
├── _bmad-output/
├── scripts/
│   ├── dev/
│   ├── build/
│   ├── codegen/
│   ├── ci/
│   └── db/
├── deploy/
│   ├── iis/
│   ├── sql/
│   └── release/
├── src/
│   ├── Server/
│   │   ├── Api/
│   │   │   ├── Program.cs
│   │   │   ├── CompositionRoot/
│   │   │   ├── Middleware/
│   │   │   ├── Authentication/
│   │   │   ├── OpenApi/
│   │   │   ├── Endpoints/
│   │   │   │   ├── Public/
│   │   │   │   ├── Private/
│   │   │   │   └── Ops/
│   │   │   ├── Contracts/
│   │   │   └── Filters/
│   │   ├── Application/
│   │   │   ├── Abstractions/
│   │   │   ├── Behaviors/
│   │   │   ├── Common/
│   │   │   ├── Catalog/
│   │   │   ├── Market/
│   │   │   ├── News/
│   │   │   ├── Fundamentals/
│   │   │   ├── Portfolio/
│   │   │   ├── Dashboard/
│   │   │   ├── Opportunities/
│   │   │   ├── Ops/
│   │   │   └── Ai/
│   │   ├── Domain/
│   │   │   ├── Common/
│   │   │   ├── Catalog/
│   │   │   ├── Market/
│   │   │   ├── News/
│   │   │   ├── Fundamentals/
│   │   │   ├── Portfolio/
│   │   │   ├── Dashboard/
│   │   │   ├── Opportunities/
│   │   │   ├── Ops/
│   │   │   └── Ai/
│   │   ├── Infrastructure/
│   │   │   ├── Persistence/
│   │   │   │   ├── SqlServer/
│   │   │   │   ├── Configurations/
│   │   │   │   ├── Migrations/
│   │   │   │   ├── Projections/
│   │   │   │   └── Seed/
│   │   │   ├── Jobs/
│   │   │   │   ├── Common/
│   │   │   │   ├── Market/
│   │   │   │   ├── News/
│   │   │   │   ├── Fundamentals/
│   │   │   │   ├── Portfolio/
│   │   │   │   ├── Ops/
│   │   │   │   └── Ai/
│   │   │   ├── Caching/
│   │   │   ├── Observability/
│   │   │   ├── Security/
│   │   │   ├── Integrations/
│   │   │   │   ├── Yahoo/
│   │   │   │   ├── GoogleNews/
│   │   │   │   ├── PdfDiscovery/
│   │   │   │   └── Ai/
│   │   │   ├── Files/
│   │   │   └── Time/
│   │   └── SharedApiContracts/
│   ├── Web/
│   │   ├── SharedApiClient/
│   │   ├── Main/
│   │   │   ├── package.json
│   │   │   ├── vite.config.ts
│   │   │   ├── tsconfig.json
│   │   │   ├── index.html
│   │   │   ├── public/
│   │   │   └── src/
│   │   │       ├── app/
│   │   │       ├── api/
│   │   │       ├── modules/
│   │   │       │   ├── home/
│   │   │       │   ├── mercado/
│   │   │       │   ├── catalogo/
│   │   │       │   ├── noticias/
│   │   │       │   ├── ficha-publica/
│   │   │       │   ├── comparador/
│   │   │       │   ├── portafolio/
│   │   │       │   ├── dashboard/
│   │   │       │   ├── oportunidades/
│   │   │       │   └── fundamentales/
│   │   │       ├── shared/
│   │   │       │   ├── ui/
│   │   │       │   ├── layouts/
│   │   │       │   ├── hooks/
│   │   │       │   ├── lib/
│   │   │       │   ├── utils/
│   │   │       │   └── types/
│   │   │       └── test/
│   │   └── Ops/
│   │       ├── package.json
│   │       ├── vite.config.ts
│   │       ├── tsconfig.json
│   │       ├── index.html
│   │       ├── public/
│   │       └── src/
│   │           ├── app/
│   │           ├── api/
│   │           ├── modules/
│   │           │   ├── dashboard-operativo/
│   │           │   ├── corridas/
│   │           │   ├── work-items/
│   │           │   ├── schedules/
│   │           │   ├── pdf-config/
│   │           │   ├── ai-mode/
│   │           │   └── auditoria/
│   │           ├── shared/
│   │           │   ├── ui/
│   │           │   ├── layouts/
│   │           │   ├── hooks/
│   │           │   ├── lib/
│   │           │   ├── utils/
│   │           │   └── types/
│   │           └── test/
└── tests/
    ├── Shared/
    │   ├── Fixtures/
    │   ├── Builders/
    │   └── Fakes/
    ├── Unit/
    │   ├── Domain.Tests/
    │   ├── Application.Tests/
    │   └── Infrastructure.Tests/
    ├── Integration/
    │   ├── Api.Tests/
    │   ├── Persistence.Tests/
    │   ├── Jobs.Tests/
    │   └── Integrations.Tests/
    ├── Contract/
    │   └── ApiCompatibility.Tests/
    └── E2E/
        ├── Main.Playwright/
        └── Ops.Playwright/
```

### Architectural Boundaries

**API Boundaries:**
- `Api/Endpoints/Public` expone Home, Mercado, Catalogo, Noticias, Ficha, Comparador y Fundamentales publicos
- `Api/Endpoints/Private` expone Portafolio, Dashboard, Oportunidades y Alertas
- `Api/Endpoints/Ops` expone dashboard operativo, corridas, work items, schedules, config PDF y AI mode
- OpenAPI se genera desde `Api` y alimenta `SharedApiClient` para `Main` y `Ops`

**Component Boundaries:**
- `Main` y `Ops` son apps separadas, sin compartir modulos de negocio
- Solo comparten primitives/UI base aprobadas, utilidades comunes controladas y cliente API generado
- No comparten stores de estado de dominio

**Service Boundaries:**
- `Application/<Module>` contiene commands, queries, handlers y contratos internos
- `Domain/<Module>` contiene entidades, value objects, reglas y eventos
- `Infrastructure` implementa persistencia, providers, jobs y concerns transversales
- Ningun modulo escribe directo en persistencia o proyecciones owned por otro modulo
- `SharedApiContracts` se limita a artefactos de contrato API externo y DTOs versionados; no debe alojar logica de negocio ni comportamiento de dominio compartido

**Data Boundaries:**
- Cada modulo posee su schema SQL
- Las proyecciones cross-module viven en `Infrastructure/Persistence/Projections` con owner explicito
- `jobs` concentra estado operativo transversal (`PipelineRun`, `WorkItem`), pero no reemplaza ownership de dominio

### Requirements to Structure Mapping

**Feature Mapping:**
- Home/Catalogo/Ficha/Comparador -> `Application|Domain|Infrastructure|Web/Main/modules/{home,catalogo,ficha-publica,comparador}`
- Mercado/Distribuciones -> `Market`
- Noticias -> `News`
- Fundamentales/PDFs -> `Fundamentals`
- Portafolio -> `Portfolio`
- Dashboard -> `Dashboard` como modulo de agregacion/read model, no como dominio core independiente
- Oportunidades/Score -> `Opportunities`
- Favoritos -> preferencias de usuario almacenadas en schema `portfolio`; no existe modulo ni schema independiente
- Centro de Procesos -> `Ops`
- AI mode / AI work items -> `Ai`

**Cross-Cutting Concerns:**
- Auth/Security -> `Api/Authentication`, `Infrastructure/Security`
- OpenAPI/typed clients -> `Api/OpenApi`, `Web/SharedApiClient`
- Jobs/Hangfire -> `Infrastructure/Jobs/*`
- Observabilidad -> `Infrastructure/Observability`
- Runtime config editable -> `Infrastructure/Persistence` + `Application/Ops` + `Web/Ops/modules/*`

### Integration Points

**Internal Communication:**
- `Api -> Application`
- `Application -> Domain`
- `Infrastructure` implementa puertos definidos arriba
- La comunicacion cross-module ocurre por contratos de Application, eventos o proyecciones explicitas

**External Integrations:**
- Yahoo -> `Infrastructure/Integrations/Yahoo`
- Google News RSS -> `Infrastructure/Integrations/GoogleNews`
- PDF discovery -> `Infrastructure/Integrations/PdfDiscovery`
- AI manual/API bridge -> `Infrastructure/Integrations/Ai`

**Data Flow:**
- Providers externos -> jobs -> persistencia -> proyecciones/read models -> API -> `SharedApiClient` -> clientes `Main`/`Ops`
- Acciones Ops -> endpoints Ops -> `Application/Ops` -> jobs/config/auditoria -> persistencia -> feedback asincronico a UI

### File Organization Patterns

**Configuration Files:**
- Configuracion .NET en raiz y `src/Server/Api`
- Configuracion frontend en cada app Vite
- Secrets fuera de repo
- Ejemplos y templates en `.env.example` y `deploy/`

**Source Organization:**
- Backend por capa + modulo
- Frontend por app + feature folder
- Shared solo para concerns genuinamente compartidos
- Los modulos listados representan el baseline inicial derivado del PRD y pueden evolucionar solo mediante actualizacion arquitectonica explicita

**Test Organization:**
- Soporte compartido en `tests/Shared`
- Backend por nivel en `tests/Unit` y `tests/Integration`
- Tests de contrato en `tests/Contract` para compatibilidad API entre backend y ambos SPAs
- Frontend unit/component dentro de cada app
- E2E en `tests/E2E`

**Asset Organization:**
- Assets publicos por app en `public/`
- Assets compartidos minimos; se prefiere duplicacion cuando los contextos UX de `Main` y `Ops` divergen

### Development Workflow Integration

**Development Server Structure:**
- Backend host unico
- Dos frontends Vite corriendo separados en local
- `SharedApiClient` regenerable desde el contrato backend y no editable manualmente; cualquier wrapper custom vive fuera del directorio generado

**Build Process Structure:**
- Build backend
- Build `Main`
- Build `Ops`
- Codegen de `SharedApiClient`
- Empaquetado de artefactos estaticos para servir desde un host unico

**Deployment Structure:**
- Deploy unico al host ASP.NET Core sobre IIS
- `Main` publicado en `/`
- `Ops` publicado en `/ops`
- Jobs y API viven en el mismo proceso deployable

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:**
Las decisiones tecnologicas y de despliegue son compatibles entre si. El stack `.NET 10 LTS + EF Core 10 + SQL Server + Hangfire in-app + React 19.2 + Vite 7 + TanStack Query v5 + React Router 7` soporta el modelo de monolito modular, dos SPAs, un solo host ASP.NET Core y operacion en IIS compartido sin contradicciones internas relevantes.

**Pattern Consistency:**
Los patrones de implementacion soportan las decisiones arquitectonicas de forma consistente. Naming, contratos API, convenciones de error, ownership de estado cliente/servidor, reglas de comunicacion cross-module y enforcement por CI quedaron alineados con el stack y con el modelo de modular monolith definido.

**Structure Alignment:**
La estructura fisica propuesta soporta correctamente las decisiones de arquitectura. Las fronteras entre `Api`, `Application`, `Domain`, `Infrastructure`, `Main`, `Ops`, `SharedApiClient` y los niveles de test son coherentes con el modelo de ownership, con los patrones normativos y con el despliegue unico.

### Requirements Coverage Validation ✅

**Epic/Feature Coverage:**
Aunque no se trabajo con epicas formales, todas las superficies funcionales del PRD tienen soporte arquitectonico suficiente: descubrimiento publico, ficha, comparador, mercado, noticias, fundamentales, portafolio unificado con dashboard, oportunidades, favoritos integrados y centro de procesos interno.

**Functional Requirements Coverage:**
Todos los FR cuentan con soporte arquitectonico. El documento cubre ingestion y procesamiento de datos, exposicion de API publica/privada/ops, tolerancia a parcialidad, configuracion operativa editable, auditoria, jobs persistentes, contratos tipados para ambos SPAs y fronteras de modulo suficientes para implementar consistentemente.

**Non-Functional Requirements Coverage:**
Los 16 NFR quedaron soportados arquitectonicamente. Rendimiento, frescura, resiliencia ante datos faltantes, almacenamiento documental, observabilidad, contrato API versionado, seguridad por roles, SEO, accesibilidad, browser support y despliegue unico sobre hosting compartido estan reflejados en decisiones, patrones y estructura.

### Implementation Readiness Validation ✅

**Decision Completeness:**
Las decisiones criticas estan documentadas con suficiente precision para implementacion. Runtime, ORM, persistencia, auth, API, frontends, caching, jobs, observabilidad y deployment tienen definiciones concretas y compatibles con el PRD.

**Structure Completeness:**
La estructura del proyecto es suficientemente especifica para arrancar implementacion sin inventar convenciones base. Directorios, fronteras, responsabilidades, integraciones y niveles de testing quedaron definidos de forma usable por multiples agentes.

**Pattern Completeness:**
Los patrones de naming, estructura, comunicacion, manejo de errores, estado, ownership y enforcement son suficientemente normativos para evitar divergencia innecesaria entre implementaciones paralelas.

### Gap Analysis Results

**Critical Gaps:**
- None.

**Important Gaps:**
- None.

**Nice-to-Have Gaps:**
- No hay gaps relevantes que bloqueen la implementacion; los riesgos restantes son normales de ejecucion y refinamiento incremental, no de definicion arquitectonica.

### Validation Issues Addressed

- Se resolvio la ambiguedad entre modulo, agregacion y ownership persistente en MVP.
- `Dashboard` queda definido como modulo de agregacion/read model, sin schema propio en MVP. La pantalla unificada `/portafolio` consolida carga, gestion y dashboard en un solo modulo frontend; no existe ruta `/dashboard` separada.
- `Opportunities` queda definido como modulo de agregacion/read model en MVP; las preferencias y pesos persistentes viven bajo ownership de `portfolio`.
- `Ops` queda definido como superficie operativa apoyada en `jobs`, auditoria y configuracion persistida, no como dominio core con schema independiente.

### Portfolio Module Decisions

Las siguientes decisiones complementan la arquitectura general para el modulo de Portafolio. Son especificas al dominio de negocio y tienen impacto directo en schema, capa de aplicacion y configuracion operativa.

**Configuracion operativa persistida**

Los parametros siguientes deben almacenarse en la configuracion operativa de base de datos, ser editables desde el Centro de Procesos sin redeploy, y leerse en tiempo de calculo de cada solicitud de portafolio.

| Parametro | Descripcion | Valor por defecto |
|---|---|---|
| `portfolio.avg_periods` | Numero de periodos historicos usados para calcular metricas AVG en portafolio y oportunidades. Aplica a fundamentales y distribuciones. | `4` |
| `portfolio.commission_factor` | Factor de comision de intermediacion aplicado como multiplicador sobre `Titulos x Costo_promedio` para obtener el Costo Total Compra. | Documentar en configuracion inicial del sistema |

**Campos que se persisten por posicion**

El schema `portfolio` almacena exclusivamente los datos de entrada del usuario. Los campos calculados se derivan en lectura cruzando con los modulos Market y Fundamentals; no se duplican en el schema de Portfolio.

| Campo persistido | Descripcion |
|---|---|
| `fibra_id` | Referencia al catalogo maestro |
| `user_id` | Propietario de la posicion |
| `titulos` | Numero de CBFIs en posesion |
| `costo_promedio` | Precio promedio de entrada por CBFI |
| `costo_total_compra` | `Titulos x Costo_promedio x (1 + commission_factor)` calculado al momento de carga o edicion |
| `uploaded_at` | Timestamp de la ultima carga o edicion de la posicion |

Solo se persisten posiciones con `titulos > 0`. Una posicion con cero titulos se elimina del portafolio activo del usuario.

**Campos calculados en lectura**

Los campos siguientes se calculan en la capa Application al momento de construir el read model del portafolio. No se pre-calculan ni se persisten por defecto en MVP salvo que el analisis de rendimiento justifique una proyeccion cacheada.

- `% Portafolio`: `(Titulos_i x Costo_promedio_i) / Suma(Titulos_j x Costo_promedio_j)` sobre todas las posiciones activas del usuario — DR-12.
- `Valor de Mercado`: `Titulos x Precio_de_Mercado` consumido desde el modulo Market.
- `Plusvalia`, `Ganancia`: diferencia entre precio de mercado y costo promedio, en porcentaje y en monto.
- Metricas AVG de fundamentales y distribuciones: promedio de los ultimos `avg_periods` registros del historico, consumidos desde los modulos Fundamentals y Distributions — DR-11.
- `Renta Anual / Trimestral`: `Distribucion x Titulos`, usando frecuencia detectada sin asumir periodicidad fija — DR-05.
- `Dividendo Ponderado Bruto`: `Renta_Anual / Inversion_Total_del_portafolio`, donde Inversion_Total es la suma de todos los `costo_total_compra` del usuario.
- `NAV vs Precio Mercado`: `(NAV - Precio_Mercado) / Precio_Mercado`, consumido desde Fundamentals y Market.
- `Dividend Yield Calculado / Decretado / AVG`: formulas descritas en la seccion de metricas del PRD.

**Regla de cambio de commission_factor**

Un cambio en `commission_factor` desde Ops aplica unicamente a calculos de lectura futuros. No retroactua el campo `costo_total_compra` persistido en posiciones existentes. Si el operador necesita recalcular con el nuevo factor, debe recargar el portafolio o editar las posiciones manualmente, lo cual regenera `costo_total_compra` con el factor vigente en ese momento.

**Ownership de datos del portafolio**

- El modulo `Portfolio` es el unico propietario de las posiciones del usuario. Ningun otro modulo escribe en el schema `portfolio`.
- Los modulos `Dashboard` y `Opportunities` consumen posiciones del usuario a traves de contratos de Application del modulo Portfolio, nunca accediendo directamente al schema.
- Los datos de mercado y fundamentales que componen los campos calculados del portafolio pertenecen a sus modulos de origen; Portfolio los consume en lectura sin copiarlos.

### Architecture Completeness Checklist

**✅ Requirements Analysis**

- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed
- [x] Technical constraints identified
- [x] Cross-cutting concerns mapped

**✅ Architectural Decisions**

- [x] Critical decisions documented with versions
- [x] Technology stack fully specified
- [x] Integration patterns defined
- [x] Performance considerations addressed

**✅ Implementation Patterns**

- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented

**✅ Project Structure**

- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements to structure mapping complete

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION

La arquitectura esta lista para guiar implementacion consistente por multiples agentes. Las decisiones criticas quedaron cerradas, los patrones son suficientemente normativos y la estructura soporta el handoff hacia ejecucion sin reabrir definiciones base.
