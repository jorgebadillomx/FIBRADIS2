namespace SharedApiContracts.Jobs;

public sealed record PipelineRunLogDto(
    Guid Id,
    string Pipeline,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string Status,
    int? ItemsProcessed,
    int? ErrorCount,
    string? TriggeredBy,
    string? Details
);
