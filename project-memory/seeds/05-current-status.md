# FIBRADIS — Estado Actual de Implementación

_Última actualización: 2026-05-19_

## Progreso General

| Épica | Título | Estado |
|---|---|---|
| Épica 1 | Fundación, Infraestructura y Acceso | done |
| Épica 2 | Catálogo Maestro y Descubrimiento Público | done |
| Épica 3 | Mercado y Frescura de Datos | done |
| Épica 4 | Noticias y Contenido | in-progress |
| Épica 5 | Centro de Procesos y Fundamentales | backlog |
| Épica 6 | Portafolio Unificado | backlog |
| Épica 7 | Oportunidades y Favoritos | backlog |

## Estado Detallado por Historia

### Épica 1 — Fundación, Infraestructura y Acceso (done)

| Story | Título | Estado |
|---|---|---|
| 1-1 | Inicialización de la solución y estructura del proyecto | done |
| 1-2 | Backend API v1 con OpenAPI y cliente tipado para los SPAs | done |
| 1-3 | Autenticación JWT y autorización por roles | done |
| 1-4 | Hangfire, health checks y observabilidad mínima | done |
| Retrospectiva | — | done |

**Entregables clave:** .NET 10 + EF Core, solución .slnx, dos SPAs Vite 7 + React 19.2, API v1 con OpenAPI, SharedApiClient TypeScript, JWT con refresh tokens rotados, Hangfire in-app, health checks con correlation IDs, 29+ tests de integración.

**Deuda técnica pendiente:**
- Health endpoint sin tipo de respuesta en OpenAPI (`responses.200.content?: never` en SharedApiClient)
- No existe test de integración para la UI de Scalar (`/swagger`)
- Validaciones manuales de 1-4 sin completar: `curl /health` en runtime + dashboard Hangfire
- Épica no desplegada en ambiente productivo

### Épica 2 — Catálogo Maestro y Descubrimiento Público (done)

| Story | Título | Estado |
|---|---|---|
| 2-1 | Catálogo maestro de FIBRAs con datos semilla iniciales | done |
| 2-2 | Home pública con búsqueda global y layout | done |
| 2-3 | Ficha pública de FIBRA | done |
| 2-4 | SEO prerender y accesibilidad WCAG 2.1 AA | done |
| 2-5 | Home TopMovers tabla y ganadores/perdedores | done |
| Retrospectiva | — | done |

**Entregables clave:** Catálogo de FIBRAs con datos semilla (FUNO11, FMTY14, DANHOS13, TERRA13, FIBRAPL14, FIBRAMQ12), home pública con búsqueda global + autocompletado (máx. 8 resultados), ficha pública completa con secciones placeholder para épicas futuras, prerender SEO, WCAG 2.1 AA verificado, tabla TopMovers con ganadores/perdedores.

### Épica 3 — Mercado y Frescura de Datos (done)

| Story | Título | Estado |
|---|---|---|
| 3-1 | Pipeline de mercado, ingesta y snapshots | done |
| 3-2 | Clasificación de frescura y estados en UI | done |
| 3-3 | Historial de precios, yield anualizado y snapshots a 90 días | done |
| Retrospectiva | — | done |

**Entregables clave:** Pipeline Yahoo Finance cada 15 min en horario BMV, snapshots de mercado, estados fresh/stale/crítico en UI, historial de precios con yield anualizado, gráfica de precios a 90 días.

### Épica 4 — Noticias y Contenido (in-progress)

| Story | Título | Estado |
|---|---|---|
| 4-1 | Ingesta RSS, blocklist y deduplicación de noticias | done |
| 4-2 | Asociación de noticias con FIBRAs y display en home y ficha | review |
| 4-3 | Soporte para AI mode en noticias (Off y Manual) | backlog |

**4-2 en review:** implementación completa pendiente de code review.

## Código Existente Confirmado

### Backend
- `src/Server/Api/` — proyecto API con endpoints v1, OpenAPI configurado
- `src/Server/Domain/` — módulos: Catalog, Market, News, Fundamentals, Portfolio, Dashboard, Opportunities, Ops, Ai, Common
- `src/Server/Infrastructure/` — EF Core, migraciones, pipelines Hangfire
- `src/Server/Application/` — servicios de aplicación por módulo
- 29+ tests de integración en `tests/Api.Tests/` y `tests/Jobs.Tests/`

### Frontend Main (`src/Web/Main/`)
- Home pública con búsqueda global + autocompletado
- Tabla TopMovers con ganadores/perdedores
- Ficha pública de FIBRA con secciones de mercado, historial, noticias (placeholders para fundamentales, distribuciones)
- Layout responsivo (360px / 768px / 1280px+)
- Header fijo con buscador

### Frontend Ops (`src/Web/Ops/`)
- Estructura creada, pendiente de implementar (Épica 5+)

### Configuración
- `FIBRADIS.slnx` — solution file (formato .slnx, no .sln)
- `Directory.Build.props` + `Directory.Packages.props` — CPM activo
- `global.json` — .NET 10 fijado
- `package.json` raíz — workspace npm con Main, Ops, SharedApiClient

## Decisiones Técnicas Importantes (post-implementación)

| Decisión | Área | Historia |
|---|---|---|
| `.slnx` en lugar de `.sln` — formato nuevo de VS, no compatible con herramientas antiguas | Build | 1-1 |
| `shadcn@4.6.0` — v4.7 tiene bug de detección de workspace | Frontend | 1-1 |
| `npm create vite@7` — `npm create vite@latest` instala Vite 8 | Frontend | 1-1 |
| CPM incompatible con `dotnet new` — limpiar versiones inline después de scaffold | Backend | 1-1 |
| `ValidateOnStart()` + MSBuild task — requerido para que OpenAPI funcione en build-time con JWT | Auth | 1-3 |
| `ExecuteUpdateAsync` con `WHERE RevokedAt IS NULL` — race condition si se omite | Auth | 1-3 |
| `Request.IsHttps` en lugar de hostname literal para `Cookie.Secure` | Auth | 1-3 |

## Parámetros Operativos Actuales

| Parámetro | Valor | Editable desde Ops |
|---|---|---|
| `AI_MODE` | `Off` | Sí (Épica 5) |
| Pipeline mercado | Cada 15 min, horario BMV | Sí (Épica 5) |
| Pipeline noticias | Cada 1 día | Sí (Épica 5) |
| Umbral `fresh` | < 20 minutos | No (código) |
| Umbral `stale` | 20 min – 6 horas | No (código) |
| Umbral `crítico` | > 6 horas | No (código) |
| `portfolio.avg_periods` | 4 periodos | Sí (Épica 5) |
| Umbral degradación universo | 30% sin precio | Sí (Épica 5) |

## Notas Importantes para Agentes

1. **No existe `/dashboard`** como ruta — está unificado en `/portafolio`
2. **Favoritos** vive en schema `portfolio`, no tiene módulo separado
3. **Dashboard** y **Opportunities** son read models — sin schema propio en MVP
4. El **comparador público** es Growth, no MVP
5. **AI_MODE=Api** es Growth — en MVP solo Off y Manual
6. Los **tests de contrato** en `tests/Contract/` validan compatibilidad del API entre backend y ambos SPAs
7. Las **migraciones** son code-first desde Infrastructure con un solo stream para el monolito
8. Las secciones de Mercado, Fundamentales, Distribuciones y Noticias en la ficha de FIBRA muestran placeholders — es comportamiento esperado, no un bug
