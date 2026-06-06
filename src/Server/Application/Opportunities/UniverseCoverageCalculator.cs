namespace Application.Opportunities;

public sealed record UniverseCoverage(
    int UniverseSize,
    int FibrasWithPrice,
    decimal MissingPct,
    int DegradationThresholdPct,
    string Status,
    DateTimeOffset? LastValidPriceAt);

public static class UniverseCoverageCalculator
{
    public const decimal SuspensionThresholdPct = 50m;

    public static UniverseCoverage Calculate(
        int universeSize,
        int fibrasWithPrice,
        int degradationThresholdPct,
        DateTimeOffset? lastValidPriceAt)
    {
        degradationThresholdPct = Math.Clamp(degradationThresholdPct, 1, 49);

        if (universeSize == 0)
            return new UniverseCoverage(0, 0, 0m, degradationThresholdPct, "Normal", lastValidPriceAt);

        var missingPct = Math.Round(
            Math.Max(0m, (decimal)(universeSize - fibrasWithPrice) / universeSize * 100m), 1);

        var status = missingPct >= SuspensionThresholdPct ? "Suspended"
            : missingPct >= degradationThresholdPct ? "Degraded"
            : "Normal";

        return new UniverseCoverage(
            universeSize, fibrasWithPrice, missingPct,
            degradationThresholdPct, status, lastValidPriceAt);
    }
}
