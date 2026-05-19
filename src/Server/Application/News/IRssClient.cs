namespace Application.News;

public record RssItem(
    string Title,
    string Source,
    DateTimeOffset PublishedAt,
    string Url,
    string? Snippet
);

public interface IRssClient
{
    Task<IReadOnlyList<RssItem>> FetchAsync(string query, CancellationToken ct = default);
}
