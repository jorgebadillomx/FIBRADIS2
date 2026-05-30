namespace Domain.News;

public sealed record NewsAiAnalysis(
    bool IsRelevant,
    string? RelevanceReason,
    string? Headline,
    string Impact,
    IReadOnlyList<string> SectorTags,
    string? Subsector,
    IReadOnlyList<string> AffectedFibers,
    IReadOnlyList<string> KeyFacts,
    IReadOnlyList<NewsKeyFigure> KeyFigures,
    string? SummaryMarkdown,
    string? InvestorTakeaway,
    double Confidence,
    string? ExtractionNotes
);
