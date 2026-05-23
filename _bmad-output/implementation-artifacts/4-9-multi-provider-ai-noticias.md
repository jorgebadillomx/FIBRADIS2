# Historia 4.9: Multi-proveedor de IA — Gemini + DeepSeek seleccionable desde Ops

Status: done

## Story

Como AdminOps,
quiero poder elegir el proveedor de IA (Gemini o DeepSeek) y el modelo específico desde el panel de Ops sin redespliegue,
para poder cambiar de `gemini-2.5-pro` (cuota limitada) a `gemini-2.5-flash` o a `deepseek-chat` cuando el proveedor activo tiene restricciones de rate,
y que ese cambio aplique tanto al pipeline automático como a los disparos manuales de resumen.

## Contexto y motivación

La historia 4.3 implementó `GeminiAiSummaryService` como única implementación de `IAiSummaryService`, con el modelo leído de `appsettings.json`. Esto impide cambiar de proveedor o modelo sin redespliegue. El problema inmediato es que `gemini-2.5-pro` devuelve 429 en el tier gratuito, y no hay forma de cambiarlo desde Ops.

Esta historia extiende la arquitectura para soportar múltiples proveedores (Gemini + DeepSeek) con configuración editable en BD, y expone un selector en el panel Ops.

## Acceptance Criteria

1. **Enum `AiProvider` y entidad `AiProviderConfig` en dominio**
   - Existe `AiProvider { Gemini, DeepSeek }` en `Domain.News`.
   - Existe `AiProviderConfig` con `Id=1, Provider, ModelId, UpdatedAt, UpdatedBy`.
   - La tabla `ai.AiProviderConfig` se crea via migración EF con seed: `Provider=Gemini, ModelId=gemini-2.5-flash`.

2. **`RoutingAiSummaryService` delega al proveedor activo en BD**
   - Existe `RoutingAiSummaryService : IAiSummaryService`.
   - Lee `AiProviderConfig` de BD en cada llamada.
   - Delega a `GeminiAiSummaryService` o `DeepSeekAiSummaryService` según `Provider`.
   - Es la implementación registrada en DI para `IAiSummaryService`.

3. **`GeminiAiSummaryService` usa el `ModelId` de BD**
   - Ya no lee `Gemini:NewsModel` de `IConfiguration`.
   - Lee el `ModelId` de `IAiProviderConfigRepository.GetConfigAsync()`.
   - `Gemini:NewsModel` en `appsettings.json` queda obsoleto (se puede mantener como fallback o eliminar; se elimina del seed para no confundir).

4. **`DeepSeekAiSummaryService` implementa `IAiSummaryService`**
   - Usa la API OpenAI-compatible de DeepSeek: `POST https://api.deepseek.com/chat/completions`.
   - Lee `DeepSeek:ApiKey` de `IConfiguration`.
   - Lee `ModelId` de `IAiProviderConfigRepository.GetConfigAsync()`.
   - Genera el mismo prompt analítico de FIBRAs que Gemini.
   - Retorna `null` si `DeepSeek:ApiKey` está vacío (→ 503 en el endpoint, igual que Gemini).
   - Lanza `AiProviderConfigurationException` si la API rechaza la credencial (401/403).
   - Lanza `InvalidOperationException` para otros errores HTTP de DeepSeek.

5. **Endpoints Ops: `GET/PUT /api/v1/ops/ai-provider`**
   - `GET` retorna `AiProviderConfigDto` con: `provider, modelId, updatedAt, updatedBy, availableProviders`.
   - `availableProviders` es lista estática hardcodeada: `[{provider:"Gemini", models:["gemini-2.5-flash","gemini-2.5-pro"]}, {provider:"DeepSeek", models:["deepseek-chat","deepseek-reasoner"]}]`.
   - `PUT` recibe `{provider, modelId}`, valida que `modelId` pertenezca al catálogo del proveedor, guarda en BD, retorna 204.
   - `PUT` retorna 400 si `provider` o `modelId` son inválidos.
   - Ambos requieren autorización `AdminOps`.

6. **Pipeline y disparos manuales usan el proveedor activo**
   - `NewsPipelineJob` usa `IAiSummaryService` (inyectado → `RoutingAiSummaryService`): sin cambio en el job.
   - `AiModeEndpoints.POST /{id}/ai-summary` usa `IAiSummaryService` (inyectado): sin cambio en el endpoint.
   - El cambio de proveedor/modelo aplica en la siguiente llamada sin reinicio.

7. **Selector de proveedor y modelo en `AiModeSection` del Ops SPA**
   - Existe una sección "Proveedor de IA" en `AiModeSection.tsx` debajo del selector On/Off.
   - Muestra el proveedor activo y el modelo activo.
   - Al seleccionar un proveedor diferente, el selector de modelo actualiza sus opciones.
   - El botón "Guardar" llama a `PUT /api/v1/ops/ai-provider`.
   - Mientras se guarda, el botón muestra "Guardando..." y está disabled.
   - Un mensaje confirma el cambio exitoso o muestra el error.

8. **Cobertura de pruebas**
   - Unit tests de `RoutingAiSummaryService`: delega a Gemini cuando `Provider=Gemini`, delega a DeepSeek cuando `Provider=DeepSeek`.
   - Unit tests de `DeepSeekAiSummaryService`: retorna null con ApiKey vacío; lanza `AiProviderConfigurationException` con 401; genera resumen en happy path.
   - Tests de integración para `GET /api/v1/ops/ai-provider` y `PUT /api/v1/ops/ai-provider`.
   - Todos los tests existentes siguen pasando.

## Tasks / Subtasks

- [x] Task 1: Dominio — AiProvider enum + AiProviderConfig entity
  - [x] 1.1 Crear `src/Server/Domain/News/AiProvider.cs` con enum `AiProvider { Gemini, DeepSeek }`
  - [x] 1.2 Crear `src/Server/Domain/News/AiProviderConfig.cs` con propiedades: `Id, Provider, ModelId, UpdatedAt, UpdatedBy`

- [x] Task 2: Application — IAiProviderConfigRepository
  - [x] 2.1 Crear `src/Server/Application/News/IAiProviderConfigRepository.cs` con `GetConfigAsync` y `SetProviderAsync(provider, modelId, actor, ct)`

- [x] Task 3: Infrastructure — EF config + migración + repositorio
  - [x] 3.1 Crear `AiProviderConfigConfiguration.cs`; tabla `ai.AiProviderConfig`; seed: `Provider=Gemini, ModelId=gemini-2.5-flash`
  - [x] 3.2 Agregar `DbSet<AiProviderConfig>` en `AppDbContext`
  - [x] 3.3 Crear `AiProviderConfigRepository.cs` (patrón upsert con retry)
  - [x] 3.4 Migración `AddAiProviderConfig` generada y aplicada
  - [x] 3.5 `dotnet ef database update` aplicado correctamente

- [x] Task 4: Infrastructure — Refactorizar GeminiAiSummaryService para leer modelo de BD
  - [x] 4.1 `IAiProviderConfigRepository` inyectado en `GeminiAiSummaryService`
  - [x] 4.2 `configuration["Gemini:NewsModel"]` reemplazado por `providerRepo.GetConfigAsync(ct).ModelId`
  - [x] 4.3 `ResolveModel`, `DefaultNewsModel`, `DefaultDocumentModel` eliminados

- [x] Task 5: Infrastructure — DeepSeekAiSummaryService
  - [x] 5.1 `DeepSeekAiSummaryService.cs` creado con API chat completions OpenAI-compatible

- [x] Task 6: Infrastructure — RoutingAiSummaryService
  - [x] 6.1 `RoutingAiSummaryService.cs` creado, delega según `AiProviderConfig.Provider`

- [x] Task 7: API — DTOs + endpoints + DI
  - [x] 7.1 `AiProviderConfigDto.cs` creado
  - [x] 7.2 `AiProviderOptionDto.cs` creado
  - [x] 7.3 `GET/PUT /api/v1/ops/ai-provider` en `AiModeEndpoints.cs`
  - [x] 7.4 DI actualizado: typed HttpClients para Gemini y DeepSeek, `RoutingAiSummaryService` como `IAiSummaryService`
  - [x] 7.5 `dotnet build` 0 errores

- [x] Task 8: Codegen + Frontend Ops
  - [x] 8.1 `npm run codegen:api` ejecutado — schema regenerado
  - [x] 8.2 `fetchAiProvider` y `setAiProvider` en `aiModeApi.ts`
  - [x] 8.3 Sección "Proveedor de IA" en `AiModeSection.tsx` con selector provider/modelo

- [x] Task 9: Tests
  - [x] 9.1 `RoutingAiSummaryServiceTests.cs` — 3 tests
  - [x] 9.2 `DeepSeekAiSummaryServiceTests.cs` — 4 tests
  - [x] 9.3 Tests de integración en `AiModeOpsEndpointTests.cs` — 4 tests nuevos
  - [x] 9.4 91/91 unit tests + 99/99 integration tests — 0 errores

- [x] Task 10: Revertir debug + validación final
  - [x] 10.1 502 detail revertido a mensaje estándar
  - [x] 10.2 `Gemini:NewsModel` eliminado de appsettings; `DeepSeek:ApiKey` añadido
  - [x] 10.3 `dotnet build FIBRADIS.slnx` — 0 errores, 0 advertencias
  - [x] 10.4 `npm run build --workspace=src/Web/Ops` — 0 errores TypeScript

## Dev Notes

### Arquitectura

`IAiSummaryService` queda como interfaz única. El flujo es:
```
AiModeEndpoints / NewsPipelineJob
    → IAiSummaryService (DI → RoutingAiSummaryService)
        → Lee AiProviderConfig.Provider de BD
        → Delega a GeminiAiSummaryService ó DeepSeekAiSummaryService
            → Lee AiProviderConfig.ModelId de BD
            → Lee ApiKey de IConfiguration
```

### DeepSeek API (chat completions OpenAI-compatible)

```http
POST https://api.deepseek.com/chat/completions
Authorization: Bearer {DeepSeek:ApiKey}
Content-Type: application/json

{
  "model": "deepseek-chat",
  "messages": [{ "role": "user", "content": "{prompt}" }],
  "max_tokens": 768
}
```

Respuesta:
```json
{
  "choices": [{ "message": { "content": "..." } }]
}
```

### Modelos disponibles (hardcoded en la API)

```csharp
private static readonly IReadOnlyList<AiProviderOptionDto> AvailableProviders =
[
    new("Gemini", ["gemini-2.5-flash", "gemini-2.5-pro"]),
    new("DeepSeek", ["deepseek-chat", "deepseek-reasoner"]),
];
```

### DI — cambio clave

```csharp
// ANTES:
builder.Services.AddHttpClient<IAiSummaryService, GeminiAiSummaryService>(...)

// DESPUÉS:
builder.Services.AddHttpClient<GeminiAiSummaryService>(...)
builder.Services.AddHttpClient<DeepSeekAiSummaryService>(...)
builder.Services.AddScoped<IAiProviderConfigRepository, AiProviderConfigRepository>()
builder.Services.AddScoped<IAiSummaryService, RoutingAiSummaryService>()
```

### Patrón retry upsert en AiProviderConfigRepository

Mismo patrón que `AiModeRepository` — `FindAsync` → update si existe, `Add + SaveChanges` con retry en `DbUpdateException`.

### Seed EF

```csharp
builder.HasData(new AiProviderConfig
{
    Id = 1,
    Provider = AiProvider.Gemini,
    ModelId = "gemini-2.5-flash",
    UpdatedAt = new DateTimeOffset(2026, 5, 22, 0, 0, 0, TimeSpan.Zero),
    UpdatedBy = "system",
});
```

### Tests: mocking de AiProviderConfigRepository

Para tests unitarios de los servicios AI, inyectar un `IAiProviderConfigRepository` fake que retorna la config deseada.

Para `RoutingAiSummaryService`, mockear los servicios concretos directamente (no vía `IAiSummaryService`).

## Dev Agent Record

### Implementation Plan
Historia nueva, sin review previo. Implementación desde cero siguiendo tasks en orden.

### Debug Log
_vacío_

### Completion Notes
_vacío_

### Review Findings

- [x] [Review][Patch] `GeminiAiSummaryService`: restaurar diferenciación `contentType` — `AiContentType.Document` debe usar `DefaultDocumentModel = "gemini-2.5-pro"` hardcodeado; solo noticias usa el `ModelId` de BD [GeminiAiSummaryService.cs:~41]

- [x] [Review][Patch] 503 detail hardcodeado a Gemini cuando el proveedor activo puede ser DeepSeek [AiModeEndpoints.cs:~155]
- [x] [Review][Patch] Log "Gemini configuration error" en catch de `AiProviderConfigurationException` cuando puede venir de DeepSeek [AiModeEndpoints.cs:~163]
- [x] [Review][Patch] `NewsBodyTextRetryJob`: no valida `!string.IsNullOrWhiteSpace(bodyText)` antes de `UpdateBodyTextAsync` — guarda cadena vacía como body_text [NewsBodyTextRetryJob.cs:~26]
- [x] [Review][Patch] `NewsBodyTextRetryJob`: errores de scraping se loguean en `Debug` en lugar de `Warning`, invisibles en producción [NewsBodyTextRetryJob.cs:~35]

- [x] [Review][Defer] Doble lectura a BD por llamada a `GenerateSummaryAsync`: `RoutingAiSummaryService` y el servicio concreto hacen `GetConfigAsync` por separado — impacto mínimo (FindAsync por PK), requeriría cambio de interfaz para corregir. — deferred, pre-existing design
- [x] [Review][Defer] `AvailableProviders` en `AiProviderEndpoints` es lista estática desacoplada del enum `AiProvider` — trampa de mantenimiento si se agrega proveedor. — deferred, pre-existing design
- [x] [Review][Defer] `NewsBodyTextRetryJob`: sin throttle de concurrencia para 200 requests HTTP secuenciales — puede disparar rate-limiting en sitios objetivo. — deferred, acceptable for current scale
- [x] [Review][Defer] HTTP 429 (TooManyRequests) de DeepSeek no se maneja distinto a error genérico — mismo gap existe en `GeminiAiSummaryService`. — deferred, pre-existing pattern
- [x] [Review][Defer] `JsonException` no capturada en `DeepSeekAiSummaryService.GenerateSummaryCoreAsync` — mismo gap existe en `GeminiAiSummaryService`. — deferred, pre-existing pattern

## File List

**Nuevos:**
- `src/Server/Domain/News/AiProvider.cs`
- `src/Server/Domain/News/AiProviderConfig.cs`
- `src/Server/Application/News/IAiProviderConfigRepository.cs`
- `src/Server/Infrastructure/Integrations/Ai/DeepSeekAiSummaryService.cs`
- `src/Server/Infrastructure/Integrations/Ai/RoutingAiSummaryService.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiProviderConfigConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/AiProviderConfigRepository.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260523003657_AddAiProviderConfig.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260523003657_AddAiProviderConfig.Designer.cs`
- `src/Server/SharedApiContracts/News/AiProviderConfigDto.cs`
- `src/Server/SharedApiContracts/News/AiProviderOptionDto.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/DeepSeekAiSummaryServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/RoutingAiSummaryServiceTests.cs`

**Modificados:**
- `src/Server/Domain/News/AiProviderConfig.cs` (nuevo)
- `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs` — inyecta `IAiProviderConfigRepository`, elimina `ResolveModel`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs` — agrega `DbSet<AiProviderConfig>`
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — agrega `AiProviderEndpoints` + `SetAiProviderRequest`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registra `RoutingAiSummaryService`, `DeepSeekAiSummaryService`, `IAiProviderConfigRepository`
- `src/Server/Api/Program.cs` — registra `MapAiProvider()`
- `src/Server/Api/appsettings.json` — elimina `Gemini:NewsModel`, agrega `DeepSeek:ApiKey`
- `src/Server/Api/appsettings.Development.json` — mismo cambio
- `src/Web/SharedApiClient/schema.d.ts` — regenerado con codegen
- `src/Web/Ops/src/api/aiModeApi.ts` — agrega `fetchAiProvider`, `setAiProvider`
- `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx` — sección proveedor/modelo
- `src/Web/Ops/src/api/newsApi.ts` — fix `bodyText ?? null`
- `src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx` — fix `Number(total)`
- `tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs` — 4 nuevos tests + helpers
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiAiSummaryServiceTests.cs` — actualiza tests de modelo + `FakeAiProviderConfigRepository`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log
- 2026-05-22: Historia creada — multi-proveedor AI (Gemini + DeepSeek), selector desde Ops
- 2026-05-23: Implementación completa — 91/91 unit tests + 99/99 integration tests, build limpio, frontend Ops actualizado
