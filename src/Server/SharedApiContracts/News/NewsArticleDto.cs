namespace SharedApiContracts.News;

public sealed record NewsArticleDto(
    Guid Id,
    string Title,
    string Source,
    DateTimeOffset PublishedAt,
    string Url,
    string? Snippet,
    string? ImageUrl,
    string? AiSummary,
    NewsAiAnalysisDto? AiAnalysis,
    IReadOnlyList<LinkedFibraDto>? LinkedFibras = null
);

public sealed record LinkedFibraDto(Guid Id, string Ticker);
