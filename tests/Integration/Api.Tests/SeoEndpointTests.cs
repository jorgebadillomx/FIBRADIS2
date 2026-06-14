using System.Net;
using Domain.News;
using Domain.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class SeoEndpointTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>
{
    private readonly ApiWebFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetSitemapIndex_ReturnsOk_WithXmlContentType()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/sitemap.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
    }

    [Fact]
    public async Task GetSitemapIndex_IsWellFormedXml_WithSitemapsOrgNamespace()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/sitemap.xml");
        var body = await response.Content.ReadAsStringAsync();

        var doc = System.Xml.Linq.XDocument.Parse(body);
        Assert.Equal("sitemapindex", doc.Root!.Name.LocalName);
        Assert.Equal("http://www.sitemaps.org/schemas/sitemap/0.9", doc.Root.Name.NamespaceName);
    }

    [Fact]
    public async Task GetSitemapIndex_ReferencesSectionSitemaps()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();

        await SeedNewsArticlesAsync(factory);
        await factory.SeedCatalogAsync();

        var response = await client.GetAsync("/sitemap.xml");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("/sitemap-static.xml", body);
        Assert.Contains("/sitemap-fibras.xml", body);
        Assert.Contains("/sitemap-noticias-1.xml", body);
    }

    [Fact]
    public async Task GetSitemapStatic_ExcludesNoindexStaticPages()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();

        await SeedSeoMetadataAsync(factory, SeoPageType.StaticPage, "/calculadora", "noindex,follow");

        var response = await client.GetAsync("/sitemap-static.xml");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("/noticias</loc>", body);
        Assert.DoesNotContain("/calculadora</loc>", body);
    }

    [Fact]
    public async Task GetSitemapFibras_ExcludesNoindexFibras()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();

        await factory.SeedCatalogAsync();
        await SeedSeoMetadataAsync(factory, SeoPageType.Fibra, "FUNO11", "noindex,follow");

        var response = await client.GetAsync("/sitemap-fibras.xml");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("fibra-uno-funo11", body);
        Assert.Contains("fibra-macquarie-fibramq12", body);
    }

    [Fact]
    public async Task GetSitemapNoticias_ExcludesNoindexAndSoftDeletedArticles()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();

        await SeedNewsArticlesAsync(factory);
        await SeedSeoMetadataAsync(factory, SeoPageType.News, "noticia-noindex", "noindex,follow");

        var response = await client.GetAsync("/sitemap-noticias-1.xml");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("/noticias/noticia-visible</loc>", body);
        Assert.DoesNotContain("/noticias/noticia-noindex</loc>", body);
        Assert.DoesNotContain("/noticias/noticia-borrada</loc>", body);
    }

    [Fact]
    public async Task GetLlmsTxt_ReturnsOk_WithPlainTextContentType()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/llms.txt");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
    }

    [Fact]
    public async Task GetLlmsTxt_ContainsKeyPages()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/llms.txt");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("# FIBRADIS", body);
        Assert.Contains("/conoce-las-fibras", body);
        Assert.Contains("/fundamentales", body);
    }

    [Fact]
    public async Task GetNoticiasPageTwo_UsesSelfCanonical_AndNoindexFollow()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();

        await factory.SeedCatalogAsync();
        var response = await client.GetAsync("/noticias?page=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/sitemap.xml")]
    [InlineData("/sitemap-static.xml")]
    [InlineData("/sitemap-fibras.xml")]
    [InlineData("/sitemap-noticias-1.xml")]
    [InlineData("/llms.txt")]
    [InlineData("/robots.txt")]
    public async Task HeadSeoEndpoints_ReturnsOk(string path)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, path));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRobotsTxt_ContainsDisallowsSitemapAndLlmsReference()
    {
        var response = await _client.GetAsync("/robots.txt");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("User-agent: *", body);
        Assert.Contains("Disallow: /ops/", body);
        Assert.Contains("Disallow: /api/", body);
        Assert.Contains("Disallow: /hangfire/", body);
        Assert.Contains("Sitemap: ", body);
        Assert.Contains("/sitemap.xml", body);
        Assert.Contains("/llms.txt", body);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsNotFound_NotSoft200()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/esta-pagina-no-existe-xyz123");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/fibras")]
    [InlineData("/noticias")]
    [InlineData("/herramientas")]
    public async Task KnownSpaRoute_ReturnsOk(string path)
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task SeedNewsArticlesAsync(ApiWebFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.NewsArticles, article => article.Slug == "noticia-visible"))
        {
            db.NewsArticles.AddRange(
                CreateNewsArticle("noticia-visible", deletedAt: null),
                CreateNewsArticle("noticia-noindex", deletedAt: null),
                CreateNewsArticle("noticia-borrada", deletedAt: DateTimeOffset.UtcNow));

            await db.SaveChangesAsync();
        }
    }

    private async Task SeedSeoMetadataAsync(ApiWebFactory factory, SeoPageType pageType, string entityKey, string robotsDirectives)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.SeoMetadata, metadata => metadata.PageType == pageType && metadata.EntityKey == entityKey))
        {
            var canonicalPath = pageType switch
            {
                SeoPageType.Home => "/",
                SeoPageType.StaticPage => entityKey.StartsWith('/') ? entityKey : $"/{entityKey}",
                SeoPageType.Fibra => $"/fibras/{entityKey.ToLowerInvariant()}",
                SeoPageType.News => $"/noticias/{entityKey}",
                _ => entityKey,
            };

            db.SeoMetadata.Add(new SeoMetadata
            {
                Id = Guid.NewGuid(),
                PageType = pageType,
                EntityKey = entityKey,
                Title = "SEO de prueba",
                MetaDescription = "Descripción de prueba.",
                CanonicalPath = canonicalPath,
                OgTitle = "SEO de prueba",
                OgDescription = "Descripción de prueba.",
                OgType = "website",
                OgImageUrl = "https://fibrasinmobiliarias.com/og-image.png",
                OgLocale = "es_MX",
                TwitterCard = "summary_large_image",
                RobotsDirectives = robotsDirectives,
                JsonLd = null,
                IsActive = true,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = "test",
            });

            await db.SaveChangesAsync();
        }
    }

    private static NewsArticle CreateNewsArticle(string slug, DateTimeOffset? deletedAt)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = slug,
            TitleNormalized = slug,
            Slug = slug,
            Source = "Test Source",
            PublishedAt = new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero),
            Url = $"https://example.com/{slug}",
            Snippet = $"Snippet de {slug}",
            Status = NewsArticleStatus.Processed,
            CapturedAt = DateTimeOffset.UtcNow,
            DeletedAt = deletedAt,
        };
}
