using Application.Jobs;
using Domain.Jobs;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Jobs;

public class PipelineErrorLogRepository(AppDbContext db) : IPipelineErrorLogRepository
{
    public async Task LogErrorAsync(PipelineErrorLog entry, CancellationToken ct = default)
    {
        db.PipelineErrorLogs.Add(entry);
        await db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<PipelineErrorLog> Items, int Total)> GetPagedAsync(string? pipeline, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.PipelineErrorLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(pipeline))
        {
            query = query.Where(x => x.Pipeline == pipeline);
        }

        query = query.OrderByDescending(x => x.Timestamp).Take(100);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
