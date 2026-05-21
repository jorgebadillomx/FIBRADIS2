namespace Application.News;

public interface IOgImageScraper
{
    Task<string?> TryGetOgImageAsync(string url, CancellationToken ct = default);
}
