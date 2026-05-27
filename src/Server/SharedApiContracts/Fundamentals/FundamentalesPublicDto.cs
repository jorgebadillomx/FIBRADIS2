namespace SharedApiContracts.Fundamentals;

public sealed record FundamentalesPublicDto(
    string Period,
    int? PeriodsAgo,
    decimal? CapRate,
    decimal? NavPerCbfi,
    decimal? Ltv,
    decimal? NoiMargin,
    decimal? FfoMargin,
    decimal? QuarterlyDistribution,
    string? Summary,
    Dictionary<string, string>? FieldNotes,
    DateTimeOffset CapturedAt);
