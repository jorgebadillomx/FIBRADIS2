using Domain.News;

namespace Application.News;

public interface INewsRepository
{
    Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetExistingUrlsAsync(IEnumerable<string> candidateUrls, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetRecentNormalizedTitlesAsync(DateTimeOffset since, CancellationToken ct = default);
    Task AddWithLinksAsync(NewsArticle article, IEnumerable<Guid> fibraIds, CancellationToken ct = default);
    Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateBodyTextAsync(Guid id, string? bodyText, CancellationToken ct = default);
    Task UpdateSummaryAsync(Guid id, string? summary, NewsArticleStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, CancellationToken ct = default);
    Task<(IReadOnlyList<NewsArticle> Items, int Total)> GetPagedForOpsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<(Guid Id, string Url)>> GetNullBodyTextArticlesAsync(int maxArticles, int daysBack, CancellationToken ct = default);
}
