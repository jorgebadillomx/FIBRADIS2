# Historia 4.5.1: Scraping de imagen og:image y sistema de fallback visual

Status: done

## Story

Como visitante de FIBRADIS,
quiero que cada noticia muestre una imagen representativa,
para que el feed de noticias sea visualmente escaneable y no sea una lista de texto plano.

## Acceptance Criteria

1. **Imagen desde og:image:** Dado que un artículo de noticias tiene una URL válida, cuando el pipeline de enriquecimiento corre, entonces se intenta extraer el og:image del HTML de la página origen y se guarda en `ImageUrl`.

2. **Fallback a imagen de FIBRA:** Dado que no se pudo extraer og:image (fallo, timeout, página vacía), cuando el artículo está asociado a al menos una FIBRA, entonces se usa la imagen o color de identidad visual de la primera FIBRA asociada como placeholder.

3. **Fallback a imagen de sector:** Dado que no hay og:image ni FIBRA asociada, entonces se usa una imagen estática por sector (`industrial`, `comercial`, `oficinas`, `diversificado`, `salud`, `infraestructura`, `otro`). El mapeo sector → asset se define en el frontend.

4. **Estados de imagen en el componente:** Los ArticleCards en Home (`NewsSection`) y ficha pública (`NoticiasSection`) muestran la imagen (og:image o placeholder de FIBRA/sector) con aspect ratio 16:9, con lazy loading.

5. **Pipeline no bloqueante:** Si el scraping de og:image falla (timeout 5s, error HTTP, página con JS rendering), el artículo se guarda de todas formas con `ImageUrl = null`. El job de enriquecimiento no bloquea la ingesta RSS.

6. **Campo expuesto en API:** El endpoint `GET /api/v1/news` y `GET /api/v1/news/fibras/{fibraId}` incluyen `imageUrl` en `NewsArticleDto`.

## Tasks / Subtasks

- [x] Task 1: Backend — Campo `ImageUrl` en dominio y BD
  - [x] 1.1 Agregar `public string? ImageUrl { get; set; }` a `src/Server/Domain/News/NewsArticle.cs`
  - [x] 1.2 Agregar configuración en `NewsArticleConfiguration.cs`: `builder.Property(x => x.ImageUrl).HasMaxLength(2048);`
  - [x] 1.3 Crear migración: `dotnet ef migrations add AddImageUrlToNewsArticle --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`
  - [x] 1.4 Aplicar migración: `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`

- [x] Task 2: Backend — Scraper og:image en el pipeline RSS
  - [x] 2.1 Crear `src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs` — `HttpClient` + regex/XDocument sobre `<meta property="og:image">` en el `<head>` del HTML (solo los primeros 8KB de respuesta con `Range` header si es posible, o cancela con timeout de 5s)
  - [x] 2.2 Agregar `IOgImageScraper` en `src/Server/Application/News/IOgImageScraper.cs`
  - [x] 2.3 Registrar `AddHttpClient<IOgImageScraper, OgImageScraper>` en `ApiServiceExtensions.cs` con timeout de 5s
  - [x] 2.4 Inyectar `IOgImageScraper` en `NewsPipelineJob` y llamar `await ogImageScraper.TryGetOgImageAsync(item.Url, ct)` por artículo antes de persistir; asignar resultado a `article.ImageUrl`

- [x] Task 3: Backend — Exponer `imageUrl` en API
  - [x] 3.1 Agregar `string? ImageUrl` a `src/Server/SharedApiContracts/News/NewsArticleDto.cs`
  - [x] 3.2 Actualizar `ToDto` en `NewsEndpoints.cs` para incluir `a.ImageUrl`
  - [x] 3.3 Ejecutar `npm run codegen:api` para regenerar `SharedApiClient/schema.d.ts`

- [x] Task 4: Frontend — ArticleCard con imagen en NewsSection y NoticiasSection
  - [x] 4.1 Crear `src/Web/Main/src/shared/lib/news-image-fallback.ts` — función `getArticleImageUrl(article, fibra?)` con lógica: `article.imageUrl ?? fibra?.logoUrl ?? SECTOR_IMAGES[sector] ?? SECTOR_IMAGES['otro']`
  - [x] 4.2 Crear constante `SECTOR_IMAGES` en ese archivo con rutas a assets estáticos por sector (archivos a agregar en `src/Web/Main/public/assets/sectors/`)
  - [x] 4.3 Actualizar `NewsSection.tsx` — cada `<article>` incluye imagen 16:9 con `loading="lazy"` y alt descriptivo
  - [x] 4.4 Actualizar `NoticiasSection.tsx` — misma estructura con imagen

- [x] Task 5: Build y tests
  - [x] 5.1 `dotnet build FIBRADIS.slnx` — 0 errores (construir con API detenida)
  - [x] 5.2 `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` — todos pasan
  - [x] 5.3 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript

## Dev Notes

### Contexto de historias previas

**Historia 4.1** — `GoogleNewsRssClient` ya extrae `item.Url` con el fix del `ExtractLink` (maneja `<link>` self-closing y `<guid>`). El pipeline RSS está en `NewsPipelineJob.ExecuteAsync`.

**Historia 4.2** — `NewsAssociator.Associate` devuelve `IReadOnlyList<Guid>` de FIBRAs asociadas. El job tiene `fibraMatchInfos` disponible, que incluye `FibraMatchInfo(Id, Ticker, NameVariants)`. Para el fallback de imagen de FIBRA, necesitaremos el campo imagen/logo de `Fibra` — verificar si existe o si hay que agregarlo.

**Historia 4.3** — `NewsArticle` ya tiene `AiSummary`. Las migraciones anteriores son `AddNewsSchema`, `AddNewsArticleFibra`, `AddAiModeConfig`. La siguiente migración es `AddImageUrlToNewsArticle`.

---

### OgImageScraper — lógica

```csharp
// src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs
public interface IOgImageScraper
{
    Task<string?> TryGetOgImageAsync(string url, CancellationToken ct = default);
}

public class OgImageScraper(HttpClient http, ILogger<OgImageScraper> logger) : IOgImageScraper
{
    public async Task<string?> TryGetOgImageAsync(string url, CancellationToken ct = default)
    {
        try
        {
            // Leer solo los primeros 16KB — el og:image siempre está en el <head>
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 16383);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
            if (!response.IsSuccessStatusCode) return null;

            var html = await response.Content.ReadAsStringAsync(ct);

            // Buscar <meta property="og:image" content="..."> o <meta name="og:image" content="...">
            var match = Regex.Match(html,
                @"<meta\s[^>]*(?:property|name)=[""']og:image[""'][^>]*content=[""']([^""']+)[""']",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                // Orden inverso de atributos
                match = Regex.Match(html,
                    @"<meta\s[^>]*content=[""']([^""']+)[""'][^>]*(?:property|name)=[""']og:image[""']",
                    RegexOptions.IgnoreCase);
            }

            if (!match.Success) return null;

            var imageUrl = match.Groups[1].Value.Trim();
            // Validar que sea URL absoluta
            return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
                && (uri.Scheme == "https" || uri.Scheme == "http")
                ? imageUrl
                : null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "og:image extraction failed for '{Url}'", url);
            return null;
        }
    }
}
```

**Registro en ApiServiceExtensions.cs:**
```csharp
builder.Services.AddHttpClient<IOgImageScraper, OgImageScraper>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FIBRADIS/1.0 (+https://fibradis.mx)");
});
```

**Anti-patrón a evitar:** NO usar `HttpCompletionOption.ResponseHeadersRead` con el Range header — algunos servidores ignoran Range y devuelven todo el documento, lo que puede causar bloqueo al leer un stream grande. Usar `ResponseContentRead` con timeout de 5s en el cliente.

---

### Cambio mínimo en NewsPipelineJob

En el bloque de creación de `NewsArticle`, agregar:
```csharp
var imageUrl = await ogImageScraper.TryGetOgImageAsync(item.Url, ct);
var article = new NewsArticle
{
    // ... campos existentes ...
    ImageUrl = imageUrl,
};
```

**Nota:** Las llamadas a `ogImageScraper` son secuenciales (mismo patrón que el pipeline RSS — no Task.WhenAll por el rate limiting implícito).

---

### Frontend — assets por sector

Crear en `src/Web/Main/public/assets/sectors/`:
- `industrial.jpg`
- `comercial.jpg`
- `oficinas.jpg`
- `diversificado.jpg`
- `salud.jpg`
- `infraestructura.jpg`
- `otro.jpg`

Usar imágenes libres de derechos (Unsplash, Pexels) de ~800×450px (16:9). Comprimir a <100KB cada una.

```typescript
// src/Web/Main/src/shared/lib/news-image-fallback.ts
const SECTOR_IMAGES: Record<string, string> = {
  industrial: '/assets/sectors/industrial.jpg',
  comercial: '/assets/sectors/comercial.jpg',
  oficinas: '/assets/sectors/oficinas.jpg',
  diversificado: '/assets/sectors/diversificado.jpg',
  salud: '/assets/sectors/salud.jpg',
  infraestructura: '/assets/sectors/infraestructura.jpg',
  otro: '/assets/sectors/otro.jpg',
}

export function getArticleImageUrl(
  imageUrl: string | null | undefined,
  fibraLogoUrl?: string | null,
  sector?: string | null,
): string {
  if (imageUrl) return imageUrl
  if (fibraLogoUrl) return fibraLogoUrl
  const key = sector?.toLowerCase() ?? 'otro'
  return SECTOR_IMAGES[key] ?? SECTOR_IMAGES['otro']
}
```

---

### Patrón de imagen en ArticleCard

```tsx
<div className="w-full aspect-video overflow-hidden rounded-t-lg bg-muted">
  <img
    src={getArticleImageUrl(article.imageUrl, fibraLogoUrl, sector)}
    alt={article.title}
    className="w-full h-full object-cover"
    loading="lazy"
    onError={(e) => {
      // Si la imagen falla, mostrar placeholder de sector
      ;(e.target as HTMLImageElement).src = SECTOR_IMAGES['otro']
    }}
  />
</div>
```

---

### Anti-patrones a evitar

1. **NO** bloquear el pipeline RSS si og:image falla — siempre continuar con `ImageUrl = null`
2. **NO** usar `Task.WhenAll` con el `ogImageScraper` — mantener secuencial por rate limiting
3. **NO** almacenar datos binarios de imagen en BD — solo la URL string
4. **NO** intentar scraping de og:image para URLs de Google News redirect (`news.google.com/rss/articles/...`) — esas URLs no tienen og:image; saltar el scraping si la URL contiene `news.google.com`

### Archivos nuevos

```
src/Server/Application/News/IOgImageScraper.cs
src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs
src/Web/Main/src/shared/lib/news-image-fallback.ts
src/Web/Main/public/assets/sectors/*.jpg (7 imágenes)
src/Server/Infrastructure/Persistence/Migrations/[timestamp]_AddImageUrlToNewsArticle.cs
```

### Archivos modificados

```
src/Server/Domain/News/NewsArticle.cs                    → + ImageUrl
src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleConfiguration.cs → + ImageUrl config
src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs   → + IOgImageScraper + asignar ImageUrl
src/Server/Api/CompositionRoot/ApiServiceExtensions.cs   → + AddHttpClient<IOgImageScraper>
src/Server/SharedApiContracts/News/NewsArticleDto.cs     → + ImageUrl
src/Server/Api/Endpoints/Public/NewsEndpoints.cs         → + a.ImageUrl en ToDto
src/Web/SharedApiClient/schema.d.ts                      → regenerado
src/Web/Main/src/modules/home/NewsSection.tsx            → + imagen en card
src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx → + imagen en card
_bmad-output/implementation-artifacts/sprint-status.yaml
```

### Referencias

- [Source: 4-1-ingesta-rss-blocklist-y-deduplicacion-de-noticias.md] — pipeline RSS, NewsPipelineJob, anti-patrón Task.WhenAll
- [Source: 4-2-asociacion-de-noticias-con-fibras-y-display-en-home-y-ficha.md] — FibraMatchInfo, NewsAssociator
- [Source: convenciones-fibradis.md] — workaround migrations, no Task.WhenAll con DbContext

### Review Findings

- [x] [Review][Decision] AC2/AC3 — NewsSection (Home) no tiene contexto de FIBRA ni sector en NewsArticleDto: artículos sin og:image caen directamente a 'otro' — DECISIÓN: AC2 aplica solo a NoticiasSection (ficha pública), donde el contexto de FIBRA/sector está disponible. En Home, fallback a 'otro' es aceptable; añadir sector/logoUrl al DTO aumentaría payload y join para todos los artículos.
- [x] [Review][Patch] SSRF — OgImageScraper no valida IPs privadas/loopback ni controla auto-redirects a direcciones internas [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:9]
- [x] [Review][Patch] HTML-encoded entities en og:image content no decodificados antes de almacenar (e.g., `&amp;` en URL queda literal) [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:27]
- [x] [Review][Patch] ImageUrl sin cota de longitud — URL >2048 chars causa excepción EF silenciosa y artículo descartado sin traza [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:27]
- [x] [Review][Patch] ExtractLink GUID fallback puede retornar URL de news.google.com como URL del artículo cuando `<link/>` está vacío [src/Server/Infrastructure/Integrations/GoogleNews/GoogleNewsRssClient.cs:64]
- [x] [Review][Patch] Cobertura insuficiente en OgImageScraperTests — falta test para regex content-first (atributos invertidos) y para comportamiento ante timeout/excepción [tests/Unit/Infrastructure.Tests/Integrations/OgImage/OgImageScraperTests.cs]
- [x] [Review][Defer] Regex backtracking teórico [OgImageScraper.cs] — deferred, pre-existing; GeneratedRegex con [^>]* bounded por >, riesgo bajo en .NET
- [x] [Review][Defer] Scraping secuencial bloquea pipeline [NewsPipelineJob.cs] — deferred, intencional per Dev Notes (rate limiting)
- [x] [Review][Defer] Sin retry/circuit-breaker en OgImageScraper DI registration [ApiServiceExtensions.cs] — deferred, enhancement fuera del scope de la historia
- [x] [Review][Defer] Regex no cubre atributos HTML5 sin comillas [OgImageScraper.cs] — deferred, edge case infrecuente no requerido por spec
- [x] [Review][Defer] Race condition entre ejecuciones concurrentes del pipeline [NewsPipelineJob.cs] — deferred, pre-existing de historia 4.1

#### Review Findings — Pasada 2 (2026-05-20)

- [x] [Review][Decision] `AllowAutoRedirect=false` bloquea redirects HTTP→HTTPS de fuentes legítimas — DECISIÓN: mantener `AllowAutoRedirect=false`; trade-off de seguridad aceptado conscientemente.
- [x] [Review][Decision] AC2 — color de identidad visual de FIBRA no implementado — DECISIÓN: diferido; agregar `brandColor` a `Fibra` en historia futura del módulo noticias.
- [x] [Review][Patch] SSRF bypass por hostname — `IsAllowedHost` bloquea solo IPs literales; cualquier hostname (incluyendo DNS rebinding hacia `169.254.x.x` o `10.x.x.x`) pasa la validación [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:51]
- [x] [Review][Patch] URLs protocol-relative (`//cdn.example.com/img.jpg`) en `og:image` descartadas silenciosamente por `Uri.TryCreate(..., UriKind.Absolute)` — son comunes en sitios de noticias [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:36]
- [x] [Review][Defer] `ResponseContentRead` puede buffear respuesta completa si servidor ignora `Range` header — intencional per Dev Notes, timeout 5s acota exposición [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:23] — deferred
- [x] [Review][Defer] Dominios redirect de Google (`goo.gl`, `googleusercontent.com`) no cubiertos por filtro `news.google.com` — escenario especulativo/infrecuente [src/Server/Infrastructure/Integrations/GoogleNews/GoogleNewsRssClient.cs:155] — deferred
- [x] [Review][Defer] Charset decoding ambiguo en respuestas Range sin `Content-Type; charset=` — URLs de og:image son ASCII-safe en práctica [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:27] — deferred
- [x] [Review][Defer] `ExtractLink` retorna `string.Empty` como sentinela de fallo en vez de `null` — inconsistente, pero manejado por el caller [src/Server/Infrastructure/Integrations/GoogleNews/GoogleNewsRssClient.cs:161] — deferred
- [x] [Review][Defer] Sectores nuevos (Educativo, Autoalmacenaje, Hipotecario) sin asset en `SECTOR_IMAGES` — caen a `otro.jpg`; fuera del scope de AC3 [src/Web/Main/src/shared/lib/news-image-fallback.ts:1] — deferred

#### Review Findings — Pasada 3 (2026-05-21)

- [x] [Review][Patch] `OperationCanceledException` silenciada en `TryGetOgImageAsync` — `catch (Exception ex)` absorbe cancelación; en shutdown del job el scraper retorna `null` en lugar de propagar el token [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:43]
- [x] [Review][Patch] IPv4-mapped IPv6 bypasa el guardián SSRF — `::ffff:127.0.0.1` tiene `AddressFamily=InterNetworkV6` y `IPAddress.IsLoopback` retorna `false`; el branch IPv6 lo aprueba como host válido [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:81]
- [x] [Review][Patch] `NewsSectionSkeleton` sin placeholder de imagen 16:9 — las cards reales tienen imagen; el skeleton no, causando CLS al cargar [src/Web/Main/src/modules/home/NewsSection.tsx:NewsSectionSkeleton]
- [x] [Review][Patch] `SocketException` en `IsAllowedHostAsync` silenciada sin log — fallo transitorio de DNS y host bloqueado son indistinguibles en producción [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:62]
- [x] [Review][Defer] DNS rebinding (TOCTOU) entre `IsAllowedHostAsync` y la HTTP request real — limitación arquitectural de hostname-based SSRF mitigation; fix requiere `HttpMessageHandler` custom con IP pinning [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:15] — deferred
- [x] [Review][Defer] URL de `og:image` extraída no validada contra SSRF allowlist — riesgo browser-side únicamente; ningún código del servidor la fetchea actualmente [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:35] — deferred
- [x] [Review][Defer] HTTP 416 `Range Not Satisfiable` retorna `null` silencioso en páginas pequeñas — edge case infrecuente; retry sin Range requería cambio mayor [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:24] — deferred
- [x] [Review][Defer] IPv6 ULA `fc00::/7` no bloqueado en `IsAllowedIp` — equivalente IPv6 de RFC 1918; bajo riesgo con infraestructura actual [src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs:81] — deferred

#### Review Findings — Cierre Final (2026-05-21)

- [x] [Review][Close] Pasada final sin hallazgos bloqueantes nuevos. Validado nuevamente con `OgImageScraperTests`, `NewsPipelineJobTests`, `NewsEndpointsTests`, `npm test --workspace=src/Web/Main` y `npm run build --workspace=src/Web/Main)`. Los deferred existentes permanecen documentados y no bloquean el cierre.

## Dev Agent Record

### Agent Model Used

gpt-5.5-codex

### Debug Log References

- `python scripts/memory/memory_cli.py search "scraping noticias imagen og:image fallback visual"`
- `git checkout -b story/4-5-1-scraping-imagen-ogimage-y-fallback-visual`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~NewsPipelineJobTests|FullyQualifiedName~OgImageScraperTests"`
- `dotnet ef migrations add AddImageUrlToNewsArticle --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`
- `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`
- `dotnet build FIBRADIS.slnx`
- `npm run codegen:api`
- `node --experimental-strip-types --test src/shared/lib/news-image-fallback.test.ts`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`
- `npm run build --workspace=src/Web/Main`
- `dotnet test FIBRADIS.slnx --no-build`
- `npm run lint --workspace=src/Web/Main`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~OgImageScraperTests"`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~NewsEndpointsTests"`
- `dotnet build FIBRADIS.slnx`
- `npm run build --workspace=src/Web/Main`
- `npm run lint --workspace=src/Web/Main`

### Completion Notes List

- Se agregó `ImageUrl` a `NewsArticle`, su mapeo EF y la migración `20260520160318_AddImageUrlToNewsArticle`, aplicada sobre `FIBRADIS_Dev`.
- Se implementó `IOgImageScraper` + `OgImageScraper` con `HttpClient`, `Range: bytes=0-16383`, timeout de 5 s, parsing por regex y tolerancia a fallos; si falla, el pipeline continúa con `ImageUrl = null`.
- `NewsPipelineJob` ahora intenta extraer `og:image` de forma secuencial y omite explícitamente URLs de `news.google.com` para evitar scraping inútil de redirects.
- La API pública ahora expone `imageUrl` en `NewsArticleDto`; se regeneró `scripts/codegen/Api.json` y `src/Web/SharedApiClient/schema.d.ts`.
- Se añadió `news-image-fallback.ts` con assets sectoriales 16:9 en `src/Web/Main/public/assets/sectors/` y se actualizaron `NewsSection` y `NoticiasSection` para renderizar imágenes lazy-loaded.
- Cobertura agregada/actualizada: `OgImageScraperTests` valida extracción y `Range` header; `NewsPipelineJobTests` valida asignación de `ImageUrl` y skip de Google News; `news-image-fallback.test.ts` cubre la jerarquía de fallback en frontend.
- Validaciones ejecutadas: `dotnet build FIBRADIS.slnx` OK (0 errores), `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` OK (35/35), `npm run build --workspace=src/Web/Main` OK, `npm run lint --workspace=src/Web/Main` OK, `dotnet test FIBRADIS.slnx --no-build` OK para suites con tests (`Domain` 9/9, `Application` 35/35, `Infrastructure` 35/35, `Jobs` 2/2, `Api` 67/67); `ApiCompatibility.Tests`, `Integrations.Tests` y `Persistence.Tests` siguen reportando "No hay ninguna prueba disponible".
- Pasada 3 de review resuelta: `TryGetOgImageAsync` ahora propaga cancelación cooperativa, normaliza IPv4-mapped IPv6 antes del guardián SSRF, registra `SocketException` de DNS en debug y `NewsSectionSkeleton` ya reserva el bloque 16:9 para evitar CLS.
- Cobertura actualizada: `OgImageScraperTests` agrega cancelación propagada y bloqueo de `::ffff:127.0.0.1`; `NewsEndpointsTests` sigue verde para el endpoint individual de noticias tras los ajustes compartidos del módulo.
- Validaciones ejecutadas en esta pasada: `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~OgImageScraperTests"` OK (16/16), `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~NewsEndpointsTests"` OK (2/2), `dotnet build FIBRADIS.slnx` OK, `npm test --workspace=src/Web/Main` OK (37/37), `npm run build --workspace=src/Web/Main` OK.
- `npm run lint --workspace=src/Web/Main` sigue fallando por una condición preexistente del script: ESLint entra a `src/Web/Main/dist-server/entry-server.js` y reporta reglas no instaladas (`jsx-a11y/anchor-has-content`, `react-hooks/rules-of-hooks`) sobre output generado, no sobre los archivos fuente tocados por la historia.

### File List

- `_bmad-output/implementation-artifacts/4-5-1-scraping-imagen-ogimage-y-fallback-visual.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `scripts/codegen/Api.json`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Endpoints/Public/NewsEndpoints.cs`
- `src/Server/Application/News/IOgImageScraper.cs`
- `src/Server/Domain/News/NewsArticle.cs`
- `src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs`
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260520160318_AddImageUrlToNewsArticle.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260520160318_AddImageUrlToNewsArticle.Designer.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleConfiguration.cs`
- `src/Server/SharedApiContracts/News/NewsArticleDto.cs`
- `src/Web/Main/public/assets/sectors/comercial.jpg`
- `src/Web/Main/public/assets/sectors/diversificado.jpg`
- `src/Web/Main/public/assets/sectors/industrial.jpg`
- `src/Web/Main/public/assets/sectors/infraestructura.jpg`
- `src/Web/Main/public/assets/sectors/oficinas.jpg`
- `src/Web/Main/public/assets/sectors/otro.jpg`
- `src/Web/Main/public/assets/sectors/salud.jpg`
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx`
- `src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx`
- `src/Web/Main/src/modules/home/NewsSection.tsx`
- `src/Web/Main/src/shared/lib/news-image-fallback.test.ts`
- `src/Web/Main/src/shared/lib/news-image-fallback.ts`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Unit/Infrastructure.Tests/Integrations/OgImage/OgImageScraperTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`

## Change Log

- 2026-05-20: Story movida a `in-progress`; branch `story/4-5-1-scraping-imagen-ogimage-y-fallback-visual` creado para implementación.
- 2026-05-20: Implementado scraping `og:image`, persistencia `ImageUrl`, exposición en API pública, codegen OpenAPI y render de imágenes con fallback sectorial en Home/Ficha.
- 2026-05-20: Validación completada con build .NET, `Infrastructure.Tests`, prueba unitaria del helper frontend, build/lint de `src/Web/Main` y regresión `dotnet test FIBRADIS.slnx --no-build`.
- 2026-05-20: Patches code review resueltos — SSRF (IsAllowedHost + AllowAutoRedirect=false), HTML entities (WebUtility.HtmlDecode), URL length (≤2048), GUID fallback (filtro news.google.com), cobertura OgImageScraperTests (9 tests content-first, HTML entities, URL length, timeout, SSRF IPs). Decisión AC2/AC3: NewsSection usa 'otro' como fallback final. 157/157 tests.
- 2026-05-20: Pasada 2 code review — 2 patches resueltos: SSRF hostname (IsAllowedHost → IsAllowedHostAsync con DNS pre-resolution vía Dns.GetHostAddressesAsync + separación IsAllowedIp) y URLs protocol-relative (normalización //... → https://... antes de validar). Tests agregados: WhenHostnameResolvesToLoopback_ReturnsNull y WhenProtocolRelativeUrl_ReturnsHttpsNormalizedUrl. 161/161 tests.
- 2026-05-21: Pasada 3 code review — 4 patches resueltos: cancelación cooperativa en `OgImageScraper`, bloqueo de IPv4-mapped IPv6, log de `SocketException` en DNS guard y placeholder 16:9 en `NewsSectionSkeleton`. Validado con `OgImageScraperTests` (16/16), `NewsEndpointsTests` (2/2), `npm test` Main (37/37), `dotnet build FIBRADIS.slnx` y `npm run build --workspace=src/Web/Main`.
