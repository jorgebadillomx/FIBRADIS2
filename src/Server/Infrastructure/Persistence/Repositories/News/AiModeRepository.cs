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
        => await UpdateConfigAsync(mode, null, null, actor, ct);

    public async Task UpdateConfigAsync(AiMode? mode, string? newsModel, int? minBodyTextLengthForAi, string actor, CancellationToken ct = default)
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var config = await db.AiModeConfigs.FindAsync([1], ct);
        if (config is null)
        {
            db.AiModeConfigs.Add(new AiModeConfig
            {
                Id = 1,
                Mode = mode ?? AiMode.Off,
                NewsModel = newsModel ?? "gemini-2.5-pro",
                MinBodyTextLengthForAi = minBodyTextLengthForAi ?? 500,
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

        var changed = false;

        if (mode is not null && config.Mode != mode.Value)
        {
            config.PreviousMode = config.Mode;
            config.Mode = mode.Value;
            changed = true;
        }

        if (newsModel is not null && !string.Equals(config.NewsModel, newsModel, StringComparison.OrdinalIgnoreCase))
        {
            config.NewsModel = newsModel;
            changed = true;
        }

        if (minBodyTextLengthForAi is not null && config.MinBodyTextLengthForAi != minBodyTextLengthForAi.Value)
        {
            config.MinBodyTextLengthForAi = minBodyTextLengthForAi.Value;
            changed = true;
        }

        if (!changed)
            return;

        config.UpdatedAt = updatedAt;
        config.UpdatedBy = actor;
        await db.SaveChangesAsync(ct);
    }
}
