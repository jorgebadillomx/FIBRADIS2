using Application.Jobs;
using Domain.Jobs;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Jobs;

public class PipelineRunLogRepository(AppDbContext db) : IPipelineRunLogRepository
{
    private static readonly string[] CompletedStatuses = ["Completed", "Failed"];

    public async Task AddAsync(PipelineRunLog entry, CancellationToken ct = default)
    {
        db.PipelineRunLogs.Add(entry);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PipelineRunLog>> GetRecentAsync(string? pipeline, int take, CancellationToken ct = default)
    {
        var normalizedTake = Math.Max(1, take);
        var query = db.PipelineRunLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(pipeline))
            query = query.Where(x => x.Pipeline == pipeline);

        return await query
            .OrderByDescending(x => x.StartedAt)
            .Take(normalizedTake)
            .ToListAsync(ct);
    }

    public async Task<PipelineRunLog?> GetLastCompletedAsync(string pipeline, CancellationToken ct = default)
        => await db.PipelineRunLogs
            .AsNoTracking()
            .Where(x => x.Pipeline == pipeline && CompletedStatuses.Contains(x.Status))
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(ct);
}
