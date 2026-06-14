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
    public void SitemapContainsCalculadora()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras);

        Assert.Contains("<loc>https://fibrasinmobiliarias.com/calculadora</loc>", xml);
    }

    [Fact]
    public void SitemapContainsAllStaticRoutes()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, []);

        Assert.Contains("<loc>https://fibrasinmobiliarias.com/</loc>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/fibras</loc>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/comparar</loc>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/noticias</loc>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/conoce-las-fibras</loc>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/calendario</loc>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/fundamentales</loc>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/calculadora</loc>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/acerca</loc>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/contacto</loc>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/privacidad</loc>", xml);
    }

    [Fact]
    public void SitemapIndexContainsSectionSitemaps()
    {
        var xml = SeoEndpoints.BuildSitemapIndexXml(BaseUrl, [
            "/sitemap-static.xml",
            "/sitemap-fibras.xml",
            "/sitemap-noticias-1.xml",
            "/sitemap-noticias-2.xml",
        ]);

        var doc = System.Xml.Linq.XDocument.Parse(xml);
        Assert.Equal("sitemapindex", doc.Root!.Name.LocalName);
        Assert.Equal("http://www.sitemaps.org/schemas/sitemap/0.9", doc.Root.Name.NamespaceName);
        Assert.Contains(doc.Root.Elements(), e => e.Element(doc.Root.Name.Namespace + "loc")?.Value == "https://fibrasinmobiliarias.com/sitemap-static.xml");
        Assert.Contains(doc.Root.Elements(), e => e.Element(doc.Root.Name.Namespace + "loc")?.Value == "https://fibrasinmobiliarias.com/sitemap-noticias-2.xml");
    }

    [Fact]
    public void SitemapDoesNotContainChangefreqOrPriority()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras, SampleNews);

        Assert.DoesNotContain("<changefreq>", xml);
        Assert.DoesNotContain("<priority>", xml);
    }

    [Fact]
    public void SitemapContainsFibraSlugUrls()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras);

        Assert.Contains("<loc>https://fibrasinmobiliarias.com/fibras/fibra-uno-funo11</loc>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/fibras/fibra-macquarie-fibramq12</loc>", xml);
        // las URLs viejas por ticker NO se incluyen
        Assert.DoesNotContain("/fibras/FUNO11", xml);
        Assert.DoesNotContain("<loc>https://fibrasinmobiliarias.com/fibras/funo11</loc>", xml);
    }

    [Fact]
    public void SitemapFibraUrls_ContainLastmod()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras);
        var doc = System.Xml.Linq.XDocument.Parse(xml);

        var fibraUrls = doc.Root!.Elements()
            .Where(u => u.Elements().First().Value.Contains("/fibras/fibra-"))
            .ToList();

        Assert.NotEmpty(fibraUrls);
        Assert.All(fibraUrls, u =>
        {
            var children = u.Elements().Select(e => e.Name.LocalName).ToArray();
            Assert.Equal(["loc", "lastmod"], children);
        });
    }

    [Fact]
    public void SitemapIsValidXml_WithSitemapsOrgNamespace()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras);

        var doc = System.Xml.Linq.XDocument.Parse(xml);
        Assert.Equal("urlset", doc.Root!.Name.LocalName);
        Assert.Equal("http://www.sitemaps.org/schemas/sitemap/0.9", doc.Root.Name.NamespaceName);
        // 11 rutas estáticas + 2 fibras
        Assert.Equal(13, doc.Root.Elements().Count());
    }

    [Fact]
    public void SitemapElementsHaveCorrectStructure()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras, SampleNews);
        var doc = System.Xml.Linq.XDocument.Parse(xml);

        foreach (var url in doc.Root!.Elements())
        {
            var names = url.Elements().Select(e => e.Name.LocalName).ToArray();
            // Primer elemento siempre es <loc>
            Assert.Equal("loc", names[0]);
            // Si hay segundo elemento, debe ser <lastmod> (FIBRA pages y noticias)
            if (names.Length == 2)
                Assert.Equal("lastmod", names[1]);
            // Nunca más de 2 elementos
            Assert.True(names.Length <= 2, $"URL entry had {names.Length} elements: {string.Join(", ", names)}");
        }
    }

    [Fact]
    public void SitemapContainsNewsSlugUrls_WithLastmod()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras, SampleNews);

        Assert.Contains(
            "<loc>https://fibrasinmobiliarias.com/noticias/funo11-reporta-resultados-del-2t25</loc>\n    <lastmod>2026-06-10</lastmod>",
            xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/noticias/danhos13-anuncia-distribucion</loc>", xml);
    }

    [Fact]
    public void SitemapWithNews_IsValidXml_WithExpectedUrlCount()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras, SampleNews);

        var doc = System.Xml.Linq.XDocument.Parse(xml);
        // 11 rutas estáticas + 2 fibras + 2 noticias
        Assert.Equal(15, doc.Root!.Elements().Count());
    }

    [Fact]
    public void SitemapWithoutNews_KeepsBackwardCompatibleShape()
    {
        // llamada sin noticias (firma previa) — no debe emitir entradas /noticias/{slug}
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras);

        Assert.DoesNotContain("/noticias/funo11-reporta", xml);
    }

    [Fact]
    public void SitemapEscapesXmlSpecialCharsInLoc()
    {
        // App:BaseUrl viene de config sin validar — un '&' debe quedar escapado, no romper el XML
        var xml = SeoEndpoints.BuildSitemapXml("https://example.com/x?a=1&b=2", SampleFibras);

        Assert.Contains("&amp;", xml);
        var doc = System.Xml.Linq.XDocument.Parse(xml);
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
    public void RobotsTxtContainsLlmsTxtReference()
    {
        var robots = SeoEndpoints.BuildRobotsTxt(BaseUrl);

        Assert.Contains("/llms.txt", robots);
    }

    [Fact]
    public void RobotsTxt_AllowsAiSearchCrawlers()
    {
        var robots = SeoEndpoints.BuildRobotsTxt(BaseUrl);

        Assert.Contains("User-agent: GPTBot\nAllow: /", robots);
        Assert.Contains("User-agent: ClaudeBot\nAllow: /", robots);
        Assert.Contains("User-agent: Google-Extended\nAllow: /", robots);
        Assert.Contains("User-agent: Applebot-Extended\nAllow: /", robots);
    }

    [Fact]
    public void RobotsTxt_BlocksTrainingOnlyCrawlers()
    {
        var robots = SeoEndpoints.BuildRobotsTxt(BaseUrl);

        Assert.Contains("User-agent: CCBot\nDisallow: /", robots);
        Assert.Contains("User-agent: Bytespider\nDisallow: /", robots);
        Assert.Contains("User-agent: meta-externalagent\nDisallow: /", robots);
    }

    [Fact]
    public void LlmsTxtContainsKeyPagesAndUsageNote()
    {
        var txt = SeoEndpoints.BuildLlmsTxt(BaseUrl, [
            ("Inicio", "Plataforma de análisis", "/"),
            ("Noticias", "Listado de noticias", "/noticias"),
        ]);

        Assert.Contains("# FIBRADIS", txt);
        Assert.Contains("[Inicio](https://fibrasinmobiliarias.com/)", txt);
        Assert.Contains("[Noticias](https://fibrasinmobiliarias.com/noticias)", txt);
        Assert.Contains("Nota de uso", txt);
    }
}
