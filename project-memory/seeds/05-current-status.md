# FIBRADIS — Estado Actual de Implementación

_Última actualización: 2026-05-15_

## Progreso General

El proyecto tiene su arquitectura completamente definida y aprobada. El PRD y el documento de arquitectura están finalizados. Se inició la implementación según los implementation artifacts del sprint.

## Stories de Implementación Identificadas

Basado en `_bmad-output/implementation-artifacts/`:

| Story | Archivo | Estado |
|---|---|---|
| 1-1 | Inicialización de solución y estructura del proyecto | Por determinar |
| 1-2 | Backend API v1 con OpenAPI y cliente tipado para los SPAs | Por determinar |
| 1-3 | Autenticación JWT y autorización por roles | Por determinar |
| 1-4 | Hangfire, health checks y observabilidad mínima | Por determinar |
| 2-1 | Catálogo maestro de FIBRAs con datos semilla iniciales | Por determinar |
| 2-2 | Home pública con búsqueda global y layout | Por determinar |

## Código Existente Confirmado

### Backend
- `src/Server/Api/` — estructura de proyecto creada con capas
- `src/Server/Domain/` — carpetas por módulo presentes (Catalog, Market, News, Fundamentals, Portfolio, Dashboard, Opportunities, Ops, Ai, Common)
- Estructura de carpetas en Infrastructure, Application parcialmente creada

### Frontend
- `src/Web/Main/src/` — estructura con `modules/`, `shared/`, `api/`, `app/` creada
- `src/Web/Ops/src/` — estructura similar para ops
- shadcn/ui + Tailwind configurado

### Configuración
- `FIBRADIS.slnx` — solution file
- `Directory.Build.props` y `Directory.Packages.props` — configuración centralizada
- `global.json` — versión .NET fijada
- `package.json` raíz — workspace npm con Main, Ops, SharedApiClient

## Integraciones Externas Configuradas

- **Yahoo Finance**: fuente de precios y distribuciones (best effort)
- **Google News RSS**: ingesta de noticias
- **PDF Discovery**: config-driven por FIBRA
- **AI Manual (MVP)**: Claude Code CLI como skill externo para importar fundamentales

## Parámetros Operativos por Defecto

| Parámetro | Valor por defecto | Editable desde Ops |
|---|---|---|
| `AI_MODE` | `Off` | Sí |
| `portfolio.avg_periods` | `4` periodos | Sí |
| `portfolio.commission_factor` | Documentar en config inicial | Sí |
| Pipeline mercado | Cada 15 min, horario BMV | Sí |
| Pipeline noticias | Cada 1 hora | Sí |
| Umbral `fresh` | < 20 minutos | No (código) |
| Umbral `stale` | 20 min – 6 horas | No (código) |
| Umbral `critico` | > 6 horas | No (código) |
| Umbral degradación universo | 30% sin precio | Sí |

## Datos Semilla Necesarios

El catálogo maestro necesita datos semilla de las FIBRAs activas en BMV. Al menos:
- FUNO11 (Fibra Uno)
- FMTY14 (Fibra Monterrey)
- DANHOS13 (Danhos)
- TERRA13 (Terra)
- FIBRAPL14 (Fibra PL)
- FIBRAMQ12 (Fibra MQ)

## Notas Importantes para Agentes

1. **No existe `/dashboard` como ruta** — está unificado en `/portafolio`
2. **Favoritos** vive en schema `portfolio`, no tiene módulo separado
3. **Dashboard** y **Opportunities** son read models — sin schema propio en MVP
4. El **comparador público** es Growth, no MVP
5. **AI_MODE=Api** es Growth — en MVP solo Off y Manual
6. Los **tests de contrato** en `tests/Contract/` validan compatibilidad del API entre backend y ambos SPAs
7. Las **migraciones** son code-first desde Infrastructure con un solo stream para el monolito

## Actualizar Este Archivo

Cuando completes una story o tomes una decisión importante, actualiza este archivo Y agrega una memoria a mem0:

```bash
python scripts/memory/memory_cli.py add "Story 1-1 completada: solución inicializada con .NET 10, estructura de carpetas según arquitectura"
```
