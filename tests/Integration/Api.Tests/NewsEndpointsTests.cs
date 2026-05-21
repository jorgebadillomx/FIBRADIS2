using System.Net;
using System.Net.Http.Json;
using Domain.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.News;

namespace Api.Tests;

public class NewsEndpointsTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>
{
    private readonly ApiWebFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetNewsArticleById_WhenArticleExists_ReturnsDto()
    {
        var articleId = Guid.Parse("99999999-0000-0000-0000-000000000001");
        await SeedNewsArticleAsync(articleId, url: "https://example.com/noticia-preview");

        var response = await _client.GetAsync($"/api/v1/news/{articleId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var article = await response.Content.ReadFromJsonAsync<NewsArticleDto>();
        Assert.NotNull(article);
        Assert.Equal(articleId, article.Id);
        Assert.Equal("FUNO11 anuncia resultados trimestrales", article.Title);
        Assert.Equal("https://example.com/preview.jpg", article.ImageUrl);
        Assert.Equal("Resumen generado por IA", article.AiSummary);
    }

    [Fact]
    public async Task GetNewsArticleById_WhenArticleDoesNotExist_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/news/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task SeedNewsArticleAsync(Guid articleId, string url)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var existing = await db.NewsArticles.FindAsync(articleId);
        if (existing is not null)
        {
            return;
        }

        db.NewsArticles.Add(new NewsArticle
        {
            Id = articleId,
            Title = "FUNO11 anuncia resultados trimestrales",
            TitleNormalized = "funo11 anuncia resultados trimestrales",
            Source = "Expansión",
            PublishedAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero),
            Url = url,
            Snippet = "Snippet de prueba",
            ImageUrl = "https://example.com/preview.jpg",
            AiSummary = "Resumen generado por IA",
            Status = NewsArticleStatus.Processed,
            CapturedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();
    }
}
