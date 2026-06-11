# Historia 11.3: Sitemap XML y robots.txt

Status: backlog

## Historia

Como SEO lead,
quiero que el sitio sirva un `sitemap.xml` dinámico con todas las rutas públicas indexables y un `robots.txt` que lo referencie,
para que Google descubra y rastree eficientemente todas las páginas del sitio — incluyendo `/calculadora` con prioridad alta — y deje de desperdiciar crawl budget en rutas privadas (`/ops/`, `/api/`).

## Criterios de Aceptación

**CA-1: Endpoint /sitemap.xml responde 200 con XML válido**
Dado que hago GET `/sitemap.xml`,
Entonces la respuesta tiene status 200, `Content-Type: application/xml; charset=utf-8`, y el XML es un sitemap válido según el schema `http://www.sitemaps.org/schemas/sitemap/0.9`.

**CA-2: /calculadora incluida con prioridad 0.9**
Dado que proceso el sitemap,
Entonces existe una entrada `<url>` con `<loc>https://fibrasinmobiliarias.com/calculadora</loc>`, `<priority>0.9</priority>`, `<changefreq>daily</changefreq>`.

**CA-3: FIBRAs activas incluidas dinámicamente**
Dado que hay N FIBRAs activas en el catálogo,
Entonces el sitemap incluye N entradas `/fibras/{ticker}` (en minúsculas), cada una con `<priority>0.8</priority>` y `<changefreq>weekly</changefreq>`.

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
  - [ ] `SitemapContainsFibraUrls()` — verifica que tickers activos se incluyen como `/fibras/{ticker}`
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

// FIBRAs dinámicas
foreach (var fibra in fibras)
{
    sb.AppendLine(BuildUrlEntry($"{baseUrl}/fibras/{fibra.Ticker.ToLowerInvariant()}", "0.8", "weekly"));
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

### Ticker en minúsculas
Los tickers en la base de datos están en mayúsculas (FUNO11, FIBRAMQ12, etc.). Para las URLs del sitemap usar `ticker.ToLowerInvariant()` para consistencia y mejores prácticas SEO (URLs en minúsculas).

### Prioridades de las rutas estáticas
Las prioridades SEO están basadas en el valor de cada ruta para el sitio:
- `/` — Home, máxima prioridad (1.0)
- `/calculadora` — Quick win SEO (posición 4.3 en GSC), prioridad alta (0.9)
- `/fibras/{ticker}` — Páginas de contenido principal (0.8)
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
