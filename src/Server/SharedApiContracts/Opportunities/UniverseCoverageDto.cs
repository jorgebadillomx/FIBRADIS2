namespace SharedApiContracts.Opportunities;

public sealed record UniverseCoverageDto(
    int UniverseSize,
    int FibrasWithPrice,
    decimal MissingPct,
    int DegradationThresholdPct,
    string Status,
    DateTimeOffset? LastValidPriceAt
);
