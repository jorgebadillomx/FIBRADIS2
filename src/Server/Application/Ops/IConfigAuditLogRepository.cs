using Domain.Ops;

namespace Application.Ops;

public interface IConfigAuditLogRepository
{
    Task<IReadOnlyList<ConfigAuditLog>> GetRecentAsync(int limit = 50, CancellationToken ct = default);
}
