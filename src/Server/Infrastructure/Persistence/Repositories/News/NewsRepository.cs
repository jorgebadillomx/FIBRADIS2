using Application.News;
using Domain.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.News;

public class NewsRepository(AppDbContext db) : INewsRepository
{
    public async Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default)
        => await db.NewsArticles.AnyAsync(n => n.Url == url, ct);

    public async Task<IReadOnlyList<string>> GetExistingUrlsAsync(IEnumerable<string> candidateUrls, CancellationToken ct = default)
    {
        var normalizedUrls = candidateUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedUrls.Length == 0)
            return [];

        return await db.NewsArticles
            .Where(n => normalizedUrls.Contains(n.Url))
            .Select(n => n.Url)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetRecentNormalizedTitlesAsync(DateTimeOffset since, CancellationToken ct = default)
        => await db.NewsArticles
            .Where(n => n.CapturedAt >= since)
            .Select(n => n.TitleNormalized)
            .ToListAsync(ct);

    public async Task AddWithLinksAsync(NewsArticle article, IEnumerable<Guid> fibraIds, CancellationToken ct = default)
    {
        db.NewsArticles.Add(article);

        foreach (var fibraId in fibraIds.Distinct())
        {
            db.NewsArticleFibras.Add(new NewsArticleFibra
            {
                NewsArticleId = article.Id,
                FibraId = fibraId,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.NewsArticles.FindAsync([id], ct).AsTask();

    public async Task UpdateSummaryAsync(Guid id, string? summary, NewsArticleStatus status, CancellationToken ct = default)
    {
        await db.NewsArticles
            .Where(article => article.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(article => article.AiSummary, summary)
                .SetProperty(article => article.Status, status), ct);
    }

    public async Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default)
        => await db.NewsArticles
            .Where(n => n.Status == NewsArticleStatus.Pending
                || n.Status == NewsArticleStatus.Processed
                || n.Status == NewsArticleStatus.Partial)
            .OrderByDescending(n => n.PublishedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, CancellationToken ct = default)
        => await db.NewsArticleFibras
            .Where(link => link.FibraId == fibraId)
            .Select(link => link.NewsArticle)
            .Where(article => article.Status == NewsArticleStatus.Pending
                || article.Status == NewsArticleStatus.Processed
                || article.Status == NewsArticleStatus.Partial)
            .OrderByDescending(article => article.PublishedAt)
            .Take(count)
            .ToListAsync(ct);
}
