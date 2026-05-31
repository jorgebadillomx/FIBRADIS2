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
using System.Text.Json;

namespace Api.Tests;

public class AiModeOpsEndpointTests
{
    [Fact]
    public async Task PostAiSummary_WhenModeIsOff_StillGeneratesSummary()
    {
        var repository = new InMemoryNewsRepository();
        await using var factory = new AiModeApiWebFactory(
            new StubAiNewsAnalysisService("Resumen generado"),
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
            new StubAiNewsAnalysisService(null),
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
            new ThrowingAiNewsAnalysisService(new TaskCanceledException("Gemini timeout")),
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
            new ThrowingAiNewsAnalysisService(new AiProviderConfigurationException("API key rechazada")),
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
            new ThrowingAiNewsAnalysisService(new InvalidOperationException("Gemini unavailable")),
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
            new StubAiNewsAnalysisService("Resumen regenerado"),
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

    [Fact]
    public async Task PostAiSummary_WhenBodyIsRefreshed_NormalizesAndPersistsCleanBodyText()
    {
        var repository = new InMemoryNewsRepository(initialStatus: NewsArticleStatus.Pending);
        await using var factory = new AiModeApiWebFactory(
            new StubAiNewsAnalysisService("Resumen regenerado"),
            new StubArticleContentScraper("""
                Compartir

                Cuerpo completo recuperado

                Cuerpo completo recuperado
                """),
            repository,
            new StubAiModeRepository(AiMode.On));
        await factory.SeedUsersAsync();

        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-summary", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, repository.UpdateBodyTextAttempts);
        Assert.Equal("Cuerpo completo recuperado", repository.Article.BodyText);
    }

    // ─── POST /ai-analysis endpoint ───────────────────────────────────────────

    [Fact]
    public async Task PostAiAnalysis_Returns200WithAnalysisDto()
    {
        var repository = new InMemoryNewsRepository();
        await using var factory = new AiModeApiWebFactory(
            new StubAiNewsAnalysisService("Resumen analítico."),
            new StubArticleContentScraper(null),
            repository,
            new StubAiModeRepository(AiMode.On));
        await factory.SeedUsersAsync();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-analysis", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<NewsAiAnalysisDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(dto);
        Assert.True(dto.IsRelevant);
        Assert.Equal("medio", dto.Impact);
        Assert.Equal("Resumen analítico.", dto.SummaryMarkdown);
        Assert.Equal(1, repository.UpdateAttempts);
        Assert.Equal(NewsArticleStatus.Processed, repository.Article.Status);
        Assert.NotNull(repository.Article.AiAnalysisJson);
    }

    [Fact]
    public async Task PostAiAnalysis_WhenAnalysisServiceReturnsNull_Returns503()
    {
        var repository = new InMemoryNewsRepository();
        await using var factory = new AiModeApiWebFactory(
            new StubAiNewsAnalysisService(null),
            new StubArticleContentScraper(null),
            repository,
            new StubAiModeRepository(AiMode.On));
        await factory.SeedUsersAsync();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-analysis", content: null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, repository.UpdateAttempts);
    }

    [Fact]
    public async Task PostAiAnalysis_WhenArticleNotFound_Returns404()
    {
        var repository = new InMemoryNewsRepository();
        await using var factory = new AiModeApiWebFactory(
            new StubAiNewsAnalysisService("resumen"),
            new StubArticleContentScraper(null),
            repository,
            new StubAiModeRepository(AiMode.On));
        await factory.SeedUsersAsync();
        using var client = await CreateAuthorizedClientAsync(factory);

        var response = await client.PostAsync($"/api/v1/ops/news/{Guid.NewGuid()}/ai-analysis", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostAiAnalysis_WithoutAuth_Returns401Or403()
    {
        var repository = new InMemoryNewsRepository();
        await using var factory = new AiModeApiWebFactory(
            new StubAiNewsAnalysisService("resumen"),
            new StubArticleContentScraper(null),
            repository,
            new StubAiModeRepository(AiMode.On));
        await factory.SeedUsersAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/ops/news/{repository.Article.Id}/ai-analysis", content: null);

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected 401 or 403, got {response.StatusCode}");
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
        IAiNewsAnalysisService aiAnalysisService,
        IArticleContentScraper articleContentScraper,
        INewsRepository? newsRepositoryOverride = null,
        IAiModeRepository? aiModeRepositoryOverride = null) : ApiWebFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAiNewsAnalysisService>();
                services.AddSingleton(aiAnalysisService);
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

    private sealed class StubAiNewsAnalysisService(string? summaryMarkdown) : IAiNewsAnalysisService
    {
        public Task<NewsAiAnalysis?> GenerateAnalysisAsync(string title, string? snippet, string? bodyText, CancellationToken ct = default)
        {
            if (summaryMarkdown is null) return Task.FromResult<NewsAiAnalysis?>(null);
            return Task.FromResult<NewsAiAnalysis?>(new NewsAiAnalysis(
                IsRelevant: true,
                RelevanceReason: "Relevante",
                Headline: null,
                Impact: "medio",
                SectorTags: [],
                Subsector: null,
                AffectedFibers: [],
                KeyFacts: [],
                KeyFigures: [],
                SummaryMarkdown: summaryMarkdown,
                InvestorTakeaway: null,
                Confidence: 0.9,
                ExtractionNotes: null));
        }
    }

    private sealed class ThrowingAiNewsAnalysisService(Exception exception) : IAiNewsAnalysisService
    {
        public Task<NewsAiAnalysis?> GenerateAnalysisAsync(string title, string? snippet, string? bodyText, CancellationToken ct = default)
            => Task.FromException<NewsAiAnalysis?>(exception);
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

        public Task UpdateAiAnalysisAsync(Guid id, string? analysisJson, string? summary, NewsArticleStatus status, CancellationToken ct = default)
        {
            UpdateAttempts++;

            if (throwOnUpdate)
            {
                throw new InvalidOperationException("Database unavailable");
            }

            Article.AiAnalysisJson = analysisJson;
            Article.AiSummary = summary;
            Article.Status = status;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, int months, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<(IReadOnlyList<NewsArticle> Items, int Total, IReadOnlyDictionary<Guid, IReadOnlyList<(Guid FibraId, string Ticker)>> TickersByArticleId)> GetPagedPublicAsync(int page, int pageSize, string? q, Guid? fibraId, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<NewsArticle>, int, IReadOnlyDictionary<Guid, IReadOnlyList<(Guid FibraId, string Ticker)>>)>(
                ([Article], 1, new Dictionary<Guid, IReadOnlyList<(Guid FibraId, string Ticker)>>()));

        public Task<(IReadOnlyList<NewsArticle> Items, int Total)> GetPagedForOpsAsync(int page, int pageSize, string? search, bool? hasAiSummary, Guid? fibraId = null, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<NewsArticle>, int)>(([Article], 1));

        public Task<IReadOnlyList<(Guid Id, string Url)>> GetNullBodyTextArticlesAsync(int maxArticles, int daysBack, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<(Guid Id, string Url)>>([]);

        public Task<IReadOnlyList<NewsArticle>> GetRelatedAsync(Guid excludeId, int count, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<NewsArticle>>([]);

        public Task<IReadOnlyList<(Guid Id, string Ticker)>> GetLinkedFibrasAsync(Guid articleId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<(Guid, string)>>([]);

        public Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
            => Task.CompletedTask;
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
                services.RemoveAll<IAiNewsAnalysisService>();
                services.AddSingleton<IAiNewsAnalysisService>(new StubAiNewsAnalysisService("resumen"));
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
