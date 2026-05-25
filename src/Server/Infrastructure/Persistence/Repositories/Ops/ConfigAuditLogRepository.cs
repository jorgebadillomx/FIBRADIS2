using Application.Ops;
using Domain.Ops;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Ops;

public class ConfigAuditLogRepository(AppDbContext db) : IConfigAuditLogRepository
{
    public async Task<IReadOnlyList<ConfigAuditLog>> GetRecentAsync(int limit = 50, CancellationToken ct = default)
        => await db.ConfigAuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.ChangedAt)
            .Take(limit)
            .ToListAsync(ct);
}
