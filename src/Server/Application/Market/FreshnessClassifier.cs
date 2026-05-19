using Domain.Market;

namespace Application.Market;

public static class FreshnessClassifier
{
    private static readonly TimeSpan FreshThreshold    = TimeSpan.FromMinutes(20);
    private static readonly TimeSpan CriticalThreshold = TimeSpan.FromHours(6);

    // Devuelve null cuando no hay dato (la UI no muestra badge).
    // Valores: "fresh" | "stale" | "off-hours" | "critical"
    public static string? Classify(PriceSnapshot? snapshot, bool isMarketOpen, DateTimeOffset utcNow)
    {
        if (snapshot is null || !snapshot.LastPrice.HasValue)
            return null;

        if (!isMarketOpen)
            return "off-hours";

        if (snapshot.Status == MarketDataStatus.Critical)
            return "critical";

        var age = utcNow - snapshot.CapturedAt;
        if (age >= CriticalThreshold)
            return "critical";
        if (age >= FreshThreshold)
            return "stale";

        return "fresh";
    }
}
