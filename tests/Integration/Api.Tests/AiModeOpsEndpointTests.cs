using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.News;
using Domain.News;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedApiContracts.Auth;

namespace Api.Tests;

public class AiModeOpsEndpointTests
{
    [Fact]
    public async Task PostAiSummary_WhenModeIsNotManual_ReturnsProblemDetails400()
    {
        var repository = new InMemoryNewsRepository();
        await using var factory = new AiModeApiWebFactory(
            new StubAiSummaryService("should not be called"),
            repository,
            new StubAiModeRepository(AiMode.Off));
        await factory.SeedUsersAsync();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-summary", content: null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("AI_MODE=Manual", body);
        Assert.Equal(0, repository.UpdateAttempts);
    }

    [Fact]
    public async Task PostAiSummary_WhenSummaryServiceReturnsNull_Returns503()
    {
        var repository = new InMemoryNewsRepository();
        await using var factory = new AiModeApiWebFactory(
            new StubAiSummaryService(null),
            repository,
            new StubAiModeRepository(AiMode.Manual));
        await factory.SeedUsersAsync();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-summary", content: null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, repository.UpdateAttempts);
        Assert.Equal(NewsArticleStatus.Pending, repository.Article.Status);
    }

    [Fact]
    public async Task PostAiSummary_WhenSummaryServiceThrows_MarksArticleAsPartialAndReturns502()
    {
        var repository = new InMemoryNewsRepository();
        await using var factory = new AiModeApiWebFactory(
            new ThrowingAiSummaryService(new TaskCanceledException("Gemini timeout")),
            repository,
            new StubAiModeRepository(AiMode.Manual));
        await factory.SeedUsersAsync();

        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-summary", content: null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(1, repository.UpdateAttempts);
        Assert.Equal(NewsArticleStatus.Partial, repository.Article.Status);
        Assert.Null(repository.Article.AiSummary);
    }

    [Fact]
    public async Task PostAiSummary_WhenPartialUpdateFails_AbsorbsSecondaryFailureAndReturns502()
    {
        var repository = new InMemoryNewsRepository(throwOnUpdate: true);
        await using var factory = new AiModeApiWebFactory(
            new ThrowingAiSummaryService(new InvalidOperationException("Gemini unavailable")),
            repository,
            new StubAiModeRepository(AiMode.Manual));
        await factory.SeedUsersAsync();

        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-summary", content: null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(1, repository.UpdateAttempts);
        Assert.Equal(NewsArticleStatus.Pending, repository.Article.Status);
        Assert.Null(repository.Article.AiSummary);
    }

    [Fact]
    public async Task PostAiSummary_WhenArticleAlreadyProcessed_ReturnsNoContentWithoutCallingUpdate()
    {
        var repository = new InMemoryNewsRepository(initialStatus: NewsArticleStatus.Processed);
        await using var factory = new AiModeApiWebFactory(
            new StubAiSummaryService("should not be called"),
            repository,
            new StubAiModeRepository(AiMode.Manual));
        await factory.SeedUsersAsync();

        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-summary", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, repository.UpdateAttempts);
    }

    private static async Task<HttpClient> CreateAuthorizedClientAsync(ApiWebFactory factory)
    {
        var client = factory.CreateClient();
        var adminLogin = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("adminops@test.com", "admin456"));
        var adminBody = await adminLogin.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBody!.AccessToken);
        return client;
    }

    private sealed class AiModeApiWebFactory(
        IAiSummaryService aiSummaryService,
        INewsRepository? newsRepositoryOverride = null,
        IAiModeRepository? aiModeRepositoryOverride = null) : ApiWebFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAiSummaryService>();
                services.AddSingleton(aiSummaryService);

                if (newsRepositoryOverride is not null)
                {
                    services.RemoveAll<INewsRepository>();
                    services.AddSingleton(newsRepositoryOverride);
                }

                if (aiModeRepositoryOverride is not null)
                {
                    services.RemoveAll<IAiModeRepository>();
                    services.AddSingleton(aiModeRepositoryOverride);
                }
            });
        }
    }

    private sealed class StubAiSummaryService(string? summary) : IAiSummaryService
    {
        public Task<string?> GenerateSummaryAsync(string title, string? snippet, AiContentType contentType = AiContentType.News, CancellationToken ct = default)
            => Task.FromResult(summary);
    }

    private sealed class ThrowingAiSummaryService(Exception exception) : IAiSummaryService
    {
        public Task<string?> GenerateSummaryAsync(string title, string? snippet, AiContentType contentType = AiContentType.News, CancellationToken ct = default)
            => Task.FromException<string?>(exception);
    }

    private sealed class StubAiModeRepository(AiMode mode) : IAiModeRepository
    {
        public Task<AiMode> GetCurrentModeAsync(CancellationToken ct = default)
            => Task.FromResult(mode);

        public Task<AiModeConfig> GetConfigAsync(CancellationToken ct = default)
            => Task.FromResult(new AiModeConfig
            {
                Id = 1,
                Mode = mode,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = "test",
            });

        public Task SetModeAsync(AiMode newMode, string actor, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class InMemoryNewsRepository(
        bool throwOnUpdate = false,
        NewsArticleStatus initialStatus = NewsArticleStatus.Pending) : INewsRepository
    {
        public NewsArticle Article { get; } = new()
        {
            Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
            Title = "FUNO11 anuncia resultados",
            TitleNormalized = "funo11 anuncia resultados",
            Source = "Fuente",
            PublishedAt = DateTimeOffset.UtcNow,
            Url = "https://example.com/noticia-timeout",
            Snippet = "Snippet original",
            Status = initialStatus,
            CapturedAt = DateTimeOffset.UtcNow,
        };

        public int UpdateAttempts { get; private set; }

        public Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> GetExistingUrlsAsync(IEnumerable<string> candidateUrls, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> GetRecentNormalizedTitlesAsync(DateTimeOffset since, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task AddWithLinksAsync(NewsArticle article, IEnumerable<Guid> fibraIds, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(id == Article.Id ? Article : null);

        public Task UpdateSummaryAsync(Guid id, string? summary, NewsArticleStatus status, CancellationToken ct = default)
        {
            UpdateAttempts++;

            if (throwOnUpdate)
            {
                throw new InvalidOperationException("Database unavailable");
            }

            Article.AiSummary = summary;
            Article.Status = status;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
