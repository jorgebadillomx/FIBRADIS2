namespace Application.Fundamentals;

public sealed record KpiExtractionResult(
    decimal? CapRate,
    string? CapRateNote,
    decimal? NavPerCbfi,
    string? NavPerCbfiNote,
    decimal? Ltv,
    string? LtvNote,
    decimal? NoiMargin,
    string? NoiMarginNote,
    decimal? FfoMargin,
    string? FfoMarginNote,
    decimal? QuarterlyDistribution,
    string? QuarterlyDistributionNote,
    string? Summary,
    string ExtractionNotes,
    bool Success,
    string? SummaryMarkdown = null,
    string? InvestorTakeaway = null,
    IReadOnlyList<string>? OperationalSignals = null,
    IReadOnlyList<string>? FinancialSignals = null,
    IReadOnlyList<string>? RiskFlags = null);
