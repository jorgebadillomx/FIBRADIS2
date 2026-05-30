namespace SharedApiContracts.News;

public sealed record NewsAiAnalysisDto(
    bool IsRelevant,
    string? RelevanceReason,
    string? Headline,
    string Impact,
    IReadOnlyList<string> SectorTags,
    string? Subsector,
    IReadOnlyList<string> AffectedFibers,
    IReadOnlyList<string> KeyFacts,
    IReadOnlyList<NewsKeyFigureDto> KeyFigures,
    string? SummaryMarkdown,
    string? InvestorTakeaway,
    double Confidence,
    string? ExtractionNotes
);
