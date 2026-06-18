using Api.Endpoints.Public;

namespace Infrastructure.Tests.Endpoints;

public class SeoEndpointsTests
{
    private const string BaseUrl = "https://fibrasinmobiliarias.com";

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
    public void SitemapIndex_EscapesXmlSpecialCharsInBaseUrl()
    {
        var xml = SeoEndpoints.BuildSitemapIndexXml("https://example.com/x?a=1&b=2", ["/sitemap-static.xml"]);

        Assert.Contains("&amp;", xml);
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        Assert.Contains(doc.Root!.Elements(), u => u.Elements().First().Value.Contains("a=1&b=2"));
    }

    [Fact]
    public void NewsSitemapXml_HasGoogleNewsNamespace()
    {
        var articles = new[]
        {
            ("https://fibrasinmobiliarias.com/noticias/funo11-resultados", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero)),
            ("https://fibrasinmobiliarias.com/noticias/danhos-distribucion", new DateTimeOffset(2026, 6, 9, 8, 0, 0, TimeSpan.Zero)),
        };

        var xml = SeoEndpoints.BuildNewsUrlSetXmlPublic(BaseUrl, articles);

        var doc = System.Xml.Linq.XDocument.Parse(xml);
        Assert.Equal("urlset", doc.Root!.Name.LocalName);
        Assert.Contains(doc.Root.Attributes(), a => a.Value == "http://www.google.com/schemas/sitemap-news/0.9");
    }

    [Fact]
    public void NewsSitemapXml_ContainsPublicationAndDate()
    {
        var articles = new[]
        {
            ("https://fibrasinmobiliarias.com/noticias/funo11-resultados", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero)),
        };

        var xml = SeoEndpoints.BuildNewsUrlSetXmlPublic(BaseUrl, articles);

        Assert.Contains("Fibras Inmobiliarias", xml);
        Assert.Contains("<news:language>es</news:language>", xml);
        Assert.Contains("<news:publication_date>2026-06-10T12:00:00", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/noticias/funo11-resultados</loc>", xml);
    }

    [Fact]
    public void NewsSitemapXml_IsValidXml()
    {
        var articles = new[]
        {
            ("https://fibrasinmobiliarias.com/noticias/funo11-resultados", new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero)),
            ("https://fibrasinmobiliarias.com/noticias/danhos-distribucion", new DateTimeOffset(2026, 6, 9, 8, 0, 0, TimeSpan.Zero)),
        };

        var xml = SeoEndpoints.BuildNewsUrlSetXmlPublic(BaseUrl, articles);

        var doc = System.Xml.Linq.XDocument.Parse(xml);
        Assert.Equal(2, doc.Root!.Elements().Count());
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

        Assert.Contains("# Fibras Inmobiliarias", txt);
        Assert.Contains("[Inicio](https://fibrasinmobiliarias.com/)", txt);
        Assert.Contains("[Noticias](https://fibrasinmobiliarias.com/noticias)", txt);
        Assert.Contains("Nota de uso", txt);
    }
}
