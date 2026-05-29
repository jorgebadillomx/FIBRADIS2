namespace Domain.Ai;

public class AiCallLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public int PromptLength { get; set; }
    public long DurationMs { get; set; }
    public bool Success { get; set; }
    public string? RequestRaw { get; set; }
    public string? ResponseRaw { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Context { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
