# Historia 11.3: Sitemap XML, robots.txt y URLs slug para fichas de FIBRA

Status: ready-for-dev

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

- [ ] Task 1: Crear `SeoEndpoints.cs`
  - [ ] Crear `src/Server/Api/Endpoints/Public/SeoEndpoints.cs`
  - [ ] Definir clase estática `SeoEndpoints` con método de extensión `MapSeo(this IEndpointRouteBuilder app)`
  - [ ] Dentro de `MapSeo`, registrar:
    - `app.MapGet("/sitemap.xml", ...)`
    - `app.MapGet("/robots.txt", ...)`
  - [ ] Ambos endpoints con `.AllowAnonymous()`
  - [ ] Ambos endpoints sin autenticación ni autorización

- [ ] Task 2: Implementar endpoint `/sitemap.xml`
  - [ ] El handler recibe `IFibraRepository fibraRepo, IConfiguration config, CancellationToken ct`
  - [ ] Leer `App:BaseUrl` de `config` (fallback: `"https://fibrasinmobiliarias.com"`)
  - [ ] Definir lista de rutas estáticas con su prioridad y changefreq (ver CA-4)
  - [ ] Llamar `await fibraRepo.GetAllActiveAsync(ct)` para obtener FIBRAs activas
  - [ ] Para cada fibra, la URL es `{baseUrl}/fibras/{FibraSlug.Build(fibra.FullName, fibra.Ticker)}` (helper de Task 9)
  - [ ] Construir XML usando `System.Text` / string builder o `XmlWriter` (ver spec en Dev Notes)
  - [ ] Retornar `Results.Content(xmlContent, "application/xml; charset=utf-8")`

- [ ] Task 3: Implementar endpoint `/robots.txt`
  - [ ] El handler recibe `IConfiguration config`
  - [ ] Leer `App:BaseUrl` de `config`
  - [ ] Retornar `Results.Content(robotsContent, "text/plain; charset=utf-8")`
  - [ ] Formato exacto (respetando newlines Unix):
    ```
    User-agent: *
    Allow: /
    Disallow: /ops/
    Disallow: /api/
    Disallow: /hangfire/
    
    Sitemap: {baseUrl}/sitemap.xml
    ```

- [ ] Task 4: Registrar endpoints en `Program.cs`
  - [ ] Agregar `using Api.Endpoints.Public;` si no está presente
  - [ ] Agregar `app.MapSeo();` junto al resto de `app.MapXxx()` calls
  - [ ] Agregar `app.MapFallback("/sitemap.xml", ...)` NO — el endpoint se registra directamente, no necesita fallback

- [ ] Task 5: Unit tests para generación del XML
  - [ ] Archivo: agregar tests al proyecto unit test existente (Infrastructure.Tests o Api.Tests)
  - [ ] `SitemapContainsCalculadora_WithPriority09()` — verifica que `/calculadora` tiene priority 0.9
  - [ ] `SitemapContainsAllStaticRoutes()` — verifica que `/`, `/catalogo`, etc. están presentes
  - [ ] `SitemapContainsFibraSlugUrls()` — verifica que fibras activas se incluyen como `/fibras/{slug}` (ej. `fibra-uno-funo11`) y NO como `/fibras/{ticker}`
  - [ ] `RobotsTxtContainsDisallowOps()` — verifica que `/ops/` está en Disallow
  - [ ] `RobotsTxtContainsSitemapUrl()` — verifica que incluye la URL del sitemap

- [ ] Task 6: Integration test para los endpoints (opcional pero recomendado)
  - [ ] En `tests/Integration/Api.Tests/` agregar test que hace GET `/sitemap.xml` y verifica Content-Type y status 200
  - [ ] Test para GET `/robots.txt` con mismo patrón

- [ ] Task 7: Renombrar menú "Catálogo" → "Fibras" en `PublicLayout.tsx`
  - [ ] En `src/Web/Main/src/shared/layouts/PublicLayout.tsx`, cambiar el label en la **nav de escritorio** (línea ~104):
    ```tsx
    // ANTES
    <Link to="/catalogo" ...>Catálogo</Link>
    // DESPUÉS
    <Link to="/catalogo" ...>Fibras</Link>
    ```
  - [ ] Cambiar el mismo label en la **nav móvil** (Dialog, línea ~219):
    ```tsx
    // ANTES
    <Link to="/catalogo" ...>Catálogo</Link>
    // DESPUÉS
    <Link to="/catalogo" ...>Fibras</Link>
    ```
  - [ ] La ruta `/catalogo` y el componente `CatalogoPage` NO cambian — solo el texto visible en el menú
  - [ ] Ejecutar `npm run build --workspace=src/Web/Main` y verificar 0 errores TypeScript

- [ ] Task 8: Verificación backend
  - [ ] `dotnet build FIBRADIS.slnx` — 0 errores
  - [ ] Ejecutar el servidor en dev y hacer `curl http://localhost:5000/sitemap.xml`
  - [ ] Verificar que el XML es válido (puede copiarse a https://www.xml-sitemaps.com/validate-xml-sitemap.html para verificación manual)

- [ ] Task 9: Backend — helper `FibraSlug` (CA: 3, 9)
  - [ ] Crear `src/Server/Application/Catalog/FibraSlug.cs` con método estático `Build(string fullName, string ticker)` (ver Dev Notes §Formato del slug)
  - [ ] Unit tests en `tests/Unit/Application.Tests/Catalog/FibraSlugTests.cs`:
    - `Build_BasicName_ReturnsKebabWithTickerSuffix` — ("Fibra Uno", "FUNO11") → `"fibra-uno-funo11"`
    - `Build_NameWithAccents_NormalizesAccents` — tildes/ñ eliminadas
    - `Build_NameWithSpecialChars_StripsNonAlphanumeric`
    - `Build_EmptyName_ReturnsTickerOnly` — ("", "FUNO11") → `"funo11"`

- [ ] Task 10: Backend — `FibraSlugRedirectMiddleware` 301 (CA: 9)
  - [ ] Crear `src/Server/Api/Middleware/FibraSlugRedirectMiddleware.cs` (ver Dev Notes §Middleware de redirección 301)
  - [ ] Constructor: `(RequestDelegate next, IServiceScopeFactory scopeFactory)` — `IFibraRepository` es Scoped, el middleware es Singleton
  - [ ] Lógica: solo GET, solo paths `/fibras/{algo}` sin extensión; extraer ticker del último segmento del slug; si la fibra existe y el path actual ≠ slug canónico → `Results`/301 con `Location` al canónico; si no existe o ya es canónico → pass-through
  - [ ] Registrar en `Program.cs` DESPUÉS de `WwwToNonWwwMiddleware` y ANTES de `SpaMetadataMiddleware` (11.2)
  - [ ] Unit tests en `tests/Unit/Infrastructure.Tests/Middleware/FibraSlugRedirectMiddlewareTests.cs`:
    - `InvokeAsync_BareTicker_Redirects301ToSlug`
    - `InvokeAsync_LowercaseTicker_Redirects301ToSlug`
    - `InvokeAsync_CanonicalSlug_PassesThrough`
    - `InvokeAsync_StaleSlugValidTicker_Redirects301ToCanonical`
    - `InvokeAsync_UnknownTicker_PassesThrough` (la SPA muestra FibraNotFound)
    - `InvokeAsync_AssetOrApiPath_PassesThrough`

- [ ] Task 11: Frontend — util `fibra-slug.ts` (CA: 8, 10, 11)
  - [ ] Crear `src/Web/Main/src/shared/lib/fibra-slug.ts` con `buildFibraSlug(fullName, ticker)` y `extractTickerFromSlug(param)` (ver Dev Notes §Helper TypeScript)
  - [ ] Unit tests (vitest) `fibra-slug.test.ts`: build básico, acentos, extracción desde slug completo, extracción desde ticker pelado (sin guiones), param vacío

- [ ] Task 12: Frontend — ruta y `FibraPage` (CA: 8, 10)
  - [ ] En `src/Web/Main/src/app/routes.tsx` cambiar `{ path: '/fibras/:ticker', ... }` → `{ path: '/fibras/:slug', ... }`
  - [ ] En `FibraPage.tsx`: leer `slug` de `useParams`, derivar `ticker = extractTickerFromSlug(slug)` (uppercase) y conservar TODAS las queries existentes por ticker (queryKeys `['fibra', ticker]`, `['fibra-history', ticker, ...]`, etc. NO cambian)
  - [ ] Al cargar la fibra: si `slug !== buildFibraSlug(fibra.fullName, fibra.ticker)` → `navigate('/fibras/' + slugCanonico, { replace: true })`
  - [ ] Corregir `canonicalUrl` (línea ~148): `https://fibrasinmobiliarias.com/fibras/${slugCanonico}` (hoy usa dominio viejo `fibradis.mx` y ticker)
  - [ ] `FibraNotFound` recibe el ticker extraído

- [ ] Task 13: Frontend — links internos a slug (CA: 11)
  - [ ] Crear hook `src/Web/Main/src/shared/hooks/useFibraSlugMap.ts`: `useQuery` sobre `fetchAllFibras()` (reusar queryKey del catálogo para dedupe) → expone `slugFor(ticker): string` con fallback `ticker.toLowerCase()` mientras carga
  - [ ] Actualizar los 9 puntos que generan links `/fibras/${ticker}`:
    - `CatalogoPage.tsx:180` — tiene `fibra.fullName` directo → `buildFibraSlug` sin hook
    - `FundamentalesPage.tsx:148`, `NoticiaPage.tsx:116` (conserva `#noticias`), `CalendarioPage.tsx:182`, `PriceCarousel.tsx:105`, `GainersLosers.tsx:59,87`, `FibraUniverseTable.tsx:157`, `GlobalSearch.tsx:34` — usar `useFibraSlugMap`
  - [ ] `npm run build --workspace=src/Web/Main` — 0 errores TypeScript

- [ ] Task 14: Prerender con slugs (CA: 12)
  - [ ] En `src/Web/Main/scripts/prerender.mjs`: cambiar `url: \`/fibras/${f.ticker}\`` por la URL slug construida desde `f.fullName` + `f.ticker` (replicar la lógica de `buildFibraSlug` o importarla); el queryKey de `initialData` sigue siendo `['fibra', f.ticker]` (el ticker extraído en `FibraPage` queda en mayúsculas)
  - [ ] Actualizar `prerender-utils.test.mjs:9` (canonical de ejemplo con slug y dominio `fibrasinmobiliarias.com`)

- [ ] Task 15: Verificación final del cambio de ruta
  - [ ] `dotnet test tests/Unit/` — todos verdes incluyendo los nuevos
  - [ ] `npm test --workspace=src/Web/Main` — todos verdes
  - [ ] Manual: navegar a `/fibras/FUNO11` → URL cambia a `/fibras/fibra-uno-funo11` y la ficha carga; `curl -I http://localhost:5000/fibras/FUNO11` → 301 con Location slug
  - [ ] E2E: los specs existentes usan `page.goto('/fibras/FUNO11')` — siguen funcionando vía canonicalización client-side; si algún spec assertea la URL, actualizarlo

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

_(A completar durante la implementación)_

### Archivos Creados/Modificados
- (pendiente)

### Decisiones Tomadas
- (pendiente)

### Tests Ejecutados
- (pendiente)

## Senior Developer Review (AI)

_(A completar durante el code review)_
