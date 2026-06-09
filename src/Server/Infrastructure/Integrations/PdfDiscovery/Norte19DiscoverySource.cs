using Application.Fundamentals;
using Domain.Catalog;
using System.Text.Json;

namespace Infrastructure.Integrations.PdfDiscovery;

public class Norte19DiscoverySource(HttpClient http) : IFundamentalsDiscoverySource
{
    private const string ApiUrl =
        "https://api.norte19.com/api/investors-financial?populate=deep&locale=es-mx";

    public string SourceName => "norte19";
    public IReadOnlyList<string> SupportedTickers { get; } = ["HCITY17"];

    public async Task<IReadOnlyList<FundamentalsDiscoveryCandidate>> DiscoverCandidatesAsync(
        Fibra fibra, CancellationToken ct)
    {
        string json;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            json = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"No se pudo obtener la API de Norte19 para {fibra.Ticker}: {ex.Message}", ex);
        }

        var root = JsonSerializer.Deserialize<Norte19Root>(json, JsonOptions);
        var reports = root?.Data?.Attributes?.Reports;
        if (reports is null)
            return [];

        var candidates = new List<FundamentalsDiscoveryCandidate>();
        foreach (var report in reports)
        {
            foreach (var quarterEntry in report.Quarters ?? [])
            {
                var attrs = quarterEntry.Pdf?.Data?.Attributes;
                if (attrs is null)
                    continue;

                var period = ParseNorte19Quarter(quarterEntry.Quarter);

                candidates.Add(new FundamentalsDiscoveryCandidate(
                    SourceName: $"official:{fibra.Ticker}",
                    SourceTitle: attrs.Name,
                    // Stable S3 object key — never changes for the same document
                    PackageUrl: $"norte19:{attrs.Hash}",
                    // Pre-signed URL valid ~15 min — must be used immediately after discovery
                    DownloadUrl: attrs.Url,
                    Period: period,
                    ReportType: period is not null ? "quarterly" : "pending-classification",
                    PublishedAt: null));
            }
        }

        return candidates;
    }

    // Parses Norte19 quarter format "1T26" → "Q1-2026"
    public static string? ParseNorte19Quarter(string? quarter)
    {
        if (string.IsNullOrWhiteSpace(quarter) || quarter.Length < 4 || quarter[1] != 'T')
            return null;
        if (!int.TryParse(quarter[..1], out var q) || q < 1 || q > 4)
            return null;
        if (!int.TryParse(quarter[2..], out var yy) || yy < 18 || yy > 50)
            return null;
        return $"Q{q}-{2000 + yy}";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record Norte19Root(Norte19Data? Data);
    private sealed record Norte19Data(Norte19Attributes? Attributes);
    private sealed record Norte19Attributes(List<Norte19Report>? Reports);
    private sealed record Norte19Report(string Year, List<Norte19QuarterEntry>? Quarters);
    private sealed record Norte19QuarterEntry(string Quarter, Norte19MediaRef? Pdf);
    private sealed record Norte19MediaRef(Norte19MediaData? Data);
    private sealed record Norte19MediaData(Norte19MediaAttributes? Attributes);
    private sealed record Norte19MediaAttributes(string Name, string Hash, string Url);
}
