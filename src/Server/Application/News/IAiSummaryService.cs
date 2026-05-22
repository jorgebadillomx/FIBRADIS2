using Domain.News;

namespace Application.News;

public interface IAiSummaryService
{
    Task<string?> GenerateSummaryAsync(
        string title,
        string? snippet,
        string? bodyText = null,
        AiContentType contentType = AiContentType.News,
        CancellationToken ct = default);
}
