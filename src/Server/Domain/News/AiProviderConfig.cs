namespace Domain.News;

public class AiProviderConfig
{
    public int Id { get; set; } = 1;
    public AiProvider Provider { get; set; } = AiProvider.Gemini;
    public string ModelId { get; set; } = "gemini-2.5-flash";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }
}
