# Historia 5.0: Ops Shell — Navegación multi-pantalla, logs de errores estructurados y gestión de prompts de IA

Status: ready-for-dev

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

### AC4 — Sin regresiones

Todos los tests existentes siguen pasando tras los cambios en `GeminiAiSummaryService`, `DeepSeekAiSummaryService` y los jobs.

## Tasks / Subtasks

### Backend

- [ ] **T1: Dominio — entidades nuevas**
  - [ ] T1.1 Crear `src/Server/Domain/News/AiPrompt.cs` — entidad `AiPrompt { Id (int), ContentType (string), PromptTemplate (string), UpdatedAt, UpdatedBy }`
  - [ ] T1.2 Crear `src/Server/Domain/Jobs/PipelineErrorLog.cs` — entidad `PipelineErrorLog { Id (Guid), Pipeline (string), Timestamp, ErrorType (string), Message (string), Context (string?), AiContext (string), CreatedAt }`

- [ ] **T2: Application — interfaces de repositorio**
  - [ ] T2.1 Crear `src/Server/Application/News/IAiPromptRepository.cs` — métodos: `GetPromptAsync(string contentType, CancellationToken ct)`, `SetPromptAsync(string contentType, string template, string actor, CancellationToken ct)`
  - [ ] T2.2 Crear `src/Server/Application/Jobs/IPipelineErrorLogRepository.cs` — métodos: `LogErrorAsync(PipelineErrorLog entry, CancellationToken ct)`, `GetPagedAsync(string? pipeline, int page, int pageSize, CancellationToken ct)`

- [ ] **T3: Infrastructure — EF + migración + repositorios**
  - [ ] T3.1 Configurar `AiPromptConfig` en `AppDbContext` / `OnModelCreating`: tabla `ai.AiPrompt`, `Id` (PK int), `ContentType` (unique, varchar 20), seed con los dos prompts actuales hardcodeados (`news`, `document`)
  - [ ] T3.2 Configurar `PipelineErrorLogConfig`: tabla `jobs.PipelineErrorLog`, `Id` (Guid, default `newsequentialid()`), índice no-único sobre `(Pipeline, CreatedAt DESC)`
  - [ ] T3.3 Generar migración EF: `dotnet ef migrations add AddAiPromptAndPipelineErrorLog --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [ ] T3.4 Crear `src/Server/Infrastructure/Persistence/Repositories/News/AiPromptRepository.cs` implementando `IAiPromptRepository`
  - [ ] T3.5 Crear `src/Server/Infrastructure/Persistence/Repositories/Jobs/PipelineErrorLogRepository.cs` implementando `IPipelineErrorLogRepository`
  - [ ] T3.6 Registrar repositorios en `ApiServiceExtensions.cs`

- [ ] **T4: Services — prompt desde BD**
  - [ ] T4.1 Actualizar `GeminiAiSummaryService`: inyectar `IAiPromptRepository`; en `BuildPrompt()` cargar template de BD con `GetPromptAsync("news")` o `GetPromptAsync("document")` según `contentType`; interpolar `{title}`, `{snippet_section}`, `{body_section}`, `{strictness_instruction}` en el template; si BD no retorna template, usar el string hardcoded actual como fallback. Cambiar de `static` a método de instancia.
  - [ ] T4.2 Aplicar el mismo cambio en `DeepSeekAiSummaryService`
  - [ ] T4.3 El seed de `ai.AiPrompt` para `news` debe ser el prompt actual completo con los placeholders `{strictness_instruction}`, `{title}`, `{snippet_section}`, `{body_section}` en las posiciones correctas

- [ ] **T5: Jobs — escribir a PipelineErrorLog**
  - [ ] T5.1 Inyectar `IPipelineErrorLogRepository` en `MarketPipelineJob`: en cada catch que ya llama `LogError`, también insertar en `PipelineErrorLog` con `AiContext` descriptivo del ticker y operación
  - [ ] T5.2 Inyectar en `NewsPipelineJob`: para errores de AI summary y errores de guardado, insertar registro con URL, título y proveedor en `AiContext`
  - [ ] T5.3 Inyectar en `NewsBodyTextRetryJob`: para errores de scraping o AI, insertar registro con articleId y URL
  - [ ] T5.4 Inyectar en `DistributionPipelineJob` si tiene manejo de errores relevante (verificar primero)

- [ ] **T6: Endpoints API**
  - [ ] T6.1 Crear `src/Server/Api/Endpoints/Ops/OpsAiPromptEndpoints.cs`:
    - `GET /api/v1/ops/ai-prompts/{contentType}` → retorna `{ contentType, promptTemplate, updatedAt, updatedBy }`
    - `PUT /api/v1/ops/ai-prompts/{contentType}` → valida placeholders requeridos, guarda, retorna 204
    - Validar que `contentType` sea `news` o `document`
    - `RequireAuthorization("AdminOps")`
  - [ ] T6.2 Crear `src/Server/Api/Endpoints/Ops/OpsPipelineLogEndpoints.cs`:
    - `GET /api/v1/ops/pipeline-logs?pipeline=all&page=1&pageSize=50` → retorna `PagedResult<PipelineErrorLogDto>`
    - `RequireAuthorization("AdminOps")`
  - [ ] T6.3 Registrar ambos endpoints en `Program.cs` (junto a los demás `Map*`)
  - [ ] T6.4 Actualizar `SharedApiClient` con `npm run codegen:api`

### Frontend (Ops SPA)

- [ ] **T7: OpsShell — navegación con React Router v7**
  - [ ] T7.1 Crear `src/Web/Ops/src/components/OpsShell.tsx`: layout con sidebar de navegación y `<Outlet />` para el contenido principal. El sidebar muestra: `AI Config` (→ `/ai-config`), `Noticias` (→ `/noticias`), `Blocklist` (→ `/blocklist`), `Logs del Pipeline` (→ `/pipeline-logs`), `Prompts de IA` (→ `/ai-prompts`). El item activo se destaca con estilos consistentes con el diseño actual (teal-700/border-teal-700).
  - [ ] T7.2 Actualizar `src/Web/Ops/src/main.tsx`: usar `createBrowserRouter` de `react-router` con las rutas definidas bajo el OpsShell. La ruta raíz `/` redirige a `/ai-config`. `OpsLoginGate` envuelve `OpsShell` (o se mantiene como wrapper del router provider).
  - [ ] T7.3 Actualizar `src/Web/Ops/src/App.tsx` (o eliminar si ya no es necesario): el estado y lógica del blocklist se mueven a `BlocklistPage.tsx`.

- [ ] **T8: Módulos existentes como páginas**
  - [ ] T8.1 `AiModeSection.tsx` ya existe — crear `src/Web/Ops/src/pages/AiConfigPage.tsx` que simplemente renderiza `<AiModeSection />`
  - [ ] T8.2 `NewsBodyTextSection.tsx` ya existe — crear `src/Web/Ops/src/pages/NewsBodyPage.tsx` que renderiza `<NewsBodyTextSection />`
  - [ ] T8.3 Crear `src/Web/Ops/src/pages/BlocklistPage.tsx`: mover la lógica del blocklist (actualmente inline en `App.tsx`) a este componente

- [ ] **T9: Nuevas páginas**
  - [ ] T9.1 Crear `src/Web/Ops/src/api/pipelineLogsApi.ts`: `fetchPipelineLogs(pipeline, page, pageSize)` usando el cliente `SharedApiClient`
  - [ ] T9.2 Crear `src/Web/Ops/src/api/aiPromptsApi.ts`: `fetchAiPrompt(contentType)` y `updateAiPrompt(contentType, template)`
  - [ ] T9.3 Crear `src/Web/Ops/src/pages/PipelineLogsPage.tsx`:
    - Tabla con columnas: Pipeline (badge de color), Timestamp, ErrorType, Message (truncado a 80 chars), AiContext (expandible)
    - Filtro de pipeline (dropdown: Todos, Market, News, Distribution, BodyTextRetry)
    - Paginación (50 por página)
    - Al hacer clic en una fila, expande para mostrar `Context` (JSON formateado) y `AiContext` completo
  - [ ] T9.4 Crear `src/Web/Ops/src/pages/AiPromptsPage.tsx`:
    - Dos secciones: una para `news`, otra para `document`
    - Cada sección tiene: `<textarea>` con el template actual, botón `Guardar`, texto de auditoría (quién y cuándo fue el último cambio)
    - Al guardar, muestra error si faltan placeholders requeridos
    - Estado `Guardando...` durante la mutación

### Tests

- [ ] **T10: Unit tests backend**
  - [ ] T10.1 `tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiAiSummaryServiceTests.cs` — agregar caso: cuando `IAiPromptRepository` retorna template, se usa para construir el prompt; cuando retorna `null`, se usa el fallback hardcoded
  - [ ] T10.2 Mismos casos para `DeepSeekAiSummaryServiceTests.cs`
  - [ ] T10.3 Crear `tests/Unit/Infrastructure.Tests/Persistence/Repositories/AiPromptRepositoryTests.cs` — happy path Get/Set

- [ ] **T11: Integration tests**
  - [ ] T11.1 `tests/Integration/Api.Tests/` — `GET /api/v1/ops/ai-prompts/news` retorna 200 con template
  - [ ] T11.2 `PUT /api/v1/ops/ai-prompts/news` con template válido retorna 204
  - [ ] T11.3 `PUT /api/v1/ops/ai-prompts/news` sin `{title}` en template retorna 400
  - [ ] T11.4 `GET /api/v1/ops/pipeline-logs` retorna 200 (vacío si no hay errores)
  - [ ] T11.5 Todos los tests existentes siguen pasando

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
- `[Source: src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs]` — patrón de endpoints AdminOps a seguir
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

_a completar por el dev agent_

### Debug Log References

_a completar_

### Completion Notes List

_a completar_

### File List

_a completar_
