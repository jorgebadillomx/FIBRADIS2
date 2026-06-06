using Application.Fundamentals;
using Domain.Catalog;

namespace Infrastructure.Integrations.PdfDiscovery;

public class AmefibraDiscoverySource(IAmefibraDiscoveryClient client) : IFundamentalsDiscoverySource
{
    private List<AmefibraListingItem>? _cachedListings;
    private readonly Dictionary<string, AmefibraPackageDetails?> _detailsCache = new(StringComparer.OrdinalIgnoreCase);

    public string SourceName => "AMEFIBRA";

    // Amefibra indexes all active FIBRAs — we don't filter by ticker at the source level
    public IReadOnlyList<string> SupportedTickers => [];

    // Returns candidates from Amefibra that match this fibra, using fuzzy name matching
    public async Task<IReadOnlyList<FundamentalsDiscoveryCandidate>> DiscoverCandidatesAsync(Fibra fibra, CancellationToken ct)
    {
        _cachedListings ??= [.. await client.GetListingItemsAsync(ct)];

        var candidates = new List<FundamentalsDiscoveryCandidate>();
        foreach (var listing in _cachedListings)
        {
            var parse = AmefibraTitleParser.Parse(listing.Title);
            if (!MatchesFibra(parse.FibraHint, fibra))
                continue;

            var downloadUrl = listing.DownloadUrl;
            DateTimeOffset? publishedAt = null;

            // Items de sitemap no tienen DownloadUrl directo: obtenerlo del detalle del paquete
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                if (!_detailsCache.TryGetValue(listing.PackageUrl, out var details))
                {
                    try { details = await client.GetPackageDetailsAsync(listing.PackageUrl, ct); }
                    catch { details = null; }
                    _detailsCache[listing.PackageUrl] = details;
                }
                if (details is null) continue;
                downloadUrl = details.DownloadUrl;
                publishedAt = details.SourcePublishedAt;
            }

            candidates.Add(new FundamentalsDiscoveryCandidate(
                SourceName,
                listing.Title,
                listing.PackageUrl,
                downloadUrl,
                parse.Period,
                parse.ReportType,
                publishedAt));
        }
        return candidates;
    }

    private static bool MatchesFibra(string? hint, Fibra fibra)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return false;

        var normalizedHint = NormalizeKey(hint);
        var candidates = new[]
            {
                fibra.Ticker,
                fibra.Ticker.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'),
                fibra.ShortName,
                fibra.FullName,
            }
            .Concat(fibra.NameVariants)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeKey);

        return candidates.Any(c =>
            c.Contains(normalizedHint, StringComparison.Ordinal) ||
            normalizedHint.Contains(c, StringComparison.Ordinal));
    }

    private static string NormalizeKey(string value)
        => string.Concat(value
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch =>
                System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) !=
                System.Globalization.UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(ch)));
}
