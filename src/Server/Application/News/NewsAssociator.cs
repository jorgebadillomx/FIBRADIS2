namespace Application.News;

public static class NewsAssociator
{
    public static IReadOnlyList<Guid> Associate(
        RssItem item,
        IReadOnlyList<FibraMatchInfo> fibras)
    {
        if (fibras.Count == 0)
            return [];

        var haystack = BuildHaystack(item.Title, item.Snippet);
        var matches = new HashSet<Guid>();

        foreach (var fibra in fibras)
        {
            if (MatchesTicker(haystack, fibra.Ticker))
            {
                matches.Add(fibra.Id);
                continue;
            }

            foreach (var variant in fibra.NameVariants)
            {
                if (MatchesVariant(haystack, variant))
                {
                    matches.Add(fibra.Id);
                    break;
                }
            }
        }

        return [.. matches];
    }

    private static string BuildHaystack(string title, string? snippet)
        => $" {NewsDeduplicator.NormalizeTitle($"{title} {snippet ?? string.Empty}")} ";

    private static bool MatchesTicker(string haystack, string ticker)
    {
        var normalizedTicker = NewsDeduplicator.NormalizeTitle(ticker);
        return !string.IsNullOrWhiteSpace(normalizedTicker)
            && haystack.Contains($" {normalizedTicker} ", StringComparison.Ordinal);
    }

    private static bool MatchesVariant(string haystack, string variant)
    {
        var normalizedVariant = NewsDeduplicator.NormalizeTitle(variant);
        return !string.IsNullOrWhiteSpace(normalizedVariant)
            && haystack.Contains($" {normalizedVariant} ", StringComparison.Ordinal);
    }
}
