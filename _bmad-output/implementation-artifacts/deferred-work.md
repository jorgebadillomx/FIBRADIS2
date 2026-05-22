# Deferred Work

Items diferidos durante code reviews. Cada sección tiene la historia origen y la fecha.

## Deferred from: code review of 4-1-ingesta-rss-blocklist-y-deduplicacion-de-noticias (2026-05-19)

- **GUIDs de seed generados con MD5** [`NewsSeed.cs:GuidFromKey`] — Si se modifica o reordena `DefaultBlocklist`, los GUIDs cambian y EF emite DELETE+INSERT en la siguiente migración. Considerar GUIDs literales hardcodeados.
- **`PublishedAt` sin valor por defecto** [`NewsArticle.cs`] — Queda como `DateTimeOffset.MinValue` si código futuro no lo asigna; puede causar ordering inesperado en queries `ORDER BY PublishedAt DESC`.
- **RSS fetches secuenciales** [`NewsPipelineJob.cs`] — N fibras × M queries = N×M llamadas HTTP en serie. Intencional (rate limiting Google News), pero el tiempo de ejecución escala linealmente. Revisar si el número de FIBRAs crece significativamente.
- **`CancellationToken.None` en Hangfire** [`Program.cs:50`] — Sin cancelación graceful en shutdown del host. Mismo patrón que `MarketPipelineJob`; abordar junto con ese job en Epic 5 si se necesita shutdown determinístico.
- **`AddAsync` con `SaveChangesAsync` individual** [`NewsRepository.cs`] — N round-trips a SQL Server en lugar de un batch. Race condition cubierta por unique constraint; aceptable en volumen MVP (~50-200 artículos/hora).
- **`Status` como `nvarchar(16)`** [`NewsArticleConfiguration.cs`] — Frágil si futuros valores del enum superan 16 chars. Ampliar a 32 en próxima migración del módulo `news`.

## Deferred from: code review of 4-1-ingesta-rss-blocklist-y-deduplicacion-de-noticias — 3ª pasada (2026-05-19)

- **`GetExistingUrlsAsync` límite 2100 parámetros SQL IN** [`NewsRepository.cs:23`] — EF Core traduce `.Contains()` en IN-clause; SQL Server limita ~2100 parámetros. Con >100 FIBRAs activas podría alcanzarse. Fix: chunking del array antes del query.
- **`FetchAsync` traga `OperationCanceledException`** [`GoogleNewsRssClient.cs:17`] — `catch (Exception)` captura cancelación. Alineado con patrón CancellationToken.None ya deferido. Resolver junto con graceful shutdown global.
- **Rate-limit/bloqueo Google News silencioso** [`NewsPipelineJob.cs`] — Si Google bloquea la IP o responde 429, todos los FetchAsync devuelven `[]` sin que el job lo distinga de "sin noticias nuevas". Saved=0/errors=0 no dispara alerta. Considerar métricas por query en Epic 5.
- **`[DisableConcurrentExecution]` ausente en `NewsPipelineJob`** [`NewsPipelineJob.cs`] — Hangfire puede solapar ejecuciones si una tarda más de 1h. El unique index en URL absorbe duplicados con `errors++` espurios. Agregar el atributo junto con la próxima modificación del job.
- **Test del pipeline no cubre general queries** [`NewsPipelineJobTests.cs`] — `FakeRssClient` retorna el mismo set para cualquier query; una regresión que elimine el `foreach (GeneralQueries)` no sería detectada. Agregar test explícito cuando se extienda el job.

## Deferred from: code review of 4-2-asociacion-de-noticias-con-fibras-y-display-en-home-y-ficha (2026-05-19)

- **`GetLatestForFibraAsync` NullReferenceException teórico** [`NewsRepository.cs:58-65`] — EF Core genera INNER JOIN; filas huérfanas son imposibles con FK+cascade. Solo relevante ante corrupción directa de BD.
- **AC2 sin test de integración** [`NewsEndpoints.cs`] — `Associate_NoMatchReturnsEmpty` cubre la lógica; falta test end-to-end que verifique artículo sin asociación aparece en `/api/v1/news` pero no en `/api/v1/news/fibras/{id}`.
- **`JSON.stringify(error)` en mensajes de throw** [`newsApi.ts`, `fibraNewsApi.ts`] — Patrón heredado de `fibrasApi.ts`. No llega al usuario final pero sería mejor serializar solo el status o el `domainCode`.

## Deferred from: code review of 4-2-asociacion-de-noticias-con-fibras-y-display-en-home-y-ficha — 2ª pasada (2026-05-19)

- **DbContext en estado sucio tras `SaveChangesAsync` fallido** [`NewsRepository.cs:AddWithLinksAsync`] — Tras una `DbUpdateException` (ej. URL duplicada), las entidades en estado `Added` no se desvinculan del tracker. El siguiente `SaveChangesAsync` del mismo scope intenta persistir AMBAS entidades (la fallida + la nueva), repitiendo el error en cascada para el resto del batch. Fix: detach entidades en error tras la excepción, o usar `IDbContextFactory` con un contexto por artículo. Pre-existing del AddAsync de story 4.1.
- **Variante de nombre que normaliza a ≤2 chars matchea cualquier token del mismo tamaño** [`NewsAssociator.cs:MatchesVariant`] — Si una FIBRA tiene una variante de nombre que `NormalizeTitle` reduce a 1-2 caracteres (datos corruptos o abreviaciones), matcheará casi cualquier artículo. Agregar guard `normalizedVariant.Length >= 3` en `MatchesVariant`. Teórico con datos actuales (variantes son frases multi-word).

## Deferred from: code review of 2-5-home-topmovers-tabla-y-ganadores-perdedores (2026-05-19)

- **`dailyChangePct = 0` excluido silenciosamente de GainersLosers** [`movers-logic.ts:39,44`] — El filtro `> 0` / `< 0` excluye valores exactamente cero sin indicación al usuario. Comportamiento no especificado en los AC; abordar si el negocio lo requiere.
- **Doble llamada a `numOf` en comparador de `getTopMovers`** [`movers-logic.ts:24-26`] — Micro-optimización: `numOf` se llama dos veces por elemento por comparación. Refactorizar a variable local si el corpus de snapshots crece.
- **`formatVolume`: rango [999_500, 1_000_000) muestra "1000K"** [`movers-logic.ts:15`] — Edge case de formateo: `(999_500 / 1_000).toFixed(0) = "1000"` → "1000K". Sin impacto con volúmenes actuales de FIBRAs.
- **`TopMovers` sin empty state cuando `snapshots = []`** [`TopMovers.tsx`] — Si la API devuelve array vacío y no hay error, el componente renderiza un contenedor vacío sin mensaje. Inconsistente con `GainersLosers` que sí tiene empty state.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual (2026-05-19)

- **Inyección de prompt via `title`/`snippet`** [`AnthropicAiSummaryService.cs:26-33`] — Inputs RSS interpolados directamente en el prompt sin sanitizar saltos de línea. Fuente RSS pre-filtrada; surface de ataque baja en MVP. Sanitizar con `ReplaceLineEndings(" ")` cuando el corpus de fuentes se amplíe.
- **TOCTOU modo Off→Manual entre check y llamada a Anthropic** [`AiModeEndpoints.cs:69,84`] — Ventana temporal muy pequeña para operación admin manual. Documentar como limitación conocida o releer el modo dentro del try si el problema emerge en producción.
- **HTTP 429 tratado igual que 500 sin retry/backoff** [`AnthropicAiSummaryService.cs:53`] — Sin distinción de códigos de error Anthropic. Implementar retry con `Retry-After` header cuando se active uso intensivo de la API.
- **Artículos `Pending` visibles en endpoints públicos** [`NewsRepository.cs`] — Comportamiento pre-existente desde historia 4.1. En modo Manual los artículos quedan en `Pending` hasta que el admin dispara el resumen; el fallback `aiSummary ?? snippet` los muestra correctamente pero sin resumen. Evaluar si se debe ocultar `Pending` en futuras historias del módulo AI.
- **`PreviousMode = null` en rama de creación de `AiModeConfig`** [`AiModeRepository.cs:27-34`] — Rama inalcanzable en producción (seed EF garantiza fila Id=1). Completar el objeto si se agrega un test para esa rama.
- **Sin tests para endpoint `POST /{id}/ai-summary`** [`AiModeEndpoints.cs`] — Task 9 del spec solo exige tests del pipeline. Los ACs 2 y 3 del endpoint de trigger manual no tienen cobertura. Agregar tests de integración en historia 5.x que extienda el módulo AI.
- **`UpdateSummaryAsync` silencioso con 0 filas afectadas** [`NewsRepository.cs:54-59`] — Si el artículo fue eliminado entre `GetByIdAsync` y `ExecuteUpdateAsync`, la operación retorna 0 sin error. Probabilidad muy baja en operación admin-only; verificar filas afectadas si se habilita borrado de artículos.
- **Token expirado sin refresh en `aiModeApi.ts`** [`aiModeApi.ts`] — Patrón heredado de `newsApi.ts`. Implementar refresh flow cuando se añada gestión de sesión al Ops SPA.
- **Cambio de modo a mitad de ejecución del pipeline** [`NewsPipelineJob.cs:25`] — Por diseño según spec (aplica en el siguiente ciclo). Documentar en las notas de operación del pipeline si la duración del job crece significativamente con más FIBRAs.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 2ª pasada (2026-05-19)

- **`opsAccessTokenStorageKey` y `getAuthHeaders()` duplicados** [`aiModeApi.ts`, `newsApi.ts`] — Patrón copy-paste pre-existente en todo el Ops SPA. Refactorizar a un módulo de auth compartido cuando se implemente gestión de sesión/refresh.
- **`SetAiModeRequest.Mode` acepta strings numéricos vía `Enum.TryParse`** [`AiModeEndpoints.cs`] — `"0"` o `"1"` son válidos como Mode (se parsean como `AiMode.Off`/`Manual`). Endpoint admin-only, impacto bajo; la respuesta serializa el enum como "Off"/"Manual" correctamente.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 4ª pasada (2026-05-19)

- **Anthropic error body descartado en fallo de `EnsureSuccessStatusCode`** [`AnthropicAiSummaryService.cs`] — El operador solo ve el código HTTP (401, 429, 500) en el log, no el mensaje de Anthropic. Leer y loguear el body del error antes de lanzar en una próxima mejora de observabilidad.
- **`GetConfigAsync` fallback in-memory con `UpdatedAt` variable** [`AiModeRepository.cs:GetConfigAsync`] — Si la fila seed no existiera, dos GETs consecutivos devolverían timestamps distintos. Inalcanzable en producción; completar solo si se añade un test explícito para esa rama.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 5ª pasada (2026-05-19)

- **`UpdatedAt` calculado antes de `SaveChangesAsync`** [`AiModeRepository.cs:25`] — En caso de retry el timestamp del primer intento se pierde; el registro de auditoría refleja el momento del intento, no del commit. Menor impacto en auditoría; asignar `DateTimeOffset.UtcNow` justo antes del `SaveChangesAsync` si la precisión importa.
- **Retry de insert concurrente puede relanzar en réplica de lectura** [`AiModeRepository.cs:44-48`] — `FindAsync` post-`ChangeTracker.Clear()` puede retornar null si la réplica de lectura no ha replicado el insert ganador, relanzando `DbUpdateException` como 500. Improbable con SQL Server single-node; resolver si se añade read replica.
- **Actor fallback `"unknown"` persiste sin log de advertencia** [`AiModeEndpoints.cs:46-49`] — La cadena Name→Email→NameIdentifier→"unknown" es funcional, pero `"unknown"` en `updated_by` no identifica al actor real. Añadir `LogWarning` cuando se cae en "unknown" para detectar JWTs con claims ausentes.
- **PK singleton `1` hardcodeado en 5 lugares** [`AiModeRepository.cs`, `AiModeConfig.cs`, `AiModeConfigConfiguration.cs`] — Extraer `public const int SingletonId = 1` en `AiModeConfig` y referenciar desde repositorio y configuración EF en próxima iteración del módulo AI.
- **Modo stale en ventana de refetch (~200ms) tras Off→Manual** [`AiModeSection.tsx:104`] — El botón de trigger queda disabled brevemente después de guardar cambio Off→Manual porque `currentMode` lee del caché obsoleto. Resolver con `queryClient.setQueryData` optimista en `saveMutation.onSuccess`.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 6ª pasada (2026-05-20)

- **Gemini API key en query param `?key=` visible en logs HTTP/telemetría** [`GeminiAiSummaryService.cs:42`] — Usar header `x-goog-api-key` sería mejor para log hygiene; el spec Dev Notes especifica la URL con `?key={apiKey}` siguiendo la documentación oficial de Gemini. Migrar si se añade observabilidad centralizada de HTTP.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 7ª pasada (2026-05-20)

- **`ILoggerFactory.CreateLogger` por request en lugar de `ILogger<T>` tipado** [`AiModeEndpoints.cs:71`] — Allocación menor en endpoint admin-only de bajo volumen. Refactorizar a inyección tipada si el endpoint crece.
- **Fallback `AiMode.Off` ante BD caída puede dejar artículos del período de falla como `Processed` permanentemente** [`NewsPipelineJob.cs:34-42`] — En modo Manual, artículos ingestados durante falla de BD quedan `Processed` sin AI; el guard de idempotencia bloquea re-proceso posterior. Trade-off del diseño de fallback; el admin puede hacer trigger manual de artículos específicos.
- **Safety-block de Gemini (HTTP 200 sin candidatos) devuelve 502 "proveedor no disponible"** [`GeminiAiSummaryService.cs:67-71`, `AiModeEndpoints.cs:125-127`] — El mensaje 502 implica falla de red cuando la causa real es bloqueo por política de contenido. El log de error contiene la `InvalidOperationException` con detalle. Mejorar al añadir observabilidad centralizada.
- **`modeQuery.data?.mode as 'Off' | 'Manual'` sin validación en runtime** [`AiModeSection.tsx:30`] — TypeScript `as` no valida en runtime; un tercer modo del backend deshabilitaría el panel de trigger sin mensaje claro. Validar al extender el enum.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 8ª pasada (2026-05-20)

- **Race condition: dos requests concurrentes de AdminOps sobre el mismo artículo `Pending` pasan el guard de idempotencia y llaman a Gemini dos veces** [`AiModeEndpoints.cs:85-100`] — Endpoint admin-only de muy bajo volumen; worst case = cuota Gemini desperdiciada + segundo summary sobreescribe al primero de forma inocua. Resolver con `ExecuteUpdateAsync WHERE Status != Processed` atómico si el uso aumenta.
- **`AiSummary` se persiste sin validar longitud máxima** [`GeminiAiSummaryService.cs:78`, `NewsRepository.cs:58`] — Columna `nvarchar(2048)`; 256 output tokens ≈ 800-1000 chars, bien bajo el límite, pero no hay truncación explícita. Si se excediera, `DbUpdateException` sería capturada como fallo de proveedor con mensaje "proveedor no disponible" que es engañoso. Agregar `text.Truncate(2048)` o aumentar límite de columna si se amplía `maxOutputTokens`.

## Deferred from: code review of 2-6-home-reorganizacion-y-tabla-universo-fibras (2026-05-21)

- **Sin roles ARIA de tabla en `FibraUniverseTable`** [`FibraUniverseTable.tsx:52-210`] — El grid CSS no lleva `role="table"/"row"/"columnheader"/"cell"`. Pre-existing pattern en GainersLosers y TopMovers (mismo enfoque visual). Revisar en historia de mejoras de accesibilidad WCAG si el score de audit lo requiere.

## Deferred from: code review of 4-5-1-scraping-imagen-ogimage-y-fallback-visual (2026-05-20)

- **Regex backtracking teórico en OgImageScraper** [`OgImageScraper.cs`] — `[^>]*` bounded por `>` limita el backtracking; GeneratedRegex de .NET optimiza el patrón. Riesgo bajo en producción. Evaluar `RegexOptions.NonBacktracking` si se detecta CPU spikes en métricas.
- **Scraping og:image secuencial bloquea pipeline ~5s por artículo nuevo** [`NewsPipelineJob.cs`] — Intencional per Dev Notes (rate limiting implícito). Revisar si el volumen de artículos nuevos crece significativamente; considerar throttled `Task.WhenAll` en ese momento.
- **Sin retry/circuit-breaker en OgImageScraper** [`ApiServiceExtensions.cs`] — Dominio caído consume 5s por artículo en cada run. Agregar Polly circuit-breaker por host en Epic 5 junto con la infraestructura de resiliencia global.
- **Regex no cubre atributos HTML5 sin comillas en `<meta>`** [`OgImageScraper.cs`] — Atributos unquoted son válidos en HTML5 pero infrecuentes. No requerido por spec; revisar si tasa de `imageUrl=null` en producción sugiere miss rate alto.
- **Race condition entre ejecuciones concurrentes del pipeline** [`NewsPipelineJob.cs`] — Pre-existing de historia 4.1 (`[DisableConcurrentExecution]` ausente). Dos runs simultáneos pueden scraper la misma URL dos veces e intentar insertar el mismo artículo.

## Deferred from: code review of 4-5-3-pagina-lectora-interna-noticias — Pasada 1 (2026-05-21)

- **AC 4.5.3/3 — logo de FIBRA en `NoticiaPage`** [`newsApi.ts`, `NoticiaPage.tsx`] — `NewsArticleDto` no lleva asociación de FIBRA; el fallback de imagen en la página lectora solo puede llegar a imagen sectorial. Requiere extender el DTO con datos de FIBRA o un endpoint auxiliar. Alineado con decisión de AC2 en historia 4.5.1.
- **`GET /api/v1/news/{id}` retorna 404 sin `ProblemDetails`** [`NewsEndpoints.cs:39`] — Inconsistencia con convención de la API (otros endpoints retornan `ProblemDetails` en error). Impacto nulo en comportamiento funcional; normalizar en próxima iteración del módulo noticias.
- **`staleTime: 10 min` cachea el sentinel `null` (404)** [`NoticiaPage.tsx:17`] — Un artículo inexistente queda cacheado como "no encontrado" durante 10 minutos. Aceptable dado que los IDs son GUIDs y artículos no se crean retroactivamente en el mismo ID.

## Deferred from: code review of 4-5-1-scraping-imagen-ogimage-y-fallback-visual — Pasada 3 (2026-05-21)

- **DNS rebinding (TOCTOU) entre `IsAllowedHostAsync` y la HTTP request real** [`OgImageScraper.cs:15`] — La resolución DNS ocurre antes del request; con TTL corto un atacante puede devolver IP pública para el check e IP privada para la conexión. Fix requiere `HttpMessageHandler` custom con IP pinning. Limitación arquitectural aceptada.
- **URL de `og:image` extraída no validada contra SSRF allowlist** [`OgImageScraper.cs:35`] — Un publisher malicioso puede poner `og:image = http://169.254.169.254/...`; la URL se almacena y se sirve como `<img src>` al browser (no fetch server-side). Riesgo browser-side únicamente con stack actual.
- **HTTP 416 `Range Not Satisfiable` retorna `null` silencioso** [`OgImageScraper.cs:24`] — Páginas HTML pequeñas que retornan 416 hacen que el scraper abandone silenciosamente en lugar de reintentar sin el header Range. Edge case infrecuente.
- **IPv6 ULA `fc00::/7` no bloqueado en `IsAllowedIp`** [`OgImageScraper.cs:81`] — Unique Local Addresses son el equivalente IPv6 de RFC 1918; no bloqueados en la validación actual. Bajo riesgo con infraestructura actual; cubrir junto con la revisión global de SSRF si se amplía el scraping.

## Deferred from: code review of 4-5-1-scraping-imagen-ogimage-y-fallback-visual — Pasada 2 (2026-05-20)

- **AC2: color de identidad visual de FIBRA no implementado en fallback de imagen** [`news-image-fallback.ts`, `NoticiasSection.tsx`] — `getArticleImageUrl` usa `fibra?.logoUrl` pero no un `brandColor`. Si la FIBRA no tiene logo, cae a sector asset. Requiere nuevo campo `brandColor` en entidad `Fibra`, migración, seed y frontend. Diferido para historia futura del módulo noticias.
- **`AllowAutoRedirect=false` silencia og:image de fuentes con redirect HTTP→HTTPS** [`ApiServiceExtensions.cs`] — Trade-off de seguridad aceptado conscientemente; el fix SSRF previo eligió esta opción. Revisitar si miss rate en producción es alto.
- **`ResponseContentRead` puede buffear respuesta completa si servidor ignora `Range` header** [`OgImageScraper.cs`] — Intencional per Dev Notes; el timeout de 5s acota la exposición total. Evaluar si hay memory pressure en métricas de producción.
- **Dominios redirect de Google (`goo.gl`, `googleusercontent.com`) no cubiertos por filtro `news.google.com`** [`GoogleNewsRssClient.cs`] — Escenario especulativo; si Google emite GUIDs con short-links, el scraper intentaría extraer og:image del landing de Google en vez del artículo real. Revisar si aparece en logs como `imageUrl=null` con origen `goo.gl`.
- **Charset decoding ambiguo en respuestas Range sin header `Content-Type; charset=`** [`OgImageScraper.cs`] — ReadAsStringAsync cae a ISO-8859-1 per HTTP spec si no hay charset. En práctica las URLs de og:image son ASCII-safe; solo afectaría titles u otras partes del HTML, no al scraping.
- **`ExtractLink` retorna `string.Empty` como sentinela de fallo en vez de `null`** [`GoogleNewsRssClient.cs`] — Inconsistente con convención `string?`; el caller actual maneja `string.Empty` correctamente. Normalizar en refactor de GoogleNewsRssClient.
- **Sectores nuevos (Educativo, Autoalmacenaje, Hipotecario) sin asset en `SECTOR_IMAGES`** [`news-image-fallback.ts`] — Caen a `otro.jpg`; fuera del scope de AC3 (7 sectores definidos). Agregar assets sectoriales en historia futura si el miss rate visual es relevante.

## Deferred from: code review of 4-6-noticias-display-fixes-y-aimode-on (2026-05-21)

- **SSRF: destino del redirect no validado en OgImageScraper** [`ApiServiceExtensions.cs`] — `AllowAutoRedirect=true` permite que un redirect externo apunte a endpoints internos; `IsAllowedHostAsync` valida solo la URL de origen. Riesgo bajo por diseño (dev notes lo acepta explícitamente). Revisar junto con SSRF global si se amplía el scraping.
- **CancellationToken swallowed en bloque AI del pipeline** [`NewsPipelineJob.cs`] — Si el job Hangfire se cancela durante la llamada a Gemini, `OperationCanceledException` es capturada por el `catch(Exception)` interno y el artículo queda como `Partial` permanentemente (deduplicador lo excluye en la próxima corrida). Recuperable via regeneración manual con el endpoint de trigger.

## Deferred from: code review of 3-4-pipeline-historico-distribuciones — 2ª pasada (2026-05-21)

- **`WithHistoryStartDate(2020-01-01)` estático vs ventana dinámica `AddYears(-5)`** [`ApiServiceExtensions.cs`] — No rompe en 2026 (filtro client-side absorbe el desfase). Actualizar si el piso de 2020 queda dentro de la ventana de 5 años solicitada.
- **Conversión `ToDateTimeUtc()` → `DateOnly` puede desplazar un día** [`YahooFinanceClient.cs:GetDividendHistoryAsync`] — FIBRAs mexicanas no tienen dividendos cerca de medianoche UTC en práctica; revisar si aparecen desfases de fecha en logs de producción.
- **`YahooQuotesHistory` singleton no implementa `IDisposable`** [`ApiServiceExtensions.cs`] — Patrón pre-existente del `YahooQuotes` singleton; abordar junto con la limpieza de singletons HTTP en Épica 5.
- **Dos dividendos el mismo día para la misma FIBRA descarta el segundo** [`DistributionConfiguration.cs`] — Diseño intencional del índice único `(FibraId, PaymentDate)`; si Yahoo reporta ajustes intradiarios, el segundo queda silenciosamente descartado.

## Deferred from: code review of 3-5-daily-snapshot-historico-y-limpieza-price-snapshots (2026-05-21)

- **`SaveChangesAsync` por candle en backfill histórico** [`DailySnapshotHistoricalJob.cs`] — Decisión de diseño intencional siguiendo el patrón de `DistributionPipelineJob`. Para un job de backfill de única ejecución es aceptable; evaluar batching si el catálogo crece significativamente.
- **`RecurringJob.AddOrUpdate<DistributionPipelineJob>` fue omitido en story 3.4 y añadido aquí** [`Program.cs`] — Omisión pre-existente. El comportamiento final es correcto; sin acción requerida.
- **`DailySnapshotHistoricalJob` usa `DateTime.UtcNow` directo en lugar de `ITimeService`** [`DailySnapshotHistoricalJob.cs:19`] — El spec Dev Notes especifica explícitamente `DateTime.UtcNow.AddYears(-5)`. Violación menor de convención de tiempo para job de backfill único.
- **Endpoint manual `/daily-snapshot-historical/run` sin guard contra re-enqueueing** [`OpsMarketEndpoints.cs`] — Múltiples POSTs acumulan jobs en la cola Hangfire; `[DisableConcurrentExecution(0)]` los serializa. Para un job de backfill manual de uso infrecuente es aceptable; agregar check via `IMonitoringApi` en Epic 5 si el abuso operacional se detecta.

## Deferred from: code review of 3-4-pipeline-historico-distribuciones (2026-05-21)

- **`CancellationToken.None` en schedule de Hangfire** [`Program.cs`] — Patrón pre-existente idéntico en `MarketPipelineJob` y `NewsPipelineJob`. Sin cancelación graceful en shutdown del host. Abordar junto con los otros jobs en Épica 5.
- **`historyClient is null` devuelve `[]` silenciosamente** [`YahooFinanceClient.cs`] — Comportamiento intencional para test isolation, documentado en dev notes. Agregar `LogWarning` si se detecta null en producción (implicaría error en el DI).
- **Sin rate limiting entre llamadas a Yahoo Finance** [`DistributionPipelineJob.cs`] — Pre-existente en toda la integración YahooQuotesApi. Un bloqueo de IP es indistinguible de "sin dividendos". Mitigar en Épica 5 con métricas de pipeline y alertas por inserted=0.
- **`Take(60)` trunca "Ver historial completo" en teoría** [`MarketEndpoints.cs`] — Sin impacto real con catálogo actual (FIBRAs trimestrales: max ~20 registros / 5 años). Agregar paginación si se añaden FIBRAs de pago mensual.
- **`FakeDistYahooClient` con dos constructores de lógica dividida** [`DistributionPipelineJobTests.cs`] — No puede combinar múltiples entries + throwForTicker. Refactorizar si se añaden tests más complejos.
- **`YahooQuotesHistory.Inner` expone tipo interno** [`YahooQuotesHistory.cs`] — Diseño documentado en dev notes; wrapper creado para resolver colisión de DI, no para encapsulamiento. Evaluar encapsular en historia de refactor si el módulo crece.
- **Sin test para `CapturedAt`** [`DistributionPipelineJobTests.cs`] — Campo de auditoría sin cobertura. Agregar en próxima iteración del módulo market.
- **Null/empty `YahooTicker` sin guardia explícita** [`DistributionPipelineJob.cs`] — Pre-existente en todos los pipelines; catálogo controlado. Agregar guard defensivo si se abre la gestión del catálogo en Épica 5.
- **React row key `d.date` puede colisionar** [`DistribucionesSection.tsx`] — El UIX garantiza unicidad en DB; sin impacto real. Usar `${d.date}-${i}` si se añade soporte para múltiples distribuciones por fecha.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — Hardening Pass (2026-05-21)

- **Token de AdminOps en `sessionStorage` expuesto a XSS** [`src/Web/Ops/src/api/opsAuth.ts`] — Decisión arquitectural aceptada para SPA; el Ops SPA tiene surface de ataque reducida (acceso restringido a AdminOps). Revisar si se migra a HttpOnly cookie en Epic 5 junto con gestión de sesión.
- **`getStoredOpsAccessToken` lee de `localStorage` sin que `storeOpsAccessToken` escriba ahí** [`src/Web/Ops/src/api/opsAuth.ts`] — Compatibilidad intencional con versiones anteriores. Evaluar si se simplifica solo a sessionStorage cuando se certifique que no hay tokens legacy en producción.
- **Bootstrap `catch` reutiliza token previo como indicador de sesión válida cuando `refreshOpsSession` lanza excepción de red** [`src/Web/Ops/src/components/OpsLoginGate.tsx`] — El primer 401 del backend recupera el estado vía `OPS_AUTH_REQUIRED_EVENT`. Mejorar con timeout y fallback explícito a `anonymous` en Epic 5.
- **Multi-tab: `clearOpsAccessToken` en Tab A no propaga estado React en Tab B** [`src/Web/Ops/src/api/opsAuth.ts`] — Limitación conocida de auth por storage; tool de uso single-admin en MVP. Implementar `storage` event listener si el uso multi-tab se vuelve necesario.
- **Estado `'checking'` sin timeout en `refreshOpsSession`** [`src/Web/Ops/src/api/authApi.ts`] — Timeout del browser como fallback; aceptable para tool admin de bajo volumen. Agregar `AbortController` con timeout configurable en Epic 5 junto con infraestructura de resiliencia global.
