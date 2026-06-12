using System.Globalization;
using System.Security;
using System.Text;
using Application.Catalog;
using Application.News;

namespace Api.Endpoints.Public;

public static class SeoEndpoints
{
    private const string DefaultBaseUrl = "https://fibrasinmobiliarias.com";
    private const int MaxNewsInSitemap = 500;

    private static readonly string[] StaticRoutes =
    [
        "/",
        "/fibras",
        "/comparar",
        "/noticias",
        "/conoce-las-fibras",
        "/calendario",
        "/fundamentales",
        "/herramientas",
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
            IConfiguration config,
            CancellationToken ct) =>
        {
            // secuencial: ambos repos comparten el mismo DbContext Scoped (no thread-safe)
            var fibras = await fibraRepo.GetAllActiveForSitemapAsync(ct);
            var newsArticles = await newsRepo.GetArticlesForSitemapAsync(MaxNewsInSitemap, ct);
            var xml = BuildSitemapXml(
                GetBaseUrl(config),
                fibras,
                newsArticles);
            return Results.Content(xml, "application/xml; charset=utf-8");
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        app.MapMethods("/robots.txt", GetAndHead, (IConfiguration config) =>
            Results.Content(BuildRobotsTxt(GetBaseUrl(config)), "text/plain; charset=utf-8"))
        .AllowAnonymous()
        .ExcludeFromDescription();

        return app;
    }

    private static string GetBaseUrl(IConfiguration config) =>
        !string.IsNullOrWhiteSpace(config["App:BaseUrl"])
            ? config["App:BaseUrl"]!.TrimEnd('/')
            : DefaultBaseUrl;

    public static string BuildSitemapXml(
        string baseUrl,
        IEnumerable<(string FullName, string Ticker)> activeFibras,
        IEnumerable<(string Slug, DateTimeOffset PublishedAt)>? newsArticles = null)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");

        foreach (var path in StaticRoutes)
            AppendUrlEntry(sb, $"{baseUrl}{path}");

        // FIBRAs: lastmod = hoy porque los precios actualizan diario
        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        foreach (var (fullName, ticker) in activeFibras)
            AppendUrlEntry(sb, $"{baseUrl}/fibras/{FibraSlug.Build(fullName, ticker)}", today);

        foreach (var (slug, publishedAt) in newsArticles ?? [])
            // InvariantCulture: con cultura de calendario no gregoriano "yyyy" emitiría un año inválido
            AppendUrlEntry(sb, $"{baseUrl}/noticias/{slug}", publishedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        sb.Append("</urlset>\n");
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
        """;

    // XSD sitemaps.org: loc, lastmod — changefreq y priority los ignora Google desde 2023
    private static void AppendUrlEntry(StringBuilder sb, string loc, string? lastmod = null)
    {
        sb.Append("  <url>\n");
        sb.Append($"    <loc>{SecurityElement.Escape(loc)}</loc>\n");
        if (lastmod is not null)
            sb.Append($"    <lastmod>{lastmod}</lastmod>\n");
        sb.Append("  </url>\n");
    }
}
