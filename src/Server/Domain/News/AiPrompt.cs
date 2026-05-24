namespace Domain.News;

public class AiPrompt
{
    public int Id { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string PromptTemplate { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string UpdatedBy { get; set; } = "system";
}
