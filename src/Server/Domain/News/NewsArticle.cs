namespace Domain.News;

public class NewsArticle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string TitleNormalized { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public string? BodyText { get; set; }
    public string? ImageUrl { get; set; }
    public string? AiSummary { get; set; }
    public string? AiAnalysisJson { get; set; }
    public NewsArticleStatus Status { get; set; } = NewsArticleStatus.Pending;
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ErrorReason { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public ICollection<NewsArticleFibra> FibraLinks { get; set; } = [];
}
