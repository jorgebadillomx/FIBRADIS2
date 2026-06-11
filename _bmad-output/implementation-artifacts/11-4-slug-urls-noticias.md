# Historia 11.4: Slug URLs y Metadata Dinámica para Noticias

Status: ready-for-dev

## Historia

Como SEO lead,
quiero que cada artículo de noticias tenga una URL legible basada en el título (`/noticias/funo11-reporta-resultados-2q25`) y que esa página entregue `<title>`, `<meta description>`, OG tags y JSON-LD en el HTML inicial sin ejecutar JavaScript,
para que Google indexe correctamente el contenido de noticias y lo muestre con rich snippets en los resultados de búsqueda.

## Criterios de Aceptación

**CA-1: Slug generado automáticamente en artículos nuevos**
Dado que se ingesta un artículo con título "FUNO11 reporta resultados del 2T25",
Entonces `NewsArticle.Slug` queda como `"funo11-reporta-resultados-del-2t25"`.
Si ya existe ese slug, queda como `"funo11-reporta-resultados-del-2t25-2"`.

**CA-2: Endpoint por slug**
Dado que hago `GET /api/v1/news/funo11-reporta-resultados-del-2t25`,
Entonces la respuesta es 200 con el artículo completo (incluye `slug` en el DTO).
Dado que hago `GET /api/v1/news/slug-inexistente`,
Entonces la respuesta es 404.

**CA-3: Endpoint por GUID sigue funcionando**
Dado que hago `GET /api/v1/news/{id:guid}` con un ID válido,
Entonces la respuesta sigue siendo 200 con el artículo (retrocompatibilidad).

**CA-4: Frontend usa slug en los links**
Dado que abro `/noticias` (listado),
Entonces cada card enlaza a `/noticias/{slug}` (no GUID).
Dado que hay artículos sin slug aún (backlog no backfillado), los links usan `slug ?? id` como fallback.

**CA-5: NoticiaPage por slug**
Dado que navego a `/noticias/funo11-reporta-resultados-del-2t25`,
Entonces el componente carga y muestra el artículo.
Dado que navego a `/noticias/{guid}` (enlace antiguo),
Entonces el componente hace redirect 301 del lado cliente a `/noticias/{slug}` si el artículo tiene slug.

**CA-6: Metadata SSR en /noticias/:slug**
Dado que hago `GET /noticias/funo11-reporta-resultados-del-2t25` sin JavaScript,
Entonces el HTML inicial incluye en `<head>`:
- `<title>{headline ?? title} — Noticias | FIBRADIS</title>`
- `<meta name="description" content="{snippet o summaryMarkdown, 120–160 chars}">` 
- `<link rel="canonical" href="https://fibrasinmobiliarias.com/noticias/{slug}">`
- `<meta property="og:title" ...>`
- `<meta property="og:description" ...>`
- `<meta property="og:type" content="article">`
- `<meta property="og:url" content="https://fibrasinmobiliarias.com/noticias/{slug}">`
- `<meta property="og:image" ...>` (si `ImageUrl` no es null)
- `<script type="application/ld+json">` con schema `NewsArticle`

**CA-7: Rutas sin slug no son afectadas**
Dado que hago `GET /noticias` (listado) sin JavaScript,
Entonces el middleware pasa sin modificar (el listado ya tiene metadata propia en el componente React).

**CA-8: Assets y rutas de API no interceptadas**
Dado que hago `GET /assets/index.js` o `GET /api/v1/news/...`,
Entonces el `NewsMetadataMiddleware` no intercepta.

**CA-9: Backfill de artículos existentes**
Dado que existen artículos sin `slug` en la BD,
Entonces el endpoint `POST /api/v1/ops/news/backfill-slugs` (AdminOps) genera slugs para todos ellos y retorna `{ "count": N }`.

**CA-10: Artículos incluidos en sitemap (hook para 11.3)**
Dado que se implementa `SeoEndpoints.cs` (historia 11.3),
Entonces el repositorio expone `GetArticlesForSitemapAsync` y el sitemap incluye `/noticias/{slug}` para los N artículos publicados más recientes (max 500), con `priority: 0.6` y `changefreq: daily`.

**CA-11: Unit tests**
- `SlugGeneratorTests`: slug básico, tildes/ñ, colisión con sufijo -2, título vacío, título muy largo.
- `GetBySlugAsyncTests`: happy path, slug inexistente, null/vacío.
- `NewsMetadataMiddlewareTests`: metadata inyectada correctamente, ruta sin artículo pasa (no crash), asset no interceptado.

## Tareas / Subtasks

### Tarea 1 — Dominio: `SlugGenerator` + columna `Slug` en `NewsArticle` (AC: 1, 9)

- [ ] T1.1 Crear `src/Server/Application/News/SlugGenerator.cs` (ver Dev Notes §Algoritmo slug)
- [ ] T1.2 Agregar propiedad `public string? Slug { get; set; }` a `src/Server/Domain/News/NewsArticle.cs`
- [ ] T1.3 Actualizar `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleConfiguration.cs`:
  ```csharp
  builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(256);
  builder.HasIndex(x => x.Slug)
      .IsUnique()
      .HasFilter("[slug] IS NOT NULL")  // SQL Server: filtered unique index
      .HasDatabaseName("IX_NewsArticle_Slug");
  ```
- [ ] T1.4 Crear migración EF Core:
  ```bash
  dotnet ef migrations add AddNewsArticleSlug \
    --project src/Server/Infrastructure \
    --startup-project src/Server/Api
  ```
  Si los DLLs están bloqueados, agregar `--configuration Release` (convención del proyecto).
- [ ] T1.5 Ejecutar migración en BD local:
  ```bash
  dotnet ef database update \
    --project src/Server/Infrastructure \
    --startup-project src/Server/Api
  ```

### Tarea 2 — Repositorio: métodos de slug (AC: 1, 2, 9, 10)

- [ ] T2.1 Agregar a `INewsRepository`:
  ```csharp
  Task<NewsArticle?> GetBySlugAsync(string slug, CancellationToken ct = default);
  Task<string> GenerateUniqueSlugAsync(string title, Guid? excludeId = null, CancellationToken ct = default);
  Task<IReadOnlyList<NewsArticle>> GetArticlesWithoutSlugAsync(int batchSize, CancellationToken ct = default);
  Task UpdateSlugAsync(Guid id, string slug, CancellationToken ct = default);
  Task<IReadOnlyList<(string Slug, DateTimeOffset PublishedAt)>> GetArticlesForSitemapAsync(int limit, CancellationToken ct = default);
  ```
- [ ] T2.2 Implementar en `NewsRepository.cs`:
  - `GetBySlugAsync`: `db.NewsArticles.FirstOrDefaultAsync(n => n.Slug == slug && n.DeletedAt == null, ct)`
  - `GenerateUniqueSlugAsync`: genera base slug con `SlugGenerator.Generate(title)`, luego en loop verifica unicidad (ver Dev Notes §Unicidad)
  - `GetArticlesWithoutSlugAsync`: `db.NewsArticles.Where(n => n.Slug == null && n.DeletedAt == null).Take(batchSize).ToListAsync(ct)`
  - `UpdateSlugAsync`: `ExecuteUpdateAsync` por ID
  - `GetArticlesForSitemapAsync`: `db.NewsArticles.Where(n => n.Slug != null && n.Status == NewsArticleStatus.Processed && n.DeletedAt == null).OrderByDescending(n => n.PublishedAt).Take(limit).Select(n => new { n.Slug!, n.PublishedAt }).ToListAsync(ct)` (proyección anónima → tuple)
- [ ] T2.3 Modificar `AddWithLinksAsync` para auto-generar slug antes de `db.SaveChangesAsync`:
  ```csharp
  article.Slug = await GenerateUniqueSlugAsync(article.Title, ct: ct);
  ```
- [ ] T2.4 Unit tests en nuevo archivo `tests/Unit/Infrastructure.Tests/Persistence/Repositories/NewsRepositorySlugTests.cs`:
  - `GetBySlugAsync_HappyPath_ReturnsArticle`
  - `GetBySlugAsync_SlugNotFound_ReturnsNull`
  - `GetBySlugAsync_DeletedArticle_ReturnsNull`
  - `GenerateUniqueSlugAsync_NoDuplicate_ReturnsFreshSlug`
  - `GenerateUniqueSlugAsync_DuplicateExists_ReturnsSuffixedSlug`

### Tarea 3 — DTO + Backend endpoints (AC: 2, 3, 9)

- [ ] T3.1 Actualizar `src/Server/SharedApiContracts/News/NewsArticleDto.cs`:
  ```csharp
  public sealed record NewsArticleDto(
      Guid Id,
      string Title,
      string Source,
      DateTimeOffset PublishedAt,
      string Url,
      string? Snippet,
      string? ImageUrl,
      string? AiSummary,
      NewsAiAnalysisDto? AiAnalysis,
      IReadOnlyList<LinkedFibraDto>? LinkedFibras = null,
      string? Slug = null   // NUEVO — último parámetro opcional para retro-compat
  );
  ```
- [ ] T3.2 Actualizar todos los `ToDto` / `ToDtoWithFibras` / `ToDtoWithTickerNames` en `NewsEndpoints.cs` para incluir `article.Slug`.
- [ ] T3.3 Agregar endpoint por slug en `NewsEndpoints.cs` (ANTES del endpoint `{id:guid}/related` y DESPUÉS de `/paged`):
  ```csharp
  group.MapGet("/{slug}", async (
      string slug,
      INewsRepository newsRepo,
      CancellationToken ct) =>
  {
      var article = await newsRepo.GetBySlugAsync(slug, ct);
      if (article is null) return Results.NotFound();
      var fibras = await newsRepo.GetLinkedFibrasAsync(article.Id, ct);
      return Results.Ok(ToDtoWithFibras(article, fibras));
  })
  .AllowAnonymous()
  .Produces<NewsArticleDto>(StatusCodes.Status200OK)
  .Produces(StatusCodes.Status404NotFound);
  ```
  **NOTA**: ASP.NET Core resuelve `/{id:guid}` (con constraint) ANTES que `/{slug}` (sin constraint). Los paths literales (`/paged`, `/fibras/`, etc.) tienen prioridad sobre ambos. No hay ambigüedad de routing.
- [ ] T3.4 Crear `src/Server/Api/Endpoints/Ops/OpsNewsManagementEndpoints.cs` con endpoint de backfill:
  ```csharp
  public static IEndpointRouteBuilder MapOpsNewsManagement(this IEndpointRouteBuilder app)
  {
      var group = app.MapGroup("/api/v1/ops/news").WithTags("OpsNewsManagement");
      
      group.MapPost("/backfill-slugs", async (
          INewsRepository newsRepo,
          CancellationToken ct) =>
      {
          const int BatchSize = 100;
          var count = 0;
          List<NewsArticle> batch;
          do
          {
              batch = (await newsRepo.GetArticlesWithoutSlugAsync(BatchSize, ct)).ToList();
              foreach (var article in batch)
              {
                  var slug = await newsRepo.GenerateUniqueSlugAsync(article.Title, article.Id, ct);
                  await newsRepo.UpdateSlugAsync(article.Id, slug, ct);
                  count++;
              }
          } while (batch.Count == BatchSize);
          
          return Results.Ok(new { count });
      })
      .RequireAuthorization("AdminOps")
      .Produces<object>(StatusCodes.Status200OK);
      
      return app;
  }
  ```
- [ ] T3.5 Registrar en `Program.cs`: agregar `app.MapOpsNewsManagement();` junto al resto de `app.MapXxx()`.
- [ ] T3.6 Verificar build: `dotnet build FIBRADIS.slnx` — 0 errores.

### Tarea 4 — Frontend: cliente API + routing (AC: 4, 5)

- [ ] T4.1 Regenerar cliente API tipado: `npm run codegen:api` desde raíz del repo.
- [ ] T4.2 Agregar `fetchArticleBySlug` en `src/Web/Main/src/api/newsApi.ts`:
  ```typescript
  export async function fetchArticleBySlug(slug: string) {
    const apiClient = getApiClient()
    const { data, error, response } = await apiClient.GET('/api/v1/news/{slug}', {
      params: { path: { slug } },
    })
    if (response.status === 404) return null
    if (error) throw new Error(`Error al obtener artículo: ${JSON.stringify(error)}`)
    return data ?? null
  }
  ```
- [ ] T4.3 Actualizar `src/Web/Main/src/app/routes.tsx`: cambiar param de `:id` a `:slug` en la ruta de detalle:
  ```tsx
  { path: '/noticias/:slug', element: <NoticiaPage /> },
  ```
- [ ] T4.4 Reescribir `src/Web/Main/src/modules/noticia/NoticiaPage.tsx` para usar slug (ver Dev Notes §NoticiaPage):
  - Leer `slug` de `useParams<{ slug: string }>()`
  - Si es GUID: `fetchArticleById(slug)`, después del load hacer `navigate('/noticias/' + article.slug, { replace: true })` si `article.slug` existe
  - Si no es GUID: `fetchArticleBySlug(slug)`
  - Emitir `<link rel="canonical" href="/noticias/{article.slug ?? article.id}">` en el render
  - Emitir `<meta property="og:type" content="article">`, `og:url`, `og:image` si `article.imageUrl`
- [ ] T4.5 Actualizar links en los 4 archivos que usan `article.id`:
  - `NoticiasListPage.tsx:174` → `to={\`/noticias/${article.slug ?? article.id}\`}`
  - `NoticiaPage.tsx:240` (RelatedNews) → `to={\`/noticias/${related.slug ?? related.id}\`}`
  - `NoticiasSection.tsx:50` → `to={\`/noticias/${article.slug ?? article.id}\`}`
  - `NewsSection.tsx:62` → `to={\`/noticias/${article.slug ?? article.id}\`}`
- [ ] T4.6 Ejecutar build TypeScript: `npm run build --workspace=src/Web/Main` — 0 errores.

### Tarea 5 — NewsMetadataMiddleware (CA: 6, 7, 8)

- [ ] T5.1 Crear `src/Server/Api/Middleware/NewsMetadataMiddleware.cs` (ver Dev Notes §NewsMetadataMiddleware).
  Constructor: `(RequestDelegate next, IWebHostEnvironment env, IConfiguration config, IServiceScopeFactory scopeFactory)`
  - `scopeFactory` es necesario porque `INewsRepository` es Scoped y el middleware es Singleton.
- [ ] T5.2 En `InvokeAsync`:
  1. Si path tiene extensión (`.js`, `.css`, etc.) → pass-through
  2. Si path empieza con `/api/`, `/ops/`, `/hangfire/` → pass-through
  3. Si path NO empieza con `/noticias/` → pass-through
  4. Extraer el segment después de `/noticias/`: `var identifier = path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1)`
  5. Si `identifier` es null o vacío → pass-through
  6. Si `identifier` == `"paged"` u otras rutas internas (no aplica en frontend) → pass-through
  7. Resolver `INewsRepository` vía scope: `using var scope = scopeFactory.CreateScope(); var repo = scope.ServiceProvider.GetRequiredService<INewsRepository>();`
  8. Detectar si es GUID: `Guid.TryParse(identifier, out var guid)` → usar `GetByIdAsync`; si no → usar `GetBySlugAsync`
  9. Si artículo es null → pass-through (SPA mostrará 404)
  10. Leer `{env.WebRootPath}/index.html` con `File.ReadAllTextAsync`
  11. Construir bloque de metadata (ver Dev Notes §Bloque HTML metadata)
  12. Reemplazar `<!-- prerender-meta -->` (convención de 11.2)
  13. `context.Response.StatusCode = 200; context.Response.ContentType = "text/html; charset=utf-8";`
  14. Escribir HTML modificado y `return`
- [ ] T5.3 Registrar en `Program.cs` DESPUÉS de `WwwToNonWwwMiddleware` y ANTES de `UseDefaultFiles`:
  ```csharp
  app.UseMiddleware<WwwToNonWwwMiddleware>();
  app.UseMiddleware<NewsMetadataMiddleware>();   // <-- aquí
  // Si 11.2 ya está implementada, SpaMetadataMiddleware va aquí también
  if (!app.Environment.IsDevelopment())
      app.UseHttpsRedirection();
  app.UseDefaultFiles();
  ```
- [ ] T5.4 Unit tests en `tests/Unit/Infrastructure.Tests/Middleware/NewsMetadataMiddlewareTests.cs`:
  - `InvokeAsync_NewsSlugPath_InjectsMetadata` — verifica que `<!-- prerender-meta -->` se reemplaza con el bloque
  - `InvokeAsync_AssetPath_PassesThrough` — path `.js` no modifica response
  - `InvokeAsync_SlugNotFound_PassesThrough` — artículo no existe, no lanza excepción
  - `InvokeAsync_ApiPath_PassesThrough` — `/api/v1/...` no interceptado

### Tarea 6 — SlugGenerator unit tests (CA: 11)

- [ ] T6.1 Crear `tests/Unit/Application.Tests/News/SlugGeneratorTests.cs`:
  - `Generate_BasicTitle_ReturnsKebabCase`
  - `Generate_TitleWithTildes_NormalizesAccents` — "FUNO11 noticias: ó, é, á, ñ" → "funo11-noticias-o-e-a-n"
  - `Generate_TitleWithSpecialChars_StripsNonAlphanumeric` — "FIBRA $MXBPO! — 2Q25" → "fibra-mxbpo-2q25"
  - `Generate_VeryLongTitle_TruncatesAt200` — título de 400 chars → slug ≤ 200 chars
  - `Generate_EmptyTitle_ReturnsFallback` — "" → algún valor no vacío (ej. `"noticia"`)

### Tarea 7 — Verificación final

- [ ] T7.1 `dotnet test tests/Unit/` — todos pasan, incluyendo los nuevos
- [ ] T7.2 Verificar en browser: navegar a `/noticias/` → cards con slugs; click → URL cambia a slug; F5 → recarga correctamente con metadata SSR en curl

---

## Dev Notes

### §Algoritmo slug — `SlugGenerator.cs`

Ubicación: `src/Server/Application/News/SlugGenerator.cs`

```csharp
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.News;

public static class SlugGenerator
{
    private static readonly Regex MultiHyphen = new(@"-{2,}", RegexOptions.Compiled);
    private static readonly Regex InvalidChars = new(@"[^a-z0-9-]", RegexOptions.Compiled);

    public static string Generate(string text, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(text)) return "noticia";

        // 1. Normalize: descomponer caracteres Unicode (separa letra de acento)
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            // Descartar marcas de no-espaciado (tildes, diéresis, etc.)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(ch);
        }

        // 2. Recomponer y bajar a minúsculas
        var clean = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // 3. Espacios → guión; eliminar chars no alfanuméricos ni guión
        clean = clean.Replace(' ', '-');
        clean = InvalidChars.Replace(clean, "");
        clean = MultiHyphen.Replace(clean, "-").Trim('-');

        // 4. Truncar respetando el límite sin cortar en guión
        if (clean.Length > maxLength)
        {
            clean = clean[..maxLength].TrimEnd('-');
        }

        return string.IsNullOrEmpty(clean) ? "noticia" : clean;
    }
}
```

### §Unicidad de slugs — `GenerateUniqueSlugAsync`

La lógica de unicidad va en `NewsRepository`:

```csharp
public async Task<string> GenerateUniqueSlugAsync(string title, Guid? excludeId = null, CancellationToken ct = default)
{
    var baseSlug = SlugGenerator.Generate(title);
    var candidate = baseSlug;

    for (var attempt = 2; attempt <= 50; attempt++)
    {
        var exists = await db.NewsArticles.AnyAsync(
            n => n.Slug == candidate && (excludeId == null || n.Id != excludeId), ct);
        if (!exists) return candidate;

        // Recortar base si está muy cerca del límite antes de agregar sufijo
        var trimmedBase = baseSlug.Length > 190 ? baseSlug[..190] : baseSlug;
        candidate = $"{trimmedBase}-{attempt}";
    }

    // Fallback último recurso (no debería ocurrir en práctica)
    return $"{baseSlug[..Math.Min(baseSlug.Length, 180)]}-{Guid.NewGuid():N}"[..200];
}
```

### §Routing ASP.NET Core — por qué no hay ambigüedad

El grupo `/api/v1/news` tiene 4 rutas relevantes después del prefijo:
```
GET /               → últimas noticias (literal vacío)
GET /paged          → paginado (literal "paged" > cualquier parámetro)
GET /fibras/{fibraId:guid}  → por fibra (literal "fibras/" + GUID)
GET /{id:guid}      → por ID GUID (constraint: solo acepta GUIDs)
GET /{id:guid}/related  → relacionadas
GET /{slug}         → por slug (nuevo, sin constraint — captura todo lo que no sea GUID)
```

ASP.NET Core aplica prioridad: **literal > constraint > sin constraint**. `paged` y `fibras` son literales y tienen prioridad. `{id:guid}` tiene constraint y captura GUIDs antes que `{slug}`. Un `slug` válido (no-GUID) cae en `{slug}`.

**Riesgo a verificar**: `GET /api/v1/news/related` (si alguien llama solo `/related` sin GUID) caería en `{slug}` y devolvería 404 (artículo no encontrado). Esto es correcto — esa ruta solo existe como `/api/v1/news/{id:guid}/related`.

### §NoticiaPage — lógica de routing dual

```tsx
const GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export function NoticiaPage() {
  const { slug: rawParam } = useParams<{ slug: string }>()
  const navigate = useNavigate()
  const isGuid = GUID_REGEX.test(rawParam ?? '')

  const { data: article, isLoading, isError } = useQuery({
    queryKey: ['news', 'article', rawParam],
    queryFn: () => isGuid ? fetchArticleById(rawParam!) : fetchArticleBySlug(rawParam!),
    enabled: !!rawParam,
    staleTime: 10 * 60_000,
  })

  // Redirect GUID → slug (backward compat para links antiguos)
  useEffect(() => {
    if (isGuid && article?.slug && !isLoading) {
      navigate(`/noticias/${article.slug}`, { replace: true })
    }
  }, [isGuid, article?.slug, isLoading, navigate])
  
  // ... resto del componente sin cambios
}
```

**OG tags a agregar en el render del artículo cargado** (complementa lo que ya hay con `<title>` y `<meta description>`):
```tsx
<meta property="og:title" content={pageTitle} />
<meta property="og:description" content={pageDescription} />
<meta property="og:type" content="article" />
<meta property="og:url" content={`https://fibrasinmobiliarias.com/noticias/${article.slug ?? article.id}`} />
{article.imageUrl ? <meta property="og:image" content={article.imageUrl} /> : null}
<link rel="canonical" href={`https://fibrasinmobiliarias.com/noticias/${article.slug ?? article.id}`} />
```

Esto complementa la metadata SSR del middleware: para Googlebot (sin JS) viene del middleware; para usuarios normales, React 19 sobreescribe con los valores reales al hidratar.

### §NewsMetadataMiddleware — bloque HTML metadata

El bloque que reemplaza `<!-- prerender-meta -->`:

```csharp
private string BuildMetaBlock(NewsArticle article, string baseUrl)
{
    var aiAnalysis = article.AiAnalysisJson != null
        ? TryDeserializeAnalysis(article.AiAnalysisJson)
        : null;

    var headline = aiAnalysis?.Headline ?? article.Title;
    var title = $"{headline} — Noticias | FIBRADIS";
    var rawDescription = aiAnalysis?.SummaryMarkdown ?? article.Snippet ?? "";
    var description = rawDescription.Length > 160
        ? rawDescription[..157] + "..."
        : rawDescription.Length >= 120
            ? rawDescription
            : (rawDescription + " — Análisis y noticias de FIBRAs inmobiliarias en FIBRADIS.").TrimEnd()[..Math.Min(160, /* ajustado */ rawDescription.Length + 55)];
    
    var canonicalPath = $"{baseUrl}/noticias/{article.Slug}";
    var publishedIso = article.PublishedAt.ToString("o");

    var jsonLd = $$"""
        {
          "@context": "https://schema.org",
          "@type": "NewsArticle",
          "headline": "{{EscapeJson(headline)}}",
          "datePublished": "{{publishedIso}}",
          "author": {"@type": "Organization", "name": "{{EscapeJson(article.Source)}}"},
          "publisher": {"@type": "Organization", "name": "FIBRADIS", "url": "{{baseUrl}}"},
          "url": "{{canonicalPath}}",
          "description": "{{EscapeJson(description)}}"
        }
        """;

    var ogImage = article.ImageUrl != null
        ? $"""<meta property="og:image" content="{WebUtility.HtmlEncode(article.ImageUrl)}" />"""
        : "";

    return $"""
        <title>{WebUtility.HtmlEncode(title)}</title>
        <meta name="description" content="{WebUtility.HtmlEncode(description)}" />
        <link rel="canonical" href="{canonicalPath}" />
        <meta property="og:title" content="{WebUtility.HtmlEncode(headline)}" />
        <meta property="og:description" content="{WebUtility.HtmlEncode(description)}" />
        <meta property="og:type" content="article" />
        <meta property="og:url" content="{canonicalPath}" />
        {ogImage}
        <script type="application/ld+json">{jsonLd}</script>
        """;
}

private static string EscapeJson(string value)
    => value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
```

**Dependencia de `IWebHostEnvironment`**: usar `env.WebRootPath` para localizar `index.html`. En producción, el `index.html` del SPA está en `wwwroot/index.html`. En tests, mock con archivo temporal.

**`IServiceScopeFactory` es obligatorio** porque `INewsRepository` es Scoped y el middleware vive como Singleton. Nunca inyectar IScoped directamente en IMiddleware singleton.

### §Dependencias con historias 11.2 y 11.3

**11.2 (SpaMetadataMiddleware)** — esta historia es INDEPENDIENTE de 11.2. Ambos middlewares coexisten:
- `NewsMetadataMiddleware` maneja `/noticias/{slug}` (dinámico)  
- `SpaMetadataMiddleware` (11.2) maneja `/`, `/calculadora`, etc. (estático)
- Si 11.2 se implementa antes que 11.4: en `Program.cs`, la convención de `<!-- prerender-meta -->` ya existirá en `index.html` — verificar que esté presente antes de T5.
- Si 11.4 se implementa antes que 11.2: agregar `<!-- prerender-meta -->` manualmente a `wwwroot/index.html` en T5. **IMPORTANTE**: 11.2 también necesitará ese comentario, así que documentarlo en el story de 11.2.

**11.3 (Sitemap XML)** — esta historia agrega el método `GetArticlesForSitemapAsync` al repositorio (T2.1), pero la integración en el sitemap la hace 11.3. Si 11.3 se implementa después de 11.4, el endpoint de sitemap en `SeoEndpoints.cs` debe incluir:
```csharp
var newsArticles = await newsRepo.GetArticlesForSitemapAsync(500, ct);
foreach (var (slug, publishedAt) in newsArticles)
{
    urls.Add($"{baseUrl}/noticias/{slug}", "0.6", "daily", publishedAt.ToString("yyyy-MM-dd"));
}
```

### §Checklist de `index.html`

El middleware necesita el comentario `<!-- prerender-meta -->` en `wwwroot/index.html`. Verificar:
```html
<head>
  <meta charset="UTF-8" />
  <!-- prerender-meta -->    ← debe estar aquí
  <meta name="viewport" ... />
  ...
</head>
```

Si no está, agregar al archivo como parte de T5.1 (antes de registrar el middleware).

### §EF Core Migrations — convención del proyecto

No existen archivos de migración en el repo (el schema se aplica via `dotnet ef database update`). El `DesignTimeDbContextFactory` en `src/Server/Infrastructure/DesignTimeDbContextFactory.cs` usa la conexión local `LAPBADIS;Database=FIBRADIS_Dev`.

Si `dotnet ef migrations add` falla por DLLs bloqueados:
```bash
dotnet ef migrations add AddNewsArticleSlug \
  --project src/Server/Infrastructure \
  --startup-project src/Server/Api \
  --configuration Release
```

### §Security Checklist — completar antes del primer commit

- [ ] **TOCTOU backfill**: el endpoint `POST /backfill-slugs` procesa en batches de 100 con `GetArticlesWithoutSlugAsync` → si se llama dos veces en paralelo, los artículos se actualizan dos veces (idempotente: el segundo `GenerateUniqueSlugAsync` usará `excludeId = article.Id`, por lo que el slug generado será el mismo si el primero ya se guardó). **Sin riesgo real**, pero el AdminOps debe saber que es idempotente.
- [ ] **Auth-gating**: el endpoint `POST /backfill-slugs` requiere `[RequireAuthorization("AdminOps")]` — verificar que `"AdminOps"` es el nombre correcto de la política en `ApiServiceExtensions.cs`. Si no, adaptar al nombre correcto.
- [ ] **Denominador cero**: no hay funciones de cálculo financiero en esta historia.
- [ ] **XSS en metadata**: toda metadata HTML se escapa con `WebUtility.HtmlEncode`. Los valores en JSON-LD se escapan con `EscapeJson`. Verificar que no quede interpolación cruda.

### §Project Structure Notes

- `SlugGenerator` va en `src/Server/Application/News/` — es lógica de negocio pura (no dominio, no infraestructura)
- `NewsMetadataMiddleware` va en `src/Server/Api/Middleware/` — igual que `WwwToNonWwwMiddleware`
- `OpsNewsManagementEndpoints.cs` va en `src/Server/Api/Endpoints/Ops/` — consistente con `NewsBlocklistEndpoints.cs`
- Tests del middleware en `tests/Unit/Infrastructure.Tests/Middleware/` (la carpeta ya existe según el git status)
- Tests del slug repository en `tests/Unit/Infrastructure.Tests/Persistence/Repositories/` (misma ubicación que `NewsRepositoryPublicPagedTests.cs`)

### §Referencias

- `NewsArticle.cs` estado actual — sin `Slug`, 21 propiedades: `Id`, `Title`, `TitleNormalized`, `Source`, `PublishedAt`, `Url`, `Snippet`, `BodyText`, `ImageUrl`, `AiSummary`, `AiAnalysisJson`, `Status`, `CapturedAt`, `ErrorReason`, `DeletedAt`, `FibraLinks`
- `NewsArticleConfiguration.cs` — tabla `news.NewsArticle`, columnas snake_case, índice único en `Url` y compuesto en `(TitleNormalized, CapturedAt)`
- `NewsEndpoints.cs` — 4 endpoints existentes: `GET /`, `GET /fibras/{fibraId:guid}`, `GET /paged`, `GET /{id:guid}`, `GET /{id:guid}/related`
- `NoticiaPage.tsx` — actualmente usa `useParams<{ id: string }>()`, llama `fetchArticleById(id!)`, tiene `<title>` y `<meta description>` pero sin OG tags
- Links a `/noticias/${article.id}` en: `NoticiasListPage.tsx:174`, `NoticiaPage.tsx:240`, `NoticiasSection.tsx:50`, `NewsSection.tsx:62`
- Convención `<!-- prerender-meta -->` definida en historia 11.2 (`_bmad-output/implementation-artifacts/11-2-spa-metadata-injection.md`)
- Middleware pipeline actual en `Program.cs`: `WwwToNonWwwMiddleware` → `UseDefaultFiles` → `UseStaticFiles` → `UseRouting` → ...
- `INewsRepository` — 13 métodos existentes; se agregan 5 nuevos en T2.1

---

## Dev Agent Record

### Agent Model Used

_pending_

### Debug Log References

### Completion Notes List

### File List

_Al completar, listar todos los archivos creados/modificados._
