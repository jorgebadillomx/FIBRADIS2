using Domain.Ai;

namespace Application.Ai;

public interface IAiCallLogRepository
{
    Task AddAsync(AiCallLog entry, CancellationToken ct = default);
    Task<(IReadOnlyList<AiCallLog> Items, int Total)> GetPagedAsync(string? operation, string? provider, bool? success, int page, int pageSize, CancellationToken ct = default);
}
