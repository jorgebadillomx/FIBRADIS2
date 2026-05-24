namespace SharedApiContracts.Jobs;

public sealed record PipelineStatusDto(
    string Pipeline,
    string DerivedStatus,
    DateTimeOffset? LastRunAt,
    int? LastDurationSeconds,
    int? LastItemsProcessed,
    int? LastErrorCount,
    IReadOnlyList<PipelineRunLogDto> RecentRuns
);
