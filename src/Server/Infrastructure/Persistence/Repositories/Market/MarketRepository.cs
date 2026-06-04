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

    public async Task<bool> UpsertDailySnapshotAsync(DailySnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await db.DailySnapshots
            .FirstOrDefaultAsync(d => d.FibraId == snapshot.FibraId && d.Date == snapshot.Date, ct);

        if (existing is null)
        {
            try
            {
                db.DailySnapshots.Add(snapshot);
                await db.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
            {
                db.Entry(snapshot).State = EntityState.Detached;
                return false;
            }
        }

        existing.MergeUpdate(snapshot);
        await db.SaveChangesAsync(ct);
        return false;
    }

    public async Task DeleteOldPriceSnapshotsAsync(DateOnly cutoff, CancellationToken ct = default)
    {
        var cutoffDt = cutoff.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        await db.PriceSnapshots
            .Where(p => p.CapturedAt < cutoffDt)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<PriceSnapshot>> GetLatestSnapshotPerFibraAsync(CancellationToken ct = default)
    {
        var latestByFibra = db.PriceSnapshots
            .GroupBy(p => p.FibraId)
            .Select(g => new { FibraId = g.Key, MaxDate = g.Max(p => p.CapturedAt) });

        return await db.PriceSnapshots
            .Where(p => latestByFibra
                .Any(l => l.FibraId == p.FibraId && l.MaxDate == p.CapturedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DailySnapshot>> GetDailySnapshotsAsync(Guid fibraId, int days, CancellationToken ct = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-days);
        return await db.DailySnapshots
            .Where(d => d.FibraId == fibraId && d.Date >= cutoff)
            .OrderBy(d => d.Date)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Distribution>> GetDistributionsAsync(Guid fibraId, int? maxDays = null, CancellationToken ct = default)
    {
        var query = db.Distributions.Where(d => d.FibraId == fibraId);
        if (maxDays.HasValue)
        {
            var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-maxDays.Value);
            query = query.Where(d => d.PaymentDate >= cutoff);
        }
        return await query.OrderByDescending(d => d.PaymentDate).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Distribution>> GetDistributionsByFibrasAsync(
        IReadOnlyList<Guid> fibraIds,
        int days,
        CancellationToken ct = default)
    {
        if (fibraIds.Count == 0)
            return [];

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-days);
        return await db.Distributions
            .Where(d => fibraIds.Contains(d.FibraId) && d.PaymentDate >= cutoff)
            .OrderByDescending(d => d.PaymentDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetWeek52AvgByFibrasAsync(
        IReadOnlyList<Guid> fibraIds,
        int days,
        CancellationToken ct = default)
    {
        if (fibraIds.Count == 0)
            return new Dictionary<Guid, decimal>();

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-days);
        var result = await db.DailySnapshots
            .Where(s => fibraIds.Contains(s.FibraId) && s.Date >= cutoff && s.Close.HasValue)
            .GroupBy(s => s.FibraId)
            .Select(g => new { FibraId = g.Key, Avg = g.Average(s => s.Close!.Value) })
            .ToListAsync(ct);

        return result.ToDictionary(r => r.FibraId, r => r.Avg);
    }

    public async Task AddDistributionAsync(Distribution dist, CancellationToken ct = default)
    {
        db.Distributions.Add(dist);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpsertDistributionAsync(Distribution dist, CancellationToken ct = default)
    {
        try
        {
            db.Distributions.Add(dist);
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            db.Entry(dist).State = EntityState.Detached;
            return false;
        }
    }
}
