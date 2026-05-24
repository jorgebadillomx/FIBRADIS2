using Domain.Jobs;

namespace Application.Jobs;

public interface IPipelineRunLogRepository
{
    Task AddAsync(PipelineRunLog entry, CancellationToken ct = default);
    Task<IReadOnlyList<PipelineRunLog>> GetRecentAsync(string? pipeline, int take, CancellationToken ct = default);
    Task<PipelineRunLog?> GetLastCompletedAsync(string pipeline, CancellationToken ct = default);
}
