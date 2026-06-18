using System.Globalization;
using System.Security;
using System.Text;
using Api.Seo;
using Application.Catalog;
using Application.News;
using Application.Seo;
using Domain.Seo;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Endpoints.Public;

public static class SeoEndpoints
{
    private const string DefaultBaseUrl = "https://fibrasinmobiliarias.com";
    private const int NewsSitemapPageSize = 45_000;
    private const string SitemapIndexCacheKey = "sitemap-index-xml";
    private const string SitemapStaticCacheKey = "sitemap-static-xml";
    private const string SitemapFibrasCacheKey = "sitemap-fibras-xml";
    private const string LlmsCacheKey = "llms-txt";

    private static readonly string[] StaticRoutes =
    [
        "/",
        "/fibras",
        "/comparar",
        "/noticias",
        "/conoce-las-fibras",
        "/calendario",
        "/fundamentales",
        "/plataforma",
        "/portafolio",
        "/calculadora",
        "/acerca",
        "/contacto",
        "/privacidad",
    ];

    private static readonly string[] GetAndHead = [HttpMethods.Get, HttpMethods.Head];

    public static IEndpointRouteBuilder MapSeo(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/sitemap.xml", GetAndHead, async (
            IFibraRepository fibraRepo,
            INewsRepository newsRepo,
            ISeoMetadataRepository seoRepo,
            SeoSitemapCacheState cacheState,
            IConfiguration config,
            IMemoryCache cache,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var xml = await GetOrCreateCachedAsync(cache, cacheState.WithVersion(SitemapIndexCacheKey), async () =>
            {
                var baseUrl = GetBaseUrl(config);
                var visibility = await LoadSitemapVisibilityAsync(seoRepo, ct);
                var newsPageCount = await GetNewsPageCountAsync(newsRepo, ct);

                var staticPaths = GetVisibleStaticRoutes(visibility);
                var fibraPaths = await GetVisibleFibraPathsAsync(fibraRepo, visibility, ct);
                var sitemapPaths = BuildSitemapIndexPaths(staticPaths.Count > 0, fibraPaths.Count > 0, newsPageCount);
                return BuildSitemapIndexXml(baseUrl, sitemapPaths);
            });

            httpContext.Response.Headers.CacheControl = "public, max-age=3600, s-maxage=3600";
            return Results.Content(xml, "application/xml; charset=utf-8");
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        app.MapMethods("/sitemap-static.xml", GetAndHead, async (
            ISeoMetadataRepository seoRepo,
            SeoSitemapCacheState cacheState,
            IConfiguration config,
            IMemoryCache cache,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var xml = await GetOrCreateCachedAsync(cache, cacheState.WithVersion(SitemapStaticCacheKey), async () =>
            {
                var visibility = await LoadSitemapVisibilityAsync(seoRepo, ct);
                var staticRoutes = GetVisibleStaticRoutes(visibility);
                return BuildUrlSetXml(staticRoutes.Select(r => (Loc: $"{GetBaseUrl(config)}{r.Path}", LastMod: r.LastMod)));
            });

            httpContext.Response.Headers.CacheControl = "public, max-age=3600, s-maxage=3600";
            return Results.Content(xml, "application/xml; charset=utf-8");
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        app.MapMethods("/sitemap-fibras.xml", GetAndHead, async (
            IFibraRepository fibraRepo,
            ISeoMetadataRepository seoRepo,
            SeoSitemapCacheState cacheState,
            IConfiguration config,
            IMemoryCache cache,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var xml = await GetOrCreateCachedAsync(cache, cacheState.WithVersion(SitemapFibrasCacheKey), async () =>
            {
                var visibility = await LoadSitemapVisibilityAsync(seoRepo, ct);
                var fibras = await GetVisibleFibraPathsAsync(fibraRepo, visibility, ct);
                var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return BuildUrlSetXml(fibras.Select(path => (Loc: $"{GetBaseUrl(config)}{path}", LastMod: today)));
            });

            httpContext.Response.Headers.CacheControl = "public, max-age=3600, s-maxage=3600";
            return Results.Content(xml, "application/xml; charset=utf-8");
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        app.MapMethods("/sitemap-noticias-{page:int}.xml", GetAndHead, async (
            int page,
            INewsRepository newsRepo,
            ISeoMetadataRepository seoRepo,
            SeoSitemapCacheState cacheState,
            IConfiguration config,
            IMemoryCache cache,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (page < 1)
                return Results.NotFound();

            // Solo se cachea XML no vacío. Las páginas fuera de rango devuelven 404 sin cachear:
            // cachear vacíos permitiría (a) crecimiento ilimitado del caché al enumerar páginas
            // arbitrarias y (b) un 404 rancio que contradiga al índice tras publicar noticias.
            var cacheKey = cacheState.WithVersion($"sitemap-noticias-{page}");
            if (!cache.TryGetValue(cacheKey, out string? xml) || string.IsNullOrEmpty(xml))
            {
                var visibility = await LoadSitemapVisibilityAsync(seoRepo, ct);
                var newsPage = await newsRepo.GetArticlesForSitemapPageAsync(page, NewsSitemapPageSize, ct);
                var totalPages = GetPageCount(newsPage.Total, NewsSitemapPageSize);

                // page 1 siempre se sirve (urlset válido aunque vacío); páginas > máximo → 404.
                if (page > Math.Max(1, totalPages))
                    return Results.NotFound();

                var newsPaths = newsPage.Items
                    .Where(article => !visibility.NewsSlugs.Contains(NormalizeEntityKey(article.Slug)))
                    .Select(article => (
                        Loc: $"{GetBaseUrl(config)}/noticias/{article.Slug}",
                        PublishedAt: article.PublishedAt));

                xml = BuildNewsUrlSetXml(newsPaths);
                cache.Set(cacheKey, xml, TimeSpan.FromHours(1));
            }

            httpContext.Response.Headers.CacheControl = "public, max-age=3600, s-maxage=3600";
            return Results.Content(xml, "application/xml; charset=utf-8");
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        app.MapMethods("/llms.txt", GetAndHead, async (
            ISpaMetadataProvider metadataProvider,
            IConfiguration config,
            IMemoryCache cache,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var txt = await GetOrCreateCachedAsync(cache, LlmsCacheKey, async () =>
            {
                var pages = new[]
                {
                    "/",
                    "/conoce-las-fibras",
                    "/fibras",
                    "/fundamentales",
                    "/comparar",
                    "/noticias",
                    "/acerca",
                    "/portafolio",
                    "/calculadora",
                    "/calendario",
                };

                var entries = new List<(string Title, string Description, string Path)>();
                foreach (var path in pages)
                {
                    var meta = await metadataProvider.GetMetaForPathAsync(path, ct);
                    if (meta is not null)
                        entries.Add((meta.Title, meta.Description, meta.CanonicalPath));
                }

                return BuildLlmsTxt(GetBaseUrl(config), entries);
            });

            httpContext.Response.Headers.CacheControl = "public, max-age=86400, s-maxage=86400";
            return Results.Content(txt, "text/plain; charset=utf-8");
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        app.MapMethods("/indexnow.txt", GetAndHead, async (
            IConfiguration config,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var key = config["Seo:IndexNowKey"];
            if (string.IsNullOrWhiteSpace(key)) return Results.NotFound();
            httpContext.Response.Headers.CacheControl = "public, max-age=86400";
            return Results.Content(key, "text/plain");
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        app.MapMethods("/robots.txt", GetAndHead, (IConfiguration config) =>
            Results.Content(BuildRobotsTxt(GetBaseUrl(config)), "text/plain; charset=utf-8"))
        .AllowAnonymous()
        .ExcludeFromDescription();

        return app;
    }

    public static string BuildSitemapIndexXml(string baseUrl, IEnumerable<string> sitemapPaths)
        => BuildIndexXml(baseUrl, sitemapPaths);

    public static string BuildLlmsTxt(string baseUrl, IReadOnlyList<(string Title, string Description, string Path)> pages)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Fibras Inmobiliarias");
        sb.AppendLine("Plataforma web integral de análisis de FIBRAs inmobiliarias mexicanas.");
        sb.AppendLine();
        sb.AppendLine("## Páginas clave");

        foreach (var (title, description, path) in pages)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(path))
                continue;

            var absoluteUrl = $"{baseUrl}{NormalizePath(path)}";
            sb.AppendLine($"- [{title}]({absoluteUrl})");
            if (!string.IsNullOrWhiteSpace(description))
                sb.AppendLine($"  - {description}");
        }

        sb.AppendLine();
        sb.AppendLine("## Nota de uso");
        sb.AppendLine("Usa estas páginas como punto de partida, cita las fuentes originales y verifica siempre la fecha de los datos.");
        return sb.ToString();
    }

    public static string BuildRobotsTxt(string baseUrl) =>
        $"""
        User-agent: *
        Allow: /
        Disallow: /ops/
        Disallow: /api/
        Disallow: /hangfire/

        User-agent: GPTBot
        Allow: /

        User-agent: ClaudeBot
        Allow: /

        User-agent: Google-Extended
        Allow: /

        User-agent: Applebot-Extended
        Allow: /

        User-agent: CCBot
        Disallow: /

        User-agent: Bytespider
        Disallow: /

        User-agent: meta-externalagent
        Disallow: /

        Sitemap: {baseUrl}/sitemap.xml
        # llms.txt: {baseUrl}/llms.txt
        """;

    private static async Task<string> GetOrCreateCachedAsync(IMemoryCache cache, string cacheKey, Func<Task<string>> factory)
    {
        if (!cache.TryGetValue(cacheKey, out string? value))
        {
            value = await factory();
            cache.Set(cacheKey, value, TimeSpan.FromHours(1));
        }

        return value!;
    }

    private static async Task<SitemapVisibility> LoadSitemapVisibilityAsync(ISeoMetadataRepository seoRepo, CancellationToken ct)
    {
        var metadata = await seoRepo.GetAllAsync(ct: ct);
        var visibility = new SitemapVisibility();

        foreach (var row in metadata.Where(row => row.IsActive))
        {
            var isNoIndex = IsNoIndex(row.RobotsDirectives);
            switch (row.PageType)
            {
                case SeoPageType.Home:
                case SeoPageType.StaticPage:
                    var path = NormalizePath(row.EntityKey);
                    if (isNoIndex)
                        visibility.StaticRoutes.Add(path);
                    else
                        visibility.StaticRouteLastMod[path] = row.UpdatedAt;
                    break;
                case SeoPageType.Fibra:
                    if (isNoIndex)
                        visibility.FibraTickers.Add(row.EntityKey.Trim().ToUpperInvariant());
                    break;
                case SeoPageType.News:
                    if (isNoIndex)
                        visibility.NewsSlugs.Add(NormalizeEntityKey(row.EntityKey));
                    break;
            }
        }

        return visibility;
    }

    private static async Task<IReadOnlyList<string>> GetVisibleFibraPathsAsync(
        IFibraRepository fibraRepo,
        SitemapVisibility visibility,
        CancellationToken ct)
    {
        var activeFibras = await fibraRepo.GetAllActiveForSitemapAsync(ct);
        return activeFibras
            .Where(fibra => !visibility.FibraTickers.Contains(fibra.Ticker.Trim().ToUpperInvariant()))
            .Select(fibra => $"/fibras/{FibraSlug.Build(fibra.FullName, fibra.Ticker)}")
            .ToList();
    }

    private static IReadOnlyList<(string Path, string LastMod)> GetVisibleStaticRoutes(SitemapVisibility visibility)
        => StaticRoutes
            .Where(route => !visibility.StaticRoutes.Contains(NormalizePath(route)))
            .Select(route =>
            {
                var normalized = NormalizePath(route);
                var lastMod = visibility.StaticRouteLastMod.TryGetValue(normalized, out var updatedAt)
                    ? updatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : "2024-01-01";
                return (route, lastMod);
            })
            .ToList();

    private static async Task<int> GetNewsPageCountAsync(INewsRepository newsRepo, CancellationToken ct)
    {
        var result = await newsRepo.GetArticlesForSitemapPageAsync(1, NewsSitemapPageSize, ct);
        return GetPageCount(result.Total, NewsSitemapPageSize);
    }

    private static IReadOnlyList<string> BuildSitemapIndexPaths(bool hasStatic, bool hasFibras, int newsPageCount)
    {
        var paths = new List<string>();
        if (hasStatic)
            paths.Add("/sitemap-static.xml");
        if (hasFibras)
            paths.Add("/sitemap-fibras.xml");
        for (var page = 1; page <= newsPageCount; page++)
            paths.Add($"/sitemap-noticias-{page}.xml");
        return paths;
    }

    private static string BuildIndexXml(string baseUrl, IEnumerable<string> sitemapPaths)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append("<sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");

        foreach (var path in sitemapPaths)
        {
            sb.Append("  <sitemap>\n");
            sb.Append($"    <loc>{SecurityElement.Escape($"{baseUrl}{path}")}</loc>\n");
            sb.Append("  </sitemap>\n");
        }

        sb.Append("</sitemapindex>\n");
        return sb.ToString();
    }

    private static string BuildUrlSetXml(IEnumerable<(string Loc, string LastMod)> entries)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");

        foreach (var (loc, lastmod) in entries)
            AppendUrlEntry(sb, loc, lastmod);

        sb.Append("</urlset>\n");
        return sb.ToString();
    }

    public static string BuildNewsUrlSetXmlPublic(string baseUrl, IEnumerable<(string Loc, DateTimeOffset PublishedAt)> entries)
        => BuildNewsUrlSetXml(entries);

    private static string BuildNewsUrlSetXml(IEnumerable<(string Loc, DateTimeOffset PublishedAt)> entries)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"\n");
        sb.Append("        xmlns:news=\"http://www.google.com/schemas/sitemap-news/0.9\">\n");

        foreach (var (loc, publishedAt) in entries)
        {
            var dateOnly = publishedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sb.Append("  <url>\n");
            sb.Append($"    <loc>{SecurityElement.Escape(loc)}</loc>\n");
            sb.Append($"    <lastmod>{dateOnly}</lastmod>\n");
            sb.Append("    <news:news>\n");
            sb.Append("      <news:publication>\n");
            sb.Append("        <news:name>Fibras Inmobiliarias</news:name>\n");
            sb.Append("        <news:language>es</news:language>\n");
            sb.Append("      </news:publication>\n");
            sb.Append($"      <news:publication_date>{publishedAt:O}</news:publication_date>\n");
            sb.Append("    </news:news>\n");
            sb.Append("  </url>\n");
        }

        sb.Append("</urlset>\n");
        return sb.ToString();
    }

    private static int GetPageCount(int totalItems, int pageSize)
        => totalItems <= 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

    private static string GetBaseUrl(IConfiguration config) =>
        !string.IsNullOrWhiteSpace(config["App:BaseUrl"])
            ? config["App:BaseUrl"]!.TrimEnd('/')
            : DefaultBaseUrl;

    // XSD sitemaps.org: loc, lastmod — changefreq y priority los ignora Google desde 2023
    private static void AppendUrlEntry(StringBuilder sb, string loc, string? lastmod = null)
    {
        sb.Append("  <url>\n");
        sb.Append($"    <loc>{SecurityElement.Escape(loc)}</loc>\n");
        if (lastmod is not null)
            sb.Append($"    <lastmod>{lastmod}</lastmod>\n");
        sb.Append("  </url>\n");
    }

    private static bool IsNoIndex(string robotsDirectives)
        => robotsDirectives.Contains("noindex", StringComparison.OrdinalIgnoreCase);

    private static string NormalizePath(string path)
    {
        var normalized = path.Trim();
        if (normalized.Length == 0)
            return "/";

        normalized = normalized.StartsWith('/') ? normalized : $"/{normalized}";
        return normalized == "/" ? "/" : normalized.TrimEnd('/');
    }

    private static string NormalizeEntityKey(string entityKey)
    {
        var normalized = entityKey.Trim();
        if (normalized.Length == 0)
            return normalized;

        return normalized == "/" ? "/" : normalized.TrimEnd('/');
    }

    private sealed class SitemapVisibility
    {
        public HashSet<string> StaticRoutes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTimeOffset> StaticRouteLastMod { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> FibraTickers { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> NewsSlugs { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
