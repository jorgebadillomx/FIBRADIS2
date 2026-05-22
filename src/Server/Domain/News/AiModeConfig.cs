namespace Domain.News;

public class AiModeConfig
{
    public int Id { get; set; } = 1;
    public AiMode Mode { get; set; } = AiMode.Off;
    public string NewsModel { get; set; } = "gemini-2.5-pro";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }
    public AiMode? PreviousMode { get; set; }
}
