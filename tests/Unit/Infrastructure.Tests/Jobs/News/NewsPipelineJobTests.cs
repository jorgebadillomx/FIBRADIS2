using Application.Catalog;
using Application.News;
using Domain.Catalog;
using Domain.News;
using Infrastructure.Jobs.News;
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
            NullLogger<NewsPipelineJob>.Instance);

        await job.ExecuteAsync();

        Assert.NotNull(newsRepo.LastCandidateUrls);
        Assert.Equal(2, newsRepo.LastCandidateUrls!.Count);
        Assert.Contains("https://example.com/1", newsRepo.LastCandidateUrls);
        Assert.Contains("https://example.com/2", newsRepo.LastCandidateUrls);
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

    public Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<NewsArticle>>([]);

    public Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<NewsArticle>>([]);
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

internal sealed class FakeRssClient(IReadOnlyList<RssItem> items) : IRssClient
{
    public Task<IReadOnlyList<RssItem>> FetchAsync(string query, CancellationToken ct = default)
        => Task.FromResult(items);
}
