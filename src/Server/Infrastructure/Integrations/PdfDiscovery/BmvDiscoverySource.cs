using AngleSharp;
using Application.Fundamentals;
using Domain.Catalog;

namespace Infrastructure.Integrations.PdfDiscovery;

public class BmvDiscoverySource(HttpClient http) : IFundamentalsDiscoverySource
{
    private const string BmvBase = "https://www.bmv.com.mx";

    public string SourceName => "bmv";
    public IReadOnlyList<string> SupportedTickers { get; } = ["HCITY17"];

    public async Task<IReadOnlyList<FundamentalsDiscoveryCandidate>> DiscoverCandidatesAsync(Fibra fibra, CancellationToken ct)
    {
        var discoveryUrl = fibra.ReportsUrl;
        if (string.IsNullOrWhiteSpace(discoveryUrl))
            return [];

        string html;
        try
        {
            html = await GetHtmlAsync(discoveryUrl, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"No se pudo obtener la página BMV para {fibra.Ticker}: {ex.Message}", ex);
        }

        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html).Address(discoveryUrl));

        var pdfLinks = document.QuerySelectorAll("a[href]")
            .Select(el => el.GetAttribute("href"))
            .Where(href => href is not null && href.Contains("indrpfn/indrpfn_", StringComparison.OrdinalIgnoreCase) && href.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .Select(href => href!.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? href : BmvBase + href)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var candidates = new List<FundamentalsDiscoveryCandidate>();
        foreach (var url in pdfLinks)
        {
            var period = ExtractBmvPeriod(url);
            var reportType = period is not null ? "quarterly" : "pending-classification";

            var fileName = Path.GetFileNameWithoutExtension(
                Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.LocalPath : url);

            candidates.Add(new FundamentalsDiscoveryCandidate(
                SourceName: $"bmv:{fibra.Ticker}",
                SourceTitle: fileName ?? url,
                PackageUrl: url,
                DownloadUrl: url,
                Period: period,
                ReportType: reportType,
                PublishedAt: null));
        }

        return candidates;
    }

    // Extracts period from BMV PDF URL pattern: indrpfn_{id}_{year}-{quarter:01-04}_1.pdf
    private static string? ExtractBmvPeriod(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var fileName = Path.GetFileNameWithoutExtension(uri.LocalPath);
        // Pattern: indrpfn_{id}_{year}-{quarter}_1
        var parts = fileName.Split('_');
        if (parts.Length < 3)
            return null;

        // The segment before "_1" at the end is "{year}-{quarter}"
        var segment = parts[^2]; // second to last segment
        return OfficialSitePeriodParser.ParseBmvSegment(segment);
    }

    private async Task<string> GetHtmlAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        request.Headers.TryAddWithoutValidation("Accept-Language", "es-MX,es;q=0.9,en;q=0.8");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
