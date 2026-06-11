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

    [Fact]
    public void SitemapContainsCalculadora_WithPriority09()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras);

        Assert.Contains(
            "<loc>https://fibrasinmobiliarias.com/calculadora</loc>\n    <priority>0.9</priority>\n    <changefreq>daily</changefreq>",
            xml);
    }

    [Fact]
    public void SitemapContainsAllStaticRoutes()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, []);

        Assert.Contains("<loc>https://fibrasinmobiliarias.com/</loc>\n    <priority>1.0</priority>\n    <changefreq>daily</changefreq>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/catalogo</loc>\n    <priority>0.8</priority>\n    <changefreq>weekly</changefreq>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/comparar</loc>\n    <priority>0.7</priority>\n    <changefreq>weekly</changefreq>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/noticias</loc>\n    <priority>0.7</priority>\n    <changefreq>daily</changefreq>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/conoce-las-fibras</loc>\n    <priority>0.6</priority>\n    <changefreq>monthly</changefreq>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/calendario</loc>\n    <priority>0.7</priority>\n    <changefreq>weekly</changefreq>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/fundamentales</loc>\n    <priority>0.7</priority>\n    <changefreq>weekly</changefreq>", xml);
        Assert.Contains("<loc>https://fibrasinmobiliarias.com/calculadora</loc>\n    <priority>0.9</priority>\n    <changefreq>daily</changefreq>", xml);
    }

    [Fact]
    public void SitemapContainsFibraSlugUrls()
    {
        var xml = SeoEndpoints.BuildSitemapXml(BaseUrl, SampleFibras);

        Assert.Contains("<loc>https://fibrasinmobiliarias.com/fibras/fibra-uno-funo11</loc>\n    <priority>0.8</priority>\n    <changefreq>weekly</changefreq>", xml);
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
        // 8 rutas estáticas + 2 fibras
        Assert.Equal(10, doc.Root.Elements().Count());
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
