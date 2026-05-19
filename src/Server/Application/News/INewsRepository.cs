using Domain.News;

namespace Application.News;

public interface INewsRepository
{
    Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetExistingUrlsAsync(IEnumerable<string> candidateUrls, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetRecentNormalizedTitlesAsync(DateTimeOffset since, CancellationToken ct = default);
    Task AddWithLinksAsync(NewsArticle article, IEnumerable<Guid> fibraIds, CancellationToken ct = default);
    Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, CancellationToken ct = default);
}
