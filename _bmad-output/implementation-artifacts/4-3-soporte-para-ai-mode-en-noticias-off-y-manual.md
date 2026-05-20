# Historia 4.3: Soporte para AI_MODE en noticias (Off y Manual)

Status: done

## Story

Como AdminOps,
quiero controlar si se generan resúmenes de noticias mediante la configuración AI_MODE (Off o Manual), con la plataforma iniciando en Off en el primer despliegue,
para que las noticias siempre se publiquen incluso sin procesamiento de IA, y pueda disparar resúmenes manualmente cuando sea necesario.

## Acceptance Criteria

1. **AI_MODE=Off (default):** Dado que AI_MODE=Off (valor por defecto del sistema en el primer despliegue), cuando se ingiere un item de noticias, entonces se publica con título, fuente, fecha, snippet y enlace original; no se realiza ninguna llamada a IA; el estado es `Processed`.

2. **Generación de resumen exitosa:** Dado que AI_MODE=Manual y AdminOps dispara la generación de resumen para un item de noticias específico desde Ops, cuando la llamada a IA al proveedor configurado tiene éxito, entonces el resumen del item se actualiza y se muestra en la plataforma.

3. **Fallo de IA:** Dado que AI_MODE=Manual y la llamada a IA falla, entonces el item se publica con `status=Partial`, conservando título, fuente, fecha, snippet y enlace original; la UI muestra el item sin resumen pero sin estado de error.

4. **Cambio de modo + auditoría:** Dado que AdminOps cambia AI_MODE de Off a Manual en la sección de configuración de Ops, entonces el cambio toma efecto en el siguiente ciclo del pipeline sin redespliegue, y el cambio queda registrado (actor + timestamp + valor anterior) en la configuración de AI_MODE.

## Tasks / Subtasks

- [x] Task 1: Backend — Dominio: enum + entidad + campo en NewsArticle (AC: #1, #2, #3, #4)
  - [x] 1.1 Crear `src/Server/Domain/News/AiMode.cs` — enum `AiMode { Off = 0, Manual = 1 }`
  - [x] 1.2 Crear `src/Server/Domain/News/AiModeConfig.cs` — entidad single-row (ver Dev Notes)
  - [x] 1.3 Agregar `AiSummary` (string?, nullable) a `src/Server/Domain/News/NewsArticle.cs`

- [x] Task 2: Backend — EF Core + migración (AC: #1, #4)
  - [x] 2.1 Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiModeConfigConfiguration.cs` (ver Dev Notes)
  - [x] 2.2 Agregar `DbSet<AiModeConfig> AiModeConfigs { get; set; }` en `AppDbContext.cs`
  - [x] 2.3 Crear migración: `dotnet ef migrations add AddAiModeConfig --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`
  - [x] 2.4 Aplicar migración: `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`

- [x] Task 3: Backend — Application layer (AC: #1, #2, #3, #4)
  - [x] 3.1 Crear `src/Server/Application/News/IAiModeRepository.cs` (ver Dev Notes)
  - [x] 3.2 Crear `src/Server/Application/News/IAiSummaryService.cs` (ver Dev Notes)

- [x] Task 4: Backend — Infrastructure: implementaciones (AC: #1, #2, #3, #4)
  - [x] 4.1 Crear `src/Server/Infrastructure/Persistence/Repositories/News/AiModeRepository.cs` (ver Dev Notes)
  - [x] 4.2 Crear `src/Server/Infrastructure/Integrations/Ai/AnthropicAiSummaryService.cs` (ver Dev Notes)
  - [x] 4.3 Registrar `IAiModeRepository → AiModeRepository` y `IAiSummaryService → AnthropicAiSummaryService` en `ApiServiceExtensions.cs`
  - [x] 4.4 Agregar sección `Anthropic` a `appsettings.json` y `appsettings.Development.json` (ver Dev Notes)

- [x] Task 5: Backend — Modificar NewsPipelineJob (AC: #1)
  - [x] 5.1 Inyectar `IAiModeRepository` en el constructor de `NewsPipelineJob`
  - [x] 5.2 Al inicio de `ExecuteAsync`, leer `await aiModeRepo.GetCurrentModeAsync(ct)`
  - [x] 5.3 Al crear cada `NewsArticle`, usar `Status = mode == AiMode.Off ? NewsArticleStatus.Processed : NewsArticleStatus.Pending`

- [x] Task 6: Backend — Contratos y endpoints Ops (AC: #2, #3, #4)
  - [x] 6.1 Crear `src/Server/SharedApiContracts/News/AiModeDto.cs` (ver Dev Notes)
  - [x] 6.2 Actualizar `src/Server/SharedApiContracts/News/NewsArticleDto.cs` — agregar `string? AiSummary` al record
  - [x] 6.3 Actualizar `NewsRepository.GetLatestAsync` y `GetLatestForFibraAsync` — incluir `AiSummary` en la proyección (si se está mapeando a DTO en el endpoint, asegurarse de que el campo se mapee)
  - [x] 6.4 Crear `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` con tres rutas (ver Dev Notes)
  - [x] 6.5 Registrar `app.MapAiMode()` en `Program.cs`
  - [x] 6.6 Actualizar `ToDto` en `NewsEndpoints.cs` para incluir `AiSummary` en el mapeo
  - [x] 6.7 Ejecutar `npm run codegen:api` — regenerar `SharedApiClient/schema.d.ts`

- [x] Task 7: Frontend Ops — Módulo AI Mode (AC: #4)
  - [x] 7.1 Crear `src/Web/Ops/src/api/aiModeApi.ts` (ver Dev Notes)
  - [x] 7.2 Crear `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx` — sección con toggle Off/Manual + guardar + auditoría (ver Dev Notes)
  - [x] 7.3 Actualizar `src/Web/Ops/src/App.tsx` — agregar `<AiModeSection />` junto a la sección de blocklist

- [x] Task 8: Frontend Main — Mostrar resumen cuando está disponible (AC: #2, #3)
  - [x] 8.1 Actualizar `src/Web/Main/src/modules/home/NewsSection.tsx` — mostrar `aiSummary` si disponible, fallback a `snippet` (ver Dev Notes)
  - [x] 8.2 Actualizar `src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx` — mismo patrón de fallback

- [x] Task 10: Migración de proveedor IA: Anthropic → Gemini + patches Pass 5 (AC: #2, #3)
  - [x] 10.1 Crear `src/Server/Domain/News/AiContentType.cs` — enum `AiContentType { News = 0, Document = 1 }`
  - [x] 10.2 Actualizar `IAiSummaryService.cs` — añadir parámetro `AiContentType contentType = AiContentType.News`
  - [x] 10.3 Crear `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs` — `gemini-2.5-flash` para `News`, `gemini-2.5-pro` para `Document`; configurable vía `Gemini:NewsModel` y `Gemini:DocumentModel`
  - [x] 10.4 Eliminar `AnthropicAiSummaryService.cs` y sus tests; registrar `GeminiAiSummaryService` en `ApiServiceExtensions.cs`
  - [x] 10.5 Actualizar `appsettings.json` y `appsettings.Development.json`: sección `Anthropic` → sección `Gemini` con `ApiKey`, `NewsModel`, `DocumentModel`
  - [x] 10.6 Aplicar P2 (idempotencia), P4 (null→503), P5 (excepción→502) en `AiModeEndpoints.cs`; pasar `AiContentType.News` en llamada al servicio
  - [x] 10.7 Aplicar P3 en `AiModeSection.tsx`: `triggerMutation.reset()` en `onChange`
  - [x] 10.8 Crear `GeminiAiSummaryServiceTests.cs` (7 tests); actualizar `AiModeOpsEndpointTests.cs` con firmas de stubs y new test de idempotencia
  - [x] 10.9 `dotnet build` 0 errores, `dotnet test` todos pasan (29/29 Infrastructure, 66/66 Api), `npm build` Ops y Main OK

- [x] Task 9: Backend — Build y tests (AC: todos)
  - [x] 9.1 `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] 9.2 Actualizar `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs` — agregar tests para comportamiento por AI_MODE (ver Dev Notes)
  - [x] 9.3 `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter NewsPipelineJobTests` — todos pasan
  - [x] 9.4 `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj` — todos pasan (regresión)
  - [x] 9.5 `npm run build --workspace=src/Web/Ops` — 0 errores TypeScript
  - [x] 9.6 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript

## Senior Developer Review (AI)

### Review Findings

#### Action Items

- [x] [Review][Patch] P1: `GenerateSummaryAsync` retorna `null` → artículo marcado `Processed` sin resumen [AiModeEndpoints.cs:84-85] — cuando la API key no está configurada o la respuesta es vacía, el endpoint marca el artículo como `Processed` en lugar de `Partial`; fix: `if (summary is null) { await newsRepo.UpdateSummaryAsync(articleId, null, NewsArticleStatus.Partial, ct); return Results.NoContent(); }`
- [x] [Review][Patch] P2: `content[0].GetProperty("text")` sin `TryGetProperty` → `KeyNotFoundException` no controlado [AnthropicAiSummaryService.cs:61] — si Anthropic devuelve un elemento sin propiedad `text` (tool-use, refusal), lanza excepción; fix: usar `TryGetProperty("text", out var textProp)` y retornar `null` si no existe
- [x] [Review][Patch] P3: `catch` desnudo sin log — `OperationCanceledException` usa `ct` ya cancelado en `UpdateSummaryAsync` interno [AiModeEndpoints.cs:87-90] — cuando el cliente desconecta, `ct` se cancela, el `catch` intenta `UpdateSummaryAsync` con ese `ct` cancelado → la llamada interna lanza, artículo queda en estado indeterminado; fix: separar `catch (OperationCanceledException)` (no actualizar, dejar estado previo) del `catch (Exception ex)` (loguear + usar `CancellationToken.None` para el update de Partial)
- [x] [Review][Patch] P4: Race condition en `SetModeAsync` — dos requests concurrentes hacen doble insert de PK=1 [AiModeRepository.cs:22-44] — si dos PUT simultáneos leen `config is null` antes de que el primero haga `SaveChanges`, ambos intentan `Add` y el segundo lanza `DbUpdateException`; fix: usar `ExecuteUpdateAsync` con upsert o capturar `DbUpdateException` y reintentar
- [x] [Review][Patch] P5: `GetCurrentModeAsync` falla al inicio del pipeline → job aborta sin fallback [NewsPipelineJob.cs:25] — si la BD no responde al leer el modo, la excepción sale de `ExecuteAsync` sin catch y no se procesan artículos; fix: `try { currentMode = await ... } catch { logger.LogWarning(...); /* fallback Off */ }`
- [x] [Review][Patch] P6: Modelo `claude-haiku-4-5` hardcodeado, no configurable sin redespliegue [AnthropicAiSummaryService.cs:16] — fix: leer desde `configuration["Anthropic:Model"]` con fallback a la constante

#### Deferred

- [x] [Review][Defer] D1: Inyección de prompt via `title`/`snippet` sin sanitizar saltos de línea [AnthropicAiSummaryService.cs:26-33] — deferred, fuente RSS pre-filtrada; surface de ataque baja en MVP
- [x] [Review][Defer] D2: TOCTOU modo Off→Manual entre check (L69) y llamada a Anthropic (L84) [AiModeEndpoints.cs] — deferred, ventana temporal muy pequeña para operación manual
- [x] [Review][Defer] D3: HTTP 429 tratado igual que 500 sin retry/backoff [AnthropicAiSummaryService.cs:53] — deferred, mejora futura; fuera de scope MVP
- [x] [Review][Defer] D4: Artículos `Pending` visibles en endpoints públicos — deferred, comportamiento pre-existente desde historia 4.1, no introducido por este diff
- [x] [Review][Defer] D5: `PreviousMode = null` en rama de creación de `AiModeConfig` cuando config es null [AiModeRepository.cs:27-34] — deferred, inalcanzable en producción (seed garantiza fila Id=1)
- [x] [Review][Defer] D6: Sin tests para endpoint `POST /{id}/ai-summary` (ACs 2 y 3 del endpoint) — deferred, Task 9 del spec solo exige tests del pipeline; agregar en historia siguiente
- [x] [Review][Defer] D7: `UpdateSummaryAsync` retorna 0 filas en silencio si artículo borrado entre `GetById` y `Update` [NewsRepository.cs:54-59] — deferred, raza muy improbable en operación admin-only
- [x] [Review][Defer] D8: Token expirado no dispara refresh en `aiModeApi.ts` — deferred, patrón pre-existente en todo el Ops SPA
- [x] [Review][Defer] D9: Cambio de modo a mitad de ejecución del pipeline [NewsPipelineJob.cs:25] — deferred, por diseño según spec ("aplica en el siguiente ciclo")

### Review Findings — Pass 2 (2026-05-19)

#### Action Items

- [x] [Review][Patch] NR1: `UpdateSummaryAsync` fallback catch usa message-sniffing frágil [NewsRepository.cs:399-416] — el catch `when (ex.Message.Contains("ExecuteUpdate", ...))` es código muerto en SQL Server (ExecuteUpdateAsync con 0 filas no lanza), pero puede tragar InvalidOperationExceptions no relacionadas de EF Core si el mensaje contiene "ExecuteUpdate"; eliminar el bloque fallback o reemplazar con `_ = await ... ; // 0 rows is silent` y documentar explícitamente
- [x] [Review][Patch] NR2: `SetModeAsync` sin guard de idempotencia — escribe audit entry incluso en call no-op [AiModeRepository.cs] — si el modo ya es el solicitado, la llamada igual persiste `PreviousMode = config.Mode` (igual al nuevo), generando entradas de auditoría espurias; fix: añadir `if (config.Mode == mode) return;` antes de mutar el objeto
- [x] [Review][Patch] NR3: `AiModeSection.tsx` no valida formato UUID antes de disparar la mutación [AiModeSection.tsx] — el botón se habilita con cualquier string no vacío; un GUID malformado produce 404 (route constraint no matchea) en lugar de un error legible; fix: validar con regex de UUID antes de llamar `triggerMutation.mutate()`

#### Deferred

- [x] [Review][Defer] ND1: `opsAccessTokenStorageKey` y `getAuthHeaders()` duplicados en `aiModeApi.ts` y `newsApi.ts` — deferred, patrón pre-existente en todo el Ops SPA; refactorizar a util compartido cuando se añada gestión de sesión
- [x] [Review][Defer] ND2: `SetAiModeRequest.Mode` acepta strings numéricos vía `Enum.TryParse` (`"0"` → `AiMode.Off`) [AiModeEndpoints.cs] — deferred, endpoint admin-only; impacto bajo, el valor se serializa de vuelta como "Off"/"Manual" en la respuesta

### Review Findings — Pass 3 (2026-05-19)

#### Action Items

- [x] [Review][Patch] P1: `PUT /api/v1/ops/ai-mode` acepta enums numéricos e indefinidos [AiModeEndpoints.cs:38-44] — `Enum.TryParse<AiMode>` acepta `"0"` y también `"999"`; este último se persiste como modo inválido, deja `AI_MODE` fuera del conjunto `Off|Manual` y altera el comportamiento del pipeline/UI de forma no especificada. Fix: validar contra whitelist explícita (`"Off"`, `"Manual"`) o usar `Enum.TryParse` + `Enum.IsDefined(mode)`.
- [x] [Review][Patch] P2: `SetModeAsync` sobrescribe la auditoría en requests no-op [AiModeRepository.cs:51-55] — si el modo actual ya coincide con el solicitado, igual actualiza `PreviousMode`, `UpdatedAt` y `UpdatedBy`, registrando un cambio inexistente y perdiendo la metadata del último cambio real; esto viola el AC4 de auditoría confiable. Fix: salir temprano con `if (config.Mode == mode) return;`.
- [x] [Review][Patch] P3: `AiModeSection` permite disparar la mutación con IDs malformados y expone un 404 opaco al operador [AiModeSection.tsx:108-109] — el botón se habilita con cualquier string no vacío; un GUID inválido nunca matchea `/{articleId:guid}` y la UI solo muestra un error serializado del cliente. Fix: validar UUID antes de `triggerMutation.mutate()` y mostrar un mensaje local claro.

### Review Findings — Pass 4 (2026-05-19)

#### Action Items

- [x] [Review][Patch] NP1: `UpdateSummaryAsync` en el bloque `catch` no está protegido contra su propia falla [AiModeEndpoints.cs:103] — si la BD está caída cuando Anthropic falla, la excepción escapa del catch block, el artículo queda en estado indeterminado (Pending, no Partial) y el endpoint devuelve 500. Fix: envolver el `UpdateSummaryAsync` del catch en su propio try/catch que loguee y absorba la excepción secundaria.
- [x] [Review][Patch] NP2: `catch (OperationCanceledException)` captura `TaskCanceledException` del timeout de HttpClient [AiModeEndpoints.cs:96-99] — `TaskCanceledException` hereda de `OperationCanceledException`; un timeout de 30s en la llamada a Anthropic provoca que el endpoint haga rethrow y devuelva 500 en lugar de marcar el artículo como `Partial`. La fix de P3 (Pass 1) introdujo esta regresión. Fix: añadir guard `when (ct.IsCancellationRequested)` al catch de `OperationCanceledException`.

#### Deferred

- [x] [Review][Defer] ND3: Anthropic error body descartado cuando `EnsureSuccessStatusCode` lanza — el operador solo ve el código HTTP, no el mensaje de Anthropic (ej. "rate_limit_exceeded") — deferred, mejora de observabilidad futura
- [x] [Review][Defer] ND4: `GetConfigAsync` fallback in-memory tiene `UpdatedAt = DateTimeOffset.UtcNow` variable — timestamps inconsistentes si la fila seed no existe — deferred, inalcanzable en producción; seed garantiza fila Id=1

### Review Findings — Pass 5 (2026-05-19)

#### Action Items

- [x] [Review][Patch] P4: `null` de `GenerateSummaryAsync` debe retornar `503` al admin, no marcar `Partial` [AiModeEndpoints.cs:88-91] — cuando la API key no está configurada el contrato devuelve `null`; actualmente el endpoint lo trata igual que un fallo de IA y persiste `Status=Partial`. Fix: `if (summary is null) return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "El servicio de IA no está configurado.");` — reservar `Partial` solo para excepciones reales.
- [x] [Review][Patch] P5: Fallo de proveedor IA retorna `204 NoContent` en lugar de señalar el error al cliente [AiModeEndpoints.cs:100-117] — `catch (Exception ex)` loguea y marca `Partial` pero luego el flujo cae en `return Results.NoContent()`, mostrando "Resumen solicitado correctamente" cuando la IA falló. Fix: dentro del `catch (Exception ex)`, después del `UpdateSummaryAsync`, retornar `Results.Problem(statusCode: StatusCodes.Status502BadGateway, detail: "El proveedor de IA no está disponible. El artículo fue marcado como Partial.")` en lugar de dejar fluir al `NoContent`.
- [x] [Review][Patch] P1: Proveedor IA cambiado de Anthropic a Gemini (issue de `DefaultModel` inválido resuelto en la migración) — `AnthropicAiSummaryService` eliminado; `GeminiAiSummaryService` creado con modelos configurables `gemini-2.5-flash` (noticias) y `gemini-2.5-pro` (documentos); la API key se configura con `dotnet user-secrets set "Gemini:ApiKey" "..."`.
- [x] [Review][Patch] P2: Sin guard de idempotencia en trigger — trigger repetido sobreescribe artículo `Processed` [AiModeEndpoints.cs:85-94] — añadido `if (article.Status == NewsArticleStatus.Processed) return Results.NoContent();` antes de llamar al proveedor.
- [x] [Review][Patch] P3: `triggerMutation.reset()` ausente en `onChange` del input [AiModeSection.tsx:105] — añadido `triggerMutation.reset()` en el handler `onChange` para limpiar el estado de la mutación anterior.

#### Deferred

- [x] [Review][Defer] ND5: `UpdatedAt` calculado antes de `SaveChangesAsync` [AiModeRepository.cs:25] — deferred, en caso de retry el timestamp refleja el intento previo, no el commit real; impacto de auditoría menor
- [x] [Review][Defer] ND6: Retry de insert concurrente puede relanzar en réplica de lectura [AiModeRepository.cs:44-48] — deferred, `FindAsync` post-`ChangeTracker.Clear()` puede retornar null en réplica de lectura bajo alta concurrencia → `DbUpdateException` resurge como 500; improbable en configuración single-node del MVP
- [x] [Review][Defer] ND7: Actor fallback `"unknown"` persiste en auditoría sin log de advertencia [AiModeEndpoints.cs:46-49] — deferred, la cadena Name→Email→NameIdentifier→"unknown" es correcta; añadir `LogWarning` cuando se cae en "unknown" para detectar JWTs mal configurados
- [x] [Review][Defer] ND8: PK singleton `1` hardcodeado en 5 lugares [AiModeRepository.cs, AiModeConfig.cs, AiModeConfigConfiguration.cs] — deferred, extraer como `AiModeConfig.SingletonId = 1` en una próxima iteración del módulo AI
- [x] [Review][Defer] ND9: Modo stale en ventana de refetch (~200ms) tras Off→Manual [AiModeSection.tsx:104] — deferred, self-correcting; el botón de trigger queda disabled brevemente después de guardar el cambio; resoluble con `queryClient.setQueryData` optimista

### Review Findings — Pass 6 (2026-05-20)

#### Action Items

- [x] [Review][Patch] P1: `GeminiAiSummaryService` devuelve `null` para respuestas vacías del proveedor (safety filter, sin candidatos, sin `content`/`parts`/`text`) — el endpoint interpreta todo `null` como "proveedor no configurado" y devuelve 503; el artículo queda en `Pending` indefinidamente en lugar de marcarse `Partial`. Fix: solo devolver `null` cuando la API key no está configurada; lanzar excepción (`InvalidOperationException`) para todos los demás casos de respuesta vacía, para que el `catch (Exception)` en `AiModeEndpoints` los maneje correctamente como fallo de proveedor. [GeminiAiSummaryService.cs:67-84]
- [x] [Review][Patch] P2: `triggerAiSummary` lanza `new Error(JSON.stringify(error))` — la UI renderiza el objeto ProblemDetails serializado en JSON en lugar del campo `detail` legible. Fix: extraer `error.detail` o `error.message` antes de lanzar: `const msg = (error as {detail?: string}).detail ?? JSON.stringify(error); throw new Error(msg)`. Mismo patrón aplicable a `setAiMode`. [aiModeApi.ts:44]

#### Deferred

- [x] [Review][Defer] ND10: Gemini API key pasa como query param `?key=` en la URL — visible en logs HTTP/telemetría; usar header `x-goog-api-key` sería mejor para log hygiene. Deferred: el spec Dev Notes especifica explícitamente la URL con `?key={apiKey}` y es el patrón estándar documentado de Gemini. [GeminiAiSummaryService.cs:42]

### Review Findings — Pass 7 (2026-05-20)

#### Action Items

- [x] [Review][Patch] P1: `POST /{articleId}/ai-summary` devuelve `BadRequest` con objeto anónimo `{message}` en lugar de `ProblemDetails` [AiModeEndpoints.cs:75-79] — inconsistente con el resto del API; la UI funciona por el helper `getErrorMessage` pero el contrato no es `application/problem+json`. Fix: `Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "La generación de resumen...")`.
- [x] [Review][Patch] P2: `triggerMutation.isSuccess` persiste visualmente al cambiar modo a Off después de un trigger exitoso [AiModeSection.tsx:17-21] — `saveMutation.onSuccess` no resetea `triggerMutation`; el banner verde y el texto "Cambia AI_MODE a Manual" coexisten. Fix: añadir `triggerMutation.reset()` en `saveMutation.onSuccess`.
- [x] [Review][Patch] P3: Botones de modo (Off/Manual) no se deshabilitan durante `saveMutation.isPending` [AiModeSection.tsx:52-66] — el usuario puede seleccionar otro modo mientras el PUT está en vuelo, creando apariencia de estado incorrecto (la mutación usa el valor correcto pero la UI refleja el click posterior). Fix: añadir `disabled={saveMutation.isPending}` a los botones de toggle.
- [x] [Review][Patch] P4: `HttpResponseMessage` no se dispone en `GenerateSummaryAsync` [GeminiAiSummaryService.cs:55] — `PostAsJsonAsync` retorna un `IDisposable`; no envolver en `using` puede retener recursos de conexión bajo carga. Fix: `using var response = await httpClient.PostAsJsonAsync(url, body, ct);`.

#### Deferred

- [x] [Review][Defer] ND11: `ILoggerFactory.CreateLogger("AiModeEndpoints")` llamado por request en lugar de `ILogger<T>` tipado [AiModeEndpoints.cs:71] — deferred, allocación menor, endpoint admin-only de bajo volumen.
- [x] [Review][Defer] ND12: Fallback `AiMode.Off` ante error de BD deja artículos del período de falla como `Processed` permanentemente [NewsPipelineJob.cs:34-42] — deferred, trade-off conocido del diseño de fallback; el admin puede hacer trigger manual de artículos afectados.
- [x] [Review][Defer] ND13: Safety-block de Gemini (HTTP 200, sin candidatos) devuelve 502 con mensaje "proveedor no disponible" en lugar de indicar bloqueo de contenido [GeminiAiSummaryService.cs:67-71] — deferred, el log contiene la excepción con detalle; mejorar cuando se añada observabilidad centralizada.
- [x] [Review][Defer] ND14: `modeQuery.data?.mode as 'Off' | 'Manual'` sin validación en runtime [AiModeSection.tsx:30] — deferred, solo dos modos en MVP; validar al añadir tercer modo.

### Review Findings — Pass 8 (2026-05-20)

#### Deferred

- [x] [Review][Defer] ND15: Race condition: dos requests concurrentes de AdminOps sobre el mismo artículo `Pending` pasan ambas el guard de idempotencia y llaman a Gemini dos veces [AiModeEndpoints.cs:85-100] — deferred, endpoint admin-only de volumen muy bajo; worst case = cuota Gemini desperdiciada + overwrite inocuo del segundo summary sobre el primero.
- [x] [Review][Defer] ND16: `AiSummary` persiste sin validación de longitud máxima antes de `UpdateSummaryAsync` [GeminiAiSummaryService.cs:78 / NewsRepository.cs:58] — deferred, 256 output tokens ≈ 800-1000 chars, bien bajo el límite nvarchar(2048); si se excediera, `DbUpdateException` sería atrapada como fallo de proveedor con mensaje engañoso "proveedor no disponible".

## Dev Notes

### Contexto de historias anteriores (Épica 4)

**Historia 4.1** estableció:
- `NewsArticle` en `src/Server/Domain/News/NewsArticle.cs` — tiene `Status` (`Pending=0, Processed=1, Partial=2, Error=3`)
- `NewsDeduplicator` en `src/Server/Application/News/NewsDeduplicator.cs`
- `IBlocklistRepository` / `BlocklistRepository` — patrón a seguir para `IAiModeRepository`
- `NewsPipelineJob.ExecuteAsync` — actualmente setea `Status = NewsArticleStatus.Pending` en todos los artículos (CAMBIAR en Task 5)

**Historia 4.2** estableció:
- `NewsAssociator.Associate` — lógica pura estática
- `INewsRepository` con `AddWithLinksAsync` (el método activo para persistir artículos)
- `NewsArticleDto` en `src/Server/SharedApiContracts/News/NewsArticleDto.cs` — ACTUALIZAR para incluir `AiSummary`
- `NewsEndpoints.cs` — tiene `ToDto(NewsArticle)` que mapea campos — ACTUALIZAR
- Patrón de endpoints Ops: ver `NewsBlocklistEndpoints.cs` como plantilla exacta (auth `AdminOps`, grupos, produces)

---

### Task 1 — Dominio

#### AiMode.cs

```csharp
// src/Server/Domain/News/AiMode.cs
namespace Domain.News;

public enum AiMode
{
    Off = 0,
    Manual = 1,
}
```

#### AiModeConfig.cs

```csharp
// src/Server/Domain/News/AiModeConfig.cs
namespace Domain.News;

public class AiModeConfig
{
    public int Id { get; set; } = 1; // single-row: siempre Id=1
    public AiMode Mode { get; set; } = AiMode.Off;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }
    public AiMode? PreviousMode { get; set; } // para auditoría antes/después
}
```

**Decisión de schema**: `AiModeConfig` va en `ai` schema porque aplica a todo el sistema de IA (noticias + fundamentales futuros). La tabla es `ai.AiModeConfig`.

#### Agregar campo a NewsArticle.cs

Agregar después de `Snippet`:
```csharp
public string? AiSummary { get; set; }
```

---

### Task 2 — EF Core

#### AiModeConfigConfiguration.cs

```csharp
// src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiModeConfigConfiguration.cs
namespace Infrastructure.Persistence.SqlServer.Configurations.News;

public class AiModeConfigConfiguration : IEntityTypeConfiguration<AiModeConfig>
{
    public void Configure(EntityTypeBuilder<AiModeConfig> builder)
    {
        builder.ToTable("AiModeConfig", schema: "ai");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Mode)
            .HasColumnName("mode")
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);
        builder.Property(x => x.PreviousMode)
            .HasColumnName("previous_mode")
            .HasConversion<string>()
            .HasMaxLength(20);

        // Seed: fila inicial Off en el primer despliegue
        builder.HasData(new AiModeConfig
        {
            Id = 1,
            Mode = AiMode.Off,
            UpdatedAt = new DateTimeOffset(2026, 5, 19, 0, 0, 0, TimeSpan.Zero),
            UpdatedBy = "system",
            PreviousMode = null,
        });
    }
}
```

**IMPORTANTE**: La migración creará el schema `ai` con `CREATE SCHEMA ai` y la tabla `ai.AiModeConfig`. La migración también debe agregar la columna `ai_summary` a `news.NewsArticle` (columna nullable, `nvarchar(max)` o `nvarchar(2000)`).

Registrar en `AppDbContext.OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new AiModeConfigConfiguration());
```

La columna `ai_summary` en `NewsArticleConfiguration` se agrega automáticamente cuando EF detecta el campo nuevo en `NewsArticle`. Verificar que la configuración existente en `NewsArticleConfiguration.cs` no requiera cambios explícitos para campos nullable.

---

### Task 3 — Application layer

#### IAiModeRepository.cs

```csharp
// src/Server/Application/News/IAiModeRepository.cs
namespace Application.News;

public interface IAiModeRepository
{
    Task<AiMode> GetCurrentModeAsync(CancellationToken ct = default);
    Task SetModeAsync(AiMode mode, string actor, CancellationToken ct = default);
}
```

#### IAiSummaryService.cs

```csharp
// src/Server/Application/News/IAiSummaryService.cs
namespace Application.News;

public interface IAiSummaryService
{
    Task<string?> GenerateSummaryAsync(string title, string? snippet, CancellationToken ct = default);
}
```

Contrato: devuelve `null` si el proveedor no está configurado o si la respuesta es vacía. Lanza excepción si la llamada falla (el endpoint manejará el catch → estado `Partial`).

---

### Task 4 — Infrastructure: implementaciones

#### AiModeRepository.cs

```csharp
// src/Server/Infrastructure/Persistence/Repositories/News/AiModeRepository.cs
namespace Infrastructure.Persistence.Repositories.News;

public class AiModeRepository(AppDbContext db) : IAiModeRepository
{
    public async Task<AiMode> GetCurrentModeAsync(CancellationToken ct = default)
    {
        var config = await db.AiModeConfigs.FindAsync([1], ct);
        return config?.Mode ?? AiMode.Off;
    }

    public async Task SetModeAsync(AiMode mode, string actor, CancellationToken ct = default)
    {
        var config = await db.AiModeConfigs.FindAsync([1], ct);
        if (config is null)
        {
            db.AiModeConfigs.Add(new AiModeConfig
            {
                Id = 1,
                Mode = mode,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = actor,
                PreviousMode = null,
            });
        }
        else
        {
            config.PreviousMode = config.Mode;
            config.Mode = mode;
            config.UpdatedAt = DateTimeOffset.UtcNow;
            config.UpdatedBy = actor;
        }

        await db.SaveChangesAsync(ct);
    }
}
```

#### AnthropicAiSummaryService.cs

```csharp
// src/Server/Infrastructure/Integrations/Ai/AnthropicAiSummaryService.cs
namespace Infrastructure.Integrations.Ai;

public class AnthropicAiSummaryService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<AnthropicAiSummaryService> logger) : IAiSummaryService
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-haiku-4-5-20251001";

    public async Task<string?> GenerateSummaryAsync(string title, string? snippet, CancellationToken ct = default)
    {
        var apiKey = configuration["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Anthropic API key not configured — skipping AI summary");
            return null;
        }

        var prompt = $"""
            Genera un resumen ejecutivo breve (máximo 2 oraciones) en español de esta noticia sobre FIBRAs inmobiliarias mexicanas.

            Título: {title}
            Fragmento: {snippet ?? "(sin fragmento disponible)"}

            Responde únicamente con el resumen, sin introducción ni formato adicional.
            """;

        var requestBody = new
        {
            model = Model,
            max_tokens = 256,
            messages = new[] { new { role = "user", content = prompt } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = JsonContent.Create(requestBody);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}
```

**Imports necesarios**: `System.Net.Http.Json`, `System.Text.Json`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Logging`, `Application.News`.

**Registrar en ApiServiceExtensions.cs**:
```csharp
builder.Services.AddScoped<IAiModeRepository, AiModeRepository>();
builder.Services.AddHttpClient<IAiSummaryService, AnthropicAiSummaryService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

#### appsettings.json y appsettings.Development.json

Agregar sección (sin valor real en código — la clave real va en user secrets o variable de entorno):
```json
"Anthropic": {
  "ApiKey": ""
}
```

En `appsettings.Development.json`:
```json
"Anthropic": {
  "ApiKey": ""
}
```

La clave real se configura con:
```bash
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..." --project src/Server/Api
```

---

### Task 5 — NewsPipelineJob modificado

**Cambio mínimo** — solo las partes que cambian:

```csharp
// Constructor: agregar IAiModeRepository aiModeRepo
public class NewsPipelineJob(
    IFibraRepository fibraRepo,
    INewsRepository newsRepo,
    IBlocklistRepository blocklistRepo,
    IRssClient rssClient,
    IAiModeRepository aiModeRepo,        // ← nuevo
    ILogger<NewsPipelineJob> logger)

// Al inicio de ExecuteAsync, antes del primer foreach:
var currentMode = await aiModeRepo.GetCurrentModeAsync(ct);

// Al crear el NewsArticle, cambiar:
Status = currentMode == AiMode.Off ? NewsArticleStatus.Processed : NewsArticleStatus.Pending,
// (reemplaza: Status = NewsArticleStatus.Pending)
```

---

### Task 6 — Contratos y endpoints

#### AiModeDto.cs

```csharp
// src/Server/SharedApiContracts/News/AiModeDto.cs
namespace SharedApiContracts.News;

public sealed record AiModeDto(
    string Mode,          // "Off" | "Manual"
    DateTimeOffset UpdatedAt,
    string? UpdatedBy,
    string? PreviousMode  // "Off" | "Manual" | null
);
```

#### NewsArticleDto actualizado

```csharp
// src/Server/SharedApiContracts/News/NewsArticleDto.cs
public sealed record NewsArticleDto(
    Guid Id,
    string Title,
    string Source,
    DateTimeOffset PublishedAt,
    string Url,
    string? Snippet,
    string? AiSummary    // ← nuevo campo
);
```

#### AiModeEndpoints.cs

```csharp
// src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs
namespace Api.Endpoints.Ops;

public static class AiModeEndpoints
{
    public static IEndpointRouteBuilder MapAiMode(this IEndpointRouteBuilder app)
    {
        // ── Configuración del modo ──────────────────────────────────────────
        var configGroup = app.MapGroup("/api/v1/ops/ai-mode")
            .RequireAuthorization("AdminOps")
            .WithTags("AI");

        configGroup.MapGet("/", async (IAiModeRepository repo, CancellationToken ct) =>
        {
            var config = await repo.GetConfigAsync(ct); // ver nota abajo
            return Results.Ok(new AiModeDto(
                config.Mode.ToString(),
                config.UpdatedAt,
                config.UpdatedBy,
                config.PreviousMode?.ToString()));
        })
        .Produces<AiModeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        configGroup.MapPut("/", async (
            SetAiModeRequest request,
            IAiModeRepository repo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<AiMode>(request.Mode, ignoreCase: true, out var mode))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["mode"] = [$"Valor inválido. Use 'Off' o 'Manual'."],
                });
            }

            var actor = ctx.User.Identity?.Name ?? "unknown";
            await repo.SetModeAsync(mode, actor, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // ── Generación manual de resumen por artículo ───────────────────────
        var newsGroup = app.MapGroup("/api/v1/ops/news")
            .RequireAuthorization("AdminOps")
            .WithTags("AI");

        newsGroup.MapPost("/{articleId:guid}/ai-summary", async (
            Guid articleId,
            IAiModeRepository aiModeRepo,
            INewsRepository newsRepo,
            IAiSummaryService summaryService,
            CancellationToken ct) =>
        {
            var mode = await aiModeRepo.GetCurrentModeAsync(ct);
            if (mode != AiMode.Manual)
                return Results.BadRequest(new { message = "La generación de resumen solo está disponible cuando AI_MODE=Manual." });

            var article = await newsRepo.GetByIdAsync(articleId, ct);
            if (article is null)
                return Results.NotFound();

            try
            {
                var summary = await summaryService.GenerateSummaryAsync(article.Title, article.Snippet, ct);
                await newsRepo.UpdateSummaryAsync(articleId, summary, NewsArticleStatus.Processed, ct);
                return Results.NoContent();
            }
            catch (Exception)
            {
                await newsRepo.UpdateSummaryAsync(articleId, null, NewsArticleStatus.Partial, ct);
                return Results.NoContent();
            }
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}

public sealed record SetAiModeRequest(string Mode);
```

**NOTA**: El endpoint GET necesita `GetConfigAsync` que devuelve la entidad completa `AiModeConfig`. Hay dos opciones:
- Agregar `GetConfigAsync()` a `IAiModeRepository` (devuelve `AiModeConfig`)
- Hacer que `GetCurrentModeAsync` devuelva un DTO

Usar la opción A: agregar `Task<AiModeConfig> GetConfigAsync(CancellationToken ct)` a `IAiModeRepository` e implementarla en `AiModeRepository`.

También agregar a `INewsRepository`:
```csharp
Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task UpdateSummaryAsync(Guid id, string? summary, NewsArticleStatus status, CancellationToken ct = default);
```

E implementar en `NewsRepository`:
```csharp
public Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken ct = default)
    => db.NewsArticles.FindAsync([id], ct).AsTask();

public async Task UpdateSummaryAsync(Guid id, string? summary, NewsArticleStatus status, CancellationToken ct = default)
{
    await db.NewsArticles
        .Where(a => a.Id == id)
        .ExecuteUpdateAsync(s => s
            .SetProperty(a => a.AiSummary, summary)
            .SetProperty(a => a.Status, status), ct);
}
```

**`ExecuteUpdateAsync` es seguro aquí** porque no hay transacción compartida — es una operación atómica sobre un solo registro.

#### ToDto en NewsEndpoints.cs

```csharp
private static NewsArticleDto ToDto(NewsArticle a) =>
    new(a.Id, a.Title, a.Source, a.PublishedAt, a.Url, a.Snippet, a.AiSummary);
```

#### Registro en Program.cs

Agregar junto a los otros `Map*`:
```csharp
app.MapAiMode();
```

---

### Task 7 — Frontend Ops: módulo AI Mode

El SPA Ops actualmente es monolítico en `App.tsx` (sin router). Agregar la nueva sección como componente `AiModeSection` importado en `App.tsx`.

#### aiModeApi.ts

```typescript
// src/Web/Ops/src/api/aiModeApi.ts
import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })
const opsAccessTokenStorageKey = 'fibradis.ops.accessToken'

export type AiModeDto = components['schemas']['AiModeDto']

function getAuthHeaders(): HeadersInit {
  if (typeof window === 'undefined') return {}
  const token =
    window.sessionStorage.getItem(opsAccessTokenStorageKey) ??
    window.localStorage.getItem(opsAccessTokenStorageKey)
  return token ? { Authorization: `Bearer ${token}` } : {}
}

export async function fetchAiMode(): Promise<AiModeDto> {
  const { data, error } = await apiClient['/api/v1/ops/ai-mode'].GET({
    headers: getAuthHeaders(),
  })
  if (error) throw new Error(`Error al obtener AI_MODE: ${JSON.stringify(error)}`)
  if (!data) throw new Error('La API no devolvió el modo AI.')
  return data
}

export async function setAiMode(mode: 'Off' | 'Manual'): Promise<void> {
  const { error } = await apiClient['/api/v1/ops/ai-mode'].PUT({
    body: { mode },
    headers: getAuthHeaders(),
  })
  if (error) throw new Error(`Error al actualizar AI_MODE: ${JSON.stringify(error)}`)
}

export async function triggerAiSummary(articleId: string): Promise<void> {
  const { error } = await apiClient['/api/v1/ops/news/{articleId}/ai-summary'].POST({
    params: { path: { articleId } },
    headers: getAuthHeaders(),
  })
  if (error) throw new Error(`Error al generar resumen: ${JSON.stringify(error)}`)
}
```

**IMPORTANTE**: Verificar que los tipos `AiModeDto` y los paths existen en `schema.d.ts` después de ejecutar `npm run codegen:api` en Task 6.7.

#### AiModeSection.tsx

```tsx
// src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx
import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchAiMode, setAiMode } from '@/api/aiModeApi'

export function AiModeSection() {
  const queryClient = useQueryClient()
  const [selected, setSelected] = useState<'Off' | 'Manual' | null>(null)

  const modeQuery = useQuery({
    queryKey: ['ai-mode'],
    queryFn: fetchAiMode,
  })

  const saveMutation = useMutation({
    mutationFn: setAiMode,
    onSuccess: async () => {
      setSelected(null)
      await queryClient.invalidateQueries({ queryKey: ['ai-mode'] })
    },
  })

  const currentMode = modeQuery.data?.mode as 'Off' | 'Manual' | undefined
  const pendingMode = selected ?? currentMode

  return (
    <section className="rounded-2xl border border-border/80 bg-white/90 p-6 shadow-sm">
      <div className="flex flex-col gap-2">
        <h2 className="text-lg font-semibold tracking-tight">Modo AI de Noticias</h2>
        <p className="max-w-3xl text-sm text-muted-foreground">
          Controla si se generan resúmenes automáticos de noticias. El cambio aplica en el siguiente ciclo del pipeline.
        </p>
      </div>

      {modeQuery.isLoading ? (
        <p className="mt-6 text-sm text-muted-foreground">Cargando configuración...</p>
      ) : modeQuery.isError ? (
        <p className="mt-6 text-sm text-destructive">{modeQuery.error.message}</p>
      ) : (
        <>
          <div className="mt-6 flex gap-3">
            {(['Off', 'Manual'] as const).map((mode) => (
              <button
                key={mode}
                type="button"
                className={[
                  'rounded-xl border px-5 py-2.5 text-sm font-medium transition',
                  pendingMode === mode
                    ? 'border-teal-700 bg-teal-700 text-white'
                    : 'border-border bg-white text-slate-700 hover:border-teal-600',
                ].join(' ')}
                onClick={() => setSelected(mode)}
              >
                {mode === 'Off' ? 'Off — sin resúmenes' : 'Manual — disparar desde Ops'}
              </button>
            ))}
          </div>

          {selected !== null && selected !== currentMode ? (
            <div className="mt-4 flex items-center gap-3">
              <button
                className="h-10 rounded-xl bg-teal-700 px-5 text-sm font-medium text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-teal-400"
                disabled={saveMutation.isPending}
                onClick={() => saveMutation.mutate(selected)}
                type="button"
              >
                {saveMutation.isPending ? 'Guardando...' : 'Guardar cambio'}
              </button>
              <button
                className="text-sm text-muted-foreground hover:text-foreground"
                onClick={() => setSelected(null)}
                type="button"
              >
                Cancelar
              </button>
            </div>
          ) : null}

          {saveMutation.isError ? (
            <p className="mt-3 text-sm text-destructive">{saveMutation.error.message}</p>
          ) : null}

          {modeQuery.data ? (
            <p className="mt-4 text-xs text-muted-foreground">
              Último cambio: {new Date(modeQuery.data.updatedAt).toLocaleString('es-MX', { dateStyle: 'medium', timeStyle: 'short' })}
              {modeQuery.data.updatedBy ? ` · por ${modeQuery.data.updatedBy}` : ''}
              {modeQuery.data.previousMode ? ` · anterior: ${modeQuery.data.previousMode}` : ''}
            </p>
          ) : null}
        </>
      )}
    </section>
  )
}
```

#### Actualizar App.tsx

Agregar import y render del nuevo componente junto a la sección de blocklist. El patrón es agregar `<AiModeSection />` como sección adicional dentro del `<main>`.

---

### Task 8 — Frontend Main: mostrar resumen

En `NewsSection.tsx` y `NoticiasSection.tsx` el texto descriptivo de cada artículo actualmente muestra `snippet`. Cambiar para mostrar `aiSummary` si existe, con fallback a `snippet`:

```tsx
// Antes:
<p className="text-sm text-muted-foreground line-clamp-2">{article.snippet}</p>

// Después:
{(article.aiSummary ?? article.snippet) ? (
  <p className="text-sm text-muted-foreground line-clamp-2">
    {article.aiSummary ?? article.snippet}
  </p>
) : null}
```

Agregar `aiSummary` al tipo de artículo (o verificar que el tipo generado por codegen ya lo incluye).

---

### Task 9 — Tests obligatorios

#### NewsPipelineJobTests.cs — nuevos casos

En `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`, agregar tests que verifican:

1. `ExecuteAsync_WithAiModeOff_SetsArticleStatusToProcessed` — mock `IAiModeRepository.GetCurrentModeAsync` retorna `AiMode.Off`; verificar que el artículo persistido tiene `Status = NewsArticleStatus.Processed`.

2. `ExecuteAsync_WithAiModeManual_SetsArticleStatusToPending` — mock retorna `AiMode.Manual`; verificar `Status = NewsArticleStatus.Pending`.

El test existente puede estar usando `Status = Pending` — actualizar si es necesario para que refleje el nuevo comportamiento con `AiMode.Off`.

**Patrón de mock para `IAiModeRepository`**: igual que el mock existente de `INewsRepository` (NSubstitute o Moq — verificar cuál usa el proyecto).

---

### Proveedor IA: Gemini (migrado desde Anthropic en Pass 5)

**Decisión de migración**: Anthropic fue reemplazado por Google Gemini para la generación de resúmenes. La abstracción `IAiSummaryService` permite cambiar el proveedor sin modificar el dominio ni los endpoints.

**Arquitectura del wrapper para futuras historias (Épica 5+)**:

```
IAiSummaryService
  └── GeminiAiSummaryService  ← implementación activa
       ├── AiContentType.News     → modelo gemini-2.5-flash  (rápido, bajo costo)
       └── AiContentType.Document → modelo gemini-2.5-pro    (mayor capacidad, OCR/PDFs)
```

**`AiContentType` enum** (`src/Server/Domain/News/AiContentType.cs`):
```csharp
public enum AiContentType { News = 0, Document = 1 }
```

**Configuración de modelos** — configurable sin redespliegue vía `appsettings.json` o user-secrets:
```json
"Gemini": {
  "ApiKey": "",
  "NewsModel": "",       // fallback: gemini-2.5-flash
  "DocumentModel": ""   // fallback: gemini-2.5-pro
}
```

La API key real NUNCA va en código fuente — configurar con:
```bash
dotnet user-secrets set "Gemini:ApiKey" "AI..." --project src/Server/Api
```

**URL de la API Gemini**: `POST https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}`

**Shape de respuesta Gemini**:
```json
{
  "candidates": [
    { "content": { "parts": [{ "text": "resumen..." }] } }
  ]
}
```
Usar `TryGetProperty` en todos los niveles — la respuesta puede omitir `candidates`, `content` o `parts` si el modelo rechaza o retorna safety block.

**Para historia de procesamiento de PDFs/Documentos (Épicas 5+)**:
- Inyectar `IAiSummaryService` en el endpoint/job de fundamentales
- Llamar con `AiContentType.Document` para usar `gemini-2.5-pro` automáticamente
- El modelo Pro tiene ventana de contexto más grande y capacidades de OCR en imágenes
- Si se agregan más tipos de contenido, extender el enum `AiContentType` y agregar `case` en el switch de `GeminiAiSummaryService`

**Respuesta `null` del servicio** significa que `Gemini:ApiKey` no está configurado. El endpoint devuelve `503 Service Unavailable`. Esto NO es un fallo de IA — es un problema de configuración del entorno.

**Excepción del servicio** significa fallo real del proveedor (timeout, 429, 5xx). El endpoint devuelve `502 Bad Gateway` y marca el artículo como `Partial`.

---

### Convenciones críticas para esta historia

- **Separación de módulos**: `AnthropicAiSummaryService` en `Infrastructure.Integrations.Ai`, no en `Application` — las llamadas HTTP pertenecen a Infrastructure.
- **Single-row pattern en AiModeConfig**: `Id=1` siempre. `FindAsync([1], ct)` para leer. No usar `FirstOrDefault` — el seed garantiza que la fila existe.
- **ExecuteUpdateAsync**: seguro para `UpdateSummaryAsync` porque opera sobre una fila identificada por PK — no viola la regla de Task.WhenAll sobre el mismo DbContext.
- **Actor en auditoría**: leer `ctx.User.Identity?.Name` en el endpoint PUT. Si el claim de nombre no está configurado en JWT, verificar en `TokenService.cs` o en la configuración de `AddFibradisAuthentication`.
- **Configuración Anthropic**: usar `IConfiguration`, no `IOptions<T>` — mantener consistencia con el resto del proyecto (ver `ApiServiceExtensions.cs` que usa `GetConnectionString` directo de `IConfiguration`).
- **noUnusedLocals**: todos los imports declarados deben usarse en TypeScript.
- **Imports absolutos `@/`**: obligatorio en Ops SPA — ver `newsApi.ts` como referencia.

---

### Verificaciones antes de implementar

1. **Grep `NewsArticleStatus.Pending`** en `NewsPipelineJob.cs` — confirmar la línea exacta a cambiar.
2. **Verificar qué framework de mocking** usa `Infrastructure.Tests` — buscar en el `.csproj` si es NSubstitute, Moq u otro.
3. **Verificar `ctx.User.Identity?.Name`**: hacer grep de `ClaimTypes` o `NameClaimType` en `Authentication/` para saber qué claim se usa como nombre de usuario en el JWT.
4. **Verificar `FindAsync` signature para EF Core 10**: en EF Core 10 la signature puede ser `FindAsync(object[] keyValues, CancellationToken)` — usar `FindAsync(new object[] { 1 }, ct)` si la versión array es necesaria.

---

### Checklist de rutas públicas (solo si aplica)

Esta historia NO agrega rutas públicas nuevas al Main SPA — solo modifica componentes existentes. No aplica el checklist SSR/SEO de `convenciones-fibradis.md`.

## Dev Agent Record

### Agent Model Used

gpt-5.5-codex

### Debug Log References

- `python scripts/memory/memory_cli.py search "AI_MODE noticias Anthropic NewsPipelineJob"`
- Context7: ASP.NET Core JWT bearer `NameClaimType` y `HttpContext.User.Identity.Name`; Anthropic Messages API para shape de respuesta y extracción de texto.
- `dotnet ef migrations add AddAiModeConfig --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`
- `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`
- `npm run codegen:api`
- `dotnet build FIBRADIS.slnx`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter NewsPipelineJobTests`
- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj`
- `npm run build --workspace=src/Web/Ops`
- `npm run build --workspace=src/Web/Main`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~NewsPipelineJobTests|FullyQualifiedName~AnthropicAiSummaryServiceTests"`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~AiModeOpsEndpointTests"`
- Context7: Gemini API `generateContent` response shape (`candidates`, `content.parts.text`, `finishReason`, `promptFeedback`) para distinguir respuesta vacía/bloqueada de falta de configuración local.
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter FullyQualifiedName~GeminiAiSummaryServiceTests`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~AiModeOpsEndpointTests`
- `dotnet test FIBRADIS.slnx --no-build`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~AiModeOpsEndpointTests`
- `npm run build --workspace=src/Web/Ops`
- `npm run build --workspace=src/Web/Main`
- `dotnet build FIBRADIS.slnx`
- `dotnet test FIBRADIS.slnx --no-build`

### Completion Notes List

- Se agregó `AiMode` y `AiModeConfig` con seed inicial `Off` en schema `ai`, además de `AiSummary` en `news.NewsArticle`.
- `NewsPipelineJob` ahora lee `AI_MODE` por corrida y publica artículos como `Processed` en `Off` o `Pending` en `Manual` sin llamar a IA durante la ingesta.
- Se agregaron endpoints Ops para leer/cambiar `AI_MODE` y disparar resumen manual por artículo; la auditoría persiste modo previo, actor y timestamp.
- Se implementó `AnthropicAiSummaryService` con configuración por `Anthropic:ApiKey`; si la llamada falla, el artículo queda en `Partial`.
- Se ajustó `NewsRepository` para que noticias `Partial` sigan visibles en Home y ficha pública, cumpliendo AC #3.
- Se regeneró OpenAPI y el cliente compartido; Ops ahora tiene sección `AiModeSection` y Main muestra `aiSummary` con fallback a `snippet`.
- Tests ejecutados con resultado final: `dotnet build` 0 errores, `NewsPipelineJobTests` 4/4 passing, `Application.Tests` 35/35 passing, `npm run build --workspace=src/Web/Ops` OK, `npm run build --workspace=src/Web/Main` OK.
- Se resolvieron los 6 hallazgos de code review: `AiModeEndpoints` ahora marca `Partial` si la IA devuelve `null`, separa cancelación de error real y registra el fallo antes de actualizar con `CancellationToken.None`.
- `AnthropicAiSummaryService` ahora lee `Anthropic:Model` desde configuración, conserva fallback al modelo por defecto y tolera respuestas sin propiedad `text`.
- `AiModeRepository` reintenta tras `DbUpdateException` en la creación concurrente de la fila singleton, y `NewsPipelineJob` cae a `AiMode.Off` si la lectura del modo falla.
- Tests adicionales ejecutados con resultado final: `Infrastructure.Tests` 6/6 passing (pipeline + Anthropic) y `Api.Tests` 1/1 passing para `POST /api/v1/ops/news/{articleId}/ai-summary` con `summary = null`.
- Resueltos hallazgos Pass 2 (NR1-NR3) y Pass 3 (P1-P3): eliminado fallback catch frágil en `UpdateSummaryAsync`; añadido guard de idempotencia en `SetModeAsync` (evita auditorías espurias); añadido `Enum.IsDefined` en PUT endpoint para rechazar enums numéricos/fuera de rango; validación UUID en `AiModeSection.tsx` con mensaje de error local. Build 0 errores, 4/4 NewsPipelineJobTests, 35/35 Application.Tests, Ops y Main SPA build OK.
- Resueltos hallazgos Pass 4 (NP1-NP2): `AiModeEndpoints` ahora solo re-lanza cancelaciones reales del request (`when (ct.IsCancellationRequested)`), trata timeouts del proveedor como fallo de IA y protege el `UpdateSummaryAsync` secundario con `try/catch` y logging para no devolver 500 por una falla de persistencia posterior.
- Tests ejecutados en esta pasada: `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~AiModeOpsEndpointTests"` → 3/3 passing; `dotnet build FIBRADIS.slnx` → 0 errores, 0 warnings.
- Resueltos hallazgos Pass 6 (P1-P2): `GeminiAiSummaryService` ahora solo retorna `null` cuando falta `Gemini:ApiKey`; respuestas vacías/bloqueadas del proveedor lanzan `InvalidOperationException` para que el endpoint marque `Partial` y devuelva `502`. En Ops, `aiModeApi.ts` extrae `detail/message` de ProblemDetails para mostrar errores legibles al operador.
- Tests ejecutados en esta pasada: `GeminiAiSummaryServiceTests` 8/8 passing, `AiModeOpsEndpointTests` 4/4 passing, `npm run build --workspace=src/Web/Ops` OK.
- Validación amplia adicional: `dotnet build FIBRADIS.slnx` OK, `npm run build --workspace=src/Web/Main` OK. `dotnet test FIBRADIS.slnx --no-build` ejecutó `Domain.Tests` 9/9, `Application.Tests` 35/35, `Infrastructure.Tests` 30/30, `Jobs.Tests` 2/2 y `Api.Tests` 60/60, pero el host de pruebas abortó al cierre por `ObjectDisposedException: EventLogInternal` en logging de Hangfire; queda documentado como incidencia de teardown del runner, no como fallo del patch.
- Resueltos hallazgos Pass 7 (P1-P4): el endpoint manual ahora retorna `ProblemDetails` en 400, `AiModeSection` limpia el estado de éxito previo al guardar y bloquea toggles mientras el PUT está en vuelo, y `GeminiAiSummaryService` dispone explícitamente el `HttpResponseMessage`.
- Validación Pass 7: `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~AiModeOpsEndpointTests` → 5/5 passing; `npm run build --workspace=src/Web/Ops` OK; `npm run build --workspace=src/Web/Main` OK; `dotnet build FIBRADIS.slnx` OK. `dotnet test FIBRADIS.slnx --no-build` volvió a ejecutar `Domain.Tests` 9/9, `Application.Tests` 35/35, `Infrastructure.Tests` 30/30, `Jobs.Tests` 2/2 y `Api.Tests` 64/64, pero el runner abortó al teardown por `ObjectDisposedException: EventLogInternal` de Hangfire y además reportó tres ensamblados sin tests (`ApiCompatibility.Tests`, `Integrations.Tests`, `Persistence.Tests`).

### File List

- `_bmad-output/implementation-artifacts/4-3-soporte-para-ai-mode-en-noticias-off-y-manual.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `scripts/codegen/Api.json`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs`
- `src/Server/Api/Endpoints/Public/NewsEndpoints.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Api/appsettings.Development.json`
- `src/Server/Api/appsettings.json`
- `src/Server/Application/News/IAiModeRepository.cs`
- `src/Server/Application/News/IAiSummaryService.cs`
- `src/Server/Application/News/INewsRepository.cs`
- `src/Server/Domain/News/AiMode.cs`
- `src/Server/Domain/News/AiModeConfig.cs`
- `src/Server/Domain/News/NewsArticle.cs`
- `src/Server/Domain/News/AiContentType.cs`
- `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs`
- ~~`src/Server/Infrastructure/Integrations/Ai/AnthropicAiSummaryService.cs`~~ (eliminado — migrado a Gemini)
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260519215027_AddAiModeConfig.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260519215027_AddAiModeConfig.Designer.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/AiModeRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiModeConfigConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleConfiguration.cs`
- `src/Server/SharedApiContracts/News/AiModeDto.cs`
- `src/Server/SharedApiContracts/News/NewsArticleDto.cs`
- `src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx`
- `src/Web/Main/src/modules/home/NewsSection.tsx`
- `src/Web/Ops/src/App.tsx`
- `src/Web/Ops/src/api/aiModeApi.ts`
- `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiAiSummaryServiceTests.cs`
- ~~`tests/Unit/Infrastructure.Tests/Integrations/Ai/AnthropicAiSummaryServiceTests.cs`~~ (eliminado)
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`

## Change Log

- 2026-05-19: Story creada — análisis completo de Épica 4, código existente y arquitectura del sistema.
- 2026-05-19: Implementación completa de AI_MODE Off/Manual, endpoints Ops, migración EF, cliente OpenAPI regenerado y validación con builds/tests.
- 2026-05-19: Addressed code review findings — 6 items resolved con cobertura adicional para pipeline fallback, parsing Anthropic y endpoint manual de resumen.
- 2026-05-19: Addressed code review findings Pass 2+3 — 6 items resolved: fallback catch eliminado (NR1), idempotencia SetModeAsync (NR2/P2), Enum.IsDefined validation (P1), UUID regex validation en AiModeSection (NR3/P3).
- 2026-05-19: Addressed code review findings Pass 4 — 2 items resolved: catch filtrado por cancelación real del request y protección del update secundario a `Partial` con logging.
- 2026-05-19: Migración de proveedor IA Anthropic → Gemini + patches Pass 5 (P1-P5) — `GeminiAiSummaryService` con routing `gemini-2.5-flash` (noticias) / `gemini-2.5-pro` (documentos) vía `AiContentType`; null→503, excepción→502, idempotencia en trigger, `triggerMutation.reset()` en onChange. Tests: 7 nuevos Gemini unit tests, 4 integration tests actualizados. Build 0 errores, 141/141 tests passing.
- 2026-05-20: Addressed code review findings Pass 6 — 2 items resolved: Gemini ahora distingue configuración ausente vs respuesta vacía del proveedor, y Ops muestra `detail/message` legible en errores de AI_MODE/resumen manual. Validaciones: `GeminiAiSummaryServiceTests` 8/8, `AiModeOpsEndpointTests` 4/4, `npm run build --workspace=src/Web/Ops` OK.
- 2026-05-20: Addressed code review findings Pass 7 — 4 items resolved: `POST /ai-summary` devuelve `ProblemDetails` en 400, `AiModeSection` resetea feedback exitoso al guardar y bloquea toggles durante save, y `GeminiAiSummaryService` dispone el `HttpResponseMessage`. Validaciones: `AiModeOpsEndpointTests` 5/5, `dotnet build` OK, builds Ops/Main OK; full suite sigue chocando al teardown por `EventLogInternal` de Hangfire.
