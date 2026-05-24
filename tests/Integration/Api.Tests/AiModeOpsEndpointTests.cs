using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.News;
using Domain.News;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedApiContracts.Auth;
using SharedApiContracts.News;

namespace Api.Tests;

public class AiModeOpsEndpointTests
{
    [Fact]
    public async Task PostAiSummary_WhenModeIsOff_StillGeneratesSummary()
    {
        var repository = new InMemoryNewsRepository();
        await using var factory = new AiModeApiWebFactory(
            new StubAiSummaryService("Resumen generado"),
            new StubArticleContentScraper(null),
            repository,
            new StubAiModeRepository(AiMode.Off));
        await factory.SeedUsersAsync();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-summary", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, repository.UpdateAttempts);
        Assert.Equal(NewsArticleStatus.Processed, repository.Article.Status);
    }

    [Fact]
    public async Task PostAiSummary_WhenSummaryServiceReturnsNull_Returns503()
    {
        var repository = new InMemoryNewsRepository();
        await using var factory = new AiModeApiWebFactory(
            new StubAiSummaryService(null),
            new StubArticleContentScraper(null),
            repository,
            new StubAiModeRepository(AiMode.On));
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
            new StubArticleContentScraper(null),
            repository,
            new StubAiModeRepository(AiMode.On));
        await factory.SeedUsersAsync();

        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-summary", content: null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(1, repository.UpdateAttempts);
        Assert.Equal(NewsArticleStatus.Partial, repository.Article.Status);
        Assert.Null(repository.Article.AiSummary);
    }

    [Fact]
    public async Task PostAiSummary_WhenSummaryServiceHasConfigurationError_Returns503WithoutUpdatingArticle()
    {
        var repository = new InMemoryNewsRepository();
        await using var factory = new AiModeApiWebFactory(
            new ThrowingAiSummaryService(new AiProviderConfigurationException("API key rechazada")),
            new StubArticleContentScraper(null),
            repository,
            new StubAiModeRepository(AiMode.On));
        await factory.SeedUsersAsync();

        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-summary", content: null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, repository.UpdateAttempts);
        Assert.Equal(NewsArticleStatus.Pending, repository.Article.Status);
        Assert.Null(repository.Article.AiSummary);
    }

    [Fact]
    public async Task PostAiSummary_WhenPartialUpdateFails_AbsorbsSecondaryFailureAndReturns502()
    {
        var repository = new InMemoryNewsRepository(throwOnUpdate: true);
        await using var factory = new AiModeApiWebFactory(
            new ThrowingAiSummaryService(new InvalidOperationException("Gemini unavailable")),
            new StubArticleContentScraper(null),
            repository,
            new StubAiModeRepository(AiMode.On));
        await factory.SeedUsersAsync();

        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-summary", content: null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(1, repository.UpdateAttempts);
        Assert.Equal(NewsArticleStatus.Pending, repository.Article.Status);
        Assert.Null(repository.Article.AiSummary);
    }

    [Fact]
    public async Task PostAiSummary_WhenArticleAlreadyProcessed_RegeneratesSummary()
    {
        var repository = new InMemoryNewsRepository(initialStatus: NewsArticleStatus.Processed);
        await using var factory = new AiModeApiWebFactory(
            new StubAiSummaryService("Resumen regenerado"),
            new StubArticleContentScraper("Cuerpo completo recuperado"),
            repository,
            new StubAiModeRepository(AiMode.On));
        await factory.SeedUsersAsync();

        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-summary", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, repository.UpdateAttempts);
        Assert.Equal(NewsArticleStatus.Processed, repository.Article.Status);
        Assert.Equal("Resumen regenerado", repository.Article.AiSummary);
    }

    // ─── AiProvider endpoints ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAiProvider_ReturnsCurrentProviderConfig()
    {
        var providerRepo = new StubAiProviderConfigRepository(AiProvider.Gemini, "gemini-2.5-flash");
        await using var factory = new AiProviderApiWebFactory(providerRepo);
        await factory.SeedUsersAsync();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.GetAsync("/api/v1/ops/ai-provider");
        var dto = await response.Content.ReadFromJsonAsync<AiProviderConfigDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal("Gemini", dto.Provider);
        Assert.Equal("gemini-2.5-flash", dto.ModelId);
        Assert.NotEmpty(dto.AvailableProviders);
    }

    [Fact]
    public async Task PutAiProvider_WithValidProviderAndModel_Returns204AndPersists()
    {
        var providerRepo = new StubAiProviderConfigRepository(AiProvider.Gemini, "gemini-2.5-flash");
        await using var factory = new AiProviderApiWebFactory(providerRepo);
        await factory.SeedUsersAsync();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PutAsJsonAsync("/api/v1/ops/ai-provider",
            new { provider = "DeepSeek", modelId = "deepseek-v4-flash" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AiProvider.DeepSeek, providerRepo.SavedProvider);
        Assert.Equal("deepseek-v4-flash", providerRepo.SavedModelId);
    }

    [Fact]
    public async Task PutAiProvider_WithInvalidProvider_Returns400()
    {
        var providerRepo = new StubAiProviderConfigRepository(AiProvider.Gemini, "gemini-2.5-flash");
        await using var factory = new AiProviderApiWebFactory(providerRepo);
        await factory.SeedUsersAsync();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PutAsJsonAsync("/api/v1/ops/ai-provider",
            new { provider = "ProveedorInexistente", modelId = "modelo" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAiProvider_WithInvalidModelForProvider_Returns400()
    {
        var providerRepo = new StubAiProviderConfigRepository(AiProvider.Gemini, "gemini-2.5-flash");
        await using var factory = new AiProviderApiWebFactory(providerRepo);
        await factory.SeedUsersAsync();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PutAsJsonAsync("/api/v1/ops/ai-provider",
            new { provider = "Gemini", modelId = "deepseek-v4-flash" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<HttpClient> CreateAuthorizedClientAsync(ApiWebFactory factory)
    {
        var client = factory.CreateClient();
        var adminLogin = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("adminops@test.com", "ops123"));
        var adminBody = await adminLogin.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBody!.AccessToken);
        return client;
    }

    private sealed class AiModeApiWebFactory(
        IAiSummaryService aiSummaryService,
        IArticleContentScraper articleContentScraper,
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
                services.RemoveAll<IArticleContentScraper>();
                services.AddSingleton(articleContentScraper);

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
        public Task<string?> GenerateSummaryAsync(string title, string? snippet, string? bodyText = null, AiContentType contentType = AiContentType.News, CancellationToken ct = default)
            => Task.FromResult(summary);
    }

    private sealed class ThrowingAiSummaryService(Exception exception) : IAiSummaryService
    {
        public Task<string?> GenerateSummaryAsync(string title, string? snippet, string? bodyText = null, AiContentType contentType = AiContentType.News, CancellationToken ct = default)
            => Task.FromException<string?>(exception);
    }

    private sealed class StubArticleContentScraper(string? bodyText) : IArticleContentScraper
    {
        public Task<string?> TryGetArticleTextAsync(string url, CancellationToken ct = default)
            => Task.FromResult(bodyText);
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
                NewsModel = "gemini-2.5-pro",
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = "test",
            });

        public Task SetModeAsync(AiMode newMode, string actor, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task UpdateConfigAsync(AiMode? newMode, string? newsModel, string actor, CancellationToken ct = default)
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
            BodyText = null,
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

        public int UpdateBodyTextAttempts { get; private set; }

        public Task UpdateBodyTextAsync(Guid id, string? bodyText, CancellationToken ct = default)
        {
            UpdateBodyTextAttempts++;
            Article.BodyText = bodyText;
            return Task.CompletedTask;
        }

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

        public Task<(IReadOnlyList<NewsArticle> Items, int Total)> GetPagedForOpsAsync(int page, int pageSize, string? search, bool? hasAiSummary, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<NewsArticle>, int)>(([Article], 1));

        public Task<IReadOnlyList<(Guid Id, string Url)>> GetNullBodyTextArticlesAsync(int maxArticles, int daysBack, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<(Guid Id, string Url)>>([]);
    }

    private sealed class AiProviderApiWebFactory(IAiProviderConfigRepository providerRepo) : ApiWebFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAiProviderConfigRepository>();
                services.AddSingleton(providerRepo);
                services.RemoveAll<IAiSummaryService>();
                services.AddSingleton<IAiSummaryService>(new StubAiSummaryService("resumen"));
            });
        }
    }

    private sealed class StubAiProviderConfigRepository(AiProvider provider, string modelId) : IAiProviderConfigRepository
    {
        public AiProvider? SavedProvider { get; private set; }
        public string? SavedModelId { get; private set; }

        public Task<AiProviderConfig> GetConfigAsync(CancellationToken ct = default)
            => Task.FromResult(new AiProviderConfig
            {
                Id = 1,
                Provider = provider,
                ModelId = modelId,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = "system",
            });

        public Task SetProviderAsync(AiProvider p, string m, string actor, CancellationToken ct = default)
        {
            SavedProvider = p;
            SavedModelId = m;
            return Task.CompletedTask;
        }
    }
}
