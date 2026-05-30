using Application.Ai;
using Domain.Ai;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Ai;

public class AiCallLogRepository(AppDbContext db) : IAiCallLogRepository
{
    public async Task AddAsync(AiCallLog entry, CancellationToken ct = default)
    {
        db.AiCallLogs.Add(entry);
        await db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<AiCallLog> Items, int Total)> GetPagedAsync(
        string? operation, string? provider, bool? success, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.AiCallLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(operation))
            query = query.Where(x => x.Operation == operation);
        if (!string.IsNullOrWhiteSpace(provider))
            query = query.Where(x => x.Provider == provider);
        if (success.HasValue)
            query = query.Where(x => x.Success == success.Value);

        query = query.OrderByDescending(x => x.Timestamp);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
