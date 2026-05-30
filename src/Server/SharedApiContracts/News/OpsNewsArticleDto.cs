namespace SharedApiContracts.News;

public sealed record OpsNewsArticleDto(
    Guid Id,
    string Title,
    string Source,
    DateTimeOffset PublishedAt,
    string Url,
    string Status,
    int? BodyTextLength,
    string? BodyTextPreview,
    bool HasAiSummary,
    string? AiSummaryPreview,
    bool HasAiAnalysis,
    string? ImpactPreview);
