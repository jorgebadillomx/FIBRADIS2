using AngleSharp;
using Application.Fundamentals;
using Domain.Catalog;

namespace Infrastructure.Integrations.PdfDiscovery;

internal sealed record OfficialSiteConfig(
    string PdfLinkSelector,
    string BaseUrl,
    Func<string, bool>? LinkFilter = null);

public class OfficialSiteDiscoverySource(HttpClient http) : IFundamentalsDiscoverySource
{
    private static readonly IReadOnlyDictionary<string, OfficialSiteConfig> Catalog =
        new Dictionary<string, OfficialSiteConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["FIBRAMQ12"] = new(
                PdfLinkSelector: "a[href*=\".pdf\"]",
                BaseUrl: "https://www.fibramacquarie.com",
                LinkFilter: href => href.Contains("-spa.pdf", StringComparison.OrdinalIgnoreCase)
                                 || href.Contains("-earnings-release-spa", StringComparison.OrdinalIgnoreCase)),

            ["VESTA15"] = new(
                PdfLinkSelector: "a[href*=\"/storage/app/uploads/\"][href$=\".pdf\"]",
                BaseUrl: "https://vesta.com.mx"),

            ["FHIPO14"] = new(
                PdfLinkSelector: "a[href*=\"wp-content/uploads/\"][href$=\".pdf\"]",
                BaseUrl: "https://fhipo.com"),

            ["FCFE18"] = new(
                PdfLinkSelector: "a[href*=\"wp-content/uploads/\"][href$=\".pdf\"]",
                BaseUrl: "https://cfecapital.com.mx"),

            ["NEXT25"] = new(
                PdfLinkSelector: "a[href*=\"site_media/uploads/documentos/\"][href$=\".pdf\"]",
                BaseUrl: "https://fibranext.mx"),

            ["FUNO11"] = new(
                PdfLinkSelector: "a[href*=\"site_media/uploads/documentos/\"][href$=\".pdf\"]",
                BaseUrl: "https://funo.mx"),
        };

    public string SourceName => "official";

    public IReadOnlyList<string> SupportedTickers { get; } =
        [.. Catalog.Keys];

    public async Task<IReadOnlyList<FundamentalsDiscoveryCandidate>> DiscoverCandidatesAsync(Fibra fibra, CancellationToken ct)
    {
        if (!Catalog.TryGetValue(fibra.Ticker, out var config))
            return [];

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
            throw new InvalidOperationException($"No se pudo obtener la página de reportes para {fibra.Ticker}: {ex.Message}", ex);
        }

        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html).Address(discoveryUrl));

        var links = document.QuerySelectorAll(config.PdfLinkSelector)
            .Select(el => el.GetAttribute("href"))
            .Where(href => !string.IsNullOrWhiteSpace(href))
            .Select(href => ResolveUrl(href!, config.BaseUrl, discoveryUrl))
            .Where(url => url is not null)
            .Cast<string>()
            .Where(url => config.LinkFilter is null || config.LinkFilter(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var candidates = new List<FundamentalsDiscoveryCandidate>();
        foreach (var url in links)
        {
            var fileName = Path.GetFileNameWithoutExtension(Uri.TryCreate(url, UriKind.Absolute, out var u)
                ? u.LocalPath : url);
            var (period, reportType) = OfficialSitePeriodParser.Parse(fileName);

            candidates.Add(new FundamentalsDiscoveryCandidate(
                SourceName: $"official:{fibra.Ticker}",
                SourceTitle: fileName ?? url,
                PackageUrl: url,
                DownloadUrl: url,
                Period: period,
                ReportType: reportType,
                PublishedAt: null));
        }

        return candidates;
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

    private static string? ResolveUrl(string href, string baseUrl, string pageUrl)
    {
        if (Uri.TryCreate(href, UriKind.Absolute, out _))
            return href;

        if (Uri.TryCreate(new Uri(pageUrl), href, out var resolved))
            return resolved.ToString();

        if (href.StartsWith('/') && Uri.TryCreate(new Uri(baseUrl), href, out var rootRelative))
            return rootRelative.ToString();

        return null;
    }
}
