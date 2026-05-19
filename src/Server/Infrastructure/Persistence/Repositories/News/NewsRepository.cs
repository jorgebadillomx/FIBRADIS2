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

    public async Task AddAsync(NewsArticle article, CancellationToken ct = default)
    {
        db.NewsArticles.Add(article);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default)
        => await db.NewsArticles
            .Where(n => n.Status == NewsArticleStatus.Pending || n.Status == NewsArticleStatus.Processed)
            .OrderByDescending(n => n.PublishedAt)
            .Take(count)
            .ToListAsync(ct);
}
