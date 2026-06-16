namespace SharedApiContracts.Fundamentals;

public sealed record FundamentalesReportDto(
    string Period,
    int? PeriodsAgo,
    decimal? CapRate,
    decimal? NavPerCbfi,
    decimal? Ltv,
    decimal? NoiMargin,
    decimal? FfoMargin,
    decimal? QuarterlyDistribution,
    string? Summary,
    string? SummaryMarkdown,
    string? InvestorTakeaway,
    string[] OperationalSignals,
    string[] FinancialSignals,
    string[] RiskFlags,
    Dictionary<string, string>? FieldNotes,
    DateTimeOffset CapturedAt);
