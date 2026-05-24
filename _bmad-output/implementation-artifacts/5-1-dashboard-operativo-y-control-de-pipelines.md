# Historia 5.1: Dashboard operativo y control de pipelines

Status: done

## Story

Como AdminOps,
quiero ver el dashboard de operaciones con salud de pipelines, historial de ejecuciones recientes, detalle de errores y un botón "Ejecutar ahora" para cada pipeline,
para poder diagnosticar rápidamente el estado del sistema y disparar ejecuciones manuales cuando sea necesario.

## Acceptance Criteria

### AC1 — Vista de estado de pipelines en `/dashboard`

**Dado que** navego a `/dashboard` en el panel Ops,
**Cuando** la página carga,
**Entonces** veo tarjetas para cada pipeline activo (Market, News, Distribution) con:
- Estado derivado del último run completado: `Completado` (errorCount = 0), `Fallando` (errorCount > 0 o status = Failed), `Sin datos` (sin runs previos)
- Timestamp de la última ejecución completada
- Duración en segundos de la última ejecución
- Items procesados y conteo de errores en la última ejecución

### AC2 — Panel de últimos 5 errores globales

**Dado que** existen registros en `jobs.PipelineErrorLog`,
**Cuando** carga la página de dashboard,
**Entonces** veo un panel con los últimos 5 errores globales de todos los pipelines, mostrando: timestamp, nombre del pipeline (badge de color) y mensaje de error.

**Dado que** hago clic en un registro de error,
**Entonces** se expande para mostrar el `AiContext` completo y el campo `Context` (JSON formateado).

### AC3 — Botón "Ejecutar ahora" con auditoría

**Dado que** hago clic en "Ejecutar ahora" en cualquier tarjeta de pipeline,
**Cuando** la petición se procesa,
**Entonces**:
- El endpoint retorna `202 Accepted`
- Se crea un registro en `jobs.PipelineRunLog` con `Status = "Queued"`, `TriggeredBy = email del actor` (extraído del JWT) y `StartedAt = DateTimeOffset.UtcNow`
- El botón muestra estado de carga mientras dura la petición HTTP; tras el `202`, vuelve a habilitarse
- El panel del pipeline refleja el nuevo trigger en el historial al refrescar (TanStack Query invalida el query tras la mutación)

### AC4 — Historial de ejecuciones por pipeline (últimas 5)

**Dado que** existen registros en `jobs.PipelineRunLog` para un pipeline,
**Cuando** veo la tarjeta de ese pipeline,
**Entonces** se listan las últimas 5 ejecuciones con: timestamp de inicio, duración, status (Completed/Failed/Queued), items procesados, errores, y quién lo disparó (`null` → "Automático", email → nombre del actor).

### AC5 — Endpoint `POST /api/v1/ops/market/run` (faltante)

**Dado que** `MarketPipelineJob` no tenía endpoint de ejecución manual,
**Entonces** se agrega `POST /api/v1/ops/market/run` en `OpsMarketEndpoints.cs` que encola `MarketPipelineJob` via `IBackgroundJobClient`.
El job ya tiene su propia lógica de `IsBmvTradingHours` — si el mercado está cerrado, completa inmediatamente sin procesar datos (comportamiento esperado y correcto).

### AC6 — Jobs registran sus ejecuciones en `PipelineRunLog`

**Dado que** `MarketPipelineJob`, `NewsPipelineJob` o `DistributionPipelineJob` se ejecutan (automático o manual),
**Cuando** el job inicia y completa,
**Entonces** se crea **un único registro** en `jobs.PipelineRunLog` con:
- `Pipeline`: `"Market"` | `"News"` | `"Distribution"`
- `StartedAt`: timestamp al inicio del `ExecuteAsync`
- `CompletedAt`: timestamp al final (incluso en caso de error inesperado)
- `Status`: `"Completed"` o `"Failed"`
- `ItemsProcessed`: items procesados con éxito
- `ErrorCount`: errores encontrados
- `TriggeredBy`: siempre `null` (los jobs no conocen al disparador)
- `Details`: JSON con métricas específicas del pipeline:
  - Market: `{ "processed": N, "errors": N, "critical": N, "totalFibras": N }`
  - News: `{ "fetched": N, "filteredIn": N, "saved": N, "errors": N }`
  - Distribution: `{ "processed": N, "errors": N }`
- La llamada a `PipelineRunLogRepository.AddAsync` está envuelta en `try/catch` para que un fallo al loggear no aborte el pipeline

### AC7 — OpsShell: agregar "Dashboard" al sidebar

**Dado que** el sidebar de `OpsShell.tsx` tiene las secciones actuales,
**Entonces** se agrega "Dashboard" como **primera entrada** del sidebar, apuntando a `/dashboard`.
La redirección por defecto `index: true → /ai-config` **no cambia**.

### AC8 — Sin regresiones

Todos los tests existentes pasan tras los cambios en jobs y endpoints.

---

## Tasks / Subtasks

### Backend

- [x] **T1: Dominio — entidad PipelineRunLog** (AC6)
  - [x] T1.1 Crear `src/Server/Domain/Jobs/PipelineRunLog.cs`:
    ```csharp
    public class PipelineRunLog
    {
        public Guid Id { get; init; }
        public string Pipeline { get; init; } = "";
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset? CompletedAt { get; init; }
        public string Status { get; init; } = ""; // "Completed" | "Failed" | "Queued"
        public int? ItemsProcessed { get; init; }
        public int? ErrorCount { get; init; }
        public string? TriggeredBy { get; init; }
        public string? Details { get; init; } // JSON
        public DateTimeOffset CreatedAt { get; init; }
    }
    ```

- [x] **T2: Application — interfaz de repositorio** (AC6, AC3)
  - [x] T2.1 Crear `src/Server/Application/Jobs/IPipelineRunLogRepository.cs`:
    - `Task AddAsync(PipelineRunLog entry, CancellationToken ct)`
    - `Task<IReadOnlyList<PipelineRunLog>> GetRecentAsync(string? pipeline, int take, CancellationToken ct)`
    - `Task<PipelineRunLog?> GetLastCompletedAsync(string pipeline, CancellationToken ct)`

- [x] **T3: Infrastructure — EF + migración + repositorio** (AC6)
  - [x] T3.1 Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Jobs/PipelineRunLogConfiguration.cs`:
    - Tabla `[jobs].[PipelineRunLog]`, `Id` GUID PK con `HasDefaultValueSql("newsequentialid()")`
    - `Pipeline` varchar(50), `Status` varchar(20), `TriggeredBy` varchar(100) nullable
    - `Details` nvarchar(max) nullable
    - `CreatedAt` con `HasDefaultValueSql("getutcdate()")` + `ValueGeneratedOnAdd`
    - Índice: `(Pipeline, StartedAt DESC)` para queries de dashboard
  - [x] T3.2 Registrar `PipelineRunLog` en `AppDbContext.OnModelCreating` (en el `DbSet<PipelineRunLog>`)
  - [x] T3.3 Generar migración: `dotnet ef migrations add AddPipelineRunLog --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [x] T3.4 Crear `src/Server/Infrastructure/Persistence/Repositories/Jobs/PipelineRunLogRepository.cs` implementando `IPipelineRunLogRepository`
  - [x] T3.5 Registrar `IPipelineRunLogRepository → PipelineRunLogRepository` en `ApiServiceExtensions.cs`

- [x] **T4: Actualizar jobs para registrar ejecuciones** (AC6)
  - [x] T4.1 `MarketPipelineJob.cs` — inyectar `IPipelineRunLogRepository`; al inicio de `ExecuteAsync` capturar `startedAt`; al final (en `finally`) llamar `AddAsync` con métricas `{processed, errors, critical, totalFibras: fibras.Count}` serializado en `Details`. Envolver en `try/catch` para no abortar el pipeline si el log falla. No loggear si Hangfire está en memoria (cuando `fibras.Count == 0` por skip de horario, registrar igualmente con `itemsProcessed = 0, errorCount = 0, status = "Completed"`)
  - [x] T4.2 `NewsPipelineJob.cs` — mismo patrón; métricas `{fetched: allItems.Count, filteredIn: filteredItems.Count, saved, errors}` en `Details`
  - [x] T4.3 `DistributionPipelineJob.cs` — verificar si tiene manejo de errores antes de inyectar; agregar `IPipelineRunLogRepository` y registrar `{processed, errors}`

- [x] **T5: Endpoints de ejecución manual con auditoría** (AC3, AC5)
  - [x] T5.1 Actualizar `OpsMarketEndpoints.cs`: agregar `POST /api/v1/ops/market/run` que encola `MarketPipelineJob` y registra `PipelineRunLog {Pipeline="Market", Status="Queued", TriggeredBy=actor, StartedAt=now}`
  - [x] T5.2 Actualizar el endpoint `POST /api/v1/ops/news-pipeline/run`: después del `Enqueue`, registrar `PipelineRunLog {Pipeline="News", Status="Queued", TriggeredBy=actor, StartedAt=now}`
  - [x] T5.3 Actualizar `POST /api/v1/ops/market/distribution/run`: igual para `Distribution`
  - [x] T5.4 En los tres endpoints: extraer actor del JWT usando el mismo patrón que `AiModeEndpoints.cs` (`ctx.User.Identity?.Name ?? FindFirstValue(Email) ?? FindFirstValue(NameIdentifier) ?? "unknown"`)
  - [x] T5.5 Inyectar `IPipelineRunLogRepository` en los endpoints via parámetro del handler (Minimal API soporta DI en parámetros del handler directamente)

- [x] **T6: Endpoint de dashboard** (AC1, AC2, AC4)
  - [x] T6.1 Crear `src/Server/SharedApiContracts/Jobs/PipelineRunLogDto.cs` (si no existe o no tiene los campos necesarios):
    ```csharp
    public sealed record PipelineDashboardDto(
        IReadOnlyList<PipelineStatusDto> Pipelines,
        IReadOnlyList<PipelineErrorLogDto> RecentErrors);
    
    public sealed record PipelineStatusDto(
        string Pipeline,
        string DerivedStatus, // "Completado" | "Fallando" | "Sin datos"
        DateTimeOffset? LastRunAt,
        int? LastDurationSeconds,
        int? LastItemsProcessed,
        int? LastErrorCount,
        IReadOnlyList<PipelineRunLogDto> RecentRuns);
    
    public sealed record PipelineRunLogDto(
        Guid Id, string Pipeline, DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt, string Status,
        int? ItemsProcessed, int? ErrorCount,
        string? TriggeredBy, string? Details);
    ```
  - [x] T6.2 Crear `src/Server/Api/Endpoints/Ops/OpsDashboardEndpoints.cs`:
    - `GET /api/v1/ops/dashboard` → retorna `PipelineDashboardDto`
    - Para cada pipeline (`"Market"`, `"News"`, `"Distribution"`):
      - Llama `GetLastCompletedAsync(pipeline)` para el status card
      - Llama `GetRecentAsync(pipeline, 5)` para el historial de 5 ejecuciones
      - Deriva `DerivedStatus`: si `lastCompleted == null` → `"Sin datos"`; si `lastCompleted.ErrorCount > 0` → `"Fallando"`; else → `"Completado"`
    - Para `RecentErrors`: llama `IPipelineErrorLogRepository.GetPagedAsync(null, 1, 5)`
    - `RequireAuthorization("AdminOps")`
  - [x] T6.3 Registrar endpoint en `Program.cs`: `app.MapOpsDashboard()`
  - [x] T6.4 Ejecutar `npm run codegen:api` para actualizar `SharedApiClient`

### Frontend (Ops SPA)

- [x] **T7: OpsShell — agregar "Dashboard" al sidebar** (AC7)
  - [x] T7.1 En `src/Web/Ops/src/components/OpsShell.tsx`: agregar `{ label: 'Dashboard', to: '/dashboard', description: 'Estado de pipelines, errores y disparos manuales.' }` como **primer elemento** de `navigationItems`
  - [x] T7.2 En `src/Web/Ops/src/main.tsx`: agregar ruta `{ path: 'dashboard', element: <DashboardPage /> }` y el import correspondiente

- [x] **T8: API client del dashboard** (AC1-AC4)
  - [x] T8.1 Crear `src/Web/Ops/src/api/dashboardApi.ts`:
    - `fetchPipelineDashboard()` — GET `/api/v1/ops/dashboard`
    - `runPipeline(pipeline: 'market' | 'news' | 'distribution')` — POST al endpoint correspondiente
    - Usar el patrón de `getAuthHeaders()` ya establecido en `aiPromptsApi.ts`

- [x] **T9: DashboardPage** (AC1-AC4)
  - [x] T9.1 Crear `src/Web/Ops/src/pages/DashboardPage.tsx`:
    - Encabezado: "Dashboard Operativo"
    - **Sección Pipelines** (3 tarjetas: Market, News, Distribution):
      - Nombre del pipeline + badge de estado (color: verde = Completado, rojo = Fallando, gris = Sin datos)
      - Última ejecución: timestamp (`toLocaleString`) + duración + items + errores
      - Botón "Ejecutar ahora" con estado `isPending` durante la mutación; al completar (202), invalida el query `['pipeline-dashboard']`
      - Subsección "Últimas 5 ejecuciones": tabla compacta con columnas: Hora, Estado (badge), Items, Errores, Disparado por (automático/email)
    - **Sección Errores Recientes** (panel):
      - Título "Últimos errores" + conteo
      - Lista de hasta 5 errores; al hacer clic en uno, expande mostrando `aiContext` + `context` (JSON formateado con `JSON.stringify(JSON.parse(ctx), null, 2)`)
    - Datos: `useQuery({ queryKey: ['pipeline-dashboard'], queryFn: fetchPipelineDashboard, staleTime: 30_000 })` — sin auto-refresh agresivo (operaciones manuales)
    - Mutación por pipeline: `useMutation` para `runPipeline(pipeline)` con `onSuccess: () => queryClient.invalidateQueries(['pipeline-dashboard'])`
    - Estados vacíos: si no hay runs para un pipeline, mostrar "Sin ejecuciones registradas" en lugar de error

### Tests

- [x] **T10: Unit tests backend** (AC6)
  - [x] T10.1 Crear `tests/Unit/Infrastructure.Tests/Persistence/Repositories/PipelineRunLogRepositoryTests.cs`:
    - `AddAsync` persiste el registro
    - `GetRecentAsync(pipeline, 5)` retorna los últimos 5 ordenados por `StartedAt DESC`
    - `GetRecentAsync(null, 5)` retorna los últimos 5 de todos los pipelines
    - `GetLastCompletedAsync` retorna el más reciente con `Status IN (Completed, Failed)` (no Queued)

- [x] **T11: Integration tests** (AC1, AC3, AC5)
  - [x] T11.1 `GET /api/v1/ops/dashboard` retorna 200 con estructura `PipelineDashboardDto` (pipelines: 3 entradas, recentErrors: array)
  - [x] T11.2 `POST /api/v1/ops/market/run` retorna 202 y crea registro `PipelineRunLog` con `Pipeline="Market"`, `Status="Queued"`, `TriggeredBy` no nulo
  - [x] T11.3 `POST /api/v1/ops/news-pipeline/run` retorna 202 y crea registro con `Pipeline="News"`, `Status="Queued"`, `TriggeredBy` no nulo
  - [x] T11.4 Los tres endpoints de "Run Now" retornan 401 sin token y 403 con rol `User` (no `AdminOps`)
  - [x] T11.5 `GET /api/v1/ops/dashboard` retorna 401 sin token

---

## Dev Notes

### Prerrequisito: story 5-0 debe estar en `done`

Esta historia modifica los mismos jobs y endpoints que 5-0. Los **patches P1-P14 de code review de 5-0** deben estar aplicados antes de implementar 5-1 para evitar conflictos de merge. Verificar que el branch `story/5-0-ops-shell-navegacion-y-modulos` ya fue mergeado a `main` antes de empezar.

En particular, el **P1 de 5-0** (LogErrorAsync sin try/catch en los jobs) debe estar resuelto, ya que este story agrega otro repository call (PipelineRunLog) en los mismos catch blocks. El patrón de `try { await pipelineRunLogRepo.AddAsync(...) } catch { logger.LogWarning(...) }` es obligatorio.

### Diseño de PipelineRunLog: dos tipos de entradas

La tabla `jobs.PipelineRunLog` almacena **dos tipos** de registros:

1. **Entradas de trigger manual** (escritas por el endpoint):
   - `Status = "Queued"`, `TriggeredBy = actor@email`, `CompletedAt = null`, `ItemsProcessed = null`
   - Propósito: auditoría de quién disparó el job y cuándo

2. **Entradas de ejecución real** (escritas por el job):
   - `Status = "Completed" | "Failed"`, `TriggeredBy = null`, `CompletedAt = timestamp real`
   - Propósito: métricas de la ejecución

`GetLastCompletedAsync(pipeline)` filtra **solo** `Status IN ("Completed", "Failed")` para el status card. `GetRecentAsync(pipeline, 5)` retorna ambos tipos para el historial (el frontend diferencia por status).

### Pattern: extracción del actor en Minimal API

Los endpoints de "Run Now" necesitan `HttpContext` para extraer el actor. En Minimal API, basta con declararlo como parámetro:

```csharp
group.MapPost("/run", async (
    IBackgroundJobClient jobClient,
    IPipelineRunLogRepository runLogRepo,
    HttpContext ctx,
    CancellationToken ct) =>
{
    var actor = ctx.User.Identity?.Name
        ?? ctx.User.FindFirstValue(ClaimTypes.Email)
        ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? "unknown";
    
    jobClient.Enqueue<MarketPipelineJob>(j => j.ExecuteAsync(CancellationToken.None));
    
    try
    {
        await runLogRepo.AddAsync(new PipelineRunLog
        {
            Pipeline = "Market",
            StartedAt = DateTimeOffset.UtcNow,
            Status = "Queued",
            TriggeredBy = actor,
        }, ct);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to write PipelineRunLog for Market manual trigger");
    }
    
    return Results.Accepted();
});
```

### Pattern: PipelineRunLog en jobs (try/finally)

```csharp
// En ExecuteAsync de cada job:
var startedAt = DateTimeOffset.UtcNow;
var status = "Failed";
int itemsProcessed = 0, errorCount = 0;
string? details = null;

try
{
    // ... lógica del job ...
    // Al finalizar:
    itemsProcessed = processed;
    errorCount = errors;
    status = "Completed";
    details = JsonSerializer.Serialize(new { processed, errors, ... });
}
catch (OperationCanceledException) { throw; }
catch (Exception ex)
{
    logger.LogError(ex, "...");
    errorCount++;
}
finally
{
    try
    {
        await pipelineRunLogRepo.AddAsync(new PipelineRunLog
        {
            Pipeline = "Market",
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            Status = status,
            ItemsProcessed = itemsProcessed,
            ErrorCount = errorCount,
            Details = details,
        }, CancellationToken.None); // No usar ct aquí — puede estar cancelado
    }
    catch (Exception logEx)
    {
        logger.LogWarning(logEx, "Failed to write PipelineRunLog for Market pipeline");
    }
}
```

**Atención**: usar `CancellationToken.None` en el `finally` para el log — el token `ct` puede estar cancelado en ese punto y haría fallar el insert.

### MarketPipelineJob: skip de horario también registra

Cuando el pipeline salta por estar fuera del horario BMV (`!bmvSchedule.IsTradingHours`), igual debe crear un `PipelineRunLog` con `Status = "Completed"`, `ItemsProcessed = 0`, `ErrorCount = 0`, `Details = {"skipped": true, "reason": "outside-trading-hours"}`. Esto permite saber que el job se ejecutó y decidió no hacer nada.

### Frontend: 3 pipelines, 3 mutaciones separadas

Usar `useMutation` **por pipeline** para aislar el estado `isPending` de cada botón "Ejecutar ahora". No usar una sola instancia compartida (bug de 5-0 P7 aplicado aquí preventivamente):

```tsx
const marketMutation = useMutation({ mutationFn: () => runPipeline('market'), onSuccess: invalidate })
const newsMutation = useMutation({ mutationFn: () => runPipeline('news'), onSuccess: invalidate })
const distMutation = useMutation({ mutationFn: () => runPipeline('distribution'), onSuccess: invalidate })
```

### Frontend: ruta `/dashboard`

El OpsShell define las rutas sin el prefijo `/ops` (se gestionan en el servidor). La ruta a agregar en `main.tsx` es `{ path: 'dashboard', element: <DashboardPage /> }`. La redirección `index: true → /ai-config` NO cambia.

### Frontend: formateo de Details JSON

Para mostrar el campo `Details` (JSON string) en la fila expandida del historial:
```tsx
const parsed = entry.details ? JSON.parse(entry.details) : null
// Mostrar con Object.entries o pre-formateado:
{parsed && Object.entries(parsed).map(([k, v]) => (
  <span key={k}>{k}: {String(v)}</span>
))}
```

### Deuda técnica: D4 de story 5-0

El item D4 diferido de 5-0 (`PipelineErrorLog sin mecanismo de retención`) aplica igualmente a `PipelineRunLog`. Al finalizar 5-1, agregar a `deferred-work.md`: `PipelineRunLog` crece indefinidamente; agregar `DeleteOldEntriesAsync` en historia futura.

### DbContext not thread-safe — `GetRecentAsync` es secuencial

El endpoint `GET /api/v1/ops/dashboard` llama a múltiples métodos del repositorio. Per convención FIBRADIS, todas las calls deben ser secuenciales (`await` individual), no en `Task.WhenAll`. Hay 3 pipelines × 2 queries + 1 error query = 7 queries secuenciales — aceptable dado el volumen de datos.

### Archivos a crear/modificar

**Nuevos (backend):**
- `src/Server/Domain/Jobs/PipelineRunLog.cs`
- `src/Server/Application/Jobs/IPipelineRunLogRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Jobs/PipelineRunLogRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Jobs/PipelineRunLogConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Migrations/YYYYMMDD_AddPipelineRunLog.*`
- `src/Server/Api/Endpoints/Ops/OpsDashboardEndpoints.cs`
- `src/Server/SharedApiContracts/Jobs/PipelineRunLogDto.cs` (nuevo o actualización)
- `src/Server/SharedApiContracts/Jobs/PipelineDashboardDto.cs` (nuevo)
- `src/Server/SharedApiContracts/Jobs/PipelineStatusDto.cs` (nuevo)

**Modificados (backend):**
- `src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs` — inyectar `IPipelineRunLogRepository`, registrar runs
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — idem
- `src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs` — idem (verificar primero)
- `src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs` — agregar `POST /market/run` + auditoría en los 3 endpoints existentes de Run
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs` — agregar `DbSet<PipelineRunLog>`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registrar `IPipelineRunLogRepository`
- `src/Server/Api/Program.cs` — `app.MapOpsDashboard()`
- `scripts/codegen/Api.json` + `src/Web/SharedApiClient/schema.d.ts` — regenerar

**Nuevos (frontend):**
- `src/Web/Ops/src/api/dashboardApi.ts`
- `src/Web/Ops/src/pages/DashboardPage.tsx`

**Modificados (frontend):**
- `src/Web/Ops/src/components/OpsShell.tsx` — agregar item "Dashboard"
- `src/Web/Ops/src/main.tsx` — agregar ruta `/dashboard`

**Tests:**
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/PipelineRunLogRepositoryTests.cs` (nuevo)
- `tests/Integration/Api.Tests/Ops/DashboardEndpointTests.cs` (nuevo)

### Referencias

- `[Source: src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs#ExecuteAsync]` — patrón de error logging a extender con PipelineRunLog
- `[Source: src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs#ExecuteAsync]` — idem
- `[Source: src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs]` — endpoints Run Now existentes a actualizar + nuevo `/market/run`
- `[Source: src/Server/Api/Endpoints/Ops/OpsPipelineLogEndpoints.cs]` — patrón endpoint paginado, y `GetPagedAsync(null, 1, 5)` para últimos errores
- `[Source: src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs#actor extraction]` — patrón para extraer actor del JWT en endpoints Minimal API
- `[Source: src/Web/Ops/src/components/OpsShell.tsx#navigationItems]` — agregar "Dashboard" como primer item
- `[Source: src/Web/Ops/src/main.tsx]` — agregar ruta dashboard
- `[Source: src/Web/Ops/src/pages/PipelineLogsPage.tsx]` — referencia de diseño para tabla de errores expandible
- `[Source: _bmad-output/planning-artifacts/epics.md#FR-36,FR-37]` — requerimientos de Dashboard y Run Now con auditoría
- `[Source: _bmad-output/planning-artifacts/convenciones-fibradis.md#EF Core — nunca Task.WhenAll]` — queries secuenciales obligatorias
- `[Source: _bmad-output/implementation-artifacts/5-0-ops-shell-navegacion-y-modulos.md#Review Findings P1]` — try/catch obligatorio en todos los log calls dentro de jobs

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `git branch -m story/5-1-dashboard-operativo-y-control-de-pipelines`
- `dotnet build FIBRADIS.slnx`
- `dotnet ef migrations add AddPipelineRunLog --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`
- `npm run codegen:api`
- `dotnet test tests/Unit/Domain.Tests/Domain.Tests.csproj --no-build`
- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj --no-build`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --no-build`
- `dotnet test tests/Integration/Jobs.Tests/Jobs.Tests.csproj --no-build`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --no-build`
- `npm run build --workspace=src/Web/Ops`
- `npm run build --workspace=src/Web/Main`

### Completion Notes List

- Se agregó `jobs.PipelineRunLog` end-to-end: entidad de dominio, repositorio de aplicación, configuración EF, repositorio SQL Server y migración `20260523160507_AddPipelineRunLog`.
- Los pipelines `Market`, `News` y `Distribution` ahora registran una corrida por ejecución real en `finally`, con `try/catch` defensivo para que fallar al auditar no aborte el job. `Market` registra también los skips fuera de horario.
- Se extendieron los endpoints Ops de ejecución manual para auditar triggers `Queued` con actor extraído del JWT y se agregó `POST /api/v1/ops/market/run`.
- Se implementó `GET /api/v1/ops/dashboard`, se regeneró OpenAPI/SharedApiClient y se construyó `DashboardPage` en Ops con 3 mutaciones separadas, historial por pipeline y panel expandible de errores recientes.
- Se agregó deuda operativa en `deferred-work.md`: `PipelineRunLog` aún no tiene política de retención/purga.
- Para estabilizar `Api.Tests` en Windows, `ApiWebFactory` limpia providers de logging y `AddApiInfrastructure` usa un `InMemoryBackgroundJobClient` cuando `Hangfire:UseInMemoryStorage=true`, evitando 500s en endpoints `Run` durante tests.

### File List

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `scripts/codegen/Api.json`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/CompositionRoot/InMemoryBackgroundJobClient.cs`
- `src/Server/Api/Endpoints/Ops/OpsDashboardEndpoints.cs`
- `src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Application/Jobs/IPipelineRunLogRepository.cs`
- `src/Server/Domain/Jobs/PipelineRunLog.cs`
- `src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs`
- `src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs`
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260523160507_AddPipelineRunLog.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260523160507_AddPipelineRunLog.Designer.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Jobs/PipelineRunLogRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Jobs/PipelineRunLogConfiguration.cs`
- `src/Server/SharedApiContracts/Jobs/PipelineDashboardDto.cs`
- `src/Server/SharedApiContracts/Jobs/PipelineRunLogDto.cs`
- `src/Server/SharedApiContracts/Jobs/PipelineStatusDto.cs`
- `src/Web/Ops/src/api/dashboardApi.ts`
- `src/Web/Ops/src/components/OpsShell.tsx`
- `src/Web/Ops/src/main.tsx`
- `src/Web/Ops/src/pages/DashboardPage.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Integration/Api.Tests/ApiWebFactory.cs`
- `tests/Integration/Api.Tests/Ops/DashboardEndpointTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/PipelineRunLogRepositoryTests.cs`

## Change Log

- 2026-05-23: Historia 5.1 implementada y validada. Se agregó auditoría `PipelineRunLog` para corridas reales y manuales, endpoint `GET /api/v1/ops/dashboard`, endpoint faltante `POST /api/v1/ops/market/run`, cliente compartido regenerado y nueva `DashboardPage` en Ops con controles manuales e historial por pipeline.

### Review Findings

- [x] [Review][Patch] `TryLogQueuedRunAsync` usa el CancellationToken del request HTTP [`OpsMarketEndpoints.cs`] — Si el cliente se desconecta antes de que `AddAsync` complete, el audit log del trigger manual se pierde aunque Hangfire ya haya encolado el job. Cambiar a `CancellationToken.None` (mismo patrón que el `finally` de los jobs: `CancellationToken.None` para garantizar que el log se escriba independientemente del ciclo de vida del request).

- [x] [Review][Patch] Falta test de integración: `POST /api/v1/ops/market/distribution/run` no verifica creación de `PipelineRunLog` Queued con actor [`DashboardEndpointTests.cs`] — `OpsMarketEndpointTests.cs:PostDistributionRun_WithAdminOpsToken_ReturnsAccepted` solo verifica 202, no verifica que `TriggeredBy = "adminops@test.com"`. Añadir `PostDistributionRun_ReturnsAcceptedAndCreatesQueuedPipelineRunLog` siguiendo el mismo patrón de los tests de Market y News (AC3).

- [x] [Review][Patch] `PipelineErrorLogRepository.GetPagedAsync` ordena por `CreatedAt` en lugar de `Timestamp` [`PipelineErrorLogRepository.cs:24`] — `OrderByDescending(x => x.CreatedAt)` usa el timestamp de inserción en BD en lugar del timestamp del error. AC2 requiere mostrar los "últimos errores" por tiempo de ocurrencia. Cambiar a `OrderByDescending(x => x.Timestamp)`.

- [x] [Review][Patch] Falta test: `GET /api/v1/ops/dashboard` con rol User retorna 403 [`DashboardEndpointTests.cs`] — Los endpoints Run tienen cobertura explícita de 403 vía `RunEndpoints_WithUserRole_ReturnForbidden`, pero el endpoint GET del dashboard no tiene test equivalente.

- [x] [Review][Patch] `FakePipelineRunLogRepository` en tests de `MarketPipelineJob` y `DistributionPipelineJob` es no-op — `AddAsync` descarta la entrada sin capturarla; ningún unit test verifica los campos del `PipelineRunLog` escrito por esos jobs (Status, ErrorCount, Details JSON shape). El fake de `NewsPipelineJobTests` sí captura entradas pero tampoco las assertea (AC6).

- [x] [Review][Patch] `CalculateDurationSeconds` sin guardia contra duración negativa [`OpsDashboardEndpoints.cs`] — Con clock skew entre instancias, `CompletedAt < StartedAt` produce un resultado negativo del `(int)Math.Round(...)`. Agregar `Math.Max(0, ...)`.

- [x] [Review][Defer] `GetActor` puede retornar GUID si falta el claim de email — fallback a `ClaimTypes.NameIdentifier` puede ser un GUID/sub claim opaco, no un email [`OpsMarketEndpoints.cs`] — deferred, pre-existing: sigue el patrón explícito de Dev Notes; email presente siempre para AdminOps en la configuración actual de JWT

- [x] [Review][Defer] `DashboardPage` muestra "Sin datos" inmediatamente tras trigger manual — `GetLastCompletedAsync` excluye Queued, así que el badge de estado no cambia hasta que el job completa [`DashboardPage.tsx`] — deferred, UX improvement no requerido por spec; el historial de runs sí muestra el entry Queued

- [x] [Review][Defer] `PipelineRunLogConfiguration` sin índice en columna `Status` — `GetLastCompletedAsync` filtra por `(Pipeline, Status)` pero el índice solo cubre `(Pipeline, StartedAt DESC)` [`PipelineRunLogConfiguration.cs`] — deferred, optimización prematura dado el volumen esperado de datos operativos

- [x] [Review][Defer] `OpsMarketEndpoints` contiene rutas del pipeline de noticias (`newsGroup`) — naming confusion pre-existente de story 5-0 [`OpsMarketEndpoints.cs`] — deferred, pre-existing, no introducido en esta historia

- [x] [Review][Defer] Jobs registran `OperationCanceledException` como `Status="Failed"` — un shutdown limpio de Hangfire deja los tres pipelines mostrando "Fallando" hasta el próximo run exitoso — deferred, por diseño per spec (Status solo admite "Completed"/"Failed"/"Queued"); añadir Status="Cancelled" requiere cambio de spec
