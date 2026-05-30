using Domain.News;

namespace Application.News;

public interface IAiNewsAnalysisService
{
    Task<NewsAiAnalysis?> GenerateAnalysisAsync(
        string title,
        string? snippet,
        string? bodyText,
        CancellationToken ct = default);
}
