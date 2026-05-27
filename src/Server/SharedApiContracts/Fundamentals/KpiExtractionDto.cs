namespace SharedApiContracts.Fundamentals;

public sealed record KpiExtractionDto(
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
    int MarkdownLength);
