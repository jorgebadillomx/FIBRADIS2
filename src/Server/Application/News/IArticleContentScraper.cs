namespace Application.News;

public interface IArticleContentScraper
{
    Task<string?> TryGetArticleTextAsync(string url, CancellationToken ct = default);
}
