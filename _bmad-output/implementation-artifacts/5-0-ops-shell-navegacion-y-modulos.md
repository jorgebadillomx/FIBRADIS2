# Historia 5.0: Ops Shell — Navegación multi-pantalla, logs de errores estructurados y gestión de prompts de IA

Status: done

## Story

Como AdminOps,
quiero que el panel Ops esté organizado en pantallas separadas con navegación lateral, con un módulo para ver los logs de errores del pipeline de forma estructurada y legible por IA, y un módulo para editar los prompts de IA usados en el pipeline de noticias y documentos,
para poder navegar eficientemente entre funcionalidades operativas, diagnosticar problemas del pipeline con contexto claro, y ajustar la calidad de los resúmenes generados sin redespliegue ni acceso a código.

## Contexto y motivación

La interfaz Ops actual (`App.tsx`) tiene todas las funcionalidades en una sola página sin navegación entre secciones. Las Épicas 5-7 requieren múltiples módulos operativos — dashboard, pipelines, catálogo, configuración — y la arquitectura actual no puede escalar a eso. Esta historia establece la fundación de navegación (OpsShell con React Router v7) y agrega dos módulos operativos críticos:

1. **Logs de errores estructurados**: Los jobs actuales solo escriben a ILogger. No hay forma de ver errores del pipeline desde Ops. Los errores nuevos incluirán `AiContext` — texto descriptivo en lenguaje natural para que una IA (como Claude Code o Codex) pueda interpretar el problema sin acceso al servidor.

2. **Prompts de IA editables**: Los prompts de `GeminiAiSummaryService` y `DeepSeekAiSummaryService` están hardcodeados en `BuildPrompt()`. Si el prompt produce resúmenes de mala calidad, hoy requiere un redespliegue. Con esta historia, los prompts se almacenan en BD y se editan desde Ops.

Esta historia es prerrequisito de todas las historias de Épica 5.

## Acceptance Criteria

### AC1 — OpsShell con navegación multi-pantalla

**Dado que** navego a cualquier ruta del panel Ops,
**Cuando** la página carga y el login está activo,
**Entonces** veo un sidebar de navegación a la izquierda con las secciones: `AI Config`, `Noticias`, `Blocklist`, `Logs del Pipeline`, `Prompts de IA`.

**Dado que** hago clic en cualquier sección del sidebar,
**Entonces** la URL cambia a la ruta correspondiente y el contenido principal muestra la página correcta, con la sección activa destacada en el sidebar.

**Dado que** navego a la raíz `/`,
**Entonces** soy redirigido automáticamente a `/ai-config`.

**Dado que** el `OpsLoginGate` envuelve el shell completo,
**Entonces** todas las rutas requieren autenticación antes de cargar — sin cambio funcional en el comportamiento de login.

**Módulos existentes migrados como rutas (sin cambios funcionales):**
- `/ai-config` → `AiModeSection` existente
- `/noticias` → `NewsBodyTextSection` existente
- `/blocklist` → Blocklist inline de `App.tsx`, extraído a `BlocklistPage`

### AC2 — Módulo de Logs del Pipeline

**Dado que** ocurre un error en `MarketPipelineJob`, `NewsPipelineJob`, `DistributionPipelineJob` o `NewsBodyTextRetryJob`,
**Cuando** el job captura la excepción,
**Entonces** se escribe un registro en `jobs.PipelineErrorLog` con los campos:
- `Pipeline`: nombre del pipeline (`Market`, `News`, `Distribution`, `BodyTextRetry`)
- `Timestamp`: momento del error (UTC)
- `ErrorType`: tipo de excepción (nombre de la clase)
- `Message`: mensaje de la excepción
- `Context`: JSON con datos relevantes del contexto (ticker, URL, articleId, etc. — según el job)
- `AiContext`: texto en lenguaje natural describiendo qué estaba haciendo el pipeline, qué datos tenía, y qué operación falló, pensado para ser interpretado por una IA sin contexto adicional (mínimo 100 caracteres, máximo 800)
- `CreatedAt`: timestamp de inserción

**Ejemplos de AiContext por pipeline:**
- Market: `"El pipeline de mercado falló al procesar el precio de {Ticker} ({Exchange}). La solicitud HTTP a Yahoo Finance retornó {StatusCode}. El batch incluía {N} FIBRAs y este fue el único ticker con error."`
- News: `"El pipeline de noticias falló al guardar el artículo '{Title}' desde la URL '{Url}' (fuente: {Source}). El artículo pasó el filtro del blocklist y tenía {N} FIBRAs asociadas. Error al persistir en BD."`
- AI summary: `"El servicio de IA falló al generar el resumen para el artículo ID {ArticleId} ('{Title}'). El proveedor activo era {Provider}/{Model}. body_text disponible: {HasBody}. El artículo fue marcado como Partial."`

**Dado que** navego a `/pipeline-logs` en Ops,
**Cuando** la página carga,
**Entonces** veo una tabla con los últimos 100 registros de `PipelineErrorLog`, paginada (50 por página), con columnas: Pipeline, Timestamp, ErrorType, Message, AiContext.

**Dado que** selecciono un filtro de pipeline (`Market`, `News`, `Distribution`, `BodyTextRetry`, o `Todos`),
**Entonces** la tabla muestra solo los registros del pipeline seleccionado.

**Dado que** expando un registro de la tabla (clic en la fila),
**Entonces** veo el campo `Context` (JSON formateado) y el `AiContext` completo.

### AC3 — Módulo de Gestión de Prompts de IA

**Dado que** existe la tabla `ai.AiPrompt` con dos registros seed,
**Cuando** los servicios `GeminiAiSummaryService` y `DeepSeekAiSummaryService` construyen el prompt para una llamada,
**Entonces** cargan el template desde BD usando `IAiPromptRepository.GetPromptAsync(contentType)` e interpollan las secciones dinámicas antes de enviar a la API.

**Dado que** navego a `/ai-prompts` en Ops,
**Cuando** la página carga,
**Entonces** veo dos editores de texto (uno para `news`, otro para `document`), cada uno con el template actual, botón `Guardar`, y un mensaje con el autor y fecha del último cambio.

**Dado que** edito el prompt de noticias y hago clic en `Guardar`,
**Entonces** el nuevo template se persiste en BD con auditoría (`UpdatedBy`, `UpdatedAt`), y el siguiente resumen generado por el pipeline usa el template actualizado.

**Dado que** el template guardado no contiene los placeholders `{title}`, `{snippet_section}` o `{body_section}`,
**Entonces** el endpoint retorna un error de validación: `"El template debe contener los placeholders: {title}, {snippet_section}, {body_section}"`.

**Dado que** la BD no tiene un registro para un `contentType`,
**Entonces** los servicios de IA usan el prompt hardcoded actual como fallback (sin lanzar excepción).

### AC5 — Tabla de noticias: columna de Resumen IA y búsqueda/filtrado

**Dado que** navego a `/noticias`,
**Cuando** la tabla carga,
**Entonces** existe una columna **"Resumen IA"** que muestra los primeros 120 caracteres del resumen generado por IA (truncado con `…`), o el texto `Sin resumen` si el campo `AiSummary` es null.

**Dado que** un artículo tiene resumen IA,
**Cuando** expando su fila (editor de body text ya existente),
**Entonces** el panel expandido muestra también el resumen IA completo en un bloque de solo lectura, etiquetado como "Resumen de IA".

**Dado que** escribo texto en el input de búsqueda de la tabla,
**Cuando** el usuario escribe al menos 2 caracteres y hace pausa (debounce ~400ms),
**Entonces** la tabla muestra solo los artículos cuyo título, body text o resumen IA contienen el texto buscado (búsqueda LIKE en backend), y la paginación se reinicia a la página 1.

**Dado que** selecciono el filtro "Solo con resumen IA" o "Solo sin resumen IA",
**Entonces** la tabla filtra en consecuencia, acumulable con la búsqueda de texto.

**Cambios backend necesarios:**
- `OpsNewsArticleDto` agrega el campo `aiSummaryPreview` (primeros 300 chars del resumen IA, o null)
- El endpoint `GET /api/v1/ops/news` acepta los query params opcionales: `search` (string, búsqueda LIKE en título + bodyText + aiSummary), `hasAiSummary` (bool?, null = todos)
- La búsqueda LIKE se aplica con `%{search}%` sobre los tres campos usando EF — es una tabla pequeña (< 10k filas en MVP), no requiere full-text search

### AC4 — Sin regresiones

Todos los tests existentes siguen pasando tras los cambios en `GeminiAiSummaryService`, `DeepSeekAiSummaryService` y los jobs.

## Tasks / Subtasks

### Backend

- [x] **T1: Dominio — entidades nuevas**
  - [x] T1.1 Crear `src/Server/Domain/News/AiPrompt.cs` — entidad `AiPrompt { Id (int), ContentType (string), PromptTemplate (string), UpdatedAt, UpdatedBy }`
  - [x] T1.2 Crear `src/Server/Domain/Jobs/PipelineErrorLog.cs` — entidad `PipelineErrorLog { Id (Guid), Pipeline (string), Timestamp, ErrorType (string), Message (string), Context (string?), AiContext (string), CreatedAt }`

- [x] **T2: Application — interfaces de repositorio**
  - [x] T2.1 Crear `src/Server/Application/News/IAiPromptRepository.cs` — métodos: `GetPromptAsync(string contentType, CancellationToken ct)`, `SetPromptAsync(string contentType, string template, string actor, CancellationToken ct)`
  - [x] T2.2 Crear `src/Server/Application/Jobs/IPipelineErrorLogRepository.cs` — métodos: `LogErrorAsync(PipelineErrorLog entry, CancellationToken ct)`, `GetPagedAsync(string? pipeline, int page, int pageSize, CancellationToken ct)`

- [x] **T3: Infrastructure — EF + migración + repositorios**
  - [x] T3.1 Configurar `AiPromptConfig` en `AppDbContext` / `OnModelCreating`: tabla `ai.AiPrompt`, `Id` (PK int), `ContentType` (unique, varchar 20), seed con los dos prompts actuales hardcodeados (`news`, `document`)
  - [x] T3.2 Configurar `PipelineErrorLogConfig`: tabla `jobs.PipelineErrorLog`, `Id` (Guid, default `newsequentialid()`), índice no-único sobre `(Pipeline, CreatedAt DESC)`
  - [x] T3.3 Generar migración EF: `dotnet ef migrations add AddAiPromptAndPipelineErrorLog --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [x] T3.4 Crear `src/Server/Infrastructure/Persistence/Repositories/News/AiPromptRepository.cs` implementando `IAiPromptRepository`
  - [x] T3.5 Crear `src/Server/Infrastructure/Persistence/Repositories/Jobs/PipelineErrorLogRepository.cs` implementando `IPipelineErrorLogRepository`
  - [x] T3.6 Registrar repositorios en `ApiServiceExtensions.cs`

- [x] **T4: Services — prompt desde BD**
  - [x] T4.1 Actualizar `GeminiAiSummaryService`: inyectar `IAiPromptRepository`; en `BuildPrompt()` cargar template de BD con `GetPromptAsync("news")` o `GetPromptAsync("document")` según `contentType`; interpolar `{title}`, `{snippet_section}`, `{body_section}`, `{strictness_instruction}` en el template; si BD no retorna template, usar el string hardcoded actual como fallback. Cambiar de `static` a método de instancia.
  - [x] T4.2 Aplicar el mismo cambio en `DeepSeekAiSummaryService`
  - [x] T4.3 El seed de `ai.AiPrompt` para `news` debe ser el prompt actual completo con los placeholders `{strictness_instruction}`, `{title}`, `{snippet_section}`, `{body_section}` en las posiciones correctas

- [x] **T5: Jobs — escribir a PipelineErrorLog**
  - [x] T5.1 Inyectar `IPipelineErrorLogRepository` en `MarketPipelineJob`: en cada catch que ya llama `LogError`, también insertar en `PipelineErrorLog` con `AiContext` descriptivo del ticker y operación
  - [x] T5.2 Inyectar en `NewsPipelineJob`: para errores de AI summary y errores de guardado, insertar registro con URL, título y proveedor en `AiContext`
  - [x] T5.3 Inyectar en `NewsBodyTextRetryJob`: para errores de scraping o AI, insertar registro con articleId y URL
  - [x] T5.4 Inyectar en `DistributionPipelineJob` si tiene manejo de errores relevante (verificar primero)

- [x] **T6: Endpoints API**
  - [x] T6.1 Crear `src/Server/Api/Endpoints/Ops/OpsAiPromptEndpoints.cs`:
    - [x] `GET /api/v1/ops/ai-prompts/{contentType}` → retorna `{ contentType, promptTemplate, updatedAt, updatedBy }`
    - [x] `PUT /api/v1/ops/ai-prompts/{contentType}` → valida placeholders requeridos, guarda, retorna 204
    - [x] Validar que `contentType` sea `news` o `document`
    - [x] `RequireAuthorization("AdminOps")`
  - [x] T6.2 Crear `src/Server/Api/Endpoints/Ops/OpsPipelineLogEndpoints.cs`:
    - [x] `GET /api/v1/ops/pipeline-logs?pipeline=all&page=1&pageSize=50` → retorna `PagedResult<PipelineErrorLogDto>`
    - [x] `RequireAuthorization("AdminOps")`
  - [x] T6.3 Registrar ambos endpoints en `Program.cs` (junto a los demás `Map*`)
  - [x] T6.4 Actualizar `SharedApiClient` con `npm run codegen:api`

### Frontend (Ops SPA)

- [x] **T7: OpsShell — navegación con React Router v7**
  - [x] T7.1 Crear `src/Web/Ops/src/components/OpsShell.tsx`: layout con sidebar de navegación y `<Outlet />` para el contenido principal. El sidebar muestra: `AI Config` (→ `/ai-config`), `Noticias` (→ `/noticias`), `Blocklist` (→ `/blocklist`), `Logs del Pipeline` (→ `/pipeline-logs`), `Prompts de IA` (→ `/ai-prompts`). El item activo se destaca con estilos consistentes con el diseño actual (teal-700/border-teal-700).
  - [x] T7.2 Actualizar `src/Web/Ops/src/main.tsx`: usar `createBrowserRouter` de `react-router` con las rutas definidas bajo el OpsShell. La ruta raíz `/` redirige a `/ai-config`. `OpsLoginGate` envuelve `OpsShell` (o se mantiene como wrapper del router provider).
  - [x] T7.3 Actualizar `src/Web/Ops/src/App.tsx` (o eliminar si ya no es necesario): el estado y lógica del blocklist se mueven a `BlocklistPage.tsx`.

- [x] **T8: Módulos existentes como páginas**
  - [x] T8.1 `AiModeSection.tsx` ya existe — crear `src/Web/Ops/src/pages/AiConfigPage.tsx` que simplemente renderiza `<AiModeSection />`
  - [x] T8.2 `NewsBodyTextSection.tsx` ya existe — crear `src/Web/Ops/src/pages/NewsBodyPage.tsx` que renderiza `<NewsBodyTextSection />`
  - [x] T8.3 Crear `src/Web/Ops/src/pages/BlocklistPage.tsx`: mover la lógica del blocklist (actualmente inline en `App.tsx`) a este componente

- [x] **T9: Nuevas páginas**
  - [x] T9.1 Crear `src/Web/Ops/src/api/pipelineLogsApi.ts`: `fetchPipelineLogs(pipeline, page, pageSize)` usando el cliente `SharedApiClient`
  - [x] T9.2 Crear `src/Web/Ops/src/api/aiPromptsApi.ts`: `fetchAiPrompt(contentType)` y `updateAiPrompt(contentType, template)`
  - [x] T9.3 Crear `src/Web/Ops/src/pages/PipelineLogsPage.tsx`:
    - [x] Tabla con columnas: Pipeline (badge de color), Timestamp, ErrorType, Message (truncado a 80 chars), AiContext (expandible)
    - [x] Filtro de pipeline (dropdown: Todos, Market, News, Distribution, BodyTextRetry)
    - [x] Paginación (50 por página)
    - [x] Al hacer clic en una fila, expande para mostrar `Context` (JSON formateado) y `AiContext` completo
  - [x] T9.4 Crear `src/Web/Ops/src/pages/AiPromptsPage.tsx`:
    - [x] Dos secciones: una para `news`, otra para `document`
    - [x] Cada sección tiene: `<textarea>` con el template actual, botón `Guardar`, texto de auditoría (quién y cuándo fue el último cambio)
    - [x] Al guardar, muestra error si faltan placeholders requeridos
    - [x] Estado `Guardando...` durante la mutación

### Tabla de noticias — columna Resumen IA + búsqueda (AC5)

- [x] **T12: Backend — ampliar endpoint y DTO de noticias**
  - [x] T12.1 Actualizar `OpsNewsArticleDto` en `AiModeEndpoints.cs`: agregar campo `string? AiSummaryPreview` (primeros 300 chars de `AiSummary`, o null)
  - [x] T12.2 Actualizar el endpoint `GET /api/v1/ops/news`: agregar query params `string? search` y `bool? hasAiSummary`
  - [x] T12.3 Actualizar `INewsRepository.GetPagedForOpsAsync` para aceptar los nuevos filtros: `(int page, int pageSize, string? search, bool? hasAiSummary, CancellationToken ct)`
  - [x] T12.4 En `NewsRepository.GetPagedForOpsAsync`: aplicar `.Where(a => a.Title.Contains(search) || (a.BodyText != null && a.BodyText.Contains(search)) || (a.AiSummary != null && a.AiSummary.Contains(search)))` cuando `search` tiene valor; aplicar `.Where(a => a.AiSummary != null)` o `.Where(a => a.AiSummary == null)` según `hasAiSummary`
  - [x] T12.5 Ejecutar `npm run codegen:api` para actualizar `SharedApiClient`

- [x] **T13: Frontend — columna y búsqueda en NewsBodyPage**
  - [x] T13.1 En `NewsBodyPage.tsx` (nueva página, o en `NewsBodyTextSection.tsx` mientras se migra): agregar input de búsqueda (`search`) con debounce de 400ms usando `setTimeout`/`clearTimeout`; al cambiar, reiniciar a `page=1`
  - [x] T13.2 Agregar filtro `hasAiSummary`: select/radio con opciones `Todos` | `Con resumen IA` | `Sin resumen IA`; pasar como query param
  - [x] T13.3 Pasar `search` y `hasAiSummary` al query key de TanStack Query y a `fetchOpsNewsList` para que los cambios disparen re-fetch
  - [x] T13.4 Agregar columna **"Resumen IA"** en la tabla: muestra `aiSummaryPreview` truncado a 120 chars con `…`, o `Sin resumen` en gris si null
  - [x] T13.5 En la fila expandida del editor de body text: mostrar el resumen IA completo en un bloque `<pre>`/`<div>` de solo lectura, etiquetado "Resumen de IA" (visible solo si `hasAiSummary === true`)

### Tests

- [x] **T10: Unit tests backend**
  - [x] T10.1 `tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiAiSummaryServiceTests.cs` — agregar caso: cuando `IAiPromptRepository` retorna template, se usa para construir el prompt; cuando retorna `null`, se usa el fallback hardcoded
  - [x] T10.2 Mismos casos para `DeepSeekAiSummaryServiceTests.cs`
  - [x] T10.3 Crear `tests/Unit/Infrastructure.Tests/Persistence/Repositories/AiPromptRepositoryTests.cs` — happy path Get/Set

- [x] **T11: Integration tests**
  - [x] T11.1 `tests/Integration/Api.Tests/` — `GET /api/v1/ops/ai-prompts/news` retorna 200 con template
  - [x] T11.2 `PUT /api/v1/ops/ai-prompts/news` con template válido retorna 204
  - [x] T11.3 `PUT /api/v1/ops/ai-prompts/news` sin `{title}` en template retorna 400
  - [x] T11.4 `GET /api/v1/ops/pipeline-logs` retorna 200 (vacío si no hay errores)
  - [x] T11.5 `GET /api/v1/ops/news?search=FUNO` retorna solo artículos que contienen "FUNO" en título, body o resumen IA
  - [x] T11.6 `GET /api/v1/ops/news?hasAiSummary=true` retorna solo artículos con resumen IA no nulo
  - [x] T11.7 Todos los tests existentes siguen pasando

## Dev Notes

### Navegación con React Router v7

El paquete ya está instalado como `"react-router": "^7"` en `src/Web/Ops/package.json`. **No es `react-router-dom`** — en v7, todo está en el paquete principal. Usar `createBrowserRouter`, `RouterProvider`, `Outlet`, `NavLink`, `Navigate` de `'react-router'`.

```tsx
// main.tsx pattern
import { createBrowserRouter, RouterProvider, Navigate } from 'react-router'

const router = createBrowserRouter([
  {
    path: '/',
    element: <OpsShell />,
    children: [
      { index: true, element: <Navigate to="/ai-config" replace /> },
      { path: 'ai-config', element: <AiConfigPage /> },
      { path: 'noticias', element: <NewsBodyPage /> },
      { path: 'blocklist', element: <BlocklistPage /> },
      { path: 'pipeline-logs', element: <PipelineLogsPage /> },
      { path: 'ai-prompts', element: <AiPromptsPage /> },
    ],
  },
])
```

`OpsLoginGate` envuelve el router completo en `main.tsx`, o puede ser el elemento raíz del router. Verificar cómo usa `OpsLoginGate` el contexto de autenticación antes de decidir.

### Diseño de plantillas de prompt con placeholders

El template almacenado en BD debe contener exactamente estos cuatro placeholders:
- `{title}` — título del artículo
- `{snippet_section}` — "Fragmento RSS: ..." o "Fragmento RSS no disponible."
- `{body_section}` — "Cuerpo del artículo: ..." o "Cuerpo completo no disponible."
- `{strictness_instruction}` — instrucción de longitud/profundidad (Standard vs. Elaborate)

**Seed del prompt de noticias** (template inicial, extraído del hardcoded actual):
```
Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces) con amplio conocimiento del mercado inmobiliario y bursátil de México.
{strictness_instruction}
Título: {title}
{snippet_section}
{body_section}
Incluye: el hecho central, su relevancia para el sector de FIBRAs o bienes raíces en México, los datos más materiales del artículo cuando existan, y una lectura analítica breve para el inversionista.
Si el artículo contiene cifras, fechas, montos, porcentajes, dividendos, emisiones, ocupación, adquisiciones o guidance, intégralos en el resumen.
No escribas menos de 5 oraciones si el cuerpo del artículo está disponible. Responde solo con el resumen, sin preámbulos.
```

**Seed del prompt de documentos** (para PDFs): igual pero adaptado a fundamentales (ver `AiContentType.Document` en el codebase).

**Interpolación en los services**: `BuildPrompt` ya no es `static`. Recibe el template de BD y hace `.Replace("{title}", title).Replace("{snippet_section}", snippetSection)...` etc. La validación de placeholders en el endpoint PUT asegura que el template guardado siempre sea válido.

### Diseño de AiContext en los jobs

El `AiContext` es el campo crítico de esta historia. Debe ser redactado por el job en el momento del error, con los datos disponibles en ese momento. **NO es el stack trace** — es una descripción operacional:

```
// MarketPipelineJob — ejemplo correcto:
var aiContext = $"El pipeline de mercado procesó {tickers.Count} FIBRAs. " +
    $"El error ocurrió al intentar actualizar el precio de mercado para {ticker} ({exchange}). " +
    $"La operación falló al {operation} con tipo de error {ex.GetType().Name}. " +
    $"Los datos recibidos de Yahoo Finance tenían {dataPoints} puntos. " +
    $"El pipeline había completado {processed} de {total} tickers antes del fallo.";
```

**Regla**: `AiContext` debe responder a: ¿qué hacía el pipeline?, ¿qué datos tenía?, ¿en qué paso exacto falló?

### Estado del sprint-status.yaml

**ATENCIÓN**: `sprint-status.yaml` tiene conflictos de merge sin resolver. NO tocar las líneas con `<<<<<<<`, `=======`, `>>>>>>>`. Solo agregar la entrada de esta historia en la sección de Épica 5.

### Archivos a crear/modificar

**Nuevos (backend):**
- `src/Server/Domain/News/AiPrompt.cs`
- `src/Server/Domain/Jobs/PipelineErrorLog.cs`
- `src/Server/Application/News/IAiPromptRepository.cs`
- `src/Server/Application/Jobs/IPipelineErrorLogRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/AiPromptRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Jobs/PipelineErrorLogRepository.cs`
- `src/Server/Api/Endpoints/Ops/OpsAiPromptEndpoints.cs`
- `src/Server/Api/Endpoints/Ops/OpsPipelineLogEndpoints.cs`
- `src/Server/Infrastructure/Persistence/Migrations/YYYYMMDD_AddAiPromptAndPipelineErrorLog.*`

**Modificados (backend):**
- `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs` — BuildPrompt desde BD
- `src/Server/Infrastructure/Integrations/Ai/DeepSeekAiSummaryService.cs` — BuildPrompt desde BD
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs` — registrar nuevas entidades
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs` — inyectar IPipelineErrorLogRepository
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — inyectar IPipelineErrorLogRepository
- `src/Server/Infrastructure/Jobs/News/NewsBodyTextRetryJob.cs` — inyectar IPipelineErrorLogRepository
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registrar repositorios
- `src/Server/Api/Program.cs` — registrar endpoints

**Nuevos (frontend):**
- `src/Web/Ops/src/components/OpsShell.tsx`
- `src/Web/Ops/src/pages/AiConfigPage.tsx`
- `src/Web/Ops/src/pages/NewsBodyPage.tsx`
- `src/Web/Ops/src/pages/BlocklistPage.tsx`
- `src/Web/Ops/src/pages/PipelineLogsPage.tsx`
- `src/Web/Ops/src/pages/AiPromptsPage.tsx`
- `src/Web/Ops/src/api/pipelineLogsApi.ts`
- `src/Web/Ops/src/api/aiPromptsApi.ts`

**Modificados (frontend):**
- `src/Web/Ops/src/App.tsx` → eliminado o vaciado (lógica del blocklist se mueve a `BlocklistPage.tsx`)
- `src/Web/Ops/src/main.tsx` → usar `createBrowserRouter` + `RouterProvider`

**Tests:**
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiAiSummaryServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/DeepSeekAiSummaryServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/AiPromptRepositoryTests.cs` (nuevo)
- `tests/Integration/Api.Tests/Ops/AiPromptEndpointTests.cs` (nuevo)
- `tests/Integration/Api.Tests/Ops/PipelineLogEndpointTests.cs` (nuevo)

### Referencia de estructura existente

- `[Source: src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs#BuildPrompt]` — prompt hardcoded a migrar
- `[Source: src/Server/Infrastructure/Integrations/Ai/DeepSeekAiSummaryService.cs#BuildPrompt]` — mismo patrón
- `[Source: src/Web/Ops/src/App.tsx]` — lógica del blocklist a extraer a `BlocklistPage.tsx`
- `[Source: src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx]` — módulo existente, migrar a ruta `/ai-config`
- `[Source: src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx]` — migrar a ruta `/noticias`
- `[Source: src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs]` — patrón de endpoints AdminOps a seguir; OpsNewsArticleDto a actualizar con `aiSummaryPreview`
- `[Source: src/Server/Application/News/INewsRepository.cs]` — `GetPagedForOpsAsync` a ampliar con `search` y `hasAiSummary`
- `[Source: src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs]` — implementar filtros LIKE + hasAiSummary
- `[Source: _bmad-output/planning-artifacts/epics.md#FR-35,FR-36]` — requerimientos del Centro de Procesos

### Prerrequisitos para implementar

1. Resolver el conflicto de merge en `sprint-status.yaml` antes de actualizar ese archivo
2. Esta historia asume que el branch `story/4-8-body-text-anglesharp-catalogo` ya fue mergeado a main (o se implementa sobre él). Si no, verificar que los cambios de 4-8/4-9 (DeepSeekAiSummaryService, RoutingAiSummaryService, IAiProviderConfigRepository) estén disponibles porque esta historia los modifica.

### Esquemas de DB

```sql
-- ai.AiPrompt
CREATE TABLE [ai].[AiPrompt] (
    [Id]             INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [ContentType]    NVARCHAR(20)   NOT NULL,  -- 'news' | 'document'
    [PromptTemplate] NVARCHAR(MAX)  NOT NULL,
    [UpdatedAt]      DATETIMEOFFSET NOT NULL,
    [UpdatedBy]      NVARCHAR(100)  NOT NULL DEFAULT 'system',
    CONSTRAINT [UQ_AiPrompt_ContentType] UNIQUE ([ContentType])
);

-- jobs.PipelineErrorLog
CREATE TABLE [jobs].[PipelineErrorLog] (
    [Id]         UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    [Pipeline]   NVARCHAR(50)   NOT NULL,    -- 'Market' | 'News' | 'Distribution' | 'BodyTextRetry'
    [Timestamp]  DATETIMEOFFSET NOT NULL,
    [ErrorType]  NVARCHAR(100)  NOT NULL,
    [Message]    NVARCHAR(MAX)  NOT NULL,
    [Context]    NVARCHAR(MAX)  NULL,         -- JSON
    [AiContext]  NVARCHAR(MAX)  NOT NULL,
    [CreatedAt]  DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX [IX_PipelineErrorLog_Pipeline_CreatedAt] ON [jobs].[PipelineErrorLog] ([Pipeline], [CreatedAt] DESC);
```

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `git checkout -b story/5-0-ops-shell-navegacion-y-modulos`
- `python scripts/memory/memory_cli.py search "ops shell navegacion modulos adminops prompts ia"`
- `dotnet build FIBRADIS.slnx`
- `dotnet ef migrations add AddAiPromptAndPipelineErrorLog --project src/Server/Infrastructure --startup-project src/Server/Api`
- `npm run codegen:api`
- `npm run build --workspace=src/Web/Ops`
- `npm run build --workspace=src/Web/Main`
- `dotnet test FIBRADIS.slnx`

### Completion Notes List

- Se agregó soporte persistido para prompts IA (`ai.AiPrompt`) con fallback a template hardcoded y edición segura desde Ops para `news` y `document`.
- Se agregó logging estructurado de errores de pipeline (`jobs.PipelineErrorLog`) y UI en Ops para filtrado/expansión con `Context` JSON y `AiContext`.
- La SPA Ops migró a `React Router v7` con `OpsShell` y páginas separadas para AI Config, Noticias, Blocklist, Logs del Pipeline y Prompts de IA.
- La tabla de noticias en Ops ahora soporta búsqueda con debounce, filtro por presencia de resumen IA, columna `Resumen IA` y panel expandido con resumen completo.
- Se regeneró `SharedApiClient`, se creó migración EF `AddAiPromptAndPipelineErrorLog` y se amplió la cobertura de tests unitarios/integración para prompts, logs y filtros de noticias.
- Validaciones ejecutadas y en verde: `dotnet build FIBRADIS.slnx`, `dotnet test FIBRADIS.slnx`, `npm run build --workspace=src/Web/Ops`, `npm run build --workspace=src/Web/Main`.

### File List

- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `scripts/codegen/Api.json`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs`
- `src/Server/Api/Endpoints/Ops/OpsAiPromptEndpoints.cs`
- `src/Server/Api/Endpoints/Ops/OpsPipelineLogEndpoints.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Application/Jobs/IPipelineErrorLogRepository.cs`
- `src/Server/Application/News/IAiPromptRepository.cs`
- `src/Server/Application/News/INewsRepository.cs`
- `src/Server/Domain/Jobs/PipelineErrorLog.cs`
- `src/Server/Domain/News/AiPrompt.cs`
- `src/Server/Infrastructure/Integrations/Ai/AiPromptTemplateDefaults.cs`
- `src/Server/Infrastructure/Integrations/Ai/DeepSeekAiSummaryService.cs`
- `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs`
- `src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs`
- `src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs`
- `src/Server/Infrastructure/Jobs/News/NewsBodyTextRetryJob.cs`
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260523143806_AddAiPromptAndPipelineErrorLog.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260523143806_AddAiPromptAndPipelineErrorLog.Designer.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Jobs/PipelineErrorLogRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/AiPromptRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Jobs/PipelineErrorLogConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiPromptConfiguration.cs`
- `src/Server/SharedApiContracts/Jobs/PipelineErrorLogDto.cs`
- `src/Server/SharedApiContracts/News/AiPromptDto.cs`
- `src/Server/SharedApiContracts/News/OpsNewsArticleDto.cs`
- `src/Server/SharedApiContracts/News/OpsNewsBodyDto.cs`
- `src/Server/SharedApiContracts/News/UpdateAiPromptRequest.cs`
- `src/Web/Ops/src/App.tsx`
- `src/Web/Ops/src/api/aiPromptsApi.ts`
- `src/Web/Ops/src/api/authApi.ts`
- `src/Web/Ops/src/api/newsApi.ts`
- `src/Web/Ops/src/api/pipelineLogsApi.ts`
- `src/Web/Ops/src/components/OpsShell.tsx`
- `src/Web/Ops/src/main.tsx`
- `src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx`
- `src/Web/Ops/src/pages/AiConfigPage.tsx`
- `src/Web/Ops/src/pages/AiPromptsPage.tsx`
- `src/Web/Ops/src/pages/BlocklistPage.tsx`
- `src/Web/Ops/src/pages/NewsBodyPage.tsx`
- `src/Web/Ops/src/pages/PipelineLogsPage.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Integration/Api.Tests/AiModeGetPutTests.cs`
- `tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs`
- `tests/Integration/Api.Tests/Ops/AiPromptEndpointTests.cs`
- `tests/Integration/Api.Tests/Ops/PipelineLogEndpointTests.cs`
- `tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/DeepSeekAiSummaryServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiAiSummaryServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/RoutingAiSummaryServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/AiPromptRepositoryTests.cs`

### Review Findings

- [x] [Review][Patch] P13: Límite de 4000 chars en PromptTemplate — agregar validación de longitud máxima en el PUT de ai-prompts [src/Server/Api/Endpoints/Ops/OpsAiPromptEndpoints.cs]
- [x] [Review][Patch] P14: Limitar GET pipeline-logs a los 100 registros más recientes antes de paginar — aplicar `.OrderByDescending(x => x.CreatedAt).Take(100)` [src/Server/Infrastructure/Persistence/Repositories/Jobs/PipelineErrorLogRepository.cs]

- [x] [Review][Patch] P1: LogErrorAsync no está envuelto en try/catch dentro de los catch de los jobs — una excepción secundaria al loggear reemplaza al error original y puede abortar el pipeline completo [src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs, MarketPipelineJob.cs, DistributionPipelineJob.cs, NewsBodyTextRetryJob.cs]
- [x] [Review][Patch] P2: Truncar ex.GetType().Name a 100 chars antes de asignar a ErrorType — la columna es varchar(100) y un tipo de excepción con nombre muy largo causa SqlException de truncación que activa P1 [src/Server/Infrastructure/Jobs]
- [x] [Review][Patch] P3: Truncar AiContext a 800 chars antes de persistir — el spec exige máximo 800 pero no hay enforcement en código ni en la configuración de la columna [src/Server/Infrastructure/Jobs, PipelineErrorLogConfiguration.cs]
- [x] [Review][Patch] P4: Limitar longitud del parámetro search en el endpoint y el repositorio — sin límite permite búsquedas LIKE arbitrariamente largas en columnas nvarchar(max) [src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs, src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs]
- [x] [Review][Patch] P5: Normalizar contentType a lowercase antes de persistir en AiPromptRepository — la validación del endpoint usa OrdinalIgnoreCase pero no normaliza el valor, permitiendo registros "News" junto a "news" [src/Server/Infrastructure/Persistence/Repositories/News/AiPromptRepository.cs]
- [x] [Review][Patch] P6: AiPromptRepository.SetPromptAsync — race condition check-then-act sin UPSERT atómico — dos requests simultáneos pueden intentar Add y el segundo lanza DbUpdateException con UniqueConstraint [src/Server/Infrastructure/Persistence/Repositories/News/AiPromptRepository.cs]
- [x] [Review][Patch] P7: AiPromptsPage usa una sola instancia de useMutation para dos secciones — el estado isError/isSuccess se muestra en ambas secciones indiscriminadamente [src/Web/Ops/src/pages/AiPromptsPage.tsx]
- [x] [Review][Patch] P8: Falta ruta catch-all en el router de Ops — una URL no registrada muestra el sidebar con contenido vacío sin mensaje de error [src/Web/Ops/src/main.tsx]
- [x] [Review][Patch] P9: Context JSON no se formatea con pretty-print en el panel expandido — se renderiza compacto sin indentación [src/Web/Ops/src/pages/PipelineLogsPage.tsx]
- [x] [Review][Patch] P10: Pipeline name sin normalizar al casing canónico antes de pasarlo al repositorio — "news" pasa la validación OrdinalIgnoreCase pero no coincide con "News" almacenado en BD con collation CS [src/Server/Api/Endpoints/Ops/OpsPipelineLogEndpoints.cs]
- [x] [Review][Patch] P11: Test de búsqueda search solo verifica que el título contenga el término — no verifica búsqueda en BodyText/AiSummary ni que artículos sin el término sean excluidos [tests/Integration/Api.Tests/AiModeGetPutTests.cs]
- [x] [Review][Patch] P12: Tests de integración faltan para ausencia de {snippet_section} y {body_section} en el PUT de ai-prompts — solo existe el test para {title} ausente [tests/Integration/Api.Tests/Ops/AiPromptEndpointTests.cs]

- [x] [Review][Defer] D1: Fallback hardcoded de AiPromptTemplateDefaults no valida que contenga los mismos placeholders que el código intenta reemplazar — riesgo de regresión silenciosa en futuros refactors [src/Server/Infrastructure/Integrations/Ai/AiPromptTemplateDefaults.cs] — deferred, pre-existing risk, no bug actual
- [x] [Review][Defer] D2: GetPromptAsync puede lanzar excepción (no retornar null) y BuildPromptAsync no tiene try/catch — el job lo atrapa pero no distingue entre fallo de IA y fallo de BD [src/Server/Infrastructure/Integrations/Ai] — deferred, pre-existing pattern
- [x] [Review][Defer] D3: CreatedAt HasDefaultValueSql("getutcdate()") sin ValueGeneratedOnAdd — la SQL default nunca se ejecuta porque EF usa el valor de C#; el resultado es correcto pero la config es letra muerta [src/Server/Infrastructure/Persistence/SqlServer/Configurations/Jobs/PipelineErrorLogConfiguration.cs] — deferred, inconsistencia no funcional
- [x] [Review][Defer] D4: PipelineErrorLog sin mecanismo de retención — la tabla puede crecer indefinidamente sin purga [src/Server/Application/Jobs/IPipelineErrorLogRepository.cs] — deferred, decisión de arquitectura futura
- [x] [Review][Defer] D5: Mensaje de validación del PUT de ai-prompts siempre muestra los tres placeholders aunque solo falte uno — feedback no específico [src/Server/Api/Endpoints/Ops/OpsAiPromptEndpoints.cs] — deferred, mejora menor de UX

## Change Log

- 2026-05-23: Implementada historia 5.0 completa — OpsShell con rutas, logs estructurados de pipeline, gestión de prompts IA en BD, filtros/resumen IA en noticias, migración EF, codegen y validación completa (`dotnet build`, `dotnet test`, `npm run build` Ops/Main).
- 2026-05-23: Code review — 2 decision-needed, 12 patches, 5 deferred, 3 dismissed.
