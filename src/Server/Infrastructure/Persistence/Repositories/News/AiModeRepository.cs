using Application.News;
using Domain.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.News;

public class AiModeRepository(AppDbContext db) : IAiModeRepository
{
    public async Task<AiMode> GetCurrentModeAsync(CancellationToken ct = default)
        => (await db.AiModeConfigs.FindAsync([1], ct))?.Mode ?? AiMode.Off;

    public async Task<AiModeConfig> GetConfigAsync(CancellationToken ct = default)
        => await db.AiModeConfigs.FindAsync([1], ct)
           ?? new AiModeConfig
           {
               Id = 1,
               Mode = AiMode.Off,
               UpdatedAt = DateTimeOffset.UtcNow,
               UpdatedBy = "system",
           };

    public async Task SetModeAsync(AiMode mode, string actor, CancellationToken ct = default)
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var config = await db.AiModeConfigs.FindAsync([1], ct);
        if (config is null)
        {
            db.AiModeConfigs.Add(new AiModeConfig
            {
                Id = 1,
                Mode = mode,
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
                config = await db.AiModeConfigs.FindAsync([1], ct);
                if (config is null)
                    throw;
            }
        }

        if (config.Mode == mode)
            return;

        config.PreviousMode = config.Mode;
        config.Mode = mode;
        config.UpdatedAt = updatedAt;
        config.UpdatedBy = actor;
        await db.SaveChangesAsync(ct);
    }
}
