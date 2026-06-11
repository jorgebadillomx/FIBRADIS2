# Historia 11.3: Sitemap XML, robots.txt y URLs slug para fichas de FIBRA

Status: done

## Historia

Como SEO lead,
quiero que el sitio sirva un `sitemap.xml` dinámico con todas las rutas públicas indexables y un `robots.txt` que lo referencie, y que las fichas de FIBRA usen URLs legibles `/fibras/{nombre-fibra}-{ticker}` (ej. `/fibras/fibra-uno-funo11`) en lugar de `/fibras/{ticker}`,
para que Google descubra y rastree eficientemente todas las páginas del sitio — incluyendo `/calculadora` con prioridad alta — con URLs descriptivas que mejoran CTR y relevancia, y deje de desperdiciar crawl budget en rutas privadas (`/ops/`, `/api/`).

> **Ampliación de alcance (2026-06-11, Jorge):** la ruta de ficha en Main hoy responde en `/fibras/:ticker`; debe cambiar a `/fibras/:nombre-fibra-ticker` (slug del nombre + ticker al final). Las URLs viejas por ticker deben seguir resolviendo con redirección 301 a la canónica.

## Criterios de Aceptación

**CA-1: Endpoint /sitemap.xml responde 200 con XML válido**
Dado que hago GET `/sitemap.xml`,
Entonces la respuesta tiene status 200, `Content-Type: application/xml; charset=utf-8`, y el XML es un sitemap válido según el schema `http://www.sitemaps.org/schemas/sitemap/0.9`.

**CA-2: /calculadora incluida con prioridad 0.9**
Dado que proceso el sitemap,
Entonces existe una entrada `<url>` con `<loc>https://fibrasinmobiliarias.com/calculadora</loc>`, `<priority>0.9</priority>`, `<changefreq>daily</changefreq>`.

**CA-3: FIBRAs activas incluidas dinámicamente con URL slug**
Dado que hay N FIBRAs activas en el catálogo,
Entonces el sitemap incluye N entradas `/fibras/{slug}` donde `slug = slugify(FullName) + "-" + ticker.ToLowerInvariant()` (ej. `/fibras/fibra-uno-funo11`), cada una con `<priority>0.8</priority>` y `<changefreq>weekly</changefreq>`. NO se incluyen las URLs viejas `/fibras/{ticker}`.

**CA-4: Rutas estáticas incluidas**
Dado que proceso el sitemap,
Entonces están presentes las siguientes rutas con sus prioridades:
- `/` → priority 1.0, changefreq daily
- `/catalogo` → priority 0.8, changefreq weekly
- `/comparar` → priority 0.7, changefreq weekly
- `/noticias` → priority 0.7, changefreq daily
- `/conoce-las-fibras` → priority 0.6, changefreq monthly
- `/calendario` → priority 0.7, changefreq weekly
- `/fundamentales` → priority 0.7, changefreq weekly
- `/calculadora` → priority 0.9, changefreq daily

**CA-5: Endpoint /robots.txt responde 200**
Dado que hago GET `/robots.txt`,
Entonces la respuesta tiene status 200, `Content-Type: text/plain; charset=utf-8`, y el contenido incluye:
```
User-agent: *
Allow: /
Disallow: /ops/
Disallow: /api/
Disallow: /hangfire/
Sitemap: https://fibrasinmobiliarias.com/sitemap.xml
```

**CA-6: robots.txt deshabilita rastreo de rutas privadas**
Dado que reviso el robots.txt,
Entonces `/ops/`, `/api/` y `/hangfire/` están en Disallow.

**CA-7: Sitemap usa BaseUrl configurable**
Dado que `App:BaseUrl` está en `appsettings.json`,
Entonces todas las URLs del sitemap usan ese valor como prefijo (no hardcoded).

**CA-8: Ruta SPA de ficha usa slug nombre+ticker**
Dado que navego a `/fibras/fibra-uno-funo11`,
Entonces `FibraPage` carga y muestra la ficha de FUNO11 (el ticker se extrae del último segmento del slug, después del último guión).
Dado que navego a `/fibras/slug-con-ticker-inexistente-xyz99`,
Entonces se muestra `FibraNotFound`.

**CA-9: Redirección 301 server-side de URLs viejas**
Dado que hago `GET /fibras/FUNO11` (o `/fibras/funo11`) directo al servidor,
Entonces la respuesta es `301 Moved Permanently` con `Location: /fibras/fibra-uno-funo11`.
Dado que hago `GET /fibras/fibra-uno-funo11` (slug canónico),
Entonces el middleware pasa sin redirigir.
Dado que hago `GET /fibras/nombre-viejo-funo11` (slug obsoleto pero ticker válido),
Entonces la respuesta es 301 a la URL canónica actual.

**CA-10: Canonicalización client-side y canonical link**
Dado que navego client-side a `/fibras/FUNO11` (link viejo o bookmark),
Entonces `FibraPage` resuelve la fibra y hace `navigate` con `replace: true` a `/fibras/fibra-uno-funo11`.
Dado que la ficha cargó,
Entonces el `<link rel="canonical">` es `https://fibrasinmobiliarias.com/fibras/{slug}` (nota: hoy apunta al dominio incorrecto `fibradis.mx` — corregir).

**CA-11: Links internos usan la URL slug**
Dado que reviso los links a fichas en Home (PriceCarousel, GainersLosers, FibraUniverseTable, GlobalSearch), `/catalogo`, `/fundamentales`, `/calendario` y `NoticiaPage`,
Entonces todos apuntan a `/fibras/{slug}` (con fallback `/fibras/{ticker}` solo mientras el catálogo no ha cargado).

**CA-12: Prerender genera rutas slug**
Dado que ejecuto el build con prerender (`prerender.mjs`),
Entonces las páginas estáticas se generan en `dist/fibras/{slug}/index.html` (no `dist/fibras/{TICKER}/`).

## Tareas / Subtareas

- [x] Task 1: Crear `SeoEndpoints.cs`
  - [x] Crear `src/Server/Api/Endpoints/Public/SeoEndpoints.cs`
  - [x] Definir clase estática `SeoEndpoints` con método de extensión `MapSeo(this IEndpointRouteBuilder app)`
  - [x] Dentro de `MapSeo`, registrar:
    - `app.MapGet("/sitemap.xml", ...)`
    - `app.MapGet("/robots.txt", ...)`
  - [x] Ambos endpoints con `.AllowAnonymous()`
  - [x] Ambos endpoints sin autenticación ni autorización

- [x] Task 2: Implementar endpoint `/sitemap.xml`
  - [x] El handler recibe `IFibraRepository fibraRepo, IConfiguration config, CancellationToken ct`
  - [x] Leer `App:BaseUrl` de `config` (fallback: `"https://fibrasinmobiliarias.com"`)
  - [x] Definir lista de rutas estáticas con su prioridad y changefreq (ver CA-4)
  - [x] Llamar `await fibraRepo.GetAllActiveAsync(ct)` para obtener FIBRAs activas
  - [x] Para cada fibra, la URL es `{baseUrl}/fibras/{FibraSlug.Build(fibra.FullName, fibra.Ticker)}` (helper de Task 9)
  - [x] Construir XML usando `System.Text` / string builder o `XmlWriter` (ver spec en Dev Notes)
  - [x] Retornar `Results.Content(xmlContent, "application/xml; charset=utf-8")`

- [x] Task 3: Implementar endpoint `/robots.txt`
  - [x] El handler recibe `IConfiguration config`
  - [x] Leer `App:BaseUrl` de `config`
  - [x] Retornar `Results.Content(robotsContent, "text/plain; charset=utf-8")`
  - [x] Formato exacto (respetando newlines Unix):
    ```
    User-agent: *
    Allow: /
    Disallow: /ops/
    Disallow: /api/
    Disallow: /hangfire/
    
    Sitemap: {baseUrl}/sitemap.xml
    ```

- [x] Task 4: Registrar endpoints en `Program.cs`
  - [x] Agregar `using Api.Endpoints.Public;` si no está presente
  - [x] Agregar `app.MapSeo();` junto al resto de `app.MapXxx()` calls
  - [x] Agregar `app.MapFallback("/sitemap.xml", ...)` NO — el endpoint se registra directamente, no necesita fallback

- [x] Task 5: Unit tests para generación del XML
  - [x] Archivo: agregar tests al proyecto unit test existente (Infrastructure.Tests o Api.Tests)
  - [x] `SitemapContainsCalculadora_WithPriority09()` — verifica que `/calculadora` tiene priority 0.9
  - [x] `SitemapContainsAllStaticRoutes()` — verifica que `/`, `/catalogo`, etc. están presentes
  - [x] `SitemapContainsFibraSlugUrls()` — verifica que fibras activas se incluyen como `/fibras/{slug}` (ej. `fibra-uno-funo11`) y NO como `/fibras/{ticker}`
  - [x] `RobotsTxtContainsDisallowOps()` — verifica que `/ops/` está en Disallow
  - [x] `RobotsTxtContainsSitemapUrl()` — verifica que incluye la URL del sitemap

- [x] Task 6: Integration test para los endpoints (opcional pero recomendado)
  - [x] En `tests/Integration/Api.Tests/` agregar test que hace GET `/sitemap.xml` y verifica Content-Type y status 200
  - [x] Test para GET `/robots.txt` con mismo patrón

- [x] Task 7: Renombrar menú "Catálogo" → "Fibras" en `PublicLayout.tsx`
  - [x] En `src/Web/Main/src/shared/layouts/PublicLayout.tsx`, cambiar el label en la **nav de escritorio** (línea ~104):
    ```tsx
    // ANTES
    <Link to="/catalogo" ...>Catálogo</Link>
    // DESPUÉS
    <Link to="/catalogo" ...>Fibras</Link>
    ```
  - [x] Cambiar el mismo label en la **nav móvil** (Dialog, línea ~219):
    ```tsx
    // ANTES
    <Link to="/catalogo" ...>Catálogo</Link>
    // DESPUÉS
    <Link to="/catalogo" ...>Fibras</Link>
    ```
  - [x] La ruta `/catalogo` y el componente `CatalogoPage` NO cambian — solo el texto visible en el menú
  - [x] Ejecutar `npm run build --workspace=src/Web/Main` y verificar 0 errores TypeScript

- [x] Task 8: Verificación backend
  - [x] `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] Ejecutar el servidor en dev y hacer `curl http://localhost:5000/sitemap.xml`
  - [x] Verificar que el XML es válido (puede copiarse a https://www.xml-sitemaps.com/validate-xml-sitemap.html para verificación manual)

- [x] Task 9: Backend — helper `FibraSlug` (CA: 3, 9)
  - [x] Crear `src/Server/Application/Catalog/FibraSlug.cs` con método estático `Build(string fullName, string ticker)` (ver Dev Notes §Formato del slug)
  - [x] Unit tests en `tests/Unit/Application.Tests/Catalog/FibraSlugTests.cs`:
    - `Build_BasicName_ReturnsKebabWithTickerSuffix` — ("Fibra Uno", "FUNO11") → `"fibra-uno-funo11"`
    - `Build_NameWithAccents_NormalizesAccents` — tildes/ñ eliminadas
    - `Build_NameWithSpecialChars_StripsNonAlphanumeric`
    - `Build_EmptyName_ReturnsTickerOnly` — ("", "FUNO11") → `"funo11"`

- [x] Task 10: Backend — `FibraSlugRedirectMiddleware` 301 (CA: 9)
  - [x] Crear `src/Server/Api/Middleware/FibraSlugRedirectMiddleware.cs` (ver Dev Notes §Middleware de redirección 301)
  - [x] Constructor: `(RequestDelegate next, IServiceScopeFactory scopeFactory)` — `IFibraRepository` es Scoped, el middleware es Singleton
  - [x] Lógica: solo GET, solo paths `/fibras/{algo}` sin extensión; extraer ticker del último segmento del slug; si la fibra existe y el path actual ≠ slug canónico → `Results`/301 con `Location` al canónico; si no existe o ya es canónico → pass-through
  - [x] Registrar en `Program.cs` DESPUÉS de `WwwToNonWwwMiddleware` y ANTES de `SpaMetadataMiddleware` (11.2)
  - [x] Unit tests en `tests/Unit/Infrastructure.Tests/Middleware/FibraSlugRedirectMiddlewareTests.cs`:
    - `InvokeAsync_BareTicker_Redirects301ToSlug`
    - `InvokeAsync_LowercaseTicker_Redirects301ToSlug`
    - `InvokeAsync_CanonicalSlug_PassesThrough`
    - `InvokeAsync_StaleSlugValidTicker_Redirects301ToCanonical`
    - `InvokeAsync_UnknownTicker_PassesThrough` (la SPA muestra FibraNotFound)
    - `InvokeAsync_AssetOrApiPath_PassesThrough`

- [x] Task 11: Frontend — util `fibra-slug.ts` (CA: 8, 10, 11)
  - [x] Crear `src/Web/Main/src/shared/lib/fibra-slug.ts` con `buildFibraSlug(fullName, ticker)` y `extractTickerFromSlug(param)` (ver Dev Notes §Helper TypeScript)
  - [x] Unit tests (vitest) `fibra-slug.test.ts`: build básico, acentos, extracción desde slug completo, extracción desde ticker pelado (sin guiones), param vacío

- [x] Task 12: Frontend — ruta y `FibraPage` (CA: 8, 10)
  - [x] En `src/Web/Main/src/app/routes.tsx` cambiar `{ path: '/fibras/:ticker', ... }` → `{ path: '/fibras/:slug', ... }`
  - [x] En `FibraPage.tsx`: leer `slug` de `useParams`, derivar `ticker = extractTickerFromSlug(slug)` (uppercase) y conservar TODAS las queries existentes por ticker (queryKeys `['fibra', ticker]`, `['fibra-history', ticker, ...]`, etc. NO cambian)
  - [x] Al cargar la fibra: si `slug !== buildFibraSlug(fibra.fullName, fibra.ticker)` → `navigate('/fibras/' + slugCanonico, { replace: true })`
  - [x] Corregir `canonicalUrl` (línea ~148): `https://fibrasinmobiliarias.com/fibras/${slugCanonico}` (hoy usa dominio viejo `fibradis.mx` y ticker)
  - [x] `FibraNotFound` recibe el ticker extraído

- [x] Task 13: Frontend — links internos a slug (CA: 11)
  - [x] Crear hook `src/Web/Main/src/shared/hooks/useFibraSlugMap.ts`: `useQuery` sobre `fetchAllFibras()` (reusar queryKey del catálogo para dedupe) → expone `slugFor(ticker): string` con fallback `ticker.toLowerCase()` mientras carga
  - [x] Actualizar los 9 puntos que generan links `/fibras/${ticker}`:
    - `CatalogoPage.tsx:180` — tiene `fibra.fullName` directo → `buildFibraSlug` sin hook
    - `FundamentalesPage.tsx:148`, `NoticiaPage.tsx:116` (conserva `#noticias`), `CalendarioPage.tsx:182`, `PriceCarousel.tsx:105`, `GainersLosers.tsx:59,87`, `FibraUniverseTable.tsx:157`, `GlobalSearch.tsx:34` — usar `useFibraSlugMap`
  - [x] `npm run build --workspace=src/Web/Main` — 0 errores TypeScript

- [x] Task 14: Prerender con slugs (CA: 12)
  - [x] En `src/Web/Main/scripts/prerender.mjs`: cambiar `url: \`/fibras/${f.ticker}\`` por la URL slug construida desde `f.fullName` + `f.ticker` (replicar la lógica de `buildFibraSlug` o importarla); el queryKey de `initialData` sigue siendo `['fibra', f.ticker]` (el ticker extraído en `FibraPage` queda en mayúsculas)
  - [x] Actualizar `prerender-utils.test.mjs:9` (canonical de ejemplo con slug y dominio `fibrasinmobiliarias.com`)

- [x] Task 15: Verificación final del cambio de ruta
  - [x] `dotnet test tests/Unit/` — todos verdes incluyendo los nuevos
  - [x] `npm test --workspace=src/Web/Main` — todos verdes
  - [x] Manual: navegar a `/fibras/FUNO11` → URL cambia a `/fibras/fibra-uno-funo11` y la ficha carga; `curl -I http://localhost:5000/fibras/FUNO11` → 301 con Location slug
  - [x] E2E: los specs existentes usan `page.goto('/fibras/FUNO11')` — siguen funcionando vía canonicalización client-side; si algún spec assertea la URL, actualizarlo

### Review Findings

- [x] [Review][Decision→Patch] Sitemap omite `/herramientas` — **resuelto (Jorge, 2026-06-11): incluirla con 0.7 weekly**. Agregada a `StaticRoutes` + tests actualizados (9 rutas estáticas)
- [x] [Review][Decision→Patch] Invariante "tickers alfanuméricos sin guiones" sin enforcement — **resuelto (Jorge, 2026-06-11): validar en Ops**. `ValidateCreateRequest` rechaza con 400 tickers con caracteres no alfanuméricos (`char.IsAsciiLetterOrDigit`) + 3 casos de integración
- [x] [Review][Patch] (Alta) Slug terminado en guión crasheaba FibraPage: `extractTickerFromSlug('fibra-uno-')` → `''` → query `enabled:false` → render con `fibra!` undefined → TypeError. Guard `if (!ticker) return <FibraNotFound/>` [src/Web/Main/src/modules/ficha-publica/FibraPage.tsx:165]
- [x] [Review][Patch] (Media) Orden de elementos violaba el XSD de sitemaps.org (`<priority>` antes de `<changefreq>`) — CA-1 exige XML válido según el schema. Orden corregido + test `SitemapElementsFollowXsdSequence` [src/Server/Api/Endpoints/Public/SeoEndpoints.cs]
- [x] [Review][Patch] (Media) `<loc>` se interpolaba sin escaping XML — ahora `SecurityElement.Escape` + test con BaseUrl conteniendo `&` [src/Server/Api/Endpoints/Public/SeoEndpoints.cs]
- [x] [Review][Patch] (Media) HEAD a `/sitemap.xml` y `/robots.txt` devolvía 405 — `MapMethods` GET+HEAD + tests de integración [src/Server/Api/Endpoints/Public/SeoEndpoints.cs]
- [x] [Review][Patch] (Media) La canonicalización client-side descartaba query string y hash (`#noticias`, UTM) — `navigate` ahora conserva `location.search + location.hash` [src/Web/Main/src/modules/ficha-publica/FibraPage.tsx]
- [x] [Review][Patch] (Media) Paridad slugify para marcas combinantes fuera de U+0300–036F — TS/mjs ahora usan `\p{Mn}` (misma clase que `UnicodeCategory.NonSpacingMark` en C#) + caso de paridad U+0483 en las tres suites [src/Web/Main/src/shared/lib/fibra-slug.ts, scripts/prerender-utils.mjs, FibraSlugTests.cs]
- [x] [Review][Patch] (Baja) Trailing slash y prefijo con mayúsculas escapaban el 301 — regex `^/fibras/([^/]+)/?$` con `IgnoreCase` (comparación canónica sigue Ordinal) + 3 tests de variantes [src/Server/Api/Middleware/FibraSlugRedirectMiddleware.cs:18]
- [x] [Review][Patch] (Baja) `/sitemap.xml` y `/robots.txt` se filtraban al contrato OpenAPI/codegen — `ExcludeFromDescription()` + `Api.json` y `schema.d.ts` regenerados [src/Server/Api/Endpoints/Public/SeoEndpoints.cs]
- [x] [Review][Patch] (Baja) `useFibraSlugMap()` invocado por fila — hoisteado a `FundamentalesPage`, el slug baja por prop [src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx]
- [x] [Review][Defer] Query a BD por cada GET/HEAD de `/fibras/*` y `/sitemap.xml` sin caché [src/Server/Api/Middleware/FibraSlugRedirectMiddleware.cs:61] — deferred, ya previsto en Dev Notes (IMemoryCache ticker→slug como deferred work)
- [x] [Review][Defer] Prerender legacy: drift `FIBRAS_SEED`↔BD, 301 append-slash de `UseDefaultFiles` si algún día se usa `build:full`, y fichas `/fibras/*` sin metadata server-side (`SpaMetadataProvider` no las cubre) [src/Web/Main/scripts/prerender.mjs:22] — deferred, pre-existing: el deploy usa `npm run build` (prerender no se despliega); candidato a historia de metadata server-side para fichas
- [x] [Review][Defer] Triple implementación del slugify (C#/TS/mjs): consolidar mjs importando el `.ts` (node ya usa `--experimental-strip-types` en tests) cuando se decida el destino del prerender legacy [src/Web/Main/scripts/prerender-utils.mjs:1] — deferred, refactor sin impacto en producción

## Dev Notes

### Contexto SEO
fibrasinmobiliarias.com no tiene sitemap.xml actualmente. GSC muestra solo 50 clicks en 90 días y 2,080 impresiones con 136 queries. Un sitemap dinámico que incluya todas las FIBRAs activas le dará a Google el mapa completo del sitio y acelerará el rastreo de páginas de fichas individuales.

### Generación del XML con string interpolation
Para un sitemap sin dependencias externas, usar un `StringBuilder` o interpolación de strings. El formato del XML es simple y no requiere serialización XML compleja:

```csharp
var sb = new StringBuilder();
sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

// Rutas estáticas
sb.AppendLine(BuildUrlEntry($"{baseUrl}/", "1.0", "daily"));
sb.AppendLine(BuildUrlEntry($"{baseUrl}/calculadora", "0.9", "daily"));
// ... etc

// FIBRAs dinámicas — URL slug (CA-3)
foreach (var fibra in fibras)
{
    sb.AppendLine(BuildUrlEntry($"{baseUrl}/fibras/{FibraSlug.Build(fibra.FullName, fibra.Ticker)}", "0.8", "weekly"));
}

sb.AppendLine("</urlset>");
return sb.ToString();

// Helper
static string BuildUrlEntry(string loc, string priority, string changefreq) => $"""
  <url>
    <loc>{loc}</loc>
    <priority>{priority}</priority>
    <changefreq>{changefreq}</changefreq>
  </url>
""";
```

### Formato del slug de FIBRA

`slug = slugify(FullName) + "-" + ticker.ToLowerInvariant()` — el ticker SIEMPRE va al final como último segmento. Ejemplos con datos reales del catálogo:

| FullName | Ticker | Slug |
|---|---|---|
| Fibra Uno | FUNO11 | `fibra-uno-funo11` |
| Fibra Macquarie | FIBRAMQ12 | `fibra-macquarie-fibramq12` |
| Fibra Hotel City Express | HCITY17 | `fibra-hotel-city-express-hcity17` |
| CFE Fibra E | FCFE18 | `cfe-fibra-e-fcfe18` |

**Por qué el ticker al final**: permite resolver la fibra extrayendo el último segmento (después del último `-`) SIN agregar columna `Slug` a la BD, sin migración, y sin tocar el endpoint `/api/v1/fibras/{ticker}`. Los tickers son alfanuméricos sin guiones (verificado en el catálogo completo de 20 fibras), así que la extracción es no-ambigua. Una URL vieja `/fibras/FUNO11` (sin guiones) extrae el string completo como ticker — la retrocompatibilidad sale gratis. El slug del nombre es puramente cosmético/SEO: si el nombre cambia, las URLs viejas siguen resolviendo y se canonicalizan vía 301.

### Helper C# — `FibraSlug.Build`

Ubicación: `src/Server/Application/Catalog/FibraSlug.cs`. Misma normalización que el `SlugGenerator` de la historia 11.4 (FormD + descartar `NonSpacingMark` + lowercase + espacios→guión + strip no-alfanuméricos + colapsar guiones):

```csharp
public static class FibraSlug
{
    public static string Build(string fullName, string ticker)
    {
        var namePart = Slugify(fullName);
        var tickerPart = ticker.ToLowerInvariant();
        return string.IsNullOrEmpty(namePart) ? tickerPart : $"{namePart}-{tickerPart}";
    }
    // Slugify: ver SlugGenerator de 11.4 (mismo algoritmo, sin lógica de unicidad — el ticker ya garantiza unicidad)
}
```

**Coordinación con 11.4**: 11.4 define `SlugGenerator` en `Application/News/`. Si 11.4 ya está implementada al tomar esta historia, extraer la normalización común a `Application/Common/Slugify.cs` y que ambos la usen. Si no, implementar aquí y dejar nota en el Dev Agent Record para que 11.4 reutilice.

### Middleware de redirección 301 — `FibraSlugRedirectMiddleware`

Mismo patrón que `WwwToNonWwwMiddleware` (11.1). Pseudocódigo:

```csharp
// 1. Solo GET; path con extensión o que empiece con /api/, /ops/, /hangfire/ → pass-through
// 2. Si path no matchea ^/fibras/([^/]+)$ → pass-through
// 3. tickerCandidate = último segmento del slug después del último '-' (o el slug completo si no hay '-'), ToUpperInvariant()
// 4. using var scope = scopeFactory.CreateScope(); var repo = scope.ServiceProvider.GetRequiredService<IFibraRepository>();
//    fibra = buscar por ticker (case-insensitive) entre las activas
// 5. fibra null → pass-through (la SPA renderiza FibraNotFound)
// 6. canonical = $"/fibras/{FibraSlug.Build(fibra.FullName, fibra.Ticker)}"
// 7. Si path actual == canonical (ordinal) → pass-through
// 8. context.Response.StatusCode = 301; context.Response.Headers.Location = canonical + context.Request.QueryString; return;
```

Preservar el query string y el fragment no viaja al servidor (los `#noticias` sobreviven el redirect del lado del browser). `IServiceScopeFactory` es obligatorio: `IFibraRepository` es Scoped y el middleware Singleton — mismo patrón documentado en 11.4 para `NewsMetadataMiddleware`. Una consulta a BD por request solo ocurre en cargas completas de página bajo `/fibras/` (las navegaciones SPA no pasan por el servidor); si fuera problema, cachear ticker→slug con `IMemoryCache` queda como deferred.

**Aprendizaje de 11.1**: el code review de `WwwToNonWwwMiddleware` dejó como defer el guard `Response.HasStarted` antes de mutar el response — incluirlo desde el inicio en este middleware (`if (context.Response.HasStarted) { await next(context); return; }`). Para buscar la fibra por ticker, reusar el método existente del repositorio que ya alimenta `GET /api/v1/fibras/{ticker}` (lookup case-insensitive) — NO agregar un método nuevo si ya existe uno equivalente.

### Helper TypeScript — `fibra-slug.ts`

Ubicación: `src/Web/Main/src/shared/lib/fibra-slug.ts` (convención utils kebab-case):

```typescript
export function buildFibraSlug(fullName: string, ticker: string): string {
  const namePart = fullName
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
  const tickerPart = ticker.toLowerCase()
  return namePart ? `${namePart}-${tickerPart}` : tickerPart
}

export function extractTickerFromSlug(param: string): string {
  const lastSegment = param.split('-').at(-1) ?? param
  return lastSegment.toUpperCase()
}
```

DEBE producir exactamente el mismo slug que `FibraSlug.Build` en C# — si difieren, el middleware 301 y la canonicalización client-side entran en loop de redirecciones. Agregar un test con los 4 ejemplos de la tabla de arriba en AMBOS lados.

### FibraPage — resolución por slug

`useParams<{ slug: string }>()` → `const ticker = extractTickerFromSlug(slug!)`. Todo lo demás (queries, secciones, favoritos) sigue trabajando con `ticker` en mayúsculas — los queryKeys NO cambian, lo que mantiene compatible el `initialData` del prerender. La canonicalización va en un `useEffect`:

```tsx
useEffect(() => {
  if (fibra && slug !== buildFibraSlug(fibra.fullName, fibra.ticker)) {
    navigate(`/fibras/${buildFibraSlug(fibra.fullName, fibra.ticker)}`, { replace: true })
  }
}, [fibra, slug, navigate])
```

### Hook `useFibraSlugMap` — links desde componentes que solo tienen ticker

`MarketSnapshotDto` NO incluye el nombre de la fibra (solo `ticker`), igual que los DTOs de calendario y fundamentales. En vez de modificar DTOs del backend, un hook compartido resuelve ticker→slug desde el catálogo (ya cacheado por TanStack Query — `fetchAllFibras` existe en `fibrasApi.ts:24` y CatalogoPage ya lo usa):

```typescript
export function useFibraSlugMap() {
  const { data } = useQuery({ queryKey: [...], queryFn: fetchAllFibras, staleTime: 60 * 60_000 })
  const map = useMemo(() => new Map(
    (data ?? []).map(f => [f.ticker, buildFibraSlug(f.fullName, f.ticker)])
  ), [data])
  return { slugFor: (ticker: string) => map.get(ticker) ?? ticker.toLowerCase() }
}
```

Reusar el MISMO queryKey que ya usa CatalogoPage para `fetchAllFibras` (verificar el key exacto al implementar) — así la query se dedupe y no hay fetch extra en páginas que ya cargan el catálogo. El fallback `ticker.toLowerCase()` produce un link viejo que el middleware/FibraPage canonicalizan — solo ocurre el instante antes de que cargue el catálogo.

### Relación con 11.2 (SpaMetadataMiddleware — in-progress)

`SpaMetadataProvider` solo maneja rutas estáticas de un diccionario (`/`, `/calculadora`, etc.) — `/fibras/*` devuelve `null` y pasa de largo, así que el cambio de ruta NO afecta a 11.2. Orden final del pipeline en `Program.cs`:

```text
WwwToNonWwwMiddleware → FibraSlugRedirectMiddleware → SpaMetadataMiddleware → (HTTPS redirect) → UseDefaultFiles → ...
```

El 301 debe resolverse ANTES de servir HTML: si `SpaMetadataMiddleware` respondiera primero, Google indexaría la URL vieja con contenido 200.

### Prioridades de las rutas estáticas
Las prioridades SEO están basadas en el valor de cada ruta para el sitio:
- `/` — Home, máxima prioridad (1.0)
- `/calculadora` — Quick win SEO (posición 4.3 en GSC), prioridad alta (0.9)
- `/fibras/{slug}` — Páginas de contenido principal (0.8)
- `/catalogo`, `/comparar`, `/fundamentales`, `/calendario`, `/noticias` — Herramientas importantes (0.7)
- `/conoce-las-fibras` — Contenido educativo, cambia poco (0.6)

### IFibraRepository.GetAllActiveAsync
Este método ya existe en `Application.Catalog.IFibraRepository`. Solo devuelve FIBRAs con `State != Disabled`. El endpoint del sitemap solo necesita el ticker de cada FIBRA:

```csharp
var fibras = await fibraRepo.GetAllActiveAsync(ct);
// fibras es IReadOnlyList<Fibra> — usa fibra.Ticker
```

### Rutas a excluir (NO incluir en sitemap)
- `/portafolio`, `/oportunidades`, `/perfil` — privadas, requieren auth
- `/login` — página de autenticación
- `/privacidad` — baja prioridad
- `/ops/*` — Centro de Procesos, AdminOps only
- `/api/*` — API endpoints
- `/hangfire/*` — Dashboard de jobs

### Caché del sitemap
El sitemap no necesita ser dinámico en cada request — la lista de FIBRAs activas cambia raramente. Considerar agregar `IMemoryCache` con expiry de 1 hora. PERO para MVP es aceptable consultar la BD en cada request (el endpoint es raramente llamado). Dejarlo sin caché en esta historia; si hay problemas de performance, agregar caché en deferred work.

### Nota sobre BaseUrl en appsettings
Si la historia 11-2 ya agregó `App:BaseUrl` a `appsettings.json`, este Task 1 es verificar que existe, no crearlo nuevamente.

## Dev Agent Record

### Archivos Creados/Modificados

**Backend — nuevos:**
- `src/Server/Application/Catalog/FibraSlug.cs` — helper `Build(fullName, ticker)` (slug canónico)
- `src/Server/Api/Endpoints/Public/SeoEndpoints.cs` — `/sitemap.xml` + `/robots.txt` con builders públicos testeables
- `src/Server/Api/Middleware/FibraSlugRedirectMiddleware.cs` — 301 a URL slug canónica

**Backend — modificados:**
- `src/Server/Api/Program.cs` — `app.MapSeo()` + `UseMiddleware<FibraSlugRedirectMiddleware>()` (después de WwwToNonWww, antes de SpaMetadata)
- `scripts/codegen/Api.json` — artefacto codegen OpenAPI regenerado (incluye los 2 endpoints nuevos)

**Frontend — nuevos:**
- `src/Web/Main/src/shared/lib/fibra-slug.ts` — `buildFibraSlug` + `extractTickerFromSlug`
- `src/Web/Main/src/shared/hooks/useFibraSlugMap.ts` — ticker→slug desde catálogo cacheado (queryKey `['fibras','all']`)

**Frontend — modificados:**
- `src/Web/Main/src/app/routes.tsx` — `/fibras/:ticker` → `/fibras/:slug`
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` — ticker desde slug, canonicalización `navigate replace`, canonical URL slug + dominio correcto
- `src/Web/Main/src/modules/catalogo/CatalogoPage.tsx` — link con `buildFibraSlug` directo (tiene fullName)
- `src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx`, `src/Web/Main/src/modules/noticia/NoticiaPage.tsx` (conserva `#noticias`), `src/Web/Main/src/modules/calendario/CalendarioPage.tsx`, `src/Web/Main/src/modules/home/PriceCarousel.tsx`, `src/Web/Main/src/modules/home/GainersLosers.tsx` (2 links), `src/Web/Main/src/modules/home/FibraUniverseTable.tsx`, `src/Web/Main/src/modules/home/GlobalSearch.tsx` — links vía `useFibraSlugMap`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx` — menú "Catálogo" → "Fibras" (desktop + móvil)
- `src/Web/Main/scripts/prerender.mjs` — rutas `/fibras/{slug}`; queryKey de initialData sigue por ticker
- `src/Web/Main/scripts/prerender-utils.mjs` — réplica `buildFibraSlug` (node puro no importa .ts)
- `src/Web/Main/scripts/prerender-utils.test.mjs` — canonical de ejemplo con slug + dominio correcto; tests de paridad
- `src/Web/Main/package.json` — `fibra-slug.test.ts` agregado al script `test`
- `src/Web/Main/tests/e2e/public-discovery.spec.ts` — assert de URL actualizado al slug canónico

**Tests — nuevos:**
- `tests/Unit/Application.Tests/Catalog/FibraSlugTests.cs`
- `tests/Unit/Infrastructure.Tests/Endpoints/SeoEndpointsTests.cs`
- `tests/Unit/Infrastructure.Tests/Middleware/FibraSlugRedirectMiddlewareTests.cs`
- `tests/Integration/Api.Tests/SeoEndpointTests.cs`
- `src/Web/Main/src/shared/lib/fibra-slug.test.ts`

### Decisiones Tomadas

1. **Paridad de slugify C# ↔ TS sobre puntuación**: el spec de `SlugGenerator` (11.4) hace espacios→guión + strip de no-alfanuméricos, lo que produce `"S.A."` → `sa`; el TS de esta historia (`[^a-z0-9]+` → `-`) produce `s-a`. Como el requisito duro es paridad exacta (si divergen hay loop de redirecciones), `FibraSlug.Slugify` usa la semántica del TS: runs de no-alfanuméricos colapsan a UN guión. Ambos lados tienen el mismo test de paridad (tabla del catálogo + caso de puntuación). **Nota para 11.4**: al implementar `SlugGenerator`, extraer la normalización común y conservar esta semántica.
2. **Middleware acepta GET y HEAD**: la verificación de T15 usa `curl -I` (HEAD) y los validadores SEO usan HEAD; mismo 301 que GET (consistente con SpaMetadataMiddleware). Test unitario agregado.
3. **`GetByTickerAsync` reutilizado sin filtro de estado** (instrucción de Dev Notes: no agregar método nuevo): una fibra Inactive también redirige a su slug — consistente con `GET /api/v1/fibras/{ticker}` que la sirve.
4. **Builders de sitemap/robots públicos** (`BuildSitemapXml`/`BuildRobotsTxt`): no hay `InternalsVisibleTo` en Api; al ser funciones puras estáticas se exponen públicas para test directo.
5. **`useFibraSlugMap` con queryKey `['fibras','all']`** (el que usan CatalogoPage/GlobalSearch/NoticiasListPage) y staleTime 5 min — dedupe total, cero fetches extra.
6. **Hallazgo pre-existente (no introducido)**: `public-discovery.spec.ts` tests "buscar y navegar" y "360px" fallan también en main sin estos cambios (el label "Top movers" ya no existe tras la reorganización de Home y el combobox no es visible a 360px). Verificado con stash. Candidato a tarea `[Deuda]` en la próxima historia de Main.

### Tests Ejecutados

```
dotnet test tests/Unit/Domain.Tests           →   8/8   verdes
dotnet test tests/Unit/Application.Tests      →  92/92  verdes (incluye 9 FibraSlugTests)
dotnet test tests/Unit/Infrastructure.Tests   → 400/400 verdes (incluye 8 SeoEndpointsTests + 15 FibraSlugRedirectMiddlewareTests)
dotnet test tests/Integration/Api.Tests       → 277/277 verdes (incluye 5 SeoEndpointTests)
npm test --workspace=src/Web/Main             → 115/115 verdes (incluye 9 fibra-slug + 7 prerender-utils)
npx playwright test public-discovery market-freshness → 9 passed, 2 failed PRE-EXISTENTES (fallan igual en main, ver Decisión 6)
```

**Verificación manual (servidor dev, puerto 5265):**
- `GET /sitemap.xml` → 200 `application/xml; charset=utf-8`, XML válido (XDocument), 27 URLs (8 estáticas + 19 fibras slug), BaseUrl de appsettings (CA-7 ✓)
- `GET /robots.txt` → 200 `text/plain; charset=utf-8` con Disallows y referencia al sitemap
- `GET/HEAD /fibras/FUNO11` y `/fibras/funo11` → 301 `Location: /fibras/fibra-uno-funo11`
- `GET /fibras/nombre-viejo-funo11` → 301 al canónico; `GET /fibras/fibra-uno-funo11` → 200 pass-through
- Query string preservado en el 301 (`?utm_source=x`)
- `dotnet build FIBRADIS.slnx` y `npm run build --workspace=src/Web/Main` → 0 errores

### Change Log

- 2026-06-11 — Historia 11.3 implementada completa: sitemap.xml + robots.txt dinámicos, helper FibraSlug (C#/TS con paridad testeada), FibraSlugRedirectMiddleware 301, ruta SPA `/fibras/:slug` con canonicalización client-side, 9 puntos de links internos migrados a slug, prerender con slugs, menú "Catálogo"→"Fibras". Status → review.
- 2026-06-11 — Code review: 11 patches aplicados (ver Review Findings), 3 defers a deferred-work.md, 10 hallazgos descartados como ruido. Suites: 93/93 Application + 405/405 Infrastructure + 282/282 integración + 116/116 frontend. Builds 0 errores. Status → done.

## Senior Developer Review (AI)

**Fecha:** 2026-06-11 · **Reviewer:** Claude (bmad-code-review, 3 capas adversariales: Blind Hunter, Edge Case Hunter, Acceptance Auditor)

**Veredicto:** los 12 CAs cumplen (verificado por el Acceptance Auditor contra el código, no contra el Dev Record). 11 patches aplicados en la misma sesión, 2 de ellos derivados de decisiones de Jorge (`/herramientas` al sitemap; validación de charset de ticker en Ops). Detalle y evidencia en "Review Findings" arriba.

**Lo más relevante:**
- **Alta:** crash de `FibraPage` con slug terminado en guión (ticker extraído vacío → render con `fibra!` undefined). Corregido con guard.
- CA-1 estaba violado silenciosamente: el XML emitía `<priority>` antes de `<changefreq>` (orden inválido según el XSD de sitemaps.org) y los unit tests blindaban el orden incorrecto.
- La paridad slugify C#↔TS (anti-patrón ya conocido de mem0) estaba resuelta para puntuación pero no para marcas combinantes fuera de U+0300–036F; unificada con `\p{Mn}`.

**Action Items (defers):** 3 en `deferred-work.md` bajo "code review of 11-3-sitemap-xml-y-robots-txt (2026-06-11)" — caché del lookup ticker→slug, destino del prerender legacy (drift seed/BD + append-slash de UseDefaultFiles + fichas sin metadata server-side), consolidación de la triple implementación del slugify.
