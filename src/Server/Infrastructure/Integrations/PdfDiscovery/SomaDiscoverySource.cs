using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Fundamentals;
using Domain.Catalog;

namespace Infrastructure.Integrations.PdfDiscovery;

public class SomaDiscoverySource(HttpClient http) : IFundamentalsDiscoverySource
{
    private const string ApiUrl = "https://fibrasoma.group/wp-json/soma/documents";

    public string SourceName => "official:SOMA21";
    public IReadOnlyList<string> SupportedTickers { get; } = ["SOMA21"];

    public async Task<IReadOnlyList<FundamentalsDiscoveryCandidate>> DiscoverCandidatesAsync(Fibra fibra, CancellationToken ct)
    {
        string json;
        try
        {
            json = await http.GetStringAsync(ApiUrl, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"No se pudo obtener el API de SOMA21: {ex.Message}", ex);
        }

        var items = JsonSerializer.Deserialize<List<SomaDocument>>(json, SerializerOptions) ?? [];

        var candidates = new List<FundamentalsDiscoveryCandidate>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Url))
                continue;

            var isQuarterly = IsQuarterlyReport(item);
            if (!isQuarterly)
                continue;

            var fileName = Path.GetFileNameWithoutExtension(
                Uri.TryCreate(item.Url, UriKind.Absolute, out var u) ? u.LocalPath : item.Url);
            var (period, reportType) = OfficialSitePeriodParser.Parse(fileName);

            DateTimeOffset? publishedAt = null;
            if (!string.IsNullOrWhiteSpace(item.Date) &&
                DateTimeOffset.TryParse(item.Date, out var dt))
                publishedAt = dt;

            candidates.Add(new FundamentalsDiscoveryCandidate(
                SourceName: SourceName,
                SourceTitle: item.Title ?? fileName ?? item.Url,
                PackageUrl: item.Url,
                DownloadUrl: item.Url,
                Period: period,
                ReportType: reportType,
                PublishedAt: publishedAt));
        }

        return candidates;
    }

    private static bool IsQuarterlyReport(SomaDocument item)
    {
        var type = item.Type ?? item.DocumentType ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(type))
            return type.Contains("quarterly", StringComparison.OrdinalIgnoreCase);

        // Fall back to title/URL heuristics
        var text = $"{item.Title} {item.Url}".ToLowerInvariant();
        if (text.Contains("annual") || text.Contains("anual") ||
            text.Contains("sostenibilidad") || text.Contains("sustainability") ||
            text.Contains("factsheet") || text.Contains("fact-sheet"))
            return false;

        return text.Contains("quarterly") || text.Contains("trimestral") ||
               OfficialSitePeriodParser.Parse(item.Url).ReportType == "quarterly";
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed class SomaDocument
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("date")] public string? Date { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("document_type")] public string? DocumentType { get; set; }
        [JsonPropertyName("link")] public string? Link { get; set; }
        [JsonPropertyName("file")] public string? File { get; set; }
    }
}
