using Application.News;
using Domain.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.News;

public class AiPromptRepository(AppDbContext db) : IAiPromptRepository
{
    public Task<AiPrompt?> GetPromptAsync(string contentType, CancellationToken ct = default)
        => db.AiPrompts
            .AsNoTracking()
            .FirstOrDefaultAsync(prompt => prompt.ContentType == contentType.ToLowerInvariant(), ct);

    public async Task SetPromptAsync(string contentType, string template, string actor, CancellationToken ct = default)
    {
        var normalizedType = contentType.ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        var prompt = await db.AiPrompts.FirstOrDefaultAsync(x => x.ContentType == normalizedType, ct);
        if (prompt is not null)
        {
            prompt.PromptTemplate = template;
            prompt.UpdatedAt = now;
            prompt.UpdatedBy = actor;
            await db.SaveChangesAsync(ct);
            return;
        }

        try
        {
            db.AiPrompts.Add(new AiPrompt
            {
                ContentType = normalizedType,
                PromptTemplate = template,
                UpdatedAt = now,
                UpdatedBy = actor,
            });
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Concurrent insert won the race — clear tracker state and update instead
            db.ChangeTracker.Clear();
            var existing = await db.AiPrompts.FirstOrDefaultAsync(x => x.ContentType == normalizedType, ct);
            if (existing is not null)
            {
                existing.PromptTemplate = template;
                existing.UpdatedAt = now;
                existing.UpdatedBy = actor;
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
