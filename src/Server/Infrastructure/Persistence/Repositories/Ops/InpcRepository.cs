using Application.Ops;
using Domain.Ops;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Ops;

public class InpcRepository(AppDbContext db) : IInpcRepository
{
    public async Task<DateOnly?> GetLatestPeriodoAsync(CancellationToken ct = default)
        => await db.InpcMonthlyEntries.MaxAsync(x => (DateOnly?)x.Periodo, ct);

    public async Task UpsertManyAsync(IEnumerable<InpcMonthlyEntry> entries, CancellationToken ct = default)
    {
        foreach (var entry in entries)
        {
            var existing = await db.InpcMonthlyEntries.FindAsync([entry.Periodo], ct);
            if (existing is null)
            {
                db.InpcMonthlyEntries.Add(entry);
                continue;
            }

            existing.InpcIndex = entry.InpcIndex;
            existing.CapturedAt = entry.CapturedAt;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<InpcMonthlyEntry>> GetLastAsync(int count, CancellationToken ct = default)
        => await db.InpcMonthlyEntries
            .OrderByDescending(x => x.Periodo)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<InpcMonthlyEntry>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
        => await db.InpcMonthlyEntries
            .Where(x => x.Periodo >= from && x.Periodo <= to)
            .OrderBy(x => x.Periodo)
            .ToListAsync(ct);
}
