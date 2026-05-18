# FIBRADIS — Arquitectura

## Principios Fundamentales

1. SQL Server es la única fuente de verdad; cache y cliente son vistas derivadas
2. Arquitectura de monolito modular — un solo deploy
3. Módulos con ownership de schema — ninguno accede al schema de otro directamente
4. Frescura, parcialidad y recuperabilidad son comportamientos de primera clase
5. El Centro de Procesos es parte de la arquitectura del producto, no un admin externo
6. El contrato API es la frontera de coordinación entre backend y ambos SPAs

## Stack Tecnológico

| Capa | Tecnología |
|---|---|
| Runtime backend | .NET 10 LTS |
| ORM | EF Core 10 |
| Base de datos | SQL Server (single DB, schema-per-module) |
| Jobs | Hangfire in-app (con SQL Server como storage) |
| Auth | JWT bearer + refresh token rotado con revocación server-side |
| SPA Main | Vite 7 + React 19.2 + TypeScript |
| SPA Ops | Vite 7 + React 19.2 + TypeScript |
| Routing frontend | React Router 7 (library mode) |
| Server state | TanStack Query v5 |
| Forms | React Hook Form + Zod |
| UI | shadcn/ui + Tailwind CSS v4 |
| Node.js | 20.19+ o 22.12+ |

## Modelo de Deploy

- **Un solo host ASP.NET Core** sobre IIS compartido
- `Main` SPA servido desde `/`
- `Ops` SPA servido desde `/ops`
- Backend API en `/api/v1/*`
- Hangfire server en el mismo proceso
- Sin Redis ni distributed cache en MVP (IMemoryCache + output caching ASP.NET)

## Schemas SQL (propietarios de datos)

| Schema | Responsable |
|---|---|
| `catalog` | Catálogo maestro de FIBRAs |
| `market` | Precios, snapshots, distribuciones |
| `news` | Noticias ingeridas y procesadas |
| `fundamentals` | PDFs, métricas por periodo |
| `portfolio` | Posiciones del usuario + favoritos |
| `ai` | Work items de IA, configuración de AI_MODE |
| `jobs` | PipelineRun, WorkItem, estado operativo transversal |

**Favoritos** → schema `portfolio` (preferencias del usuario). No existe schema `alerts` en MVP.

## Autenticación

- JWT bearer access token para llamadas API desde SPAs
- Refresh token con rotación + revocación server-side (hash en DB)
- Refresh token en cookie `HttpOnly` segura
- Roles: `User` (mundo privado) y `AdminOps` (ops)
- Rutas públicas: anónimas
- Rutas `/portafolio/*`, `/oportunidades/*`: requieren `User`
- Rutas `/ops/*` y endpoints ops API: requieren `AdminOps`

## Capas del Backend

```
Api/           → Controllers/Endpoints, OpenAPI, Auth middleware, filters
Application/   → Use cases, CQRS (commands/queries/handlers), ports
Domain/        → Entities, value objects, domain rules, events
Infrastructure → EF Core, Hangfire jobs, integraciones externas, caching, observabilidad
SharedApiContracts → DTOs versionados expuestos a frontends (sin lógica de negocio)
```

## Flujo de Datos

```
Providers externos → Jobs → Persistencia → Proyecciones/Read models
→ API → SharedApiClient → Main/Ops SPAs
```

## Comunicación Cross-Módulo

- **NO**: acceso directo a repositorio/schema de otro módulo
- **SÍ**: contratos de capa Application, domain events, proyecciones explícitas
- Work items (en schema `jobs`) para orquestación de flows largos entre módulos

## Caching

- `IMemoryCache` para datos de referencia y read models computados de corta vida
- ASP.NET output caching para GETs públicos seleccionados
- Sin Redis en MVP

## Observabilidad

- Structured logging con correlation ID por request/job
- Health checks separados: API, persistencia, frescura de pipeline
- Read models operativos para `PipelineRun` y `WorkItem` visibles desde Ops

## Frontend

- **Route-level code splitting** en ambos SPAs
- **Server state** bajo TanStack Query (no duplicar en store cliente)
- **URL search params** para filtros/sort navegables
- Client store solo para concerns de shell/sesión
- Rutas públicas indexables requieren HTML rastreable (prerender o equivalente) con title, meta description, canonical

## Reglas de Integridad Arquitectónica

- Un módulo nunca escribe en tablas de otro módulo
- `SharedApiContracts` solo DTOs — sin lógica de negocio
- `Dashboard` y `Opportunities` son read models/aggregation — no tienen schema propio en MVP
- Preferencias y pesos de score persisten bajo ownership de `portfolio`
- Excepciones requieren ADR documentado
