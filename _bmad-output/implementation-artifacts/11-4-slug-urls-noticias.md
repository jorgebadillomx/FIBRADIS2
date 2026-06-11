# Historia 11.4: Slug URLs y Metadata Dinámica para Noticias

Status: done

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

- [x] T1.1 Crear `src/Server/Application/News/SlugGenerator.cs` (ver Dev Notes §Algoritmo slug)
- [x] T1.2 Agregar propiedad `public string? Slug { get; set; }` a `src/Server/Domain/News/NewsArticle.cs`
- [x] T1.3 Actualizar `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleConfiguration.cs`:
  ```csharp
  builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(256);
  builder.HasIndex(x => x.Slug)
      .IsUnique()
      .HasFilter("[slug] IS NOT NULL")  // SQL Server: filtered unique index
      .HasDatabaseName("IX_NewsArticle_Slug");
  ```
- [x] T1.4 Crear migración EF Core:
  ```bash
  dotnet ef migrations add AddNewsArticleSlug \
    --project src/Server/Infrastructure \
    --startup-project src/Server/Api
  ```
  Si los DLLs están bloqueados, agregar `--configuration Release` (convención del proyecto).
- [x] T1.5 Ejecutar migración en BD local:
  ```bash
  dotnet ef database update \
    --project src/Server/Infrastructure \
    --startup-project src/Server/Api
  ```

### Tarea 2 — Repositorio: métodos de slug (AC: 1, 2, 9, 10)

- [x] T2.1 Agregar a `INewsRepository`:
  ```csharp
  Task<NewsArticle?> GetBySlugAsync(string slug, CancellationToken ct = default);
  Task<string> GenerateUniqueSlugAsync(string title, Guid? excludeId = null, CancellationToken ct = default);
  Task<IReadOnlyList<NewsArticle>> GetArticlesWithoutSlugAsync(int batchSize, CancellationToken ct = default);
  Task UpdateSlugAsync(Guid id, string slug, CancellationToken ct = default);
  Task<IReadOnlyList<(string Slug, DateTimeOffset PublishedAt)>> GetArticlesForSitemapAsync(int limit, CancellationToken ct = default);
  ```
- [x] T2.2 Implementar en `NewsRepository.cs`:
  - `GetBySlugAsync`: `db.NewsArticles.FirstOrDefaultAsync(n => n.Slug == slug && n.DeletedAt == null, ct)`
  - `GenerateUniqueSlugAsync`: genera base slug con `SlugGenerator.Generate(title)`, luego en loop verifica unicidad (ver Dev Notes §Unicidad)
  - `GetArticlesWithoutSlugAsync`: `db.NewsArticles.Where(n => n.Slug == null && n.DeletedAt == null).Take(batchSize).ToListAsync(ct)`
  - `UpdateSlugAsync`: `ExecuteUpdateAsync` por ID
  - `GetArticlesForSitemapAsync`: `db.NewsArticles.Where(n => n.Slug != null && n.Status == NewsArticleStatus.Processed && n.DeletedAt == null).OrderByDescending(n => n.PublishedAt).Take(limit).Select(n => new { n.Slug!, n.PublishedAt }).ToListAsync(ct)` (proyección anónima → tuple)
- [x] T2.3 Modificar `AddWithLinksAsync` para auto-generar slug antes de `db.SaveChangesAsync`:
  ```csharp
  article.Slug = await GenerateUniqueSlugAsync(article.Title, ct: ct);
  ```
- [x] T2.4 Unit tests en nuevo archivo `tests/Unit/Infrastructure.Tests/Persistence/Repositories/NewsRepositorySlugTests.cs`:
  - `GetBySlugAsync_HappyPath_ReturnsArticle`
  - `GetBySlugAsync_SlugNotFound_ReturnsNull`
  - `GetBySlugAsync_DeletedArticle_ReturnsNull`
  - `GenerateUniqueSlugAsync_NoDuplicate_ReturnsFreshSlug`
  - `GenerateUniqueSlugAsync_DuplicateExists_ReturnsSuffixedSlug`

### Tarea 3 — DTO + Backend endpoints (AC: 2, 3, 9)

- [x] T3.1 Actualizar `src/Server/SharedApiContracts/News/NewsArticleDto.cs`:
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
- [x] T3.2 Actualizar todos los `ToDto` / `ToDtoWithFibras` / `ToDtoWithTickerNames` en `NewsEndpoints.cs` para incluir `article.Slug`.
- [x] T3.3 Agregar endpoint por slug en `NewsEndpoints.cs` (ANTES del endpoint `{id:guid}/related` y DESPUÉS de `/paged`):
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
- [x] T3.4 Crear `src/Server/Api/Endpoints/Ops/OpsNewsManagementEndpoints.cs` con endpoint de backfill:
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
- [x] T3.5 Registrar en `Program.cs`: agregar `app.MapOpsNewsManagement();` junto al resto de `app.MapXxx()`.
- [x] T3.6 Verificar build: `dotnet build FIBRADIS.slnx` — 0 errores.

### Tarea 4 — Frontend: cliente API + routing (AC: 4, 5)

- [x] T4.1 Regenerar cliente API tipado: `npm run codegen:api` desde raíz del repo.
- [x] T4.2 Agregar `fetchArticleBySlug` en `src/Web/Main/src/api/newsApi.ts`:
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
- [x] T4.3 Actualizar `src/Web/Main/src/app/routes.tsx`: cambiar param de `:id` a `:slug` en la ruta de detalle:
  ```tsx
  { path: '/noticias/:slug', element: <NoticiaPage /> },
  ```
- [x] T4.4 Reescribir `src/Web/Main/src/modules/noticia/NoticiaPage.tsx` para usar slug (ver Dev Notes §NoticiaPage):
  - Leer `slug` de `useParams<{ slug: string }>()`
  - Si es GUID: `fetchArticleById(slug)`, después del load hacer `navigate('/noticias/' + article.slug, { replace: true })` si `article.slug` existe
  - Si no es GUID: `fetchArticleBySlug(slug)`
  - Emitir `<link rel="canonical" href="/noticias/{article.slug ?? article.id}">` en el render
  - Emitir `<meta property="og:type" content="article">`, `og:url`, `og:image` si `article.imageUrl`
- [x] T4.5 Actualizar links en los 4 archivos que usan `article.id`:
  - `NoticiasListPage.tsx:174` → `to={\`/noticias/${article.slug ?? article.id}\`}`
  - `NoticiaPage.tsx:240` (RelatedNews) → `to={\`/noticias/${related.slug ?? related.id}\`}`
  - `NoticiasSection.tsx:50` → `to={\`/noticias/${article.slug ?? article.id}\`}`
  - `NewsSection.tsx:62` → `to={\`/noticias/${article.slug ?? article.id}\`}`
- [x] T4.6 Ejecutar build TypeScript: `npm run build --workspace=src/Web/Main` — 0 errores.

### Tarea 5 — NewsMetadataMiddleware (CA: 6, 7, 8)

- [x] T5.1 Crear `src/Server/Api/Middleware/NewsMetadataMiddleware.cs` (ver Dev Notes §NewsMetadataMiddleware).
  Constructor: `(RequestDelegate next, IWebHostEnvironment env, IConfiguration config, IServiceScopeFactory scopeFactory)`
  - `scopeFactory` es necesario porque `INewsRepository` es Scoped y el middleware es Singleton.
- [x] T5.2 En `InvokeAsync`:
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
- [x] T5.3 Registrar en `Program.cs` DESPUÉS de `WwwToNonWwwMiddleware` y ANTES de `UseDefaultFiles`:
  ```csharp
  app.UseMiddleware<WwwToNonWwwMiddleware>();
  app.UseMiddleware<NewsMetadataMiddleware>();   // <-- aquí
  // Si 11.2 ya está implementada, SpaMetadataMiddleware va aquí también
  if (!app.Environment.IsDevelopment())
      app.UseHttpsRedirection();
  app.UseDefaultFiles();
  ```
- [x] T5.4 Unit tests en `tests/Unit/Infrastructure.Tests/Middleware/NewsMetadataMiddlewareTests.cs`:
  - `InvokeAsync_NewsSlugPath_InjectsMetadata` — verifica que `<!-- prerender-meta -->` se reemplaza con el bloque
  - `InvokeAsync_AssetPath_PassesThrough` — path `.js` no modifica response
  - `InvokeAsync_SlugNotFound_PassesThrough` — artículo no existe, no lanza excepción
  - `InvokeAsync_ApiPath_PassesThrough` — `/api/v1/...` no interceptado

### Tarea 6 — SlugGenerator unit tests (CA: 11)

- [x] T6.1 Crear `tests/Unit/Application.Tests/News/SlugGeneratorTests.cs`:
  - `Generate_BasicTitle_ReturnsKebabCase`
  - `Generate_TitleWithTildes_NormalizesAccents` — "FUNO11 noticias: ó, é, á, ñ" → "funo11-noticias-o-e-a-n"
  - `Generate_TitleWithSpecialChars_StripsNonAlphanumeric` — "FIBRA $MXBPO! — 2Q25" → "fibra-mxbpo-2q25"
  - `Generate_VeryLongTitle_TruncatesAt200` — título de 400 chars → slug ≤ 200 chars
  - `Generate_EmptyTitle_ReturnsFallback` — "" → algún valor no vacío (ej. `"noticia"`)

### Tarea 7 — Verificación final

- [x] T7.1 `dotnet test tests/Unit/` — todos pasan, incluyendo los nuevos
- [x] T7.2 Verificar en browser: navegar a `/noticias/` → cards con slugs; click → URL cambia a slug; F5 → recarga correctamente con metadata SSR en curl

### Review Findings

**Decision needed:**

- [x] [Review][Decision] Soft-404: slug inexistente o artículo borrado responde 200 con shell SPA — RESUELTO (usuario, 2026-06-11): servir el shell SPA con status 404 cuando el path es `/noticias/{identifier}` y el artículo no resuelve → convertido en P14.

**Patches:**

- [x] [Review][Patch] P14 (Media) Soft-404: cuando `/noticias/{identifier}` (2 segmentos) no resuelve a un artículo vivo, servir el shell SPA con status 404 en lugar de pass-through 200 — extensión aprobada de T5.2 paso 9 [src/Server/Api/Middleware/NewsMetadataMiddleware.cs]

- [x] [Review][Patch] P1 (Alta) Carrera check-then-insert en unicidad de slug: colisión concurrente (pipelines paralelos o backfill+pipeline) viola `IX_NewsArticle_Slug` y el artículo completo se pierde (`NewsPipelineJob` traga la excepción) — retry con regeneración ante `DbUpdateException` [src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs]
- [x] [Review][Patch] P2 (Media) `EscapeJson` artesanal no escapa tab/control chars U+0000–U+001F/U+2028, y `baseUrl` se interpola crudo → JSON-LD inválido que Google descarta — usar serialización JSON real [src/Server/Api/Middleware/NewsMetadataMiddleware.cs:221]
- [x] [Review][Patch] P3 (Media) `og:title` del middleware emite el headline pelado ≠ `<title>` con sufijo ≠ cliente (`pageTitle`) — viola checklist SSR/SEO "og:title mismo texto que title" [src/Server/Api/Middleware/NewsMetadataMiddleware.cs]
- [x] [Review][Patch] P4 (Media) Cadena de fallback de description difiere server (`SummaryMarkdown ?? Snippet`, omite `AiSummary`) vs cliente (`summaryMarkdown ?? aiSummary ?? snippet`), y reglas de truncado distintas — alinear [NewsMetadataMiddleware.cs + src/Web/Main/src/modules/noticia/NoticiaPage.tsx]
- [x] [Review][Patch] P5 (Media) Backfill: un fallo a mitad pierde el `count` acumulado y un artículo que falla reintenta infinito (el `do/while` relee el mismo batch) — try/catch por artículo + skip + respuesta parcial [src/Server/Api/Endpoints/Ops/OpsNewsManagementEndpoints.cs]
- [x] [Review][Patch] P6 (Media) Slugs que colisionan con literales de ruta (`paged`, `fibras`, `related`) o GUID-parseables quedan inalcanzables o devuelven contenido equivocado con 200 — tratarlos como colisión en `GenerateUniqueSlugAsync` [NewsRepository.cs]
- [x] [Review][Patch] P7 (Baja) CA-6: description queda en ~58 chars (solo sufijo de marca) cuando el artículo no tiene snippet ni summary — garantizar piso 120 con fallback genérico [NewsMetadataMiddleware.cs]
- [x] [Review][Patch] P8 (Baja) Truncado por índice de char parte surrogate pairs (emoji) en el corte 157 → `�` en SERPs / JSON-LD inválido — guard `char.IsHighSurrogate` [NewsMetadataMiddleware.cs]
- [x] [Review][Patch] P9 (Baja) Markdown crudo (`**`, `[]()`, `#`) fluye a meta description y JSON-LD — strip básico de sintaxis [NewsMetadataMiddleware.cs + NoticiaPage.tsx]
- [x] [Review][Patch] P10 (Baja) `lastmod` del sitemap sin `CultureInfo.InvariantCulture` — año Hijri con cultura `ar-SA` [src/Server/Api/Endpoints/Public/SeoEndpoints.cs:245]
- [x] [Review][Patch] P11 (Baja) Off-by-one en loop de unicidad: el candidato `-50` se genera pero nunca se verifica, salta directo al fallback GUID [NewsRepository.cs]
- [x] [Review][Patch] P12 (Baja) Catch de lectura de `index.html` solo cubre `IOException`; `UnauthorizedAccessException` (deploy/ACL) → 500 [NewsMetadataMiddleware.cs]
- [x] [Review][Patch] P13 (Baja) `identifier` sin límite de longitud viaja directo al `WHERE` de SQL (path de 8KB = query por request de bot) — guard de longitud > 256 → pass-through [NewsMetadataMiddleware.cs]

**Deferred:**

- [x] [Review][Defer] GUID legacy responde 200+canonical en vez de 301 server-side — CA-5 prescribe redirect client-side; consolidar con el patrón `FibraSlugRedirectMiddleware` en historia futura — deferred, decisión de spec
- [x] [Review][Defer] `BASE_URL` hardcodeado client-side en NoticiaPage [NoticiaPage.tsx:12] — mismo patrón pre-existente que FibraPage.tsx:165 (11.3 done); centralizar en config en historia futura — deferred, pre-existing
- [x] [Review][Defer] Sin caché de `index.html` ni del lookup BD por request — consistente con `SpaMetadataMiddleware` (11.2); optimización futura (P13 mitiga el vector de carga) — deferred, pre-existing
- [x] [Review][Defer] Tests InMemory no ejercitan índice único filtrado, colación CI de SQL Server ni `ExecuteUpdateAsync` — limitación documentada del toolchain de tests — deferred, pre-existing
- [x] [Review][Defer] `GetBySlugAsync` no filtra `Status` (artículos Pending/Failed accesibles por URL adivinable) — paridad con `GetByIdAsync` pre-existente (`FindAsync` sin filtros) — deferred, pre-existing
- [x] [Review][Defer] Metas duplicadas tras hidratación (bloque SSR estático + tags de React 19) — inherente al enfoque 11.x en todas las páginas públicas; mitigado al alinear contenidos (P3/P4) — deferred, pre-existing

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

claude-fable-5 (Claude Code)

### Debug Log References

- Primer intento de `POST /backfill-slugs` en dev devolvió 500 por timeout SQL (Error -2): los Hangfire jobs arrancaron con backlog al levantar la API y saturaron LAPBADIS. No es bug del endpoint — se re-verificó con `Hangfire__UseInMemoryStorage=true` y respondió 200 en ambas pasadas.

### Completion Notes List

- **T1**: `SlugGenerator` implementado según §Algoritmo slug del story (espacios→guión + strip de no-alfanuméricos), con `GeneratedRegex` (idioma del proyecto, como `FibraSlug`). La semántica difiere deliberadamente de `FibraSlug` ("S.A."→"sa" vs "s-a"): el antipatrón de paridad de 11.3 NO aplica aquí porque el slug de noticias se genera SOLO en backend y el frontend lo consume verbatim del DTO — no hay regeneración client-side ni riesgo de loop 301. Migración `20260611161638_AddNewsArticleSlug` aplicada en BD local (columna `slug` nvarchar(256) + índice único filtrado `IX_NewsArticle_Slug`).
- **T2**: 5 métodos nuevos en `INewsRepository`/`NewsRepository`. `GetBySlugAsync` con guard de null/vacío (sin él, `n.Slug == null` matchearía artículos sin slug). `AddWithLinksAsync` usa `??=` para no pisar slugs ya asignados. Fakes de tests actualizados (`FakeNewsRepository`, `InMemoryNewsRepository`). `UpdateSlugAsync` (ExecuteUpdateAsync) no se unit-testea con InMemory porque el provider no lo soporta — verificado en vivo vía backfill.
- **T3**: endpoint `GET /api/v1/news/{slug}` sin constraint (prioridad literal > constraint > sin constraint — sin ambigüedad). Backfill retorna `BackfillSlugsResultDto(int Count)` tipado en lugar de `new { count }` anónimo del spec — mejora el contrato OpenAPI/codegen (mismo criterio que patch P8 del review 11.3). JSON serializa como `{"count":N}` igual que CA-9.
- **T4**: `NoticiaPage` con routing dual GUID/slug + redirect `replace` GUID→slug. El `<title>` client-side se alineó al formato del middleware (`{headline} — Noticias | FIBRADIS`) para que la hidratación no cambie la metadata SSR (checklist SSR/SEO: og:title == title).
- **T5**: `NewsMetadataMiddleware` replica las convenciones endurecidas en el review de 11.2: solo GET/HEAD, guard de `<!-- prerender-meta -->`, sustitución del `<title>` estático (evita títulos duplicados), `HtmlEncoder(UnicodeRanges.All)`, escape `<` en JSON-LD, `Cache-Control: no-cache`, fail-fast de `App:BaseUrl`, catch de `IOException`. Registrado antes de `SpaMetadataMiddleware` (que cubre `/noticias` listado — CA-7). Filtra soft-deleted también en la rama GUID (`GetByIdAsync` no filtra `DeletedAt`).
- **CA-10**: como 11.3 ya está en `done`, la integración del sitemap se hizo directamente: `BuildSitemapXml` acepta parámetro opcional de noticias (retro-compatible) y emite `lastmod` (PublishedAt yyyy-MM-dd) + `changefreq daily` + `priority 0.6`, respetando la secuencia XSD loc→lastmod→changefreq→priority.
- **Security checklist (§Dev Notes)**: ✅ TOCTOU backfill verificado idempotente en vivo (2a pasada `count:0`); ✅ política `AdminOps` confirmada en `AddAuthorizationExtensions.cs` y 401 sin token verificado; ✅ sin divisiones; ✅ XSS: HTML-encoding + EscapeJson + `<` cubiertos por tests (`InvokeAsync_EncodesHtml_AndEscapesJsonLd`).
- **Tests**: 47 nuevos (7 SlugGenerator + 14 NewsRepositorySlug + 20 NewsMetadataMiddleware + 6 SeoEndpoints nuevos/ajustados). Suites completas: 550/550 unit (100 Application + 8 Domain + 442 Infrastructure), 282/282 integration, 116/116 frontend. Comandos: `dotnet test tests/Unit/{Application,Domain,Infrastructure}.Tests`, `dotnet test tests/Integration/Api.Tests`, `npm test --workspace=src/Web/Main`.
- **Verificación en vivo (curl, API dev)**: CA-1 backfill 1992 slugs, 0 duplicados en BD; CA-2 200 por slug + 404 inexistente; CA-3 200 por GUID; CA-5 canonical desde URL GUID apunta al slug; CA-6 title/canonical/og:type=article/description=160 chars/JSON-LD NewsArticle inyectados sin JS; CA-7 listado servido por SpaMetadataMiddleware; CA-8 assets `text/javascript` y API `application/json` intactos; CA-10 sitemap con 500 noticias (cap), XML válido, 528 URLs totales.

### File List

**Nuevos:**
- src/Server/Application/News/SlugGenerator.cs
- src/Server/Api/Middleware/NewsMetadataMiddleware.cs
- src/Server/Api/Endpoints/Ops/OpsNewsManagementEndpoints.cs
- src/Server/SharedApiContracts/News/BackfillSlugsResultDto.cs
- src/Server/Infrastructure/Migrations/SqlServer/20260611161638_AddNewsArticleSlug.cs
- src/Server/Infrastructure/Migrations/SqlServer/20260611161638_AddNewsArticleSlug.Designer.cs
- tests/Unit/Application.Tests/News/SlugGeneratorTests.cs
- tests/Unit/Infrastructure.Tests/Persistence/Repositories/NewsRepositorySlugTests.cs
- tests/Unit/Infrastructure.Tests/Middleware/NewsMetadataMiddlewareTests.cs

**Modificados:**
- src/Server/Domain/News/NewsArticle.cs
- src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleConfiguration.cs
- src/Server/Infrastructure/Migrations/SqlServer/AppDbContextModelSnapshot.cs
- src/Server/Application/News/INewsRepository.cs
- src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs
- src/Server/SharedApiContracts/News/NewsArticleDto.cs
- src/Server/Api/Endpoints/Public/NewsEndpoints.cs
- src/Server/Api/Endpoints/Public/SeoEndpoints.cs
- src/Server/Api/Program.cs
- src/Web/Main/src/api/newsApi.ts
- src/Web/Main/src/app/routes.tsx
- src/Web/Main/src/modules/noticia/NoticiaPage.tsx
- src/Web/Main/src/modules/noticias/NoticiasListPage.tsx
- src/Web/Main/src/modules/home/NewsSection.tsx
- src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx
- tests/Unit/Infrastructure.Tests/Endpoints/SeoEndpointsTests.cs
- tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs
- tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs

**Regenerados (codegen):**
- scripts/codegen/Api.json
- src/Web/SharedApiClient/schema.d.ts

## Senior Developer Review (AI)

- 2026-06-11 — Review adversarial (Blind Hunter + Edge Case Hunter + Acceptance Auditor). Veredicto: 11/11 CAs CUMPLEN (3 con desviación justificada). 1 decisión resuelta (soft-404 → P14 aprobado por Jorge), 14 patches aplicados, 6 defers documentados en deferred-work.md, 6 hallazgos descartados como ruido.
- Patches aplicados: P1 retry ante colisión de slug concurrente en `AddWithLinksAsync` (DbUpdateException + regeneración, 3 intentos); P2 JSON-LD vía `JsonSerializer` con `JavaScriptEncoder` (sustituye `EscapeJson` artesanal que dejaba pasar control chars); P3 og:title = `<title>` completo; P4 cadena de fallback de description alineada server/client (`summaryMarkdown ?? aiSummary ?? snippet`) y reglas de truncado espejo en `NoticiaPage.buildDescription`; P5 backfill resiliente (try/catch por artículo, skip de fallidos sin loop infinito, conteo parcial + logging); P6 slugs reservados (`paged`/`fibras`/`related`) y GUID-parseables tratados como colisión; P7 piso 120 chars de description con sufijo de marca extendido; P8 truncado seguro de surrogate pairs; P9 strip de Markdown en descriptions; P10 `lastmod` con `InvariantCulture`; P11 off-by-one del loop de unicidad (candidato -50 ahora se verifica); P12 catch de `UnauthorizedAccessException` en lectura de index.html; P13 guard de longitud ≤256 del identifier antes de consultar BD; P14 soft-404: shell SPA con status 404 para `/noticias/{x}` no resoluble (decisión aprobada, sustituye pass-through 200 de T5.2 paso 9).
- Tests del review: 7 nuevos backend (3 middleware: piso description, strip markdown, identifier largo→404; 4 repo: literales reservados + GUID-shaped) y 2 actualizados a comportamiento 404. Suites: 557/557 unit (100 App + 8 Domain + 449 Infra), 282/282 integration, 116/116 frontend, builds backend y frontend 0 errores.

## Change Log

- 2026-06-11 — Code review: 14 patches aplicados (ver Senior Developer Review), 6 defers a deferred-work.md. Suites completas verdes. Status → done.
- 2026-06-11 — Historia 11.4 implementada completa: columna `Slug` + migración EF, `SlugGenerator`, 5 métodos de repositorio, endpoint público por slug, backfill Ops idempotente, frontend con routing dual y links por slug, `NewsMetadataMiddleware` (SSR dinámico), noticias en sitemap (integración directa con 11.3 ya en done). 47 tests nuevos; 550 unit + 282 integration + 116 frontend verdes. Status → review.
