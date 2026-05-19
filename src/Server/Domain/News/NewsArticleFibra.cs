namespace Domain.News;

public class NewsArticleFibra
{
    public Guid NewsArticleId { get; set; }
    public Guid FibraId { get; set; }
    public NewsArticle NewsArticle { get; set; } = null!;
}
