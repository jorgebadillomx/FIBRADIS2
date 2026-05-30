namespace Domain.Fundamentals;

public sealed record FundamentalAiAnalysis(
    string? SummaryMarkdown,
    string? InvestorTakeaway,
    IReadOnlyList<string> OperationalSignals,
    IReadOnlyList<string> FinancialSignals,
    IReadOnlyList<string> RiskFlags,
    string? ExtractionNotes);
