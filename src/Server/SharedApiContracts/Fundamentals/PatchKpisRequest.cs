namespace SharedApiContracts.Fundamentals;

public sealed record PatchKpisRequest(
    decimal? CapRate,
    decimal? NavPerCbfi,
    decimal? Ltv,
    decimal? NoiMargin,
    decimal? FfoMargin,
    decimal? QuarterlyDistribution,
    string? Summary);
