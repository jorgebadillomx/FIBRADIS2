using Api.Endpoints.Public;

namespace Infrastructure.Tests.Endpoints;

public class SeoEndpointsTests
{
    private const string BaseUrl = "https://fibrasinmobiliarias.com";

    private static readonly (string FullName, string Ticker)[] SampleFibras =
    [
        ("Fibra Uno", "FUNO11"),
        ("Fibra Macquarie", "FIBRAMQ12"),
    ];

    private static readonly (string Slug, DateTimeOffset PublishedAt)[] SampleNews =
    [
        ("funo11-reporta-resultados-del-2t25", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero)),
        ("danhos13-anuncia-distribucion", new DateTimeOffset(2026, 6, 9, 8, 0, 0, TimeSpan.Zero)),
    ];

    [Fact]
    public void SitemapContainsCalculadora_WithPriority09()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras);

        Assert.Contains(
            "<loc>https://fibrasinmobiliarias.com/calculadora</loc>\n    <changefreq>daily</changefreq>\n    <priority>0.9</priority>",
            xml);
    }

    [Fact]
    public void SitemapContainsAllStaticRoutes()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, []);

        Assert.Contains("<loc>https://fibrasinmobiliarias.com/</loc>\n    <changefreq>daily</changefreq>\n    <priority>1.0</priority>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/catalogo</loc>\n    <changefreq>weekly</changefreq>\n    <priority>0.8</priority>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/comparar</loc>\n    <changefreq>weekly</changefreq>\n    <priority>0.7</priority>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/noticias</loc>\n    <changefreq>daily</changefreq>\n    <priority>0.7</priority>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/conoce-las-fibras</loc>\n    <changefreq>monthly</changefreq>\n    <priority>0.6</priority>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/calendario</loc>\n    <changefreq>weekly</changefreq>\n    <priority>0.7</priority>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/fundamentales</loc>\n    <changefreq>weekly</changefreq>\n    <priority>0.7</priority>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/herramientas</loc>\n    <changefreq>weekly</changefreq>\n    <priority>0.7</priority>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/calculadora</loc>\n    <changefreq>daily</changefreq>\n    <priority>0.9</priority>", xml);
    }

    [Fact]
    public void SitemapContainsFibraSlugUrls()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras);

        Assert.Contains("<loc>https://fibrasinmobiliarias.com/fibras/fibra-uno-funo11</loc>\n    <changefreq>weekly</changefreq>\n    <priority>0.8</priority>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/fibras/fibra-macquarie-fibramq12</loc>", xml);
        // las URLs viejas por ticker NO se incluyen (CA-3)
        Assert.DoesNotContain("/fibras/FUNO11", xml);
        Assert.DoesNotContain("<loc>https://fibrasinmobiliarias.com/fibras/funo11</loc>", xml);
    }

    [Fact]
    public void SitemapIsValidXml_WithSitemapsOrgNamespace()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras);

        var doc = System.Xml.Linq.XDocument.Parse(xml); // lanza si el XML está mal formado
        Assert.Equal("urlset", doc.Root!.Name.LocalName);
        Assert.Equal("http://www.sitemaps.org/schemas/sitemap/0.9", doc.Root.Name.NamespaceName);
        // 9 rutas estáticas + 2 fibras
        Assert.Equal(11, doc.Root.Elements().Count());
    }

    [Fact]
    public void SitemapElementsFollowXsdSequence_ChangefreqBeforePriority()
    {
        // El XSD de sitemaps.org define la secuencia loc, lastmod, changefreq, priority —
        // un validador estricto rechaza priority antes de changefreq (CA-1)
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras, SampleNews);
        var doc = System.Xml.Linq.XDocument.Parse(xml);

        foreach (var url in doc.Root!.Elements())
        {
            var names = url.Elements().Select(e => e.Name.LocalName).ToArray();
            // las entradas de noticias incluyen lastmod (PublishedAt); el resto no
            if (names.Length == 4)
                Assert.Equal(["loc", "lastmod", "changefreq", "priority"], names);
            else
                Assert.Equal(["loc", "changefreq", "priority"], names);
        }
    }

    [Fact]
    public void SitemapContainsNewsSlugUrls_WithPriority06DailyAndLastmod()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras, SampleNews);

        Assert.Contains(
            "<loc>https://fibrasinmobiliarias.com/noticias/funo11-reporta-resultados-del-2t25</loc>\n    <lastmod>2026-06-10</lastmod>\n    <changefreq>daily</changefreq>\n    <priority>0.6</priority>",
            xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/noticias/danhos13-anuncia-distribucion</loc>", xml);
    }

    [Fact]
    public void SitemapWithNews_IsValidXml_WithExpectedUrlCount()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras, SampleNews);

        var doc = System.Xml.Linq.XDocument.Parse(xml);
        // 9 rutas estáticas + 2 fibras + 2 noticias
        Assert.Equal(13, doc.Root!.Elements().Count());
    }

    [Fact]
    public void SitemapWithoutNews_KeepsBackwardCompatibleShape()
    {
        // llamada sin noticias (firma previa) — no debe emitir entradas /noticias/{slug}
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras);

        Assert.DoesNotContain("/noticias/", xml);
    }

    [Fact]
    public void SitemapEscapesXmlSpecialCharsInLoc()
    {
        // App:BaseUrl viene de config sin validar — un '&' debe quedar escapado, no romper el XML
        var xml = SeoEndpoints.BuildSitemapXml("https://example.com/x?a=1&b=2", SampleFibras);

        Assert.Contains("&amp;", xml);
        var doc = System.Xml.Linq.XDocument.Parse(xml); // lanza si quedó un '&' crudo
        Assert.Contains(doc.Root!.Elements(), u => u.Elements().First().Value.Contains("a=1&b=2"));
    }

    [Fact]
    public void SitemapUsesConfigurableBaseUrl()
    {
        var xml = SeoEndpoints.BuildSitemapXml("https://staging.example.com", SampleFibras);

        Assert.Contains("<loc>https://staging.example.com/calculadora</loc>", xml);
        Assert.DoesNotContain("fibrasinmobiliarias.com", xml);
    }

    [Fact]
    public void RobotsTxtContainsDisallowOps()
    {
        var robots = SeoEndpoints.BuildRobotsTxt(BaseUrl);

        Assert.Contains("Disallow: /ops/\n", robots);
        Assert.Contains("Disallow: /api/\n", robots);
        Assert.Contains("Disallow: /hangfire/\n", robots);
    }

    [Fact]
    public void RobotsTxtContainsSitemapUrl()
    {
        var robots = SeoEndpoints.BuildRobotsTxt(BaseUrl);

        Assert.Contains("Sitemap: https://fibrasinmobiliarias.com/sitemap.xml", robots);
    }

    [Fact]
    public void RobotsTxt_HasExactExpectedFormat()
    {
        var robots = SeoEndpoints.BuildRobotsTxt(BaseUrl);

        Assert.Equal(
            "User-agent: *\nAllow: /\nDisallow: /ops/\nDisallow: /api/\nDisallow: /hangfire/\n\nSitemap: https://fibrasinmobiliarias.com/sitemap.xml\n",
            robots);
    }
}
