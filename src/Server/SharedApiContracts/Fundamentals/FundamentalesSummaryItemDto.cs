namespace SharedApiContracts.Fundamentals;

public sealed record FundamentalesSummaryItemDto(
    string Ticker,
    string Name,
    string Period,
    decimal? CapRate,
    decimal? NavPerCbfi,
    decimal? Ltv,
    decimal? NoiMargin,
    decimal? FfoMargin,
    decimal? QuarterlyDistribution,
    DateTimeOffset CapturedAt
);
