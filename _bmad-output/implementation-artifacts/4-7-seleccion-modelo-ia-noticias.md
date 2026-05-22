# Historia 4.7: Selección de Modelo IA para Noticias en Ops

Status: done

## Story

Como AdminOps,
quiero seleccionar el modelo de IA (Gemini Flash o Pro) desde el panel Ops al configurar el modo IA de noticias,
para que pueda controlar el balance calidad/costo de los resúmenes sin redespliegue, tanto en el pipeline automático como en la regeneración manual.

## Acceptance Criteria

1. **GET incluye newsModel:** Dado que AdminOps consulta la configuración de IA, cuando `GET /api/v1/ops/ai-mode`, entonces la respuesta incluye el campo `newsModel` con el identificador del modelo actual (ej. `"gemini-2.5-pro"`).

2. **PUT actualiza newsModel:** Dado que AdminOps quiere cambiar el modelo, cuando `PUT /api/v1/ops/ai-mode` con `{ newsModel: "gemini-2.5-flash" }` (campo `mode` es opcional), entonces el modelo se persiste y el siguiente GET devuelve el nuevo modelo. Si solo se envía `newsModel` sin `mode`, el modo actual no cambia. Si solo se envía `mode` sin `newsModel`, el modelo actual no cambia.

3. **Modelo inválido rechazado:** Dado que AdminOps envía un modelo no permitido, cuando `PUT /api/v1/ops/ai-mode` con `{ newsModel: "gemini-invalid" }`, entonces la API responde 400 con mensaje descriptivo. Valores permitidos: `"gemini-2.5-flash"` y `"gemini-2.5-pro"`.

4. **Pipeline usa el modelo seleccionado:** Dado que AI mode es On y `newsModel = "gemini-2.5-flash"`, cuando el job `NewsPipelineJob` se ejecuta, entonces los resúmenes se generan usando `gemini-2.5-flash`.

5. **Trigger manual usa el modelo seleccionado:** Dado que `newsModel = "gemini-2.5-flash"`, cuando `POST /api/v1/ops/news/{id}/ai-summary`, entonces el resumen se genera usando `gemini-2.5-flash`.

6. **Delay entre llamadas a Gemini — pipeline:** Dado que el pipeline procesa múltiples artículos en modo On, cuando genera el resumen IA de cada artículo, entonces espera 5 segundos (`await Task.Delay(TimeSpan.FromSeconds(5), ct)`) antes de procesar el siguiente artículo, para respetar los rate limits de la API de Gemini.

7. **Delay en trigger manual:** Dado que AdminOps lanza la regeneración manual de un artículo, cuando `POST /api/v1/ops/news/{id}/ai-summary`, entonces después de llamar a Gemini se espera 5 segundos antes de retornar la respuesta 204.

8. **UI muestra selector de modelo:** Dado que AdminOps abre el panel Ops, cuando la sección de AI mode carga, entonces se muestra un selector con las opciones "Flash (gemini-2.5-flash)" y "Pro (gemini-2.5-pro)". El selector es visible siempre (independiente del modo On/Off).

9. **Guardar persiste modo y modelo juntos:** Dado que AdminOps selecciona "On" y "Flash", cuando hace clic en "Guardar cambio", entonces se envía un único PUT con `{ mode: "On", newsModel: "gemini-2.5-flash" }`. El botón "Guardar cambio" aparece cuando hay cualquier diferencia con el estado actual (modo O modelo).

## Tasks / Subtasks

- [x] Task 1: Dominio — agregar `NewsModel` a `AiModeConfig` (AC: 1, 2, 4, 5)
  - [x] 1.1 `src/Server/Domain/News/AiModeConfig.cs`: agregar propiedad `NewsModel`:
    ```csharp
    public string NewsModel { get; set; } = "gemini-2.5-pro";
    ```

- [x] Task 2: EF Core — configuración y migración (AC: 1, 2)
  - [x] 2.1 Buscar configuración EF de `AiModeConfig` (puede estar en `AiModeConfigConfiguration.cs` bajo `Configurations/` o inline en `AppDbContext`). Si no existe, crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiModeConfigConfiguration.cs` siguiendo el patrón de `NewsArticleConfiguration.cs`. Añadir:
    ```csharp
    builder.Property(x => x.NewsModel)
        .HasColumnName("news_model")
        .HasMaxLength(100)
        .IsRequired();
    ```
  - [x] 2.2 Crear migración EF Core:
    ```bash
    dotnet ef migrations add AddAiModeConfigNewsModel --project src/Server/Infrastructure --startup-project src/Server/Api
    ```
    Verificar que el archivo generado añade `news_model NVARCHAR(100) NOT NULL DEFAULT 'gemini-2.5-pro'` en la tabla `ai.AiModeConfig` y actualiza `AppDbContextModelSnapshot.cs`.
  - [x] 2.3 Aplicar migración:
    ```bash
    dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api
    ```

- [x] Task 3: Repositorio — extender `IAiModeRepository` (AC: 2)
  - [x] 3.1 `src/Server/Application/News/IAiModeRepository.cs`: añadir método:
    ```csharp
    Task UpdateConfigAsync(AiMode? mode, string? newsModel, string actor, CancellationToken ct = default);
    ```
  - [x] 3.2 `src/Server/Infrastructure/Persistence/Repositories/News/AiModeRepository.cs`: implementar `UpdateConfigAsync`. Seguir el mismo patrón de upsert con manejo de `DbUpdateException` que tiene `SetModeAsync`. La lógica:
    - Si `config` no existe, crear nuevo registro con los valores proporcionados (usar actuales como default para los no provistos).
    - Si `mode != null` y difiere del actual → actualizar `PreviousMode`, `Mode`.
    - Si `newsModel != null` → actualizar `NewsModel`.
    - Siempre actualizar `UpdatedAt` y `UpdatedBy`.
    - Si nada cambió (mismo modo y mismo modelo) → retornar sin hacer `SaveChangesAsync`.

- [x] Task 4: Application — agregar parámetro `model` a `IAiSummaryService` (AC: 4, 5)
  - [x] 4.1 `src/Server/Application/News/IAiSummaryService.cs`: añadir parámetro opcional `model`:
    ```csharp
    Task<string?> GenerateSummaryAsync(
        string title,
        string? snippet,
        string? bodyText = null,
        AiContentType contentType = AiContentType.News,
        string? model = null,
        CancellationToken ct = default);
    ```

- [x] Task 5: Infrastructure — usar `model` en `GeminiAiSummaryService` (AC: 4, 5)
  - [x] 5.1 `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs`: actualizar la firma de `GenerateSummaryAsync` para aceptar el nuevo parámetro `model`. Cuando `model` es no nulo, usarlo para seleccionar el modelo en la llamada a la API de Gemini en lugar del valor leído desde `IConfiguration`. La resolución del modelo efectivo queda como:
    ```csharp
    var effectiveModel = model
        ?? _config["Gemini:NewsModel"]
        ?? "gemini-2.5-pro";
    ```
    Aplicar tanto en el primer intento como en el reintento con prompt reforzado (ambas llamadas dentro del mismo método usan `effectiveModel`).

- [x] Task 6: Endpoints — actualizar GET, PUT y POST en `AiModeEndpoints.cs` (AC: 1, 2, 3, 5, 7)
  - [x] 6.1 `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — **GET**: actualizar el DTO de respuesta (`AiModeDto`) para incluir `NewsModel`. Si el DTO es un record local al endpoint, añadir la propiedad `string NewsModel`. Si está en un archivo separado, actualizar ahí.
  - [x] 6.2 **PUT**: actualizar el request body para que ambos campos sean opcionales:
    ```csharp
    record UpdateAiModeRequest(AiMode? Mode, string? NewsModel);
    ```
    Validaciones a agregar:
    - Si ambos `Mode` y `NewsModel` son null → 400: "Se debe proporcionar al menos `mode` o `newsModel`."
    - Si `Mode` tiene valor → `Enum.IsDefined(typeof(AiMode), mode)` (ya existente, ajustar al nullable).
    - Si `NewsModel` tiene valor → validar contra lista permitida:
      ```csharp
      private static readonly HashSet<string> AllowedNewsModels =
          new(StringComparer.OrdinalIgnoreCase) { "gemini-2.5-flash", "gemini-2.5-pro" };
      ```
      Si no pertenece → 400: "Modelo no permitido. Valores válidos: gemini-2.5-flash, gemini-2.5-pro."
    - Llamar a `repository.UpdateConfigAsync(request.Mode, request.NewsModel, actor, ct)` en lugar de `SetModeAsync`.
  - [x] 6.3 **POST** (manual trigger): leer `newsModel` desde `GetConfigAsync()` y pasarlo a `GenerateSummaryAsync`. Agregar delay de 5 segundos después de la llamada a Gemini y antes de retornar la respuesta:
    ```csharp
    var config = await repository.GetConfigAsync(ct);
    // ... llamada a GenerateSummaryAsync con model: config.NewsModel ...
    await Task.Delay(TimeSpan.FromSeconds(5), ct);
    return Results.NoContent();
    ```

- [x] Task 7: Jobs — pasar `newsModel` al pipeline (AC: 4, 6)
  - [x] 7.1 `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`: cambiar `GetCurrentModeAsync()` → `GetConfigAsync()` para obtener tanto `Mode` como `NewsModel`. Usar `config.Mode` donde antes se usaba el resultado de `GetCurrentModeAsync()`. Pasar `config.NewsModel` al llamar a `GenerateSummaryAsync`.
  - [x] 7.2 Agregar delay de 5 segundos entre artículos procesados con IA. El delay va **después** de la generación del resumen (exitosa o fallida con `Partial`) y **antes** de continuar al siguiente artículo del loop. Usar `await Task.Delay(TimeSpan.FromSeconds(5), ct)`.

- [x] Task 8: Tests unitarios (AC: 4, 5, 6, 7)
  - [x] 8.1 `tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiAiSummaryServiceTests.cs`: agregar test que verifica que cuando se pasa `model: "gemini-2.5-flash"`, el request HTTP a Gemini usa `"gemini-2.5-flash"` como nombre de modelo (mockeando el `HttpMessageHandler`).
  - [x] 8.2 `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`: agregar test que verifica que:
    - Cuando `AiModeConfig.NewsModel = "gemini-2.5-flash"`, `GenerateSummaryAsync` recibe `model: "gemini-2.5-flash"`.
    - El delay de 5 segundos se invoca entre artículos (verificar que `Task.Delay` es llamado una vez por cada artículo en modo On). **Nota:** Para hacer esto testeable, inyectar el delay como un `Func<TimeSpan, CancellationToken, Task>` con default a `Task.Delay` o simplemente verificar que el job completa correctamente con artículos múltiples.

- [x] Task 9: Tests de integración (AC: 1, 2, 3)
  - [x] 9.1 `tests/Integration/Api.Tests/AiModeGetPutTests.cs` (o archivo equivalente para endpoints de AiMode): agregar/actualizar tests:
    - `GET /api/v1/ops/ai-mode` incluye `newsModel` en el body de respuesta.
    - `PUT` con `{ newsModel: "gemini-2.5-flash" }` (sin `mode`) actualiza el modelo y el siguiente GET lo refleja.
    - `PUT` con `{ mode: "On" }` (sin `newsModel`) preserva el modelo actual.
    - `PUT` con `{ newsModel: "gemini-invalid" }` retorna 400.
    - `PUT` con body vacío `{}` retorna 400.

- [x] Task 10: Codegen y frontend — regenerar cliente y actualizar Ops UI (AC: 8, 9)
  - [x] 10.1 Regenerar el cliente OpenAPI tipado:
    ```bash
    npm run codegen:api
    ```
    Verificar que `AiModeDto` en el cliente generado incluye `newsModel: string`.
  - [x] 10.2 `src/Web/Ops/src/api/aiModeApi.ts`: actualizar `setAiMode` para aceptar `{ mode?: 'Off' | 'On', newsModel?: 'gemini-2.5-flash' | 'gemini-2.5-pro' }`:
    ```typescript
    export async function setAiConfig(payload: {
      mode?: 'Off' | 'On'
      newsModel?: 'gemini-2.5-flash' | 'gemini-2.5-pro'
    }): Promise<void>
    ```
    Mantener `setAiMode(mode)` como wrapper de compatibilidad si se usa en otros lugares, o reemplazar directamente (solo se llama desde `AiModeSection`).
  - [x] 10.3 `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx`: añadir estado y UI para selección de modelo:
    - Nuevo estado: `const [selectedModel, setSelectedModel] = useState<'gemini-2.5-flash' | 'gemini-2.5-pro' | null>(null)`
    - `pendingModel = selectedModel ?? currentModel` donde `currentModel = modeQuery.data?.newsModel as 'gemini-2.5-flash' | 'gemini-2.5-pro' | undefined`
    - Selector de modelo: dos botones (misma estética que los botones Off/On existentes):
      - `Flash (gemini-2.5-flash)` → valor `'gemini-2.5-flash'`
      - `Pro (gemini-2.5-pro)` → valor `'gemini-2.5-pro'`
    - Colocar el selector debajo del selector Off/On, con heading pequeño "Modelo IA".
    - "Guardar cambio" aparece cuando `(selected !== null && selected !== currentMode) || (selectedModel !== null && selectedModel !== currentModel)`.
    - Al guardar, llamar `setAiConfig({ mode: pendingMode, newsModel: pendingModel })`.
    - Al cancelar, resetear ambos `selected` y `selectedModel` a null.
    - Al éxito del `saveMutation`, resetear también `selectedModel`.

- [x] Task 11: Tests E2E — actualizar fixtures y spec (AC: 8, 9)
  - [x] 11.1 `src/Web/Main/tests/e2e/fixtures/ops-news-api.ts`: actualizar `AiModeState` para incluir `newsModel`:
    ```typescript
    type AiModeState = { mode: 'Off' | 'On', newsModel: string, updatedAt: string, updatedBy: string | null, previousMode: string | null }
    ```
    Actualizar los mocks para incluir `newsModel: 'gemini-2.5-pro'` en la respuesta por defecto.
  - [x] 11.2 `src/Web/Main/tests/e2e/news-epic4.spec.ts`: agregar test de selección de modelo:
    - Cargar sección AI mode.
    - Verificar que el selector de modelos está visible con las dos opciones.
    - Hacer clic en "Flash", verificar que el botón "Guardar cambio" aparece.
    - Guardar, verificar que el PUT incluye `newsModel: "gemini-2.5-flash"`.

## Dev Notes

### Contexto de implementación

Esta historia extiende la configuración de `AiModeConfig` (tabla `ai.AiModeConfig`, singleton id=1) para persistir también el modelo de IA, de forma que el modelo sea configurable sin redespliegue, exactamente igual que el modo Off/On.

### Estado actual de los archivos clave que se modifican

**`src/Server/Domain/News/AiModeConfig.cs`** — Estado actual:
```csharp
public class AiModeConfig
{
    public int Id { get; set; } = 1;
    public AiMode Mode { get; set; } = AiMode.Off;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }
    public AiMode? PreviousMode { get; set; }
}
```
Agregar `public string NewsModel { get; set; } = "gemini-2.5-pro";` preservando todos los campos existentes.

**`src/Server/Application/News/IAiModeRepository.cs`** — Estado actual:
```csharp
public interface IAiModeRepository
{
    Task<AiMode> GetCurrentModeAsync(CancellationToken ct = default);
    Task<AiModeConfig> GetConfigAsync(CancellationToken ct = default);
    Task SetModeAsync(AiMode mode, string actor, CancellationToken ct = default);
}
```
Añadir `UpdateConfigAsync` sin eliminar `SetModeAsync` (puede quedar como delegación interna o legacy).

**`src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs`** — la resolución del modelo ya ocurre desde `IConfiguration["Gemini:NewsModel"]`. Agregar el parámetro `model` con fallback a esa configuración. Asegurarse de que el parámetro `model` se usa en **ambos** intentos (primer intento y reintento con prompt reforzado).

**`src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`** — actualmente llama a `GetCurrentModeAsync()`. Cambiar a `GetConfigAsync()` para obtener también `NewsModel`. El delay de 5 segundos va al final del bloque donde se genera el resumen (dentro del `if (currentMode == AiMode.On)` o equivalente), antes de continuar al siguiente artículo del loop. Usar `ct` del job para que sea cancelable.

**`src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx`** — la estructura actual tiene tres bloques: (1) selector Off/On, (2) botón guardar/cancelar condicional, (3) sección manual. El selector de modelo se añade entre (1) y (2), con su propio heading pequeño. La condición de visibilidad del botón guardar debe extenderse para incluir cambio de modelo.

**`src/Web/Ops/src/api/aiModeApi.ts`** — `setAiMode(mode: 'Off' | 'On')` pasa a ser `setAiConfig(payload: { mode?, newsModel? })`. El endpoint subyacente ya era `PUT /api/v1/ops/ai-mode`. Solo cambia el body.

### Delay de 5 segundos — `Task.Delay`

El delay es para throttling de la API de Gemini. Equivale a `time.sleep(5)` de Python:
```csharp
await Task.Delay(TimeSpan.FromSeconds(5), ct);
```
Siempre pasar el `CancellationToken` para que sea cancelable si el job es interrumpido.

- **Pipeline:** el delay va al final del procesamiento de cada artículo que requirió llamada a Gemini (dentro del bloque `if (currentMode == AiMode.On)`), antes del `continue` o de pasar al siguiente artículo.
- **Trigger manual:** el delay va después de la llamada a `GenerateSummaryAsync` y antes del `return Results.NoContent()`. Esto hace que el endpoint tarde ~5s más en responder, lo cual es aceptable dado que es una operación administrativa poco frecuente.

### EF Core — posible configuración existente de `AiModeConfig`

Verificar si existe algún archivo `AiModeConfigConfiguration.cs` bajo `src/Server/Infrastructure/Persistence/SqlServer/Configurations/`. Si no existe, la entidad puede estar mapeada por convención o inline en `AppDbContext.OnModelCreating`. En cualquier caso, agregar la propiedad `news_model` en la configuración EF siguiendo exactamente el mismo patrón que `NewsArticleConfiguration.cs`.

### Migración — historia 4-5-4 en vuelo

La historia 4-5-4 está en estado `ready-for-dev` en la misma rama del proyecto. Verificar si tiene migraciones pendientes en `src/Server/Infrastructure/Persistence/Migrations/` antes de generar la de esta historia. Si 4-5-4 ya tiene una migración, asegurarse de que esta se genera **después** (timestamp más reciente).

### Valores permitidos de modelos

Solo se permiten exactamente estos dos identificadores de modelo (case-insensitive en validación, almacenados en lowercase):
- `"gemini-2.5-flash"` — Flash, más rápido y económico
- `"gemini-2.5-pro"` — Pro, mayor calidad

El default de la columna en BD es `'gemini-2.5-pro'` (coincide con `appsettings.json: Gemini:NewsModel`).

### Codegen OpenAPI — orden obligatorio

Primero construir el backend (`dotnet build FIBRADIS.slnx`) para que el OpenAPI spec se actualice, **luego** ejecutar `npm run codegen:api`. Si el codegen se ejecuta antes del build, `AiModeDto` no tendrá `newsModel`.

### Tests — consideraciones

- Los integration tests usan `ApiWebFactory.cs`. Ver el estado de `tests/Integration/Api.Tests/AiModeGetPutTests.cs` (o el equivalente) — si no existe ese archivo, los tests de AiMode pueden estar en `AiModeOpsEndpointTests.cs`.
- Para los unit tests del pipeline con el delay, la forma más pragmática de probar que el delay no rompe el flujo es simplemente ejecutar el job con artículos múltiples y verificar que todos se procesan. No es necesario mockear `Task.Delay` específicamente; basta verificar el comportamiento observable (resúmenes generados, estado correcto).

### Project Structure Notes

- Dominio: `src/Server/Domain/News/` (entidades, enums)
- Application: `src/Server/Application/News/` (interfaces, contratos)
- Infrastructure: `src/Server/Infrastructure/` (implementaciones, migraciones, jobs)
- Endpoints: `src/Server/Api/Endpoints/Ops/`
- Frontend Ops: `src/Web/Ops/src/` (componentes, api)
- Tests integración: `tests/Integration/Api.Tests/`
- Tests unitarios: `tests/Unit/Infrastructure.Tests/`
- Tests E2E: `src/Web/Main/tests/e2e/`

### References

- `src/Server/Domain/News/AiModeConfig.cs` — entidad actual (sin NewsModel)
- `src/Server/Application/News/IAiModeRepository.cs` — interfaz del repositorio
- `src/Server/Infrastructure/Persistence/Repositories/News/AiModeRepository.cs` — implementación con patrón upsert+retry
- `src/Server/Application/News/IAiSummaryService.cs` — interfaz del servicio de resúmenes
- `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs` — lógica de selección de modelo via `IConfiguration`
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — GET/PUT/POST endpoints
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — uso de `GetCurrentModeAsync()`
- `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx` — UI actual (Off/On + trigger manual)
- `src/Web/Ops/src/api/aiModeApi.ts` — cliente API actual
- `_bmad-output/implementation-artifacts/4-6-noticias-display-fixes-y-aimode-on.md` — historia predecesora con el renombramiento Manual→On
- `_bmad-output/implementation-artifacts/4-3-soporte-para-ai-mode-en-noticias-off-y-manual.md` — historia fundacional del AiMode

## Senior Developer Review (AI)

### Review Findings

- [x] [Review][Decision] ¿El delay de 5s en POST /ai-summary debe aplicarse también en rutas de error (502, 503)? — Decidido: sí, aplica en rutas de error. `Task.Delay(5s, CancellationToken.None)` añadido antes del return 502 en `catch (Exception ex)`. [AiModeEndpoints.cs] ✅
- [x] [Review][Patch] newsModel no se normaliza a lowercase antes de persistir — `.ToLowerInvariant()` añadido al pasar `request.NewsModel` a `UpdateConfigAsync`. [AiModeEndpoints.cs:74] ✅
- [x] [Review][Patch] Migration `defaultValue: ""` → `"gemini-2.5-pro"` — Corregido en `AddColumn`. [20260522173635_AddAiModeConfigNewsModel.cs:20] ✅
- [x] [Review][Patch] `OperationCanceledException` del `Task.Delay` en el pipeline es tragada — `catch (OperationCanceledException) { throw; }` añadido antes del catch genérico en el loop de items. [NewsPipelineJob.cs] ✅
- [x] [Review][Patch] Botón "Guardar cambio" no se deshabilita cuando `triggerMutation.isPending` — `disabled` actualizado a `saveMutation.isPending || triggerMutation.isPending`; "Generar resumen" también deshabilita con `saveMutation.isPending`. [AiModeSection.tsx] ✅
- [x] [Review][Patch] `SetAiModeRequest` dead code eliminado. [AiModeEndpoints.cs] ✅
- [x] [Review][Defer] Whitelist de modelos duplicada en C#/TS/UI — Los valores `"gemini-2.5-flash"` / `"gemini-2.5-pro"` aparecen en `AllowedNewsModels` (C#), `NewsModel` union type (TS), botones de UI (TSX) y fixtures E2E. Deuda de mantenibilidad: un nuevo modelo requiere cambios en ≥4 archivos. Sin fuente única de verdad. [AiModeEndpoints.cs / aiModeApi.ts / AiModeSection.tsx] — deferred, pre-existing
- [x] [Review][Defer] `UpdateConfigAsync(null, null, ...)` silently no-ops sin audit trail — La firma permite pasar ambos null; el método retorna sin SaveChangesAsync ni UpdatedAt/UpdatedBy. No hay llamador actual con ambos null (el endpoint valida antes), pero la interfaz lo permite sin error. [AiModeRepository.cs] — deferred, pre-existing
- [x] [Review][Defer] Upsert-on-insert asume singleton cuando solo `newsModel` es proporcionado — Si se crea la fila inexistente con `mode=null`, se inserta con `Mode = Off` como default; puede sorprender si la intención era preservar el modo actual. Inalcanzable con seed existente (fila id=1 siempre presente). [AiModeRepository.cs:32-35] — deferred, pre-existing
- [x] [Review][Defer] Conflicto route handler LIFO en test E2E — El test "Guardar con Flash envía PUT" registra un segundo handler sobre el mock ya registrado por `mockOpsNewsApi`; Playwright los ejecuta en LIFO. Funcional hoy, pero frágil ante reordenamiento de setup. [news-epic4.spec.ts:~427] — deferred, pre-existing

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- `AiModeConfig` extendida con `NewsModel` (default `"gemini-2.5-pro"`) — migración `AddAiModeConfigNewsModel` aplicada a BD.
- `IAiModeRepository.UpdateConfigAsync` implementado en `AiModeRepository` con upsert+retry; `SetModeAsync` delegado internamente.
- `IAiSummaryService.GenerateSummaryAsync` extendida con parámetro `string? model` — `GeminiAiSummaryService` usa `model` como prioridad sobre `IConfiguration`.
- Endpoints: GET devuelve `newsModel`, PUT acepta `UpdateAiModeRequest(Mode?, NewsModel?)` con validaciones, POST trigger manual lee config y pasa `newsModel` + delay de 5s.
- `NewsPipelineJob` migrado de `GetCurrentModeAsync` a `GetConfigAsync`; pasa `newsModel` a Gemini; delay de 5s al final de cada artículo en modo On.
- Frontend Ops: `setAiConfig(payload)` reemplaza `setAiMode`; `AiModeSection` con selector de modelo Flash/Pro; botón Guardar visible con cualquier cambio (modo O modelo).
- Tests: 84 unit + 100 integration — todos pasan. 1 test unitario nuevo en `GeminiAiSummaryServiceTests`, 1 en `NewsPipelineJobTests`, 5 en `AiModeGetPutTests`. 3 tests E2E nuevos.
- Fix colateral: `newsApi.ts` y `NewsBodyTextSection.tsx` corregidos para schema generado (`null` vs `undefined`, `Number()` cast).

### File List

- `src/Server/Domain/News/AiModeConfig.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiModeConfigConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260522173635_AddAiModeConfigNewsModel.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260522173635_AddAiModeConfigNewsModel.Designer.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Application/News/IAiModeRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/AiModeRepository.cs`
- `src/Server/Application/News/IAiSummaryService.cs`
- `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs`
- `src/Server/SharedApiContracts/News/AiModeDto.cs`
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs`
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`
- `src/Web/SharedApiClient/schema.d.ts`
- `src/Web/Ops/src/api/aiModeApi.ts`
- `src/Web/Ops/src/api/newsApi.ts`
- `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx`
- `src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiAiSummaryServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`
- `tests/Integration/Api.Tests/AiModeGetPutTests.cs`
- `tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs`
- `src/Web/Main/tests/e2e/fixtures/ops-news-api.ts`
- `src/Web/Main/tests/e2e/news-epic4.spec.ts`
- `_bmad-output/implementation-artifacts/4-7-seleccion-modelo-ia-noticias.md`
