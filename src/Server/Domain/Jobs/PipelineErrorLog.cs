namespace Domain.Jobs;

public class PipelineErrorLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Pipeline { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
    public string AiContext { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
