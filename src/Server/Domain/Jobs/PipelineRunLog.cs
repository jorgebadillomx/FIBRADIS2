namespace Domain.Jobs;

public class PipelineRunLog
{
    public Guid Id { get; init; }
    public string Pipeline { get; init; } = string.Empty;
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public int? ItemsProcessed { get; init; }
    public int? ErrorCount { get; init; }
    public string? TriggeredBy { get; init; }
    public string? Details { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
