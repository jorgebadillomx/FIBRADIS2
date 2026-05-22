using System.Net;
using System.Text.Json;
using Domain.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class NewsLatestEndpointTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>
{
    private readonly ApiWebFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    private static bool _seeded;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private async Task EnsureSeededAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_seeded) return;

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .AnyAsync(db.NewsArticles, a => a.Id == Guid.Parse("77777777-0001-0000-0000-000000000001")))
            {
                db.NewsArticles.AddRange(
                    MakeArticle(Guid.Parse("77777777-0001-0000-0000-000000000001"), "FUNO11 obtiene calificación crediticia mejorada", DateTimeOffset.UtcNow.AddHours(-1)),
                    MakeArticle(Guid.Parse("77777777-0002-0000-0000-000000000001"), "DANHOS13 inaugura nuevo centro comercial", DateTimeOffset.UtcNow.AddHours(-2)),
                    MakeArticle(Guid.Parse("77777777-0003-0000-0000-000000000001"), "FMTY14 reporta ocupación al 98%", DateTimeOffset.UtcNow.AddHours(-3))
                );
                await db.SaveChangesAsync();
            }

            _seeded = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    [Fact]
    public async Task GetLatestNews_ReturnsOkWithArray()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/news");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetLatestNews_ReturnsAtMostFiveArticles()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/news");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetLatestNews_EachArticleHasRequiredFields()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/news");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        foreach (var article in doc.RootElement.EnumerateArray())
        {
            Assert.True(article.TryGetProperty("id", out _), "missing: id");
            Assert.True(article.TryGetProperty("title", out _), "missing: title");
            Assert.True(article.TryGetProperty("source", out _), "missing: source");
            Assert.True(article.TryGetProperty("publishedAt", out _), "missing: publishedAt");
            Assert.True(article.TryGetProperty("url", out _), "missing: url");
        }
    }

    [Fact]
    public async Task GetLatestNews_IncludesSeededArticles()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/news");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("FUNO11 obtiene calificación crediticia mejorada", json);
    }

    [Fact]
    public async Task GetNewsByFibraId_ReturnsOkWithArray()
    {
        await EnsureSeededAsync();

        // Cualquier GUID de fibra activa (FUNO11 via HasData)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        var funo = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(db.Fibras, f => f.Ticker == "FUNO11");

        Assert.NotNull(funo);
        var response = await _client.GetAsync($"/api/v1/news/fibras/{funo.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetNewsByFibraId_FibraWithNoAssociatedNews_ReturnsEmptyArray()
    {
        await EnsureSeededAsync();

        // DANHOS13 no tiene noticias asociadas en el seed
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        var danhos = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(db.Fibras, f => f.Ticker == "DANHOS13");

        Assert.NotNull(danhos);
        var response = await _client.GetAsync($"/api/v1/news/fibras/{danhos.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    private static NewsArticle MakeArticle(Guid id, string title, DateTimeOffset publishedAt) =>
        new()
        {
            Id = id,
            Title = title,
            TitleNormalized = title.ToLowerInvariant(),
            Source = "El Financiero",
            PublishedAt = publishedAt,
            Url = $"https://example.com/noticias/{id}",
            Snippet = "Snippet de prueba.",
            ImageUrl = null,
            AiSummary = null,
            Status = NewsArticleStatus.Processed,
            CapturedAt = DateTimeOffset.UtcNow,
        };
}
