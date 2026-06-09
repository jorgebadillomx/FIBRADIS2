using System.Text.Json;
using Application.Portfolio;
using Domain.Portfolio;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Portfolio;

public class PortfolioRepository(AppDbContext db) : IPortfolioRepository
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

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
        await ExecuteInTransactionAsync(async () =>
        {
            var existing = await GetByUserIdAsync(userId, ct);
            if (existing.Count > 0)
            {
                await UpsertSnapshotAsync(userId, existing, DateTimeOffset.UtcNow, ct);
                db.PortfolioPositions.RemoveRange(existing);
            }

            db.PortfolioPositions.AddRange(positions);
            await db.SaveChangesAsync(ct);
        }, ct);
    }

    public async Task ArchivePortfolioAsync(Guid userId, CancellationToken ct)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            var positions = await GetByUserIdAsync(userId, ct);
            if (positions.Count == 0)
                return;

            await UpsertSnapshotAsync(userId, positions, DateTimeOffset.UtcNow, ct);
            db.PortfolioPositions.RemoveRange(positions);

            await db.SaveChangesAsync(ct);
        }, ct);
    }

    public async Task<bool> RestoreSnapshotAsync(Guid userId, CancellationToken ct)
    {
        var snapshot = await GetSnapshotAsync(userId, ct);
        if (snapshot is null)
            return false;

        var snapshotPositions = DeserializeSnapshotPositions(snapshot.PositionsJson);
        if (snapshotPositions is null)
            return false;

        await ExecuteInTransactionAsync(async () =>
        {
            var activePositions = await GetByUserIdAsync(userId, ct);
            if (activePositions.Count > 0)
                db.PortfolioPositions.RemoveRange(activePositions);

            db.PortfolioPositions.AddRange(snapshotPositions.Select(position => new PortfolioPosition
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FibraId = position.FibraId,
                Titulos = position.Titulos,
                CostoPromedio = position.CostoPromedio,
                CostoTotalCompra = position.CostoTotalCompra,
                UploadedAt = position.UploadedAt,
            }));

            db.PortfolioSnapshots.Remove(snapshot);
            await db.SaveChangesAsync(ct);
        }, ct);
        return true;
    }

    public async Task<PortfolioSnapshot?> GetSnapshotAsync(Guid userId, CancellationToken ct)
        => await db.PortfolioSnapshots
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task MergePositionsAsync(
        Guid userId,
        IReadOnlyList<PortfolioPosition> positions,
        CancellationToken ct)
    {
        if (positions.Count == 0)
            return;

        await ExecuteInTransactionAsync(async () =>
        {
            var existingPositions = await GetByUserIdAsync(userId, ct);
            var existingByFibra = existingPositions.ToDictionary(p => p.FibraId);

            foreach (var position in positions)
            {
                if (existingByFibra.TryGetValue(position.FibraId, out var existing))
                {
                    var totalTitulos = existing.Titulos + position.Titulos;
                    existing.CostoPromedio = (
                        (existing.Titulos * existing.CostoPromedio)
                        + (position.Titulos * position.CostoPromedio))
                        / totalTitulos;
                    existing.Titulos = totalTitulos;
                    existing.CostoTotalCompra += position.CostoTotalCompra;
                    existing.UploadedAt = position.UploadedAt;
                    continue;
                }

                db.PortfolioPositions.Add(position);
            }

            await db.SaveChangesAsync(ct);
        }, ct);
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

        var existing = await db.Set<UserPortfolioSettings>()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (existing is not null)
        {
            existing.ColumnConfigJson = columnConfigJson;
            existing.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
            return;
        }

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
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
        {
            // Concurrent insert won the race; retry as update
            db.Entry(settings).State = EntityState.Detached;
            var retried = await db.Set<UserPortfolioSettings>()
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);
            if (retried is not null)
            {
                retried.ColumnConfigJson = columnConfigJson;
                retried.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
            }
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

    private async Task UpsertSnapshotAsync(
        Guid userId,
        IReadOnlyList<PortfolioPosition> positions,
        DateTimeOffset archivedAt,
        CancellationToken ct)
    {
        var snapshotJson = JsonSerializer.Serialize(
            positions.Select(position => new SnapshotPositionDto(
                position.FibraId,
                position.Titulos,
                position.CostoPromedio,
                position.CostoTotalCompra,
                position.UploadedAt)).ToArray(),
            SnapshotJsonOptions);

        var snapshot = await db.PortfolioSnapshots
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (snapshot is null)
        {
            db.PortfolioSnapshots.Add(new PortfolioSnapshot
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ArchivedAt = archivedAt,
                PositionsJson = snapshotJson,
            });
            return;
        }

        snapshot.ArchivedAt = archivedAt;
        snapshot.PositionsJson = snapshotJson;
    }

    private async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct)
    {
        if (!db.Database.IsRelational())
        {
            await action();
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await action();
        await tx.CommitAsync(ct);
    }

    private static IReadOnlyList<SnapshotPositionDto>? DeserializeSnapshotPositions(string positionsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<SnapshotPositionDto>>(positionsJson, SnapshotJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record SnapshotPositionDto(
        Guid FibraId,
        int Titulos,
        decimal CostoPromedio,
        decimal CostoTotalCompra,
        DateTimeOffset UploadedAt);
}
