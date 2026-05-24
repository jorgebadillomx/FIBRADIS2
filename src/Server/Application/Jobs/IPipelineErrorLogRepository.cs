using Domain.Jobs;

namespace Application.Jobs;

public interface IPipelineErrorLogRepository
{
    Task LogErrorAsync(PipelineErrorLog entry, CancellationToken ct = default);
    Task<(IReadOnlyList<PipelineErrorLog> Items, int Total)> GetPagedAsync(string? pipeline, int page, int pageSize, CancellationToken ct = default);
}
