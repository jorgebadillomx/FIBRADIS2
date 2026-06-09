using AngleSharp;
using Application.Fundamentals;
using Domain.Catalog;
using System.Text.RegularExpressions;

namespace Infrastructure.Integrations.PdfDiscovery;

public partial class EconomaticaDiscoverySource(HttpClient http) : IFundamentalsDiscoverySource
{
    // HTTP only — SSL cert is expired on www.economatica.mx
    private const string BaseUrl = "http://www.economatica.mx";

    public string SourceName => "economatica";

    // Economatica uses the base ticker without the 2-digit series suffix.
    // NEXT25 has no data on Economatica.
    public IReadOnlyList<string> SupportedTickers { get; } = [
        "FUNO11", "DANHOS13", "TERRA13", "FIBRAMQ12", "FMTY14", "FINN13", "FIHO12",
        "VESTA15", "HCITY17", "EDUCA18", "FIBRAPL14", "FIBRAUP18", "FNOVA17", "FPLUS16",
        "FSHOP13", "SOMA21", "STORAGE18", "FHIPO14", "FCFE18"
    ];

    public async Task<IReadOnlyList<FundamentalsDiscoveryCandidate>> DiscoverCandidatesAsync(
        Fibra fibra, CancellationToken ct)
    {
        // Strip 2-digit series suffix: "FHIPO14" → "FHIPO"
        var econTicker = fibra.Ticker[..^2];
        var pageUrl = $"{BaseUrl}/{econTicker}/REPORTES%20TRIMESTRALES/";

        string html;
        try
        {
            html = await GetHtmlAsync(pageUrl, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"No se pudo obtener la página de Economatica para {fibra.Ticker}: {ex.Message}", ex);
        }

        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html).Address(pageUrl));

        var candidates = new List<FundamentalsDiscoveryCandidate>();

        foreach (var el in document.QuerySelectorAll("a.ico_pdf[href]"))
        {
            var href = el.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href) || !href.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                continue;

            var downloadUrl = $"{BaseUrl}/{econTicker}/REPORTES%20TRIMESTRALES/{href}";
            var fileName = Path.GetFileNameWithoutExtension(href);
            var period = ParseEconomaticaPeriod(fileName);

            candidates.Add(new FundamentalsDiscoveryCandidate(
                SourceName: $"economatica:{fibra.Ticker}",
                SourceTitle: fileName ?? href,
                PackageUrl: downloadUrl,
                DownloadUrl: downloadUrl,
                Period: period,
                ReportType: period is not null ? "quarterly" : "pending-classification",
                PublishedAt: null));
        }

        return candidates;
    }

    // Parses Economatica filename format: {TICKER}_RT_{YEAR}_{Q}T--{SUFFIX}
    // e.g. "FHIPO_RT_2025_4T--DIS" → "Q4-2025", "FUNO_RT_2014_1T--BYH" → "Q1-2014"
    public static string? ParseEconomaticaPeriod(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        var match = EconomaticaFilenameRegex().Match(fileName);
        if (!match.Success) return null;
        var year = int.Parse(match.Groups["year"].Value);
        var q = int.Parse(match.Groups["q"].Value);
        if (year is < 2010 or > 2040) return null;
        return $"Q{q}-{year}";
    }

    private async Task<string> GetHtmlAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        request.Headers.TryAddWithoutValidation("Accept-Language", "es-MX,es;q=0.9,en;q=0.8");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    [GeneratedRegex(@"_RT_(?<year>\d{4})_(?<q>[1-4])T", RegexOptions.IgnoreCase)]
    private static partial Regex EconomaticaFilenameRegex();
}
