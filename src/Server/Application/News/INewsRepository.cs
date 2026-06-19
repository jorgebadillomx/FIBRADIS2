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
    Task UpdateAiAnalysisAsync(Guid id, string? analysisJson, string? summary, NewsArticleStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<NewsArticle>> GetLatestByCapturedAtAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, int months, CancellationToken ct = default);
    Task<IReadOnlyList<NewsArticle>> GetRelatedAsync(Guid excludeId, int count, CancellationToken ct = default);
    Task<IReadOnlyList<(Guid Id, string Ticker)>> GetLinkedFibrasAsync(Guid articleId, CancellationToken ct = default);
    Task<(IReadOnlyList<NewsArticle> Items, int Total, IReadOnlyDictionary<Guid, IReadOnlyList<(Guid FibraId, string Ticker)>> TickersByArticleId)>
        GetPagedPublicAsync(int page, int pageSize, string? q, Guid? fibraId, CancellationToken ct = default);
    Task<(IReadOnlyList<(string Slug, string Title, DateTimeOffset PublishedAt)> Items, int Total)> GetArticlesForSitemapPageAsync(int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<NewsArticle> Items, int Total)> GetPagedForOpsAsync(int page, int pageSize, string? search, bool? hasAiSummary, Guid? fibraId = null, CancellationToken ct = default);
    Task<IReadOnlyList<(Guid Id, string Url)>> GetNullBodyTextArticlesAsync(int maxArticles, int daysBack, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task<NewsArticle?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<string> GenerateUniqueSlugAsync(string title, Guid? excludeId = null, CancellationToken ct = default);
    Task<IReadOnlyList<NewsArticle>> GetArticlesWithoutSlugAsync(int batchSize, CancellationToken ct = default);
    Task UpdateSlugAsync(Guid id, string slug, CancellationToken ct = default);
    Task<IReadOnlyList<(string Slug, string Title, DateTimeOffset PublishedAt)>> GetArticlesForSitemapAsync(int limit, CancellationToken ct = default);
}
