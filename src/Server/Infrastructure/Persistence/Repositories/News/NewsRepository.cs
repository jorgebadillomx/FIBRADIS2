using System.Data;
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
        article.Slug ??= await GenerateUniqueSlugAsync(article.Title, ct: ct);

        db.NewsArticles.Add(article);

        foreach (var fibraId in fibraIds.Distinct())
        {
            db.NewsArticleFibras.Add(new NewsArticleFibra
            {
                NewsArticleId = article.Id,
                FibraId = fibraId,
            });
        }

        // La unicidad del slug se decide con check-then-insert: una ingesta concurrente con el
        // mismo título puede ganar la carrera y violar IX_NewsArticle_Slug. Sin retry el artículo
        // completo se pierde (NewsPipelineJob trata la excepción como fallo de guardado).
        for (var retry = 0; ; retry++)
        {
            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException ex) when (retry < 3 && IsSlugUniqueViolation(ex))
            {
                article.Slug = await GenerateUniqueSlugAsync(article.Title, article.Id, ct);
            }
        }
    }

    private static bool IsSlugUniqueViolation(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("IX_NewsArticle_Slug", StringComparison.OrdinalIgnoreCase) == true;

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
                && n.AiAnalysisJson != null
                && (n.Status == NewsArticleStatus.Pending
                    || n.Status == NewsArticleStatus.Processed
                    || n.Status == NewsArticleStatus.Partial))
            .OrderByDescending(n => n.PublishedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, int months, CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddMonths(-months);
        return await db.NewsArticleFibras
            .Where(link => link.FibraId == fibraId)
            .Select(link => link.NewsArticle)
            .Where(article => article.DeletedAt == null
                && article.PublishedAt >= since
                && (article.Status == NewsArticleStatus.Pending
                    || article.Status == NewsArticleStatus.Processed
                    || article.Status == NewsArticleStatus.Partial))
            .OrderByDescending(article => article.PublishedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<NewsArticle> Items, int Total, IReadOnlyDictionary<Guid, IReadOnlyList<(Guid FibraId, string Ticker)>> TickersByArticleId)>
        GetPagedPublicAsync(int page, int pageSize, string? q, Guid? fibraId, CancellationToken ct = default)
    {
        var query = db.NewsArticles
            .Where(article => article.DeletedAt == null && article.Status == NewsArticleStatus.Processed);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var trimmedQuery = q.Trim();
            query = query.Where(article => article.Title.Contains(trimmedQuery));
        }

        if (fibraId.HasValue)
        {
            query = query.Where(article =>
                db.NewsArticleFibras.Any(link => link.NewsArticleId == article.Id && link.FibraId == fibraId.Value));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(article => article.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var articleIds = items.Select(article => article.Id).ToList();
        if (articleIds.Count == 0)
        {
            return (items, total, new Dictionary<Guid, IReadOnlyList<(Guid FibraId, string Ticker)>>());
        }

        var links = await db.NewsArticleFibras
            .Where(link => articleIds.Contains(link.NewsArticleId))
            .Join(
                db.Fibras.Where(fibra => fibra.State == Domain.Catalog.FibraState.Active),
                link => link.FibraId,
                fibra => fibra.Id,
                (link, fibra) => new { link.NewsArticleId, FibraId = fibra.Id, fibra.Ticker })
            .ToListAsync(ct);

        var tickerMap = links
            .GroupBy(link => link.NewsArticleId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<(Guid FibraId, string Ticker)>)group
                    .DistinctBy(link => link.Ticker, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(link => link.Ticker)
                    .Select(link => (link.FibraId, link.Ticker))
                    .ToList());

        return (items, total, tickerMap);
    }

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

    public async Task<IReadOnlyList<NewsArticle>> GetRelatedAsync(Guid excludeId, int count, CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddMonths(-6);

        var fibraIds = await db.NewsArticleFibras
            .Where(x => x.NewsArticleId == excludeId)
            .Select(x => x.FibraId)
            .ToListAsync(ct);

        if (fibraIds.Count > 0)
        {
            // SQL Server no permite DISTINCT en columnas text; primero obtenemos los IDs únicos.
            var relatedIds = await db.NewsArticleFibras
                .Where(x => fibraIds.Contains(x.FibraId) && x.NewsArticleId != excludeId)
                .Select(x => x.NewsArticleId)
                .Distinct()
                .ToListAsync(ct);

            if (relatedIds.Count > 0)
            {
                var related = await db.NewsArticles
                    .Where(a => relatedIds.Contains(a.Id)
                        && a.DeletedAt == null
                        && a.AiAnalysisJson != null
                        && a.PublishedAt >= since
                        && (a.Status == NewsArticleStatus.Pending
                            || a.Status == NewsArticleStatus.Processed
                            || a.Status == NewsArticleStatus.Partial))
                    .OrderByDescending(a => a.PublishedAt)
                    .Take(count)
                    .ToListAsync(ct);

                if (related.Count > 0)
                    return related;
            }
        }

        return await db.NewsArticles
            .Where(n => n.DeletedAt == null
                && n.Id != excludeId
                && n.AiAnalysisJson != null
                && n.PublishedAt >= since
                && (n.Status == NewsArticleStatus.Pending
                    || n.Status == NewsArticleStatus.Processed
                    || n.Status == NewsArticleStatus.Partial))
            .OrderByDescending(n => n.PublishedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<(Guid Id, string Ticker)>> GetLinkedFibrasAsync(Guid articleId, CancellationToken ct = default)
    {
        var rows = await db.NewsArticleFibras
            .Where(x => x.NewsArticleId == articleId)
            .Join(db.Fibras, x => x.FibraId, f => f.Id, (_, f) => new { f.Id, f.Ticker })
            .ToListAsync(ct);
        return rows.Select(r => (r.Id, r.Ticker)).ToList();
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await db.NewsArticles
            .Where(n => n.Id == id && n.DeletedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, now), ct);
    }

    public async Task<NewsArticle?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        return await db.NewsArticles
            .FirstOrDefaultAsync(n => n.Slug == slug && n.DeletedAt == null, ct);
    }

    // Literales bajo /api/v1/news y /noticias que un slug no debe ocupar: la ruta literal
    // ganaría sobre /{slug} y el artículo quedaría inalcanzable (o devolvería otro contenido)
    private static readonly string[] ReservedSlugs = ["paged", "fibras", "related"];

    public async Task<string> GenerateUniqueSlugAsync(string title, Guid? excludeId = null, CancellationToken ct = default)
    {
        var baseSlug = SlugGenerator.Generate(title);
        var candidate = baseSlug;

        // <= 51: la última iteración verifica el candidato "-50" generado en la anterior
        for (var attempt = 2; attempt <= 51; attempt++)
        {
            var exists = !IsRouteSafe(candidate) || await db.NewsArticles.AnyAsync(
                n => n.Slug == candidate && (excludeId == null || n.Id != excludeId), ct);
            if (!exists) return candidate;

            // Recortar base si está muy cerca del límite antes de agregar sufijo
            var trimmedBase = baseSlug.Length > 190 ? baseSlug[..190] : baseSlug;
            candidate = $"{trimmedBase}-{attempt}";
        }

        // Fallback último recurso (no debería ocurrir en práctica)
        var guidCandidate = $"{baseSlug[..Math.Min(baseSlug.Length, 160)]}-{Guid.NewGuid():N}";
        return guidCandidate.Length > 200 ? guidCandidate[..200] : guidCandidate;
    }

    // Un slug GUID-parseable lo capturaría la ruta /{id:guid} (constraint > sin constraint)
    private static bool IsRouteSafe(string candidate)
        => !ReservedSlugs.Contains(candidate) && !Guid.TryParse(candidate, out _);

    public async Task<IReadOnlyList<NewsArticle>> GetArticlesWithoutSlugAsync(int batchSize, CancellationToken ct = default)
        => await db.NewsArticles
            .Where(n => n.Slug == null && n.DeletedAt == null)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task UpdateSlugAsync(Guid id, string slug, CancellationToken ct = default)
    {
        await db.NewsArticles
            .Where(n => n.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Slug, slug), ct);
    }

    public async Task<IReadOnlyList<(string Slug, DateTimeOffset PublishedAt)>> GetArticlesForSitemapAsync(int limit, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, ct);
        var rows = await db.NewsArticles
            .AsNoTracking()
            .Where(n => n.Slug != null && n.Status == NewsArticleStatus.Processed && n.DeletedAt == null)
            .OrderByDescending(n => n.PublishedAt)
            .Take(limit)
            .Select(n => new { n.Slug, n.PublishedAt })
            .ToListAsync(ct);
        await tx.CommitAsync(ct);
        return rows.Select(r => (r.Slug!, r.PublishedAt)).ToList();
    }
}
