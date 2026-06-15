using System.Data;
using Application.News;
using Application.Seo;
using Domain.News;
using Domain.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.News;

// Las dependencias SEO son opcionales (= null) para que la auto-población (AC-5 de 12-1) ocurra
// en producción (DI las resuelve) sin romper los `new NewsRepository(db)` de los unit tests que
// no ejercitan SEO. Si faltan deps o App:BaseUrl, el auto-llenado se omite silenciosamente: el
// endpoint de backfill (AC-7) es la red de recuperación idempotente.
public class NewsRepository(
    AppDbContext db,
    ISeoMetadataRepository? seoMetadata = null,
    ISeoDefaultsBuilder? seoDefaults = null,
    IConfiguration? configuration = null,
    ILogger<NewsRepository>? logger = null) : INewsRepository
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
                // Auto-llenado SEO tras conocer el slug definitivo (AC-5). Se hace después del save
                // del artículo para usar el slug final como EntityKey; el upsert es idempotente.
                await PopulateSeoAsync(article, ct);
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

    private string? ResolveBaseUrl()
    {
        var baseUrl = configuration?["App:BaseUrl"];
        return string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.TrimEnd('/');
    }

    private async Task PopulateSeoAsync(NewsArticle article, CancellationToken ct)
    {
        if (seoMetadata is null || seoDefaults is null)
            return;

        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null)
            return;

        try
        {
            var metadata = seoDefaults.BuildNews(article, baseUrl, DateTimeOffset.UtcNow);
            // overrideMode:false ⇒ regeneración que respeta flags de override (no pisa ediciones
            // manuales si la fila ya existe por una ejecución previa).
            await seoMetadata.UpsertAsync(metadata, overrideMode: false, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex,
                "Auto-llenado SEO falló para la noticia {Slug}; recuperable vía POST /api/v1/ops/seo/backfill",
                article.Slug ?? article.Id.ToString());
        }
    }

    // Regenera la fila SEO de una noticia tras enriquecimiento IA (headline/summary cambian la
    // description/JSON-LD). El slug NO cambia en estos updates, así que la EntityKey es estable.
    private async Task RegenerateSeoAsync(Guid id, CancellationToken ct)
    {
        if (seoMetadata is null || seoDefaults is null)
            return;

        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null)
            return;

        var article = await db.NewsArticles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (article is null)
            return;

        try
        {
            var metadata = seoDefaults.BuildNews(article, baseUrl, DateTimeOffset.UtcNow);
            await seoMetadata.UpsertAsync(metadata, overrideMode: false, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex,
                "Regeneración SEO falló para la noticia {ArticleId}; recuperable vía backfill", id);
        }
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

        await RegenerateSeoAsync(id, ct);
    }

    public async Task UpdateAiAnalysisAsync(Guid id, string? analysisJson, string? summary, NewsArticleStatus status, CancellationToken ct = default)
    {
        await db.NewsArticles
            .Where(article => article.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(article => article.AiAnalysisJson, analysisJson)
                .SetProperty(article => article.AiSummary, summary)
                .SetProperty(article => article.Status, status), ct);

        await RegenerateSeoAsync(id, ct);
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

    // Backfill SEO (AC-7): últimas N noticias por fecha de captura (CapturedAt), sin exigir
    // AiAnalysisJson — el backfill debe cubrir también noticias aún no enriquecidas por IA.
    public async Task<IReadOnlyList<NewsArticle>> GetLatestByCapturedAtAsync(int count, CancellationToken ct = default)
        => await db.NewsArticles
            .Where(n => n.DeletedAt == null)
            .OrderByDescending(n => n.CapturedAt)
            .ThenByDescending(n => n.Id)
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
        var (items, _) = await GetArticlesForSitemapPageAsync(1, limit, ct);
        return items;
    }

    public async Task<(IReadOnlyList<(string Slug, DateTimeOffset PublishedAt)> Items, int Total)> GetArticlesForSitemapPageAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.NewsArticles
            .AsNoTracking()
            .Where(n => n.Slug != null && n.Status == NewsArticleStatus.Processed && n.DeletedAt == null);

        // Solo las filas SeoMetadata activas aplican (IsActive es el gate activo/soft-delete del
        // proyecto); debe coincidir con el filtro del endpoint en SeoEndpoints.LoadSitemapVisibilityAsync.
        if (db.Database.IsRelational())
        {
            query = query.Where(n => !db.SeoMetadata.Any(seo =>
                seo.PageType == SeoPageType.News
                && seo.IsActive
                && seo.EntityKey == n.Slug
                && EF.Functions.Like(seo.RobotsDirectives, "%noindex%")));
        }
        else
        {
            var noIndexSlugs = await db.SeoMetadata
                .AsNoTracking()
                .Where(seo => seo.PageType == SeoPageType.News && seo.IsActive)
                .Select(seo => new { seo.EntityKey, seo.RobotsDirectives })
                .ToListAsync(ct);

            var noIndexSlugSet = noIndexSlugs
                .Where(seo => seo.RobotsDirectives.Contains("noindex", StringComparison.OrdinalIgnoreCase))
                .Select(seo => seo.EntityKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (noIndexSlugSet.Count > 0)
                query = query.Where(n => !noIndexSlugSet.Contains(n.Slug!));
        }

        var total = await query.CountAsync(ct);

        // Guard de overflow: page viene de la ruta {page:int} (hasta int.MaxValue). El offset en int
        // desbordaría con páginas enormes (negativo → SQL rechaza OFFSET) provocando un 500 anónimo.
        // Calcularlo en long y cortocircuitar fuera de rango evita el overflow y la query inútil.
        var skip = (long)Math.Max(0, page - 1) * pageSize;
        if (skip >= total)
            return ([], total);

        var itemsQuery = query
            // Tiebreaker determinista: sin él, empates de PublishedAt pueden duplicar/omitir filas
            // entre sub-sitemaps (consultas independientes) cuando hay más de un page de noticias.
            .OrderByDescending(n => n.PublishedAt)
            .ThenByDescending(n => n.Id)
            .Skip((int)skip)
            .Take(pageSize)
            .Select(n => new { n.Slug, n.PublishedAt });

        // READ UNCOMMITTED evita contención de locks al generar el sitemap; el provider
        // InMemory (tests) no soporta transacciones con isolation level, así que se omite
        if (!db.Database.IsRelational())
        {
            var rowsNoTx = await itemsQuery.ToListAsync(ct);
            return (rowsNoTx.Select(r => (r.Slug!, r.PublishedAt)).ToList(), total);
        }

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, ct);
        var rows = await itemsQuery.ToListAsync(ct);
        await tx.CommitAsync(ct);
        return (rows.Select(r => (r.Slug!, r.PublishedAt)).ToList(), total);
    }
}
