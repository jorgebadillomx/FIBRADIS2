using Application.News;
using Domain.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.News;

public class AiProviderConfigRepository(AppDbContext db) : IAiProviderConfigRepository
{
    public async Task<AiProviderConfig> GetConfigAsync(CancellationToken ct = default)
        => await db.AiProviderConfigs.FindAsync([1], ct)
           ?? new AiProviderConfig
           {
               Id = 1,
               Provider = AiProvider.Gemini,
               ModelId = "gemini-2.5-flash",
               UpdatedAt = DateTimeOffset.UtcNow,
               UpdatedBy = "system",
           };

    public async Task SetProviderAsync(AiProvider provider, string modelId, string actor, CancellationToken ct = default)
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var config = await db.AiProviderConfigs.FindAsync([1], ct);
        if (config is null)
        {
            db.AiProviderConfigs.Add(new AiProviderConfig
            {
                Id = 1,
                Provider = provider,
                ModelId = modelId,
                UpdatedAt = updatedAt,
                UpdatedBy = actor,
            });

            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                config = await db.AiProviderConfigs.FindAsync([1], ct);
                if (config is null)
                    throw;
            }
        }

        config.Provider = provider;
        config.ModelId = modelId;
        config.UpdatedAt = updatedAt;
        config.UpdatedBy = actor;
        await db.SaveChangesAsync(ct);
    }
}
