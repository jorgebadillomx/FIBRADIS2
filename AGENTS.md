# FIBRADIS — Instrucciones para Agentes de IA

Este documento es leído automáticamente por **Claude** y **Codex** al entrar al repositorio.

## Memoria Persistente entre Agentes

Este proyecto usa **mem0** como sistema de memoria compartida entre agentes. El modo recomendado es `managed` (mem0.ai, tier gratuito — sin necesitar API de OpenAI ni Anthropic):

```powershell
# Primera vez — configura el modo y la key de mem0.ai
$env:MEM0_MODE    = "managed"
$env:MEM0_API_KEY = "m0-..."   # obtén tu key gratis en https://app.mem0.ai

pip install -r scripts/memory/requirements.txt
python scripts/memory/setup_mem0.py
```

Alternativa offline con Ollama (sin ninguna API key):

```powershell
$env:MEM0_MODE = "ollama"   # requiere Ollama corriendo con nomic-embed-text + llama3.2
```

### Uso diario

```bash
# Antes de trabajar en una tarea — carga contexto relevante
python scripts/memory/memory_cli.py search "portafolio módulo schema"

# Después de una decisión importante — guarda en mem0
python scripts/memory/memory_cli.py add "Decisión: X porque Y"

# Ver todas las memorias
python scripts/memory/memory_cli.py list
```

Los archivos semilla con el conocimiento base están en `project-memory/seeds/`.

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
