# FIBRADIS — Convenciones de Código

## Nomenclatura de Base de Datos

| Elemento | Convención | Ejemplo |
|---|---|---|
| Schemas | lowercase singular | `catalog`, `market`, `portfolio` |
| Tablas | singular PascalCase | `Fibra`, `PriceSnapshot`, `PipelineRun` |
| Columnas | snake_case | `fibra_id`, `captured_at`, `error_reason` |
| PK | siempre `id` | `id` |
| FKs | `<entidad>_id` | `fibra_id`, `user_id` |
| Índices | `IX_<Table>_<ColumnList>` | `IX_PriceSnapshot_fibra_id` |
| Unique constraints | `UX_<Table>_<ColumnList>` | `UX_Fibra_ticker` |

Las convenciones de naming aplican solo en persistencia/API — los domain models no se doblan a estas convenciones.

## Nomenclatura de API

| Elemento | Convención | Ejemplo |
|---|---|---|
| Base route | `/api/v1` | `/api/v1/fibras` |
| Segmentos | lowercase kebab-case, plural | `/api/v1/market/snapshots` |
| Route params | `{id}` en specs, `:id` en frontend router | `{fibraId}` |
| Query params | camelCase | `?pageSize=20&sortBy=ticker` |
| JSON payload fields | camelCase | `{ "fibraId": "...", "capturedAt": "..." }` |
| Fechas/timestamps | ISO 8601 UTC | `"2026-03-30T14:00:00Z"` |
| Enums | strings estables | `"fresh"`, `"stale"`, `"partial"` (no ordinales numéricos) |

## Nomenclatura de Código C# (Backend)

- Tipos y miembros públicos: `PascalCase`
- Campos privados: `_camelCase`
- Un tipo público principal por archivo
- Domain events: PascalCase past-tense → `PdfReportDetected`, `MarketSnapshotStored`
- Event payloads: solo identificadores, timestamps, correlationId, contexto mínimo necesario

## Nomenclatura de Código TypeScript (Frontend)

| Elemento | Convención | Archivo |
|---|---|---|
| Componentes React | PascalCase | `FibraCard.tsx` |
| Custom hooks | camelCase con prefijo `use` | `useFibraSearch.ts` |
| Utilidades (no hooks) | kebab-case | `format-currency.ts` |
| Un componente principal por archivo | — | — |

## Respuestas API

| Situación | Formato |
|---|---|
| Recurso único exitoso | Recurso directo (sin wrapper) |
| Colección exitosa | `{ items, page, pageSize, total }` |
| Comando async operativo | `202 Accepted` + `{ pipelineRunId }` o similar |
| Error | `ProblemDetails` con `domainCode` + `correlationId` |
| Datos faltantes | `null` explícito (nunca campo ausente para indicar "no hay dato") |
| Estados parciales | campo `status: "partial"` o equivalente explícito |

## Estado de Datos — Estados Permitidos

Siempre representar explícitamente — nunca inferir de campo ausente:
- `fresh` — dato actualizado dentro de umbrales
- `stale` — dato desactualizado pero dentro de ventana aceptable
- `critico` — dato muy desactualizado o pipeline fallando
- `fuera-de-horario` — mercado cerrado, dato es el último precio de cierre válido
- `partial` — algunos componentes disponibles, otros no
- `error` — fallo en procesamiento
- `null` — dato genuinamente no disponible (mostrar `—` en UI)
- `no evaluable` — imposible calcular (ej: sin precio en oportunidades)

## Organización de Archivos Backend

```
src/Server/
  Api/
    Endpoints/Public/    ← Home, Mercado, Catálogo, Noticias, Ficha, Comparador
    Endpoints/Private/   ← Portafolio, Dashboard, Oportunidades
    Endpoints/Ops/       ← Dashboard ops, Pipelines, Fundamentales, Catálogo, Config
  Application/<Module>/  ← Commands, Queries, Handlers, puertos internos
  Domain/<Module>/       ← Entities, Value Objects, Domain Events
  Infrastructure/
    Persistence/Configurations/  ← EF Core entity configurations
    Persistence/Migrations/      ← Code-first migrations (separadas por módulo)
    Persistence/Projections/     ← Read models cross-module con owner explícito
    Jobs/<Module>/               ← Hangfire job implementations
    Integrations/Yahoo/          ← Cliente Yahoo Finance
    Integrations/GoogleNews/     ← Cliente Google News RSS
    Integrations/PdfDiscovery/   ← Discovery de PDFs por FIBRA
    Integrations/Ai/             ← Bridge proveedor IA
```

## Organización de Archivos Frontend

```
src/Web/Main/src/
  app/          ← Router, providers, shell
  api/          ← Cliente generado desde OpenAPI (NO editar manualmente)
  modules/      ← Feature folders por módulo de negocio
    home/
      components/
      hooks/
      index.ts
  shared/
    ui/         ← Primitivos shadcn/ui personalizados
    hooks/      ← Hooks compartidos
    lib/        ← Configuraciones de librerías (queryClient, etc.)
    utils/      ← Utilidades puras
    types/      ← Tipos compartidos que no vienen del API client
```

## Reglas de Módulos Frontend

- Main y Ops son apps separadas — no comparten módulos de negocio
- Solo comparten: primitivos UI aprobados, utilidades comunes controladas, cliente API generado
- No comparten stores de estado de dominio
- `api/` generado desde OpenAPI — nunca editarlo manualmente; cualquier wrapper custom va fuera del directorio generado

## Migrations

- Code-first, manejadas desde `Infrastructure`
- Un stream de migrations para el monolito deployable
- Nombrado con prefijo de módulo: `Catalog_InitialTables`, `Market_AddPriceSnapshot`

## Prohibiciones Explícitas

- Un módulo NO lee directo las tablas de otro módulo en sus repositorios
- `SharedApiContracts` NO contiene lógica de negocio ni comportamiento de dominio
- Frontend store NO duplica server state que está bajo TanStack Query
- NO usar umbrales numéricos absolutos en el score (es por percentil dentro del universo activo)
- NO asumir frecuencia trimestral fija de distribuciones (detectar del patrón real)
- NO inventar datos ni inferir cifras ausentes en resúmenes IA
