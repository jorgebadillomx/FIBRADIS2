# Historia 5.8: Observabilidad completa de llamadas IA — log de cada llamada en Ops

Status: ready-for-dev

## Story

Como operador de FIBRADIS que supervisa el uso de IA,
quiero que cada llamada a un proveedor de IA (Gemini o DeepSeek) quede registrada con su resultado, duración y contexto,
para diagnosticar fallos, auditar el volumen de uso y detectar patrones de degradación desde la pantalla Ops.

## Acceptance Criteria

### AC1 — Toda llamada HTTP al proveedor de IA queda en `AiCallLog`

**Dado que** el sistema realiza cualquier llamada a Gemini o DeepSeek (resumen de noticias, resumen manual, extracción KPI),
**Cuando** la llamada completa o falla,
**Entonces** se persiste un registro en la tabla `AiCallLog` con:
- `Operation`: `"NewsSummary"` | `"KpiExtraction"`
- `Provider`: `"Gemini"` | `"DeepSeek"`
- `Model`: el modelo efectivo (ej. `"gemini-2.5-flash"`)
- `Success`: `true` si retornó contenido utilizable; `false` en cualquier error o respuesta vacía
- `DurationMs`: tiempo en ms desde inicio de la llamada HTTP hasta respuesta (o excepción)
- `InputChars`: longitud del prompt enviado
- `OutputChars`: longitud del texto devuelto (0 si `Success=false`)
- `ErrorType`: nombre de la excepción o código de fallo semántico si `Success=false`; null si exitoso
- `ErrorMessage`: primer fragmento (≤ 500 chars) del mensaje de error; null si exitoso
- `Timestamp` + `CreatedAt`: UTC

**Nota**: si el API key no está configurado, no se hace llamada HTTP y no se crea registro.

### AC2 — Los tres escenarios sin log en BD quedan cubiertos en `PipelineErrorLog`

**Dado que** existen tres situaciones actualmente no persistidas en BD:
1. PDF vacío (`markdownContent` vacío antes de llamar a la IA) — solo app logger hoy
2. Fallo de extracción markdown del PDF (`PdfPig`/conversión) — solo app logger hoy
3. Fallo de lectura de `AiModeConfig` en `NewsPipelineJob` — silencioso hoy (fallback a Off)

**Entonces** cada uno genera una entrada en `PipelineErrorLog` con `Pipeline = "KpiExtraction"` (1 y 2) o `"News"` (3), con `ErrorType` y `Message` descriptivos.

### AC3 — El filtro de `PipelineLogsPage` incluye KpiExtraction y ManualAiSummary

**Dado que** `AllowedPipelines` en el endpoint y el dropdown de la UI no incluían `"ManualAiSummary"` ni `"KpiExtraction"`,
**Entonces**:
- `OpsPipelineLogEndpoints.cs` agrega ambos valores a `AllowedPipelines`
- El `<select>` de `PipelineLogsPage.tsx` incluye las opciones "ManualAiSummary" y "KpiExtraction" con sus badges de color

### AC4 — Ops expone una nueva pantalla "Llamadas IA" con el historial de llamadas

**Dado que** un operador accede a `/ai-calls` en Ops,
**Entonces** ve una tabla con columnas: Timestamp, Operación, Proveedor, Modelo, Duración (ms), Entrada (chars), Salida (chars), Estado (badge verde/rojo), ErrorType (si falló).
- Filtros disponibles: Proveedor (all | Gemini | DeepSeek), Operación (all | NewsSummary | KpiExtraction), Estado (all | Éxito | Fallo)
- Paginación de 50 items, máximo 100 por página
- El ítem aparece en el menú lateral de `OpsShell` como "Llamadas IA" entre "Logs del Pipeline" y "Prompts de IA"

### AC5 — Fallo al persistir `AiCallLog` no interrumpe la operación de IA

**Dado que** `IAiCallLogRepository.LogAsync` puede fallar (BD caída, timeout),
**Entonces** el error de logging se captura silenciosamente con `logger.LogWarning` y la operación de IA continúa/retorna normalmente. El fallo de auditoría nunca eleva una excepción visible al usuario.

---

## Tasks / Subtasks

### T1 — Dominio: entidad `AiCallLog` y repositorio (AC1)

- [ ] T1.1 — Crear `src/Server/Domain/Jobs/AiCallLog.cs`:
  ```csharp
  namespace Domain.Jobs;

  public class AiCallLog
  {
      public Guid Id { get; set; } = Guid.NewGuid();
      public DateTimeOffset Timestamp { get; set; }
      public string Operation { get; set; } = string.Empty;   // max 50
      public string Provider { get; set; } = string.Empty;    // max 20
      public string Model { get; set; } = string.Empty;       // max 50
      public bool Success { get; set; }
      public int DurationMs { get; set; }
      public int InputChars { get; set; }
      public int OutputChars { get; set; }
      public string? ErrorType { get; set; }                  // max 100
      public string? ErrorMessage { get; set; }               // max 500
      public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
  }
  ```

- [ ] T1.2 — Crear `src/Server/Application/Jobs/IAiCallLogRepository.cs`:
  ```csharp
  using Domain.Jobs;

  namespace Application.Jobs;

  public interface IAiCallLogRepository
  {
      Task LogAsync(AiCallLog entry, CancellationToken ct = default);
      Task<(IReadOnlyList<AiCallLog> Items, int Total)> GetPagedAsync(
          string? provider, string? operation, bool? success,
          int page, int pageSize, CancellationToken ct = default);
  }
  ```

### T2 — Infraestructura: EF Core config, repositorio y migración (AC1)

- [ ] T2.1 — Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Jobs/AiCallLogConfiguration.cs`:
  ```csharp
  using Domain.Jobs;
  using Microsoft.EntityFrameworkCore;
  using Microsoft.EntityFrameworkCore.Metadata.Builders;

  namespace Infrastructure.Persistence.SqlServer.Configurations.Jobs;

  public class AiCallLogConfiguration : IEntityTypeConfiguration<AiCallLog>
  {
      public void Configure(EntityTypeBuilder<AiCallLog> builder)
      {
          builder.ToTable("AiCallLog", "jobs");
          builder.HasKey(x => x.Id);
          builder.Property(x => x.Operation).HasMaxLength(50).IsRequired();
          builder.Property(x => x.Provider).HasMaxLength(20).IsRequired();
          builder.Property(x => x.Model).HasMaxLength(50).IsRequired();
          builder.Property(x => x.ErrorType).HasMaxLength(100);
          builder.Property(x => x.ErrorMessage).HasMaxLength(500);
          builder.Property(x => x.CreatedAt).HasDefaultValueSql("getutcdate()").ValueGeneratedOnAdd();
          builder.HasIndex(x => new { x.Provider, x.CreatedAt });
          builder.HasIndex(x => new { x.Operation, x.CreatedAt });
      }
  }
  ```
  
  **Nota**: a diferencia de `PipelineErrorLogConfiguration`, usar `ValueGeneratedOnAdd()` alineado con `HasDefaultValueSql` para que EF no envíe el valor C# en el INSERT (ver deferred D3 de story 5-0).

- [ ] T2.2 — Registrar `AiCallLog` en `AppDbContext` (archivo existente en el proyecto EF Core): añadir `public DbSet<AiCallLog> AiCallLogs { get; set; }` y registrar `AiCallLogConfiguration` en `OnModelCreating`.

- [ ] T2.3 — Crear `src/Server/Infrastructure/Persistence/Repositories/Jobs/AiCallLogRepository.cs`:
  ```csharp
  using Application.Jobs;
  using Domain.Jobs;
  using Infrastructure.Persistence.SqlServer;
  using Microsoft.EntityFrameworkCore;

  namespace Infrastructure.Persistence.Repositories.Jobs;

  public class AiCallLogRepository(AppDbContext db) : IAiCallLogRepository
  {
      public async Task LogAsync(AiCallLog entry, CancellationToken ct = default)
      {
          db.AiCallLogs.Add(entry);
          await db.SaveChangesAsync(ct);
      }

      public async Task<(IReadOnlyList<AiCallLog> Items, int Total)> GetPagedAsync(
          string? provider, string? operation, bool? success,
          int page, int pageSize, CancellationToken ct = default)
      {
          var query = db.AiCallLogs.AsQueryable();
          if (!string.IsNullOrWhiteSpace(provider))
              query = query.Where(x => x.Provider == provider);
          if (!string.IsNullOrWhiteSpace(operation))
              query = query.Where(x => x.Operation == operation);
          if (success.HasValue)
              query = query.Where(x => x.Success == success.Value);

          var total = await query.CountAsync(ct);
          var items = await query
              .OrderByDescending(x => x.CreatedAt)
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .ToListAsync(ct);

          return (items, total);
      }
  }
  ```

- [ ] T2.4 — Registrar `IAiCallLogRepository → AiCallLogRepository` en `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` (junto a `IPipelineErrorLogRepository`).

- [ ] T2.5 — Generar migración EF Core:
  ```
  dotnet ef migrations add AddAiCallLog --project src/Server/Infrastructure --startup-project src/Server/Api
  ```

### T3 — Instrumentar servicios Gemini (resumen de noticias) (AC1, AC5)

- [ ] T3.1 — En `GeminiAiSummaryService.cs`: añadir `IAiCallLogRepository _aiCallLog` al constructor.

- [ ] T3.2 — Añadir método de ayuda `TryLogAsync` en la clase:
  ```csharp
  private async Task TryLogAsync(
      bool success, string model, int durationMs,
      int inputChars, int outputChars,
      string? errorType, string? errorMessage, CancellationToken ct)
  {
      try
      {
          await _aiCallLog.LogAsync(new AiCallLog
          {
              Timestamp = DateTimeOffset.UtcNow,
              Operation = "NewsSummary",
              Provider = "Gemini",
              Model = model,
              Success = success,
              DurationMs = durationMs,
              InputChars = inputChars,
              OutputChars = outputChars,
              ErrorType = errorType?[..Math.Min(errorType.Length, 100)],
              ErrorMessage = errorMessage?[..Math.Min(errorMessage.Length, 500)],
          }, ct);
      }
      catch (Exception ex)
      {
          logger.LogWarning(ex, "No se pudo persistir AiCallLog para Gemini NewsSummary.");
      }
  }
  ```

- [ ] T3.3 — En `GenerateSummaryCoreAsync`: medir duración con `Stopwatch` y llamar `TryLogAsync`:
  - Antes del cuerpo del método: `var sw = Stopwatch.StartNew();`
  - En el `return text.Trim()`: llamar `await TryLogAsync(true, model, (int)sw.ElapsedMilliseconds, prompt.Length, text.Trim().Length, null, null, ct)` antes del return.
  - En el `catch` existente (o envolviendo el bloque): llamar `await TryLogAsync(false, model, (int)sw.ElapsedMilliseconds, prompt.Length, 0, ex.GetType().Name, ex.Message, ct)` antes de `throw`.
  
  **El `prompt` ya es parámetro de `GenerateSummaryCoreAsync`** — no requiere cambio de firma.

### T4 — Instrumentar servicios DeepSeek (resumen de noticias) (AC1, AC5)

- [ ] T4.1 — Aplicar el mismo patrón de T3.1–T3.3 en `DeepSeekAiSummaryService.cs`:
  - Añadir `IAiCallLogRepository _aiCallLog` al constructor.
  - `TryLogAsync` con `Provider = "DeepSeek"` y `Operation = "NewsSummary"`.
  - Instrumentar `GenerateSummaryCoreAsync`.

### T5 — Instrumentar servicios KPI (AC1, AC5)

- [ ] T5.1 — En `GeminiKpiExtractorService.cs`: añadir `IAiCallLogRepository _aiCallLog` al constructor.

- [ ] T5.2 — En `ExtractAsync`: medir desde antes de `SendRequestAsync` hasta después de `KpiExtractionJsonParser.Parse`:
  ```csharp
  // antes del SendRequestAsync:
  var sw = Stopwatch.StartNew();
  try
  {
      using var response = await SendRequestAsync(url, body, providerConfig.ModelId, ct);
      // ... parseo existente ...
      sw.Stop();
      await TryLogAsync(result.Success, providerConfig.ModelId, (int)sw.ElapsedMilliseconds,
          prompt.Length, result.ExtractionNotes?.Length ?? 0, null, null, ct);
      return result;
  }
  catch (Exception ex)
  {
      sw.Stop();
      await TryLogAsync(false, providerConfig.ModelId, (int)sw.ElapsedMilliseconds,
          prompt.Length, 0, ex.GetType().Name, ex.Message, ct);
      throw;
  }
  ```
  
  - `TryLogAsync` con `Provider = "Gemini"`, `Operation = "KpiExtraction"`.
  - El prompt está disponible como variable local `prompt` ya en el método.

- [ ] T5.3 — Aplicar el mismo patrón en `DeepSeekKpiExtractorService.cs` con `Provider = "DeepSeek"`.

### T6 — Corregir los tres escenarios sin log (AC2)

- [ ] T6.1 — En `OpsFundamentalsEndpoints.cs`: inyectar `IPipelineErrorLogRepository pipelineErrorLog` en los endpoints que necesita. En el bloque de PDF vacío (actualmente solo `LogWarning`), agregar:
  ```csharp
  await pipelineErrorLog.LogErrorAsync(new PipelineErrorLog
  {
      Pipeline = "KpiExtraction",
      Timestamp = DateTimeOffset.UtcNow,
      ErrorType = "EmptyPdf",
      Message = $"PDF sin texto extraíble tras conversión: {fileName}",
      AiContext = $"Archivo: {fileName} ({fileSizeKb} KB). El PDF se procesó correctamente pero no produjo texto extraíble.",
  }, ct);
  ```

- [ ] T6.2 — En `OpsFundamentalsEndpoints.cs`: en el bloque `catch` del fallo de lectura/conversión del PDF (`PdfPig`/`MarkdownPdfConverter` o equivalente), que actualmente solo usa `LogError`, agregar `await pipelineErrorLog.LogErrorAsync(...)` con `Pipeline="KpiExtraction"`, `ErrorType=ex.GetType().Name`, `Message=ex.Message`.

- [ ] T6.3 — En `NewsPipelineJob.cs`: en el `catch` del fallo de lectura de `AiModeConfig` (actualmente silencioso con fallback a `Off`), agregar `await _pipelineErrorLog.LogErrorAsync(new PipelineErrorLog { Pipeline="News", ErrorType=ex.GetType().Name, Message="Fallo leyendo AiModeConfig, usando Off como fallback: " + ex.Message, ... })`.

### T7 — Actualizar filtro PipelineLogsPage (AC3)

- [ ] T7.1 — En `OpsPipelineLogEndpoints.cs`: añadir `"ManualAiSummary"` y `"KpiExtraction"` al array `AllowedPipelines`.

- [ ] T7.2 — En `PipelineLogsPage.tsx`: añadir `'ManualAiSummary'` y `'KpiExtraction'` al array `pipelines` y sus colores en `getPipelineBadgeClass` (ej. ManualAiSummary → rose, KpiExtraction → indigo).

### T8 — Backend: DTO + endpoint `/ai-call-logs` (AC4)

- [ ] T8.1 — Crear `src/Server/SharedApiContracts/Jobs/AiCallLogDto.cs`:
  ```csharp
  namespace SharedApiContracts.Jobs;

  public record AiCallLogDto(
      Guid Id,
      DateTimeOffset Timestamp,
      string Operation,
      string Provider,
      string Model,
      bool Success,
      int DurationMs,
      int InputChars,
      int OutputChars,
      string? ErrorType,
      string? ErrorMessage,
      DateTimeOffset CreatedAt
  );
  ```

- [ ] T8.2 — Crear `src/Server/Api/Endpoints/Ops/OpsAiCallLogEndpoints.cs`:
  - Ruta: `GET /api/v1/ops/ai-call-logs`
  - Parámetros query: `provider?` (all|Gemini|DeepSeek), `operation?` (all|NewsSummary|KpiExtraction), `success?` (bool?), `page=1`, `pageSize=50`
  - Validar `provider` y `operation` contra sus allowed lists; retornar 400 si valor no reconocido
  - Clampar `pageSize` entre 1 y 100
  - Retornar `PagedResult<AiCallLogDto>`
  - RequireAuthorization("AdminOps")

- [ ] T8.3 — Registrar `app.MapOpsAiCallLogs()` en `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` o el archivo `Program.cs` / endpoint registration junto a los demás endpoints Ops.

- [ ] T8.4 — Regenerar cliente OpenAPI: `npm run codegen:api`

### T9 — Frontend Ops: página `AiCallsPage` (AC4)

- [ ] T9.1 — Crear `src/Web/Ops/src/api/aiCallLogsApi.ts`:
  - `fetchAiCallLogs(provider, operation, success, page, pageSize)` → llama `GET /api/v1/ops/ai-call-logs`
  - Usa las funciones de auth de `opsAuth.ts` (mismo patrón que `pipelineLogsApi.ts`)

- [ ] T9.2 — Crear `src/Web/Ops/src/pages/AiCallsPage.tsx`:
  - Sección con eyebrow "Diagnóstico · IA", título "Llamadas al proveedor de IA"
  - Filtros: `Proveedor` (all | Gemini | DeepSeek), `Operación` (all | NewsSummary | KpiExtraction), `Estado` (all | Éxito | Fallo)
  - Tabla con columnas: Timestamp, Operación (badge), Proveedor (badge), Modelo, Duración, Entrada, Salida, Estado (badge éxito/fallo), ErrorType
  - Fila expandible: muestra `ErrorMessage` completo si `!Success`
  - Paginación (mismo patrón que `PipelineLogsPage`)
  - Badges de color: Gemini → teal, DeepSeek → violet, NewsSummary → sky, KpiExtraction → amber, Éxito → emerald, Fallo → rose

- [ ] T9.3 — Registrar en `src/Web/Ops/src/main.tsx`: importar `AiCallsPage` y añadir ruta `{ path: 'ai-calls', element: <AiCallsPage /> }`.

- [ ] T9.4 — En `src/Web/Ops/src/components/OpsShell.tsx`: añadir entrada en `navigationItems`:
  ```ts
  { label: 'Llamadas IA', to: '/ai-calls', description: 'Historial de llamadas a Gemini y DeepSeek con duración y estado.' }
  ```
  Insertar entre "Logs del Pipeline" y "Prompts de IA".

### T10 — Tests (AC1, AC5)

- [ ] T10.1 — Unit test `GeminiAiSummaryServiceTests` (o nuevo archivo en `tests/Unit/Infrastructure.Tests/`):
  - Dado respuesta exitosa de Gemini → `IAiCallLogRepository.LogAsync` se llama con `Success=true`, `Provider="Gemini"`, `Operation="NewsSummary"`, `OutputChars > 0`
  - Dado respuesta HTTP 500 de Gemini → `IAiCallLogRepository.LogAsync` se llama con `Success=false`, `ErrorType` no null; la excepción sigue propagándose

- [ ] T10.2 — Unit test `GeminiKpiExtractorServiceTests`:
  - Dado respuesta Gemini con candidatos y JSON válido → log con `Success=true`, `Operation="KpiExtraction"`
  - Dado respuesta Gemini sin candidatos (safety block) → log con `Success=false`

- [ ] T10.3 — Integration test: `GET /api/v1/ops/ai-call-logs` sin filtros → 200, retorna `PagedResult<AiCallLogDto>` con `total >= 0`

- [ ] T10.4 — Integration test: filtro `provider=InvalidValue` → 400

---

## Dev Notes

### Patrón del método `TryLogAsync`

Agregar `TryLogAsync` como método `private async Task` en cada servicio. El método envuelve `IAiCallLogRepository.LogAsync` en try/catch que solo loguea con `logger.LogWarning`. El caller NO hace await de la tarea — hace await del método `TryLogAsync` completo (que ya es async). Así el logging es "best-effort" sin afectar el flujo.

**IMPORTANTE**: `Stopwatch.StartNew()` debe declararse antes del bloque `try` que contiene el HTTP call, para que el `DurationMs` sea correcto tanto en éxito como en fallo.

### Ubicación del `Stopwatch` en los servicios de resumen

En `GeminiAiSummaryService.GenerateSummaryCoreAsync`, el método actual es:
```
private async Task<string> GenerateSummaryCoreAsync(string model, string apiKey, string prompt, int maxOutputTokens, CancellationToken ct)
```
El `Stopwatch` va al inicio de este método. El log va en:
- `return text.Trim()` → `TryLogAsync(success: true, ..., outputChars: text.Trim().Length, ...)`
- En el `catch (Exception ex) when (ex is not OperationCanceledException)` del `SendRequestAsync` que relanza → no, el catch está en `SendRequestAsync`. El mejor lugar es en `GenerateSummaryCoreAsync` envolviendo la llamada a `SendRequestAsync` y el parseo subsiguiente en un try/catch propio para capturar tanto errores HTTP como de parseo.

En la práctica: wrap el body de `GenerateSummaryCoreAsync` en try/finally o try/catch-and-rethrow, midiendo desde el inicio hasta el return exitoso o la excepción.

### Por qué dos índices en `AiCallLog`

`(Provider, CreatedAt)` permite consultar "¿Cuántas llamadas ha hecho Gemini esta semana?"  
`(Operation, CreatedAt)` permite consultar "¿Cuántas extracciones KPI hubo hoy?"

No se añade índice compuesto triple para no complicar la escritura en un MVP de observabilidad.

### `ValueGeneratedOnAdd` en `AiCallLogConfiguration`

A diferencia de `PipelineErrorLogConfiguration` (que tiene `HasDefaultValueSql` sin `ValueGeneratedOnAdd` — deferred D3 de story 5-0), esta configuración DEBE incluir ambos para evitar el bug pre-existente. El campo `CreatedAt` tiene `HasDefaultValueSql("getutcdate()").ValueGeneratedOnAdd()` para que EF no envíe el valor C# y la SQL default se ejecute.

### Inyectar `IAiCallLogRepository` en servicios AI

Los 4 servicios (`GeminiAiSummaryService`, `DeepSeekAiSummaryService`, `GeminiKpiExtractorService`, `DeepSeekKpiExtractorService`) usan primary constructor (C# 12). Añadir `IAiCallLogRepository aiCallLog` como parámetro al constructor y guardarlo en `_aiCallLog` o usar directamente como `aiCallLog` si no hay ambigüedad.

### `AllowedPipelines` en endpoint de pipeline-logs

El array actual en `OpsPipelineLogEndpoints.cs` es:
```csharp
private static readonly string[] AllowedPipelines = ["Market", "News", "Distribution", "BodyTextRetry"];
```
`"ManualAiSummary"` y `"KpiExtraction"` existen en la tabla pero no son filtrables desde el endpoint. Se añaden ambos al array. El frontend también debe agregarlos al `<select>`.

### Archivos a crear

**Backend (NEW)**:
- `src/Server/Domain/Jobs/AiCallLog.cs`
- `src/Server/Application/Jobs/IAiCallLogRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Jobs/AiCallLogConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Jobs/AiCallLogRepository.cs`
- `src/Server/SharedApiContracts/Jobs/AiCallLogDto.cs`
- `src/Server/Api/Endpoints/Ops/OpsAiCallLogEndpoints.cs`
- Migración EF Core: `YYYYMMDDHHMMSS_AddAiCallLog.cs`

**Backend (UPDATE)**:
- `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs`
- `src/Server/Infrastructure/Integrations/Ai/DeepSeekAiSummaryService.cs`
- `src/Server/Infrastructure/Integrations/Ai/GeminiKpiExtractorService.cs`
- `src/Server/Infrastructure/Integrations/Ai/DeepSeekKpiExtractorService.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` (T6.1, T6.2)
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` (T6.3) — verificar nombre exacto del archivo
- `src/Server/Api/Endpoints/Ops/OpsPipelineLogEndpoints.cs` (T7.1)
- `scripts/codegen/Api.json` + `src/Web/SharedApiClient/schema.d.ts`

**Frontend (NEW)**:
- `src/Web/Ops/src/api/aiCallLogsApi.ts`
- `src/Web/Ops/src/pages/AiCallsPage.tsx`

**Frontend (UPDATE)**:
- `src/Web/Ops/src/main.tsx`
- `src/Web/Ops/src/components/OpsShell.tsx`
- `src/Web/Ops/src/pages/PipelineLogsPage.tsx` (T7.2)

**Tests (NEW o UPDATE)**:
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiAiSummaryServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiKpiExtractorServiceTests.cs`
- `tests/Integration/Api.Tests/Jobs/AiCallLogTests.cs`

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

### File List

---

## Change Log

| Fecha | Cambio |
|-------|--------|
| 2026-05-26 | Story creada — observabilidad llamadas IA: AiCallLog entity, log en 4 servicios AI, fix 3 gaps PipelineErrorLog, nueva página Ops "Llamadas IA" |
