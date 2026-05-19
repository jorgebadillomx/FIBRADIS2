using Application.Market;
using Domain.Market;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Market;

public class MarketRepository(AppDbContext db) : IMarketRepository
{
    public async Task AddPriceSnapshotAsync(PriceSnapshot snapshot, CancellationToken ct = default)
    {
        db.PriceSnapshots.Add(snapshot);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PriceSnapshot>> GetLastSnapshotsAsync(
        Guid fibraId, int count, CancellationToken ct = default)
        => await db.PriceSnapshots
            .Where(p => p.FibraId == fibraId)
            .OrderByDescending(p => p.CapturedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task UpsertDailySnapshotAsync(DailySnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await db.DailySnapshots
            .FirstOrDefaultAsync(d => d.FibraId == snapshot.FibraId && d.Date == snapshot.Date, ct);

        if (existing is null)
        {
            db.DailySnapshots.Add(snapshot);
        }
        else
        {
            existing.Open = snapshot.Open;
            existing.High = snapshot.High;
            existing.Low = snapshot.Low;
            existing.Close = snapshot.Close;
            existing.Volume = snapshot.Volume;
        }

        await db.SaveChangesAsync(ct);
    }
}
