using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Domain.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;
using SharedApiContracts.Seo;

namespace Api.Tests;

public class SeoRobotsEndpointTests
{
    private const string RecommendedRobots = "index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1";
    private const string NoIndexRobots = "noindex,nofollow";

    [Fact]
    public async Task OpsSeoEndpoints_RequireAdminOps()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();

        var anonClient = factory.CreateClient();
        var userClient = await CreateAuthenticatedClientAsync(factory, "user@test.com", "password123");

        var anonResponse = await anonClient.GetAsync("/api/v1/ops/seo");
        var userResponse = await userClient.GetAsync("/api/v1/ops/seo");

        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);
    }

    [Fact]
    public async Task GetOpsSeo_WithoutPageType_ReturnsActiveRows()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();

        var adminClient = await CreateAuthenticatedClientAsync(factory, "adminops@test.com", "ops123");
        await SeedSeoMetadataAsync(factory, RecommendedRobots);

        // "Todos" en el editor no envía pageType: debe responder 200 (sin filtro), no 400.
        var response = await adminClient.GetAsync("/api/v1/ops/seo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = await response.Content.ReadFromJsonAsync<List<SeoMetadataDto>>();
        Assert.NotNull(rows);
        Assert.Contains(rows!, row => row.EntityKey == "/fundamentales");
        Assert.All(rows!, row => Assert.True(row.IsActive));
    }

    [Fact]
    public async Task PutOpsSeo_NoIndex_UpdatesMetaAndRemovesFromSitemap()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();

        var adminClient = await CreateAuthenticatedClientAsync(factory, "adminops@test.com", "ops123");
        var anonClient = factory.CreateClient();
        var row = await SeedSeoMetadataAsync(factory, RecommendedRobots);

        var beforeResponse = await anonClient.GetAsync("/fundamentales");
        var beforeBody = await beforeResponse.Content.ReadAsStringAsync();
        Assert.Contains($"""<meta name="robots" content="{RecommendedRobots}" />""", beforeBody);

        var warmSitemapResponse = await anonClient.GetAsync("/sitemap-static.xml");
        var warmSitemapBody = await warmSitemapResponse.Content.ReadAsStringAsync();
        Assert.Contains("/fundamentales</loc>", warmSitemapBody);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/v1/ops/seo/{row.Id}",
            new UpdateSeoMetadataRequest(NoIndexRobots));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SeoMetadataDto>();
        Assert.NotNull(updated);
        Assert.Equal(NoIndexRobots, updated!.RobotsDirectives);
        Assert.True(updated.RobotsDirectivesIsOverridden);

        var afterResponse = await anonClient.GetAsync("/fundamentales");
        var afterBody = await afterResponse.Content.ReadAsStringAsync();
        Assert.Contains("""<meta name="robots" content="noindex,nofollow" />""", afterBody);

        var sitemapResponse = await anonClient.GetAsync("/sitemap-static.xml");
        var sitemapBody = await sitemapResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("/fundamentales</loc>", sitemapBody);
    }

    [Fact]
    public async Task PutOpsSeo_IndexablePreset_PersistsRecommendedRobots()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();

        var adminClient = await CreateAuthenticatedClientAsync(factory, "adminops@test.com", "ops123");
        var anonClient = factory.CreateClient();
        var row = await SeedSeoMetadataAsync(factory, NoIndexRobots);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/v1/ops/seo/{row.Id}",
            new UpdateSeoMetadataRequest(RecommendedRobots));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SeoMetadataDto>();
        Assert.NotNull(updated);
        Assert.Equal(RecommendedRobots, updated!.RobotsDirectives);
        Assert.True(updated.RobotsDirectivesIsOverridden);

        var publicResponse = await anonClient.GetAsync("/fundamentales");
        var publicBody = await publicResponse.Content.ReadAsStringAsync();
        Assert.Contains($"""<meta name="robots" content="{RecommendedRobots}" />""", publicBody);
    }

    [Fact]
    public async Task PutOpsSeo_InvalidRobotsDirectives_ReturnsBadRequest()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();

        var adminClient = await CreateAuthenticatedClientAsync(factory, "adminops@test.com", "ops123");
        var row = await SeedSeoMetadataAsync(factory, RecommendedRobots);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/v1/ops/seo/{row.Id}",
            new UpdateSeoMetadataRequest("index,noindex,follow"));

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(ApiWebFactory factory, string email, string password)
    {
        var loginClient = factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
        return client;
    }

    private static async Task<SeoMetadata> SeedSeoMetadataAsync(ApiWebFactory factory, string robotsDirectives)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var row = await db.SeoMetadata.FirstOrDefaultAsync(metadata =>
            metadata.PageType == SeoPageType.StaticPage &&
            metadata.EntityKey == "/fundamentales");

        if (row is null)
        {
            row = new SeoMetadata
            {
                Id = Guid.NewGuid(),
                PageType = SeoPageType.StaticPage,
                EntityKey = "/fundamentales",
                Title = "Fundamentales FIBRAs — Cap Rate, NAV, NOI | Fibras Inmobiliarias",
                MetaDescription = "Panel de fundamentales para FIBRAs mexicanas con Cap Rate, NAV/CBFI, LTV y márgenes.",
                CanonicalPath = "/fundamentales",
                OgTitle = "Fundamentales FIBRAs — Cap Rate, NAV, NOI | Fibras Inmobiliarias",
                OgDescription = "Panel de fundamentales para FIBRAs mexicanas con Cap Rate, NAV/CBFI, LTV y márgenes.",
                OgType = "website",
                OgImageUrl = "https://fibrasinmobiliarias.com/og-image.png",
                OgLocale = "es_MX",
                TwitterCard = "summary_large_image",
                RobotsDirectives = robotsDirectives,
                JsonLd = null,
                IsActive = true,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = "test",
            };
            db.SeoMetadata.Add(row);
        }
        else
        {
            row.RobotsDirectives = robotsDirectives;
            row.RobotsDirectivesIsOverridden = false;
            row.UpdatedAt = DateTimeOffset.UtcNow;
            row.UpdatedBy = "test";
        }

        await db.SaveChangesAsync();
        return row;
    }
}
