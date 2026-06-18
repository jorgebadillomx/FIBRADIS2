---
title: 'SEO #18/#24 — IndexNow y limpieza dead code OG en FibraProfileMetadataMiddleware'
type: 'feature+refactor'
created: '2026-06-18'
status: 'done'
baseline_commit: ''
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**#24 — Dead code OG images:** `FibraProfileMetadataMiddleware` tiene un overload `BuildMetaBlock(Fibra, string, string)` (línea 251) que nunca es llamado — la ruta activa en línea 187 siempre usa `BuildMetaBlock(SeoMetadata, ...)` que ya lee `seoMetadata.OgImageUrl` (asignado con el URL per-ticker en línea 175). El overload muerto arrastra ~100 líneas: seis miembros privados (`MaxDescriptionLength`, `JsonLdOptions`, `MarkdownSyntaxRegex`, `WhitespaceRunRegex`, `BuildDescription`, `Sanitize`, `TruncateAtWordBoundary`). Eliminar todo y el `using System.Text.Json` que queda huérfano.

**#18 — IndexNow:** Cuando se publica una noticia o se actualizan precios de FIBRAs, notificar proactivamente a los motores de búsqueda (Bing/Yandex/Seznam) vía el protocolo IndexNow. La clave se guarda en `appsettings.json`; el archivo de verificación se sirve en `/indexnow.txt`. El ping es fire-and-forget: nunca debe fallar el job.

## Boundaries & Constraints

**Always:**
- `PingAsync` nunca lanza excepción — captura internamente y registra `LogWarning`.
- El ping se descarta (`_ =`) si la clave `Seo:IndexNowKey` está vacía en config.
- `/indexnow.txt` retorna 404 si la clave no está configurada.
- IndexNow se engancha en `NewsPipelineJob` solo si `article.Slug` es no-nulo tras el save.
- En `MarketPipelineJob` el ping solo ocurre cuando `!batchFailed && processed > 0`.
- Las URLs en el payload de IndexNow son absolutas: `{baseUrl}/noticias/{slug}` y `{baseUrl}/fibras/{FibraSlug.Build(fullName, ticker)}`.
- `keyLocation` en el body de IndexNow apunta a `{baseUrl}/indexnow.txt`.

**Never:**
- No agregar `IndexNowKey` a `OperationalConfig` ni crear migraciones EF para esto.
- No bloquear el hilo del job esperando respuesta de IndexNow — siempre `_ = PingAsync(...)`.
- No eliminar `TitleTagRegex()` (línea 46-47 de `FibraProfileMetadataMiddleware.cs`) — es activo (línea 186 lo usa).
- No eliminar `using System.Text.RegularExpressions` — sigue necesario para `TitleTagRegex()`.

</frozen-after-approval>

## Code Map

**#24 — miembros a eliminar en `FibraProfileMetadataMiddleware.cs`:**
- `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs:3` — `using System.Text.Json;` (huérfano tras borrar dead code)
- `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs:31` — `private const int MaxDescriptionLength = 155;`
- `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs:36-39` — `private static readonly JsonSerializerOptions JsonLdOptions`
- `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs:49-50` — `[GeneratedRegex(@"[#|*>]+")] private static partial Regex MarkdownSyntaxRegex();`
- `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs:52-53` — `[GeneratedRegex(@"\s+")] private static partial Regex WhitespaceRunRegex();`
- `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs:251-319` — overload `BuildMetaBlock(Fibra fibra, string canonicalSlug, string baseUrl)` completo
- `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs:326-358` — `BuildDescription`, `Sanitize`, `TruncateAtWordBoundary`

**#18 — archivos nuevos:**
- `src/Server/Application/Seo/IIndexNowService.cs` — interfaz
- `src/Server/Infrastructure/Seo/IndexNowService.cs` — implementación

**#18 — archivos modificados:**
- `src/Server/Api/appsettings.json` — agregar sección `"Seo": { "IndexNowKey": "" }`
- `src/Server/Api/Endpoints/Public/SeoEndpoints.cs` — nuevo endpoint `/indexnow.txt`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registrar typed HttpClient
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — inyectar `IIndexNowService` + `IConfiguration`; hook post-save
- `src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs` — inyectar `IIndexNowService` + `IConfiguration`; hook post-batch

## Tasks & Acceptance

**Execution:**

**#24 — Dead code cleanup:**

- [ ] `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs` -- Eliminar `using System.Text.Json;` (línea 3) + `MaxDescriptionLength` (línea 31) + `JsonLdOptions` (líneas 36-39) + `MarkdownSyntaxRegex` atributo+declaración (líneas 49-50) + `WhitespaceRunRegex` atributo+declaración (líneas 52-53) + el overload completo `BuildMetaBlock(Fibra fibra, string canonicalSlug, string baseUrl)` (líneas 251-319) + `BuildDescription(Fibra fibra)` (líneas 326-336) + `Sanitize(string text)` (líneas 340-344) + `TruncateAtWordBoundary(string text)` (líneas 346-358). Verificar que el build compile sin errores antes de continuar. -- el código activo en línea 187 ya usa `BuildMetaBlock(SeoMetadata, ...)` que correctamente lee `seoMetadata.OgImageUrl` (URL per-ticker); el overload eliminado nunca fue llamado

**#18 — IIndexNowService:**

- [ ] `src/Server/Application/Seo/IIndexNowService.cs` -- Crear interfaz:
  ```csharp
  namespace Application.Seo;
  public interface IIndexNowService
  {
      Task PingAsync(IEnumerable<string> urls, CancellationToken ct = default);
  }
  ```
  -- contrato mínimo; la implementación decide si el ping procede según la clave configurada

- [ ] `src/Server/Infrastructure/Seo/IndexNowService.cs` -- Crear implementación con constructor `(HttpClient http, IConfiguration config, ILogger<IndexNowService> logger)`:
  - Leer `config["Seo:IndexNowKey"]`; si vacío, `logger.LogDebug` + return
  - Leer `config["App:BaseUrl"]!.TrimEnd('/')` para `host` y `keyLocation`
  - Extraer `host` del baseUrl como `new Uri(baseUrl).Host`
  - POST `https://api.indexnow.org/indexnow` con Content-Type `application/json` y body:
    ```json
    { "host": "{host}", "key": "{key}", "keyLocation": "{baseUrl}/indexnow.txt", "urlList": [...] }
    ```
  - Si `!response.IsSuccessStatusCode`, `logger.LogWarning("IndexNow ping failed: {Status}", response.StatusCode)`
  - Si excepción, `logger.LogWarning(ex, "IndexNow ping exception")` — nunca relanzar
  -- fire-and-forget seguro: absorbe todos los errores

**#18 — Configuración y endpoint:**

- [ ] `src/Server/Api/appsettings.json` -- Agregar al JSON raíz (después del bloque `"App"`): `"Seo": { "IndexNowKey": "" }` -- placeholder vacío; el operador pega aquí la clave real generada en indexnow.org

- [ ] `src/Server/Api/Endpoints/Public/SeoEndpoints.cs` -- Agregar antes del endpoint de `robots.txt`:
  ```csharp
  app.MapMethods("/indexnow.txt", GetAndHead, async (
      IConfiguration config,
      CancellationToken ct) =>
  {
      var key = config["Seo:IndexNowKey"];
      return string.IsNullOrWhiteSpace(key)
          ? Results.NotFound()
          : Results.Content(key, "text/plain; charset=utf-8");
  })
  .AllowAnonymous()
  .ExcludeFromDescription();
  ```
  -- IndexNow verifica propiedad del dominio descargando este archivo; si la clave coincide con el body del POST, el motor acepta los pings

- [ ] `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` -- Agregar cerca de los otros `AddScoped` de Infrastructure/Seo:
  ```csharp
  builder.Services.AddHttpClient<IIndexNowService, IndexNowService>(c =>
      c.Timeout = TimeSpan.FromSeconds(10));
  ```
  -- typed HttpClient: DI inyecta `HttpClient` directamente en `IndexNowService`

**#18 — Hooks en jobs:**

- [ ] `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` -- (1) Agregar `IIndexNowService indexNowService` e `IConfiguration config` al constructor (primary constructor, después de los parámetros existentes). (2) Agregar `using Application.Seo;` en los usings. (3) Después de `saved++;` (línea 237), antes del cierre del `try` del item:
  ```csharp
  if (!string.IsNullOrWhiteSpace(article.Slug))
  {
      var baseUrl = config["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
      _ = indexNowService.PingAsync([$"{baseUrl}/noticias/{article.Slug}"], CancellationToken.None);
  }
  ```
  -- notifica al motor de búsqueda inmediatamente tras persistir el artículo; `CancellationToken.None` porque es fire-and-forget independiente del CT del job

- [ ] `src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs` -- (1) Agregar `IIndexNowService indexNowService` e `IConfiguration config` al constructor. (2) Agregar `using Application.Catalog; using Application.Seo;` en los usings. (3) Dentro del bloque `if (!batchFailed && processed > 0)` (línea 187), después del bloque `try/catch` de `DeleteOldPriceSnapshotsAsync`, agregar:
  ```csharp
  var baseUrl = config["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
  var fibraUrls = fibras
      .Select(f => $"{baseUrl}/fibras/{FibraSlug.Build(f.FullName, f.Ticker)}")
      .ToList();
  _ = indexNowService.PingAsync(fibraUrls, CancellationToken.None);
  ```
  -- los precios actualizados cambian el contenido real de /fibras/{slug}; IndexNow acelera re-crawl de ~20 páginas simultáneamente

**Cierre:**

- [ ] `ACTION-PLAN.md` -- Marcar #18 y #24 como ✅ con nota de implementación

**Acceptance Criteria:**

- Dado que `Seo:IndexNowKey` está vacío en config, cuando se hace GET `/indexnow.txt`, entonces la respuesta es HTTP 404
- Dado que `Seo:IndexNowKey` tiene valor "abc123", cuando se hace GET `/indexnow.txt`, entonces la respuesta es HTTP 200 con body `abc123` y Content-Type `text/plain`
- Dado que `FibraProfileMetadataMiddleware.cs` compila, cuando se ejecuta `dotnet build`, entonces sin errores ni warnings CS0168/CS8019 relacionados con dead code o usings huérfanos
- Dado que el job de noticias guarda un artículo con Slug no-nulo, cuando completa `AddWithLinksAsync`, entonces se lanza (fire-and-forget) `PingAsync` con la URL absoluta de la noticia
- Dado que el job de market procesa `processed > 0` fibras con lote exitoso, cuando finaliza el ciclo de snapshots, entonces se lanza (fire-and-forget) `PingAsync` con las URLs de todos los perfiles de FIBRA activos

## Design Notes

**¿Por qué `appsettings.json` y no `OperationalConfig`?** La clave IndexNow se genera una vez, es constante (no requiere edición por el operador en tiempo de ejecución), y agregarla a `OperationalConfig` requeriría una migración EF + endpoint Ops + `UpdateAsync` adicional. `IConfiguration` es el lugar correcto para valores de infra que no cambian en producción.

**¿Por qué `api.indexnow.org`?** Es el endpoint genérico que distribuye a Bing, Yandex, Seznam y Naver. Submitir a un motor automáticamente notifica a todos los participantes del esquema.

**¿Por qué fire-and-forget sin `await`?** IndexNow es una optimización de velocidad de indexación, no un paso crítico. Si falla, el motor simplemente no re-crawlea inmediatamente — el crawl orgánico eventualmente ocurriría. Fallar el job de noticias (o de precios) por un error de red a api.indexnow.org sería desproporcionado.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` — 0 errores, confirmar que `MaxDescriptionLength`, `JsonLdOptions`, `BuildDescription`, `Sanitize`, `TruncateAtWordBoundary` ya no aparecen en ningún archivo
- `dotnet test tests/Unit/Infrastructure.Tests` — todos los tests existentes pasan sin cambios

**Manual checks:**
- `curl http://localhost:5000/indexnow.txt` con clave vacía → 404
- Pegar clave de prueba en appsettings.Development.json, reiniciar → 200 con la clave como body
- Ejecutar job de noticias en modo test → verificar en logs: `IndexNow ping` o ausencia de error

## Suggested Review Order

**Dead code cleanup (#24):**

- Punto de entrada activo — confirmar que línea 187 llama a `BuildMetaBlock(SeoMetadata, ...)` y nunca al overload Fibra.
  [`FibraProfileMetadataMiddleware.cs:187`](../../src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs#L187)

- Overload activo — usa `metadata.OgImageUrl` (per-ticker), no `$"{baseUrl}/og-image.png"`.
  [`FibraProfileMetadataMiddleware.cs:218`](../../src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs#L218)

**IndexNow (#18):**

- Interfaz mínima.
  [`IIndexNowService.cs`](../../src/Server/Application/Seo/IIndexNowService.cs)

- Implementación: key guard + POST + logging.
  [`IndexNowService.cs`](../../src/Server/Infrastructure/Seo/IndexNowService.cs)

- Endpoint de verificación de dominio.
  [`SeoEndpoints.cs`](../../src/Server/Api/Endpoints/Public/SeoEndpoints.cs)

- Hook en news — fire-and-forget tras `saved++`.
  [`NewsPipelineJob.cs`](../../src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs)

- Hook en market — bulk ping de fibras tras `processed > 0`.
  [`MarketPipelineJob.cs`](../../src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs)
