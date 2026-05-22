namespace Application.News;

public interface IGoogleNewsUrlDecoder
{
    Task<string?> TryDecodeAsync(string googleNewsUrl, CancellationToken ct = default);
}
