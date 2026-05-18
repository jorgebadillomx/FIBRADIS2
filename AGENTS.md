# FIBRADIS — Instrucciones para Agentes de IA

Este documento es leído automáticamente por **Claude** y **Codex** al entrar al repositorio.

## Flujo de trabajo entre agentes

**Claude Code** implementa historias con `/bmad-dev-story`.  
**Codex** revisa el código con `/bmad-code-review` (o el skill equivalente).  
La comunicación entre agentes ocurre a través del **story file** — no de mem0.

El story file es la fuente de verdad de cada historia:
- Dev Notes → contexto de implementación
- Dev Agent Record → decisiones tomadas durante implementación
- Senior Developer Review (AI) → hallazgos del code review
- Status → estado actual (`ready-for-dev` → `in-progress` → `review` → `done`)

### Convenciones obligatorias para todos los agentes

Lee `_bmad-output/planning-artifacts/convenciones-fibradis.md` antes de empezar cualquier historia o review. Contiene las reglas de stack, código y flujo que NO deben ignorarse.

Lee `workflow-rules.md` para las reglas operativas del proyecto: branch por historia, merge a main al completar, y unit tests obligatorios antes de `done`. **Son de cumplimiento estricto — no opcionales.**

### mem0 — solo cuando el contexto no vive en ningún archivo

**No usar mem0 por defecto.** Usarlo únicamente si:

1. Tomaste una decisión que contradice o extiende el story file Y afectará historias futuras.
2. Detectaste un patrón de error recurrente no documentado en ningún story ni aquí.
3. Encontraste una restricción del proyecto no documentada en ningún archivo.

Si aplica:

```bash
python scripts/memory/memory_cli.py search "<tema>"     # buscar
python scripts/memory/memory_cli.py add "decisión: ..."  # guardar
```

Los archivos semilla están en `project-memory/seeds/`.

## Qué es FIBRADIS

Plataforma web integral de análisis de **FIBRAs inmobiliarias mexicanas**. No ejecuta operaciones bursátiles. Convierte información dispersa (PDFs, Yahoo Finance, Google News) en métricas estructuradas con score configurable.

**Tres superficies:**
- **Mundo público** — Home, catálogo, ficha, comparador (sin auth)
- **Mundo privado** — Portafolio, dashboard, oportunidades (auth requerida)
- **Centro de Procesos `/ops`** — Solo `AdminOps`

## Stack

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core (.NET 10) + EF Core 10 + SQL Server |
| Jobs | Hangfire in-app |
| SPA Main | Vite 7 + React 19.2 + TypeScript |
| SPA Ops | Vite 7 + React 19.2 + TypeScript |
| UI | shadcn/ui + Tailwind CSS v4 |
| State | TanStack Query v5 + React Router 7 |

## Reglas Críticas (No Violar)

1. **Módulos**: ningún módulo accede directo a la persistencia de otro módulo
2. **SQL Server** es la única fuente de verdad; cache y cliente son vistas derivadas
3. **OpenAPI** generado desde backend → `SharedApiClient` para ambos SPAs
4. **No existe `/dashboard`** como ruta separada — está unificado en `/portafolio`
5. Estado de datos siempre explícito: `fresh`, `stale`, `partial`, `error`, `null` — nunca inferido
6. Toda acción manual de Ops queda auditada con actor + timestamp

## Schemas SQL (propietarios de datos)

`catalog` | `market` | `news` | `fundamentals` | `portfolio` | `ai` | `jobs`

Favoritos vive en schema `portfolio`. No existe schema `alerts` en MVP.

## Convenciones de Código

- **DB**: tablas `PascalCase`, columnas `snake_case`, schemas `lowercase`
- **API routes**: `/api/v1/resource-name` (plural, kebab-case)
- **JSON fields**: `camelCase`
- **C#**: tipos `PascalCase`, campos privados `_camelCase`
- **TypeScript**: componentes `PascalCase.tsx`, hooks `useThing.ts`, utils `kebab-case.ts`

## Estado Actual del Proyecto

Ver `project-memory/seeds/05-current-status.md` para el estado detallado de implementación.

## Documentación Completa

- `docs/req/prd.md` — Product Requirements Document completo
- `docs/req/architecture.md` — Decisiones arquitectónicas
- `project-memory/seeds/` — Memoria condensada del proyecto
