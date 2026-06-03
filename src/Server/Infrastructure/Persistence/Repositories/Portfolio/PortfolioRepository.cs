using Application.Portfolio;
using Domain.Portfolio;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Portfolio;

public class PortfolioRepository(AppDbContext db) : IPortfolioRepository
{
    public async Task<IReadOnlyList<PortfolioPosition>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        => await db.PortfolioPositions
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.FibraId)
            .ToListAsync(ct);

    public async Task UpsertPortfolioAsync(
        Guid userId,
        IReadOnlyList<PortfolioPosition> positions,
        CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.PortfolioPositions
            .Where(p => p.UserId == userId)
            .ExecuteDeleteAsync(ct);

        db.PortfolioPositions.AddRange(positions);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
    }

    public async Task<int> GetPositionCountByUserIdAsync(Guid userId, CancellationToken ct)
        => await db.PortfolioPositions
            .CountAsync(p => p.UserId == userId, ct);

    public async Task<UserPortfolioSettings?> GetSettingsAsync(Guid userId, CancellationToken ct)
        => await db.Set<UserPortfolioSettings>()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task UpsertSettingsAsync(Guid userId, string? columnConfigJson, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var updated = await db.Set<UserPortfolioSettings>()
            .Where(s => s.UserId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.ColumnConfigJson, columnConfigJson)
                .SetProperty(s => s.UpdatedAt, now), ct);

        if (updated > 0)
            return;

        var settings = new UserPortfolioSettings
        {
            UserId = userId,
            ColumnConfigJson = columnConfigJson,
            UpdatedAt = now,
        };

        db.Set<UserPortfolioSettings>().Add(settings);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            db.Entry(settings).State = EntityState.Detached;
            await db.Set<UserPortfolioSettings>()
                .Where(s => s.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.ColumnConfigJson, columnConfigJson)
                    .SetProperty(s => s.UpdatedAt, now), ct);
        }
    }

    public async Task<PortfolioPosition?> GetPositionAsync(Guid userId, Guid fibraId, CancellationToken ct)
        => await db.PortfolioPositions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.FibraId == fibraId, ct);

    public async Task UpdatePositionAsync(PortfolioPosition position, CancellationToken ct)
    {
        db.PortfolioPositions.Update(position);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeletePositionAsync(Guid userId, Guid fibraId, CancellationToken ct)
    {
        var position = await db.PortfolioPositions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.FibraId == fibraId, ct);
        if (position is null)
            return false;
        db.PortfolioPositions.Remove(position);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
