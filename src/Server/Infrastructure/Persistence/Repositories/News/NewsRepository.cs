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
            .Where(n => n.DeletedAt == null && n.CapturedAt >= since)
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

    public async Task UpdateBodyTextAsync(Guid id, string? bodyText, CancellationToken ct = default)
    {
        var article = await db.NewsArticles.FindAsync([id], ct);
        if (article is not null)
        {
            article.BodyText = bodyText;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateSummaryAsync(Guid id, string? summary, NewsArticleStatus status, CancellationToken ct = default)
    {
        await db.NewsArticles
            .Where(article => article.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(article => article.AiSummary, summary)
                .SetProperty(article => article.Status, status), ct);
    }

    public async Task UpdateAiAnalysisAsync(Guid id, string? analysisJson, string? summary, NewsArticleStatus status, CancellationToken ct = default)
    {
        await db.NewsArticles
            .Where(article => article.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(article => article.AiAnalysisJson, analysisJson)
                .SetProperty(article => article.AiSummary, summary)
                .SetProperty(article => article.Status, status), ct);
    }

    public async Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default)
        => await db.NewsArticles
            .Where(n => n.DeletedAt == null
                && (n.Status == NewsArticleStatus.Pending
                    || n.Status == NewsArticleStatus.Processed
                    || n.Status == NewsArticleStatus.Partial))
            .OrderByDescending(n => n.PublishedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, CancellationToken ct = default)
        => await db.NewsArticleFibras
            .Where(link => link.FibraId == fibraId)
            .Select(link => link.NewsArticle)
            .Where(article => article.DeletedAt == null
                && (article.Status == NewsArticleStatus.Pending
                    || article.Status == NewsArticleStatus.Processed
                    || article.Status == NewsArticleStatus.Partial))
            .OrderByDescending(article => article.PublishedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<NewsArticle> Items, int Total)> GetPagedForOpsAsync(int page, int pageSize, string? search, bool? hasAiSummary, Guid? fibraId = null, CancellationToken ct = default)
    {
        var query = db.NewsArticles.Where(a => a.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();
            query = query.Where(a =>
                a.Title.Contains(trimmedSearch)
                || (a.BodyText != null && a.BodyText.Contains(trimmedSearch))
                || (a.AiSummary != null && a.AiSummary.Contains(trimmedSearch)));
        }

        if (hasAiSummary is true)
        {
            query = query.Where(a => a.AiSummary != null);
        }
        else if (hasAiSummary is false)
        {
            query = query.Where(a => a.AiSummary == null);
        }

        if (fibraId is not null)
        {
            query = query.Where(a => db.NewsArticleFibras.Any(f => f.NewsArticleId == a.Id && f.FibraId == fibraId));
        }

        query = query.OrderByDescending(n => n.PublishedAt);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<(Guid Id, string Url)>> GetNullBodyTextArticlesAsync(int maxArticles, int daysBack, CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-daysBack);
        var rows = await db.NewsArticles
            .Where(n => n.DeletedAt == null && n.BodyText == null && n.Url != null && n.CapturedAt >= since)
            .OrderByDescending(n => n.CapturedAt)
            .Take(maxArticles)
            .Select(n => new { n.Id, n.Url })
            .ToListAsync(ct);
        return rows.Select(r => (r.Id, r.Url!)).ToList();
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await db.NewsArticles
            .Where(n => n.Id == id && n.DeletedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, now), ct);
    }
}
