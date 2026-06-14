using Application.Seo;
using Domain.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Seo;

public class FaqRepository(AppDbContext db) : IFaqRepository
{
    public async Task<IReadOnlyList<FaqItem>> GetByPageAsync(
        SeoPageType pageType,
        string entityKey,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var normalizedKey = NormalizeEntityKey(entityKey);

        var query = db.FaqItems
            .AsNoTracking()
            .Where(item => item.PageType == pageType && item.EntityKey == normalizedKey);

        if (!includeInactive)
            query = query.Where(item => item.IsActive);

        return await query
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Question)
            .ToListAsync(ct);
    }

    public Task<FaqItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.FaqItems.FirstOrDefaultAsync(item => item.Id == id, ct);

    public Task<FaqItem?> GetByNaturalKeyAsync(SeoPageType pageType, string entityKey, string question, CancellationToken ct = default)
    {
        var normalizedKey = NormalizeEntityKey(entityKey);
        var normalizedQuestion = question.Trim();

        return db.FaqItems.FirstOrDefaultAsync(
            item => item.PageType == pageType &&
                    item.EntityKey == normalizedKey &&
                    item.Question == normalizedQuestion,
            ct);
    }

    public async Task<bool> ExistsAsync(SeoPageType pageType, string entityKey, string question, CancellationToken ct = default)
        => await GetByNaturalKeyAsync(pageType, entityKey, question, ct) is not null;

    public async Task AddAsync(FaqItem item, CancellationToken ct = default)
    {
        item.EntityKey = NormalizeEntityKey(item.EntityKey);
        item.Question = item.Question.Trim();
        item.Answer = item.Answer.Trim();
        db.FaqItems.Add(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> AddIfMissingAsync(FaqItem item, CancellationToken ct = default)
    {
        item.EntityKey = NormalizeEntityKey(item.EntityKey);
        item.Question = item.Question.Trim();
        item.Answer = item.Answer.Trim();
        if (await GetByNaturalKeyAsync(item.PageType, item.EntityKey, item.Question, ct) is not null)
            return false;

        db.FaqItems.Add(item);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            db.Entry(item).State = EntityState.Detached;
            return false;
        }
    }

    public async Task UpdateAsync(FaqItem item, CancellationToken ct = default)
    {
        item.EntityKey = NormalizeEntityKey(item.EntityKey);
        item.Question = item.Question.Trim();
        item.Answer = item.Answer.Trim();
        db.FaqItems.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeactivateAsync(Guid id, string updatedBy, CancellationToken ct = default)
    {
        var item = await GetByIdAsync(id, ct);
        if (item is null)
            return false;

        if (!item.IsActive)
            return false;

        item.IsActive = false;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedBy = updatedBy;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string NormalizeEntityKey(string entityKey)
    {
        var normalized = entityKey.Trim();
        if (normalized.Length == 0)
            return normalized;

        return normalized == "/" ? "/" : normalized.TrimEnd('/');
    }
}
