using Application.Market;
using Domain.Market;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Data.SqlClient;
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
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
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
        var latestProcessedByFibra = db.PriceSnapshots
            .Where(p => p.Status == MarketDataStatus.Processed)
            .GroupBy(p => p.FibraId)
            .Select(g => new { FibraId = g.Key, MaxDate = g.Max(p => p.CapturedAt) });

        return await db.PriceSnapshots
            .Where(p => p.Status == MarketDataStatus.Processed
                     && latestProcessedByFibra
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

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<DailySnapshot>>> GetDailySnapshotsByFibrasAsync(
        IReadOnlyList<Guid> fibraIds, int days, CancellationToken ct = default)
    {
        if (fibraIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<DailySnapshot>>();

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-days);
        var snapshots = await db.DailySnapshots
            .Where(d => fibraIds.Contains(d.FibraId) && d.Date >= cutoff)
            .OrderBy(d => d.Date)
            .ToListAsync(ct);

        return snapshots
            .GroupBy(d => d.FibraId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<DailySnapshot>)g.ToList());
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

    public async Task<IReadOnlyList<Distribution>> GetDistributionsByRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
        => await db.Distributions
            .Where(d => (d.PaymentDate >= from && d.PaymentDate <= to)
                     || (d.ExDividendDate >= from && d.ExDividendDate <= to))
            .OrderBy(d => d.PaymentDate)
            .ThenBy(d => d.Ticker)
            .ToListAsync(ct);

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
        var updated = await UpdateDistributionAmountAsync(dist.FibraId, dist.PaymentDate, dist.AmountPerUnit, ct);
        if (updated)
            return false;

        try
        {
            db.Distributions.Add(dist);
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
        {
            db.Entry(dist).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<int> GetDistributionCountAsync(CancellationToken ct = default)
        => await db.Distributions.CountAsync(ct);

    public async Task<Distribution?> GetDistributionByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Distributions.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<bool> UpdateDistributionAmountAsync(
        Guid fibraId,
        DateOnly paymentDate,
        decimal amount,
        CancellationToken ct = default)
    {
        var existing = await db.Distributions
            .FirstOrDefaultAsync(d => d.FibraId == fibraId && d.PaymentDate == paymentDate, ct);

        if (existing is null)
            return false;

        if (existing.AmountPerUnit != amount)
        {
            existing.AmountPerUnit = amount;
            await db.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<bool> UpdateDistributionBreakdownAsync(
        Guid fibraId,
        DateOnly paymentDate,
        DateOnly? exDate,
        decimal? taxable,
        decimal? capital,
        string? avisoUrl,
        CancellationToken ct = default)
    {
        var existing = await db.Distributions
            .FirstOrDefaultAsync(d => d.FibraId == fibraId && d.PaymentDate == paymentDate, ct);

        // Yahoo Finance stores the ex-dividend date as PaymentDate, not the actual payment date.
        // If the primary lookup fails, try matching by ex-dividend date from MasDividendos.
        if (existing is null && exDate.HasValue)
        {
            existing = await db.Distributions
                .FirstOrDefaultAsync(d => d.FibraId == fibraId && d.PaymentDate == exDate.Value, ct);
        }

        if (existing is null)
            return false;

        var changed = false;

        if (existing.ExDividendDate is null && exDate.HasValue)
        {
            existing.ExDividendDate = exDate;
            changed = true;
        }
        else if (exDate.HasValue && existing.ExDividendDate != exDate)
        {
            existing.ExDividendDate = exDate;
            changed = true;
        }

        if (existing.TaxableAmount is null && taxable.HasValue)
        {
            existing.TaxableAmount = taxable;
            changed = true;
        }
        else if (taxable.HasValue && existing.TaxableAmount.HasValue
                 && Math.Abs(existing.TaxableAmount.Value - taxable.Value) > 0.000001m)
        {
            existing.TaxableAmount = taxable;
            changed = true;
        }

        if (existing.CapitalReturnAmount is null && capital.HasValue)
        {
            existing.CapitalReturnAmount = capital;
            changed = true;
        }
        else if (capital.HasValue && existing.CapitalReturnAmount.HasValue
                 && Math.Abs(existing.CapitalReturnAmount.Value - capital.Value) > 0.000001m)
        {
            existing.CapitalReturnAmount = capital;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(existing.AvisoUrl) && !string.IsNullOrWhiteSpace(avisoUrl))
        {
            existing.AvisoUrl = avisoUrl.Trim();
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(ct);

        return true;
    }

    public async Task UpdateDistributionAsync(Distribution distribution, CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);

    public async Task<bool> DeleteDistributionAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await db.Distributions.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (existing is null)
            return false;

        db.Distributions.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
