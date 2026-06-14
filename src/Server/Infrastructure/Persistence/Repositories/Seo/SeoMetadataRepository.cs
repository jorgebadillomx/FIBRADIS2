using Application.Seo;
using Domain.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Seo;

public class SeoMetadataRepository(AppDbContext db) : ISeoMetadataRepository
{
    public async Task<SeoMetadata?> GetAsync(SeoPageType pageType, string entityKey, CancellationToken ct = default)
    {
        var normalizedKey = NormalizeEntityKey(entityKey);
        return await db.SeoMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(
                metadata => metadata.PageType == pageType && metadata.EntityKey == normalizedKey,
                ct);
    }

    public async Task<IReadOnlyList<SeoMetadata>> GetAllAsync(SeoMetadataQuery? query = null, CancellationToken ct = default)
    {
        var dbQuery = db.SeoMetadata.AsNoTracking().AsQueryable();

        if (query?.PageType is not null)
        {
            var pageType = query.PageType.Value;
            dbQuery = dbQuery.Where(metadata => metadata.PageType == pageType);
        }

        if (!string.IsNullOrWhiteSpace(query?.Search))
        {
            var search = query.Search.Trim();
            dbQuery = dbQuery.Where(metadata =>
                metadata.EntityKey.Contains(search) ||
                metadata.Title.Contains(search));
        }

        return await dbQuery
            .OrderBy(metadata => metadata.PageType)
            .ThenBy(metadata => metadata.EntityKey)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(SeoPageType pageType, string entityKey, CancellationToken ct = default)
        => await db.SeoMetadata.AnyAsync(
            metadata => metadata.PageType == pageType && metadata.EntityKey == NormalizeEntityKey(entityKey),
            ct);

    public async Task<SeoMetadata> UpsertAsync(SeoMetadata metadata, bool overrideMode = false, CancellationToken ct = default)
    {
        metadata.EntityKey = NormalizeEntityKey(metadata.EntityKey);

        var existing = await db.SeoMetadata
            .FirstOrDefaultAsync(
                item => item.PageType == metadata.PageType && item.EntityKey == metadata.EntityKey,
                ct);

        if (existing is null)
        {
            db.SeoMetadata.Add(metadata);
            try
            {
                await db.SaveChangesAsync(ct);
                return metadata;
            }
            catch (DbUpdateException)
            {
                // El insert falló (carrera con otro upsert); 'metadata' sigue en estado Added.
                // Sin detach, el SaveChanges final reintentaría el insert junto al update de
                // 'existing' y re-dispararía la violación de índice único.
                db.Entry(metadata).State = EntityState.Detached;

                existing = await db.SeoMetadata
                    .FirstOrDefaultAsync(
                        item => item.PageType == metadata.PageType && item.EntityKey == metadata.EntityKey,
                        ct);
                if (existing is null)
                    throw;
            }
        }

        ApplyMetadata(existing!, metadata, overrideMode);
        await db.SaveChangesAsync(ct);
        return existing!;
    }

    public async Task<IReadOnlyList<(SeoPageType PageType, string EntityKey)>> GetExistingKeysAsync(
        IEnumerable<(SeoPageType PageType, string EntityKey)> keys,
        CancellationToken ct = default)
    {
        var results = new List<(SeoPageType PageType, string EntityKey)>();

        foreach (var (pageType, entityKey) in keys)
        {
            if (await ExistsAsync(pageType, entityKey, ct))
            {
                results.Add((pageType, NormalizeEntityKey(entityKey)));
            }
        }

        return results;
    }

    private static void ApplyMetadata(SeoMetadata target, SeoMetadata source, bool overrideMode)
    {
        if (overrideMode)
        {
            target.Title = source.Title;
            target.MetaDescription = source.MetaDescription;
            target.CanonicalPath = source.CanonicalPath;
            target.OgTitle = source.OgTitle;
            target.OgDescription = source.OgDescription;
            target.OgType = source.OgType;
            target.OgImageUrl = source.OgImageUrl;
            target.OgLocale = source.OgLocale;
            target.TwitterCard = source.TwitterCard;
            target.RobotsDirectives = source.RobotsDirectives;
            target.JsonLd = source.JsonLd;
            target.TitleIsOverridden = source.TitleIsOverridden;
            target.MetaDescriptionIsOverridden = source.MetaDescriptionIsOverridden;
            target.CanonicalPathIsOverridden = source.CanonicalPathIsOverridden;
            target.OgTitleIsOverridden = source.OgTitleIsOverridden;
            target.OgDescriptionIsOverridden = source.OgDescriptionIsOverridden;
            target.OgTypeIsOverridden = source.OgTypeIsOverridden;
            target.OgImageUrlIsOverridden = source.OgImageUrlIsOverridden;
            target.OgLocaleIsOverridden = source.OgLocaleIsOverridden;
            target.TwitterCardIsOverridden = source.TwitterCardIsOverridden;
            target.RobotsDirectivesIsOverridden = source.RobotsDirectivesIsOverridden;
            target.JsonLdIsOverridden = source.JsonLdIsOverridden;
            target.IsActive = source.IsActive;
        }
        else
        {
            if (!target.TitleIsOverridden) target.Title = source.Title;
            if (!target.MetaDescriptionIsOverridden) target.MetaDescription = source.MetaDescription;
            if (!target.CanonicalPathIsOverridden) target.CanonicalPath = source.CanonicalPath;
            if (!target.OgTitleIsOverridden) target.OgTitle = source.OgTitle;
            if (!target.OgDescriptionIsOverridden) target.OgDescription = source.OgDescription;
            if (!target.OgTypeIsOverridden) target.OgType = source.OgType;
            if (!target.OgImageUrlIsOverridden) target.OgImageUrl = source.OgImageUrl;
            if (!target.OgLocaleIsOverridden) target.OgLocale = source.OgLocale;
            if (!target.TwitterCardIsOverridden) target.TwitterCard = source.TwitterCard;
            if (!target.RobotsDirectivesIsOverridden) target.RobotsDirectives = source.RobotsDirectives;
            if (!target.JsonLdIsOverridden) target.JsonLd = source.JsonLd;
        }

        target.UpdatedAt = source.UpdatedAt;
        target.UpdatedBy = source.UpdatedBy;
    }

    private static string NormalizeEntityKey(string entityKey)
    {
        var normalized = entityKey.Trim();
        if (normalized.Length == 0)
            return normalized;

        return normalized == "/" ? "/" : normalized.TrimEnd('/');
    }
}
