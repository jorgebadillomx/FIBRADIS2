using System.Security;
using System.Text;
using Application.Catalog;

namespace Api.Endpoints.Public;

public static class SeoEndpoints
{
    private const string DefaultBaseUrl = "https://fibrasinmobiliarias.com";

    // Prioridades según valor SEO de cada ruta (ver Dev Notes 11.3):
    // Home 1.0, /calculadora 0.9 (quick win GSC), contenido principal 0.8,
    // herramientas 0.7, contenido educativo 0.6
    private static readonly (string Path, string Priority, string Changefreq)[] StaticRoutes =
    [
        ("/", "1.0", "daily"),
        ("/catalogo", "0.8", "weekly"),
        ("/comparar", "0.7", "weekly"),
        ("/noticias", "0.7", "daily"),
        ("/conoce-las-fibras", "0.6", "monthly"),
        ("/calendario", "0.7", "weekly"),
        ("/fundamentales", "0.7", "weekly"),
        ("/herramientas", "0.7", "weekly"),
        ("/calculadora", "0.9", "daily"),
    ];

    // GET y HEAD: los validadores SEO y curl -I usan HEAD — MapGet solo responde GET (405)
    private static readonly string[] GetAndHead = [HttpMethods.Get, HttpMethods.Head];

    public static IEndpointRouteBuilder MapSeo(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/sitemap.xml", GetAndHead, async (
            IFibraRepository fibraRepo,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var fibras = await fibraRepo.GetAllActiveAsync(ct);
            var xml = BuildSitemapXml(
                GetBaseUrl(config),
                fibras.Select(f => (f.FullName, f.Ticker)));
            return Results.Content(xml, "application/xml; charset=utf-8");
        })
        .AllowAnonymous()
        .ExcludeFromDescription(); // infraestructura SEO — fuera del contrato OpenAPI/codegen

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
        IEnumerable<(string FullName, string Ticker)> activeFibras)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");

        foreach (var (path, priority, changefreq) in StaticRoutes)
            AppendUrlEntry(sb, $"{baseUrl}{path}", priority, changefreq);

        foreach (var (fullName, ticker) in activeFibras)
            AppendUrlEntry(sb, $"{baseUrl}/fibras/{FibraSlug.Build(fullName, ticker)}", "0.8", "weekly");

        sb.Append("</urlset>\n");
        return sb.ToString();
    }

    public static string BuildRobotsTxt(string baseUrl) =>
        $"User-agent: *\nAllow: /\nDisallow: /ops/\nDisallow: /api/\nDisallow: /hangfire/\n\nSitemap: {baseUrl}/sitemap.xml\n";

    // Orden según el XSD de sitemaps.org: loc, lastmod, changefreq, priority.
    // <loc> con entity-escaping: App:BaseUrl viene de config sin validar (un '&' rompería el XML)
    private static void AppendUrlEntry(StringBuilder sb, string loc, string priority, string changefreq)
    {
        sb.Append("  <url>\n");
        sb.Append($"    <loc>{SecurityElement.Escape(loc)}</loc>\n");
        sb.Append($"    <changefreq>{changefreq}</changefreq>\n");
        sb.Append($"    <priority>{priority}</priority>\n");
        sb.Append("  </url>\n");
    }
}
