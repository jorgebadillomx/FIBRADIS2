using System.Net;

namespace Api.Tests;

public class SeoEndpointTests(ApiWebFactory factory)
    : IClassFixture<ApiWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetSitemap_ReturnsOk_WithXmlContentType()
    {
        var response = await _client.GetAsync("/sitemap.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
    }

    [Fact]
    public async Task GetSitemap_IsWellFormedXml_WithSitemapsOrgNamespace()
    {
        var response = await _client.GetAsync("/sitemap.xml");
        var body = await response.Content.ReadAsStringAsync();

        var doc = System.Xml.Linq.XDocument.Parse(body);
        Assert.Equal("urlset", doc.Root!.Name.LocalName);
        Assert.Equal("http://www.sitemaps.org/schemas/sitemap/0.9", doc.Root.Name.NamespaceName);
    }

    [Fact]
    public async Task GetSitemap_ContainsActiveFibrasAsSlugUrls()
    {
        // HasData siembra las FIBRAs activas al crear el store InMemory
        await factory.SeedCatalogAsync();
        var response = await _client.GetAsync("/sitemap.xml");
        var body = await response.Content.ReadAsStringAsync();

        // FUNO11 ("Fibra Uno") viene del seed HasData — URL slug, no ticker (CA-3)
        Assert.Contains("/fibras/fibra-uno-funo11</loc>", body);
        Assert.DoesNotContain("/fibras/FUNO11</loc>", body);
    }

    [Fact]
    public async Task GetRobotsTxt_ReturnsOk_WithPlainTextContentType()
    {
        var response = await _client.GetAsync("/robots.txt");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
    }

    [Theory]
    [InlineData("/sitemap.xml")]
    [InlineData("/robots.txt")]
    public async Task HeadSeoEndpoints_ReturnsOk(string path)
    {
        // los validadores SEO y curl -I usan HEAD — MapGet solo respondería 405
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, path));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRobotsTxt_ContainsDisallowsAndSitemapReference()
    {
        var response = await _client.GetAsync("/robots.txt");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("User-agent: *", body);
        Assert.Contains("Disallow: /ops/", body);
        Assert.Contains("Disallow: /api/", body);
        Assert.Contains("Disallow: /hangfire/", body);
        Assert.Contains("Sitemap: ", body);
        Assert.Contains("/sitemap.xml", body);
    }
}
