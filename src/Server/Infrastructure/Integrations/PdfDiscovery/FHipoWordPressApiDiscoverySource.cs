using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Fundamentals;
using Domain.Catalog;

namespace Infrastructure.Integrations.PdfDiscovery;

public class FHipoWordPressApiDiscoverySource(HttpClient http) : IFundamentalsDiscoverySource
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string SourceName => "official:FHIPO14";

    public IReadOnlyList<string> SupportedTickers { get; } = ["FHIPO14"];

    public async Task<IReadOnlyList<FundamentalsDiscoveryCandidate>> DiscoverCandidatesAsync(Fibra fibra, CancellationToken ct)
    {
        var candidates = new List<FundamentalsDiscoveryCandidate>();
        var page = 1;
        int totalPages;

        do
        {
            var apiUrl = $"https://fhipo.com/wp-json/wp/v2/media?media_type=application&per_page=100&page={page}";
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("Accept-Language", "es-MX,es;q=0.9,en;q=0.8");

            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"WordPress Media API de FHIPO devolvió {(int)response.StatusCode} {response.ReasonPhrase}. " +
                    "El sitio puede tener protección WAF activa en esta IP.");

            totalPages = response.Headers.TryGetValues("X-WP-TotalPages", out var pv) &&
                         int.TryParse(pv.FirstOrDefault(), out var tp)
                ? tp : 1;

            var json = await response.Content.ReadAsStringAsync(ct);
            var items = JsonSerializer.Deserialize<WpMediaItem[]>(json, JsonOptions);
            if (items is null) break;

            foreach (var item in items)
            {
                var pdfUrl = item.Guid?.Rendered;
                if (string.IsNullOrWhiteSpace(pdfUrl) || !pdfUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(
                    Uri.TryCreate(pdfUrl, UriKind.Absolute, out var uri) ? uri.LocalPath : pdfUrl);
                var (period, reportType) = OfficialSitePeriodParser.Parse(fileName);

                if (period is null && reportType != "annual")
                    continue;

                candidates.Add(new FundamentalsDiscoveryCandidate(
                    SourceName: SourceName,
                    SourceTitle: item.Title?.Rendered ?? fileName ?? pdfUrl,
                    PackageUrl: pdfUrl,
                    DownloadUrl: pdfUrl,
                    Period: period,
                    ReportType: reportType,
                    PublishedAt: item.DateGmt));
            }

            page++;
        } while (page <= totalPages);

        return candidates;
    }

    private sealed class WpMediaItem
    {
        [JsonPropertyName("guid")]
        public WpRendered? Guid { get; init; }

        [JsonPropertyName("title")]
        public WpRendered? Title { get; init; }

        [JsonPropertyName("date_gmt")]
        public DateTimeOffset? DateGmt { get; init; }
    }

    private sealed class WpRendered
    {
        [JsonPropertyName("rendered")]
        public string? Rendered { get; init; }
    }
}
