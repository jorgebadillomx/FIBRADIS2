using Application.News;
using Application.Seo;
using Domain.Catalog;
using Domain.News;
using Infrastructure.Jobs.News;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs.News;

public class NewsPipelineJobThresholdTests
{
    [Fact]
    public async Task ExecuteAsync_WithAiModeOn_WhenBodyTextIsNull_SkipsAiAndSavesPartial()
    {
        var newsRepo = new FakeNewsRepository();
        var aiService = new TrackingAiNewsAnalysisService("Resumen.");
        var job = CreateThresholdJob(newsRepo, aiService, bodyText: null, minBodyLength: 500);

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Equal(NewsArticleStatus.Partial, article.Status);
        Assert.Null(article.AiSummary);
        Assert.Equal(0, aiService.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithAiModeOn_WhenBodyTextBelowThreshold_SkipsAiAndSavesPartial()
    {
        var newsRepo = new FakeNewsRepository();
        var aiService = new TrackingAiNewsAnalysisService("Resumen.");
        var shortBody = new string('x', 300);
        var job = CreateThresholdJob(newsRepo, aiService, bodyText: shortBody, minBodyLength: 500);

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Equal(NewsArticleStatus.Partial, article.Status);
        Assert.Null(article.AiSummary);
        Assert.Equal(0, aiService.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithAiModeOn_WhenBodyTextMeetsThreshold_CallsAiAndSavesProcessed()
    {
        var newsRepo = new FakeNewsRepository();
        var aiService = new TrackingAiNewsAnalysisService("Resumen.");
        var sufficientBody = new string('x', 600);
        var job = CreateThresholdJob(newsRepo, aiService, bodyText: sufficientBody, minBodyLength: 500);

        await job.ExecuteAsync();

        var article = Assert.Single(newsRepo.SavedArticles);
        Assert.Equal(NewsArticleStatus.Processed, article.Status);
        Assert.Equal("Resumen.", article.AiSummary);
        Assert.Equal(1, aiService.CallCount);
    }

    private static NewsPipelineJob CreateThresholdJob(
        FakeNewsRepository newsRepo,
        TrackingAiNewsAnalysisService aiService,
        string? bodyText,
        int minBodyLength)
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
            new FakeAiModeRepository(AiMode.On, minBodyTextLengthForAi: minBodyLength),
            new FakeAiProviderConfigRepository(),
            new FakeOgImageScraper(null),
            new FakeArticleContentScraper(bodyText),
            aiService,
            new FakePipelineErrorLogRepository(),
            new FakePipelineRunLogRepository(),
            new FakeIndexNowService(),
            new ConfigurationBuilder().Build(),
            NullLogger<NewsPipelineJob>.Instance);
    }
}

internal sealed class TrackingAiNewsAnalysisService(string? summaryMarkdown) : IAiNewsAnalysisService
{
    public int CallCount { get; private set; }

    public Task<Domain.News.NewsAiAnalysis?> GenerateAnalysisAsync(
        string title,
        string? snippet,
        string? bodyText,
        CancellationToken ct = default)
    {
        CallCount++;

        if (summaryMarkdown is null) return Task.FromResult<Domain.News.NewsAiAnalysis?>(null);

        return Task.FromResult<Domain.News.NewsAiAnalysis?>(new Domain.News.NewsAiAnalysis(
            IsRelevant: true,
            RelevanceReason: "Relevante.",
            Headline: null,
            Impact: "medio",
            SectorTags: [],
            Subsector: null,
            AffectedFibers: [],
            KeyFacts: [],
            KeyFigures: [],
            SummaryMarkdown: summaryMarkdown,
            InvestorTakeaway: null,
            Confidence: 0.85,
            ExtractionNotes: null));
    }
}
