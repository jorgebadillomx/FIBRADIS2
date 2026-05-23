using Application.Catalog;
using Application.News;
using Domain.Catalog;
using Domain.News;
using Infrastructure.Jobs.News;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs.News;

public class NewsPipelineJobTests
{
    [Fact]
    public async Task ExecuteAsync_UsesOnlyCandidateUrlsForExistingUrlLookup()
    {
        var fibra = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = "FUNO11",
            NameVariants = ["Fibra Uno"],
            State = FibraState.Active,
        };
        var rssItems = new[]
        {
            new RssItem("FUNO11 sube", "Fuente", DateTimeOffset.UtcNow, "https://example.com/1", null),
            new RssItem("Fibra Uno sube", "Fuente", DateTimeOffset.UtcNow, "https://example.com/2", null),
        };
        var newsRepo = new FakeNewsRepository();
        var job = new NewsPipelineJob(
            new FakeNewsFibraRepository([fibra]),
            newsRepo,
            new FakeNewsBlocklistRepository([]),
            new FakeRssClient(rssItems),
            new FakeAiModeRepository(AiMode.Off),
            new FakeOgImageScraper(null),
            new FakeArticleContentScraper(null),
            new FakeAiSummaryService("resumen"),
            NullLogger<NewsPipelineJob>.Instance);

        await job.ExecuteAsync();

        Assert.NotNull(newsRepo.LastCandidateUrls);
        Assert.Equal(2, newsRepo.LastCandidateUrls!.Count);
        Assert.Contains("https://example.com/1", newsRepo.LastCandidateUrls);
        Assert.Contains("https://example.com/2", newsRepo.LastCandidateUrls);
    }

    [Fact]
    public async Task ExecuteAsync_WithAiModeOff_SetsArticleStatusToProcessedWithoutSummary()
    {
        var newsRepo = new FakeNewsRepository();
        var job = CreateJob(newsRepo, AiMode.Off);

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Equal(NewsArticleStatus.Processed, article.Status);
        Assert.Null(article.AiSummary);
    }

    [Fact]
    public async Task ExecuteAsync_WithAiModeOn_GeneratesSummaryAndSetsStatusToProcessed()
    {
        var newsRepo = new FakeNewsRepository();
        var summaryService = new FakeAiSummaryService("Resumen profesional FIBRA.");
        var job = CreateJob(newsRepo, AiMode.On, summaryService: summaryService);

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Equal(NewsArticleStatus.Processed, article.Status);
        Assert.Equal("Resumen profesional FIBRA.", article.AiSummary);
    }

    [Fact]
    public async Task ExecuteAsync_WhenArticleBodyIsAvailable_SavesBodyText()
    {
        var newsRepo = new FakeNewsRepository();
        var articleScraper = new FakeArticleContentScraper("Cuerpo completo del articulo con contexto relevante para el resumen IA.");
        var job = CreateJob(newsRepo, ogImageScraper: new FakeOgImageScraper(null), articleContentScraper: articleScraper);

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Equal("Cuerpo completo del articulo con contexto relevante para el resumen IA.", article.BodyText);
        Assert.Equal(["https://example.com/1"], articleScraper.RequestedUrls);
    }

    [Fact]
    public async Task ExecuteAsync_WithAiModeOn_WhenSummaryServiceThrows_SetsStatusToPartial()
    {
        var newsRepo = new FakeNewsRepository();
        var summaryService = new FakeAiSummaryService(shouldThrow: true);
        var job = CreateJob(newsRepo, AiMode.On, summaryService: summaryService);

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Equal(NewsArticleStatus.Partial, article.Status);
        Assert.Null(article.AiSummary);
    }

    [Fact]
    public async Task ExecuteAsync_WhenScraperReturnsNull_SavesArticleWithNullBodyText()
    {
        var newsRepo = new FakeNewsRepository();
        var job = CreateJob(newsRepo, articleContentScraper: new FakeArticleContentScraper(null));

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Null(article.BodyText);
        Assert.Equal(NewsArticleStatus.Processed, article.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAiModeLookupFails_FallsBackToProcessed()
    {
        var newsRepo = new FakeNewsRepository();
        var job = CreateJob(newsRepo, shouldThrowOnModeLookup: true);

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Equal(NewsArticleStatus.Processed, article.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOgImageIsAvailable_AssignsImageUrl()
    {
        var newsRepo = new FakeNewsRepository();
        var scraper = new FakeOgImageScraper("https://cdn.example.com/image.jpg");
        var job = CreateJob(newsRepo, ogImageScraper: scraper);

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Equal("https://cdn.example.com/image.jpg", article.ImageUrl);
        Assert.Equal(["https://example.com/1"], scraper.RequestedUrls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenArticleUrlIsGoogleNews_CallsOgImageScraper()
    {
        var fibra = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = "FUNO11",
            NameVariants = ["Fibra Uno"],
            State = FibraState.Active,
        };
        var newsRepo = new FakeNewsRepository();
        var scraper = new FakeOgImageScraper("https://cdn.example.com/image.jpg");
        var rssItems = new[]
        {
            new RssItem(
                "FUNO11 sube",
                "Fuente",
                DateTimeOffset.UtcNow,
                "https://news.google.com/rss/articles/CBMiT2h0dHBzOi8vZXhhbXBsZS5jb20vbm90aWNpYQ?oc=5",
                "Snippet"),
        };

        var job = new NewsPipelineJob(
            new FakeNewsFibraRepository([fibra]),
            newsRepo,
            new FakeNewsBlocklistRepository([]),
            new FakeRssClient(rssItems),
            new FakeAiModeRepository(AiMode.Off),
            scraper,
            new FakeArticleContentScraper(null),
            new FakeAiSummaryService("resumen"),
            NullLogger<NewsPipelineJob>.Instance);

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Equal("https://cdn.example.com/image.jpg", article.ImageUrl);
        Assert.NotEmpty(scraper.RequestedUrls);
    }

    [Fact]
    public async Task ExecuteAsync_WithAiModeOn_CallsAiSummaryServiceAndSavesResult()
    {
        var newsRepo = new FakeNewsRepository();
        var summaryService = new FakeAiSummaryService("Resumen flash.");
        var fibra = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = "FUNO11",
            NameVariants = ["Fibra Uno"],
            State = FibraState.Active,
        };
        var rssItems = new[]
        {
            new RssItem("FUNO11 sube", "Fuente", DateTimeOffset.UtcNow, "https://example.com/1", "Snippet"),
        };

        var job = new NewsPipelineJob(
            new FakeNewsFibraRepository([fibra]),
            newsRepo,
            new FakeNewsBlocklistRepository([]),
            new FakeRssClient(rssItems),
            new FakeAiModeRepository(AiMode.On),
            new FakeOgImageScraper(null),
            new FakeArticleContentScraper(null),
            summaryService,
            NullLogger<NewsPipelineJob>.Instance);

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Equal(NewsArticleStatus.Processed, article.Status);
        Assert.Equal("Resumen flash.", article.AiSummary);
    }

    private static NewsPipelineJob CreateJob(FakeNewsRepository newsRepo, AiMode mode)
        => CreateJob(newsRepo, mode, shouldThrowOnModeLookup: false);

    private static NewsPipelineJob CreateJob(
        FakeNewsRepository newsRepo,
        AiMode mode = AiMode.Off,
        bool shouldThrowOnModeLookup = false,
        IOgImageScraper? ogImageScraper = null,
        IArticleContentScraper? articleContentScraper = null,
        FakeAiSummaryService? summaryService = null)
    {
        var fibra = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = "FUNO11",
            NameVariants = ["Fibra Uno"],
            State = FibraState.Active,
        };
        var rssItems = new[]
        {
            new RssItem("FUNO11 sube", "Fuente", DateTimeOffset.UtcNow, "https://example.com/1", "Snippet"),
        };

        return new NewsPipelineJob(
            new FakeNewsFibraRepository([fibra]),
            newsRepo,
            new FakeNewsBlocklistRepository([]),
            new FakeRssClient(rssItems),
            new FakeAiModeRepository(mode, shouldThrowOnModeLookup),
            ogImageScraper ?? new FakeOgImageScraper(null),
            articleContentScraper ?? new FakeArticleContentScraper(null),
            summaryService ?? new FakeAiSummaryService("resumen"),
            NullLogger<NewsPipelineJob>.Instance);
    }
}

internal sealed class FakeNewsFibraRepository(IReadOnlyList<Fibra> fibras) : IFibraRepository
{
    public Task<(IReadOnlyList<Fibra> Items, int Total)> GetActivePagedAsync(FibraFilter filter, CancellationToken ct = default)
        => Task.FromResult<(IReadOnlyList<Fibra>, int)>((fibras, fibras.Count));

    public Task<Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default)
        => Task.FromResult(fibras.FirstOrDefault(f => f.Ticker == ticker));

    public Task<IReadOnlyList<Fibra>> GetAllActiveAsync(CancellationToken ct = default)
        => Task.FromResult(fibras);
}

internal sealed class FakeNewsRepository : INewsRepository
{
    public List<string>? LastCandidateUrls { get; private set; }
    public List<NewsArticle> SavedArticles { get; } = [];
    public List<Guid> SavedFibraLinks { get; } = [];

    public Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default) => Task.FromResult(false);

    public Task<IReadOnlyList<string>> GetExistingUrlsAsync(IEnumerable<string> candidateUrls, CancellationToken ct = default)
    {
        LastCandidateUrls = candidateUrls.ToList();
        return Task.FromResult<IReadOnlyList<string>>([]);
    }

    public Task<IReadOnlyList<string>> GetRecentNormalizedTitlesAsync(DateTimeOffset since, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task AddWithLinksAsync(NewsArticle article, IEnumerable<Guid> fibraIds, CancellationToken ct = default)
    {
        SavedArticles.Add(article);
        SavedFibraLinks.AddRange(fibraIds);
        return Task.CompletedTask;
    }

    public Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(SavedArticles.FirstOrDefault(article => article.Id == id));

    public Task UpdateBodyTextAsync(Guid id, string? bodyText, CancellationToken ct = default)
    {
        var article = SavedArticles.FirstOrDefault(saved => saved.Id == id);
        if (article is not null)
            article.BodyText = bodyText;

        return Task.CompletedTask;
    }

    public Task UpdateSummaryAsync(Guid id, string? summary, NewsArticleStatus status, CancellationToken ct = default)
    {
        var article = SavedArticles.FirstOrDefault(saved => saved.Id == id);
        if (article is not null)
        {
            article.AiSummary = summary;
            article.Status = status;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<NewsArticle>>([]);

    public Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<NewsArticle>>([]);

    public Task<(IReadOnlyList<NewsArticle> Items, int Total)> GetPagedForOpsAsync(int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult<(IReadOnlyList<NewsArticle>, int)>(([],  0));

    public Task<IReadOnlyList<(Guid Id, string Url)>> GetNullBodyTextArticlesAsync(int maxArticles, int daysBack, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<(Guid Id, string Url)>>([]);
}

internal sealed class FakeNewsBlocklistRepository(IReadOnlyList<string> terms) : IBlocklistRepository
{
    public Task<IReadOnlyList<BlocklistTerm>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<BlocklistTerm>>(
            terms.Select(term => new BlocklistTerm { Term = term }).ToList());

    public Task<IReadOnlyList<string>> GetAllTermsAsync(CancellationToken ct = default)
        => Task.FromResult(terms);

    public Task<bool> ExistsAsync(string term, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<BlocklistTerm> AddAsync(string term, CancellationToken ct = default)
        => Task.FromResult(new BlocklistTerm { Term = term });

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(true);
}

internal sealed class FakeAiModeRepository(AiMode mode, bool shouldThrowOnModeLookup = false, string newsModel = "gemini-2.5-pro") : IAiModeRepository
{
    private AiModeConfig _config = new()
    {
        Id = 1,
        Mode = mode,
        NewsModel = newsModel,
        PreviousMode = null,
        UpdatedAt = DateTimeOffset.UtcNow,
        UpdatedBy = "test",
    };

    public Task<AiMode> GetCurrentModeAsync(CancellationToken ct = default)
    {
        if (shouldThrowOnModeLookup)
            throw new InvalidOperationException("AI mode lookup failed.");

        return Task.FromResult(_config.Mode);
    }

    public Task<AiModeConfig> GetConfigAsync(CancellationToken ct = default)
    {
        if (shouldThrowOnModeLookup)
            throw new InvalidOperationException("AI mode lookup failed.");

        return Task.FromResult(_config);
    }

    public Task SetModeAsync(AiMode mode, string actor, CancellationToken ct = default)
    {
        _config = new AiModeConfig
        {
            Id = 1,
            Mode = mode,
            NewsModel = _config.NewsModel,
            PreviousMode = _config.Mode,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = actor,
        };

        return Task.CompletedTask;
    }

    public Task UpdateConfigAsync(AiMode? mode, string? newsModel, string actor, CancellationToken ct = default)
    {
        if (mode is not null) _config.Mode = mode.Value;
        if (newsModel is not null) _config.NewsModel = newsModel;
        _config.UpdatedAt = DateTimeOffset.UtcNow;
        _config.UpdatedBy = actor;
        return Task.CompletedTask;
    }
}

internal sealed class FakeRssClient(IReadOnlyList<RssItem> items) : IRssClient
{
    public Task<IReadOnlyList<RssItem>> FetchAsync(string query, CancellationToken ct = default)
        => Task.FromResult(items);
}

internal sealed class FakeOgImageScraper(string? imageUrl) : IOgImageScraper
{
    public List<string> RequestedUrls { get; } = [];

    public Task<string?> TryGetOgImageAsync(string url, CancellationToken ct = default)
    {
        RequestedUrls.Add(url);
        return Task.FromResult(imageUrl);
    }
}

internal sealed class FakeArticleContentScraper(string? bodyText) : IArticleContentScraper
{
    public List<string> RequestedUrls { get; } = [];

    public Task<string?> TryGetArticleTextAsync(string url, CancellationToken ct = default)
    {
        RequestedUrls.Add(url);
        return Task.FromResult(bodyText);
    }
}

internal sealed class FakeAiSummaryService(string? summary = null, bool shouldThrow = false) : IAiSummaryService
{
    public Task<string?> GenerateSummaryAsync(
        string title,
        string? snippet,
        string? bodyText = null,
        AiContentType contentType = AiContentType.News,
        CancellationToken ct = default)
    {
        if (shouldThrow)
            throw new InvalidOperationException("AI summary service failed.");

        return Task.FromResult(summary);
    }
}
