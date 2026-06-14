using AngleSharp;
using Application.Fundamentals;
using Domain.Catalog;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Infrastructure.Integrations.PdfDiscovery;

public partial class EconomaticaDiscoverySource(
    HttpClient http,
    ILogger<EconomaticaDiscoverySource>? logger = null) : IFundamentalsDiscoverySource
{
    // HTTP only — SSL cert is expired on www.economatica.mx
    private const string BaseUrl = "http://www.economatica.mx";

    public string SourceName => "economatica";

    // Economatica indexes most FIBRAs by a short ticker code. We don't gate by a
    // whitelist — the source is universal and tries several ticker forms per fibra,
    // returning empty (without throwing) when none resolve to a report page.
    public IReadOnlyList<string> SupportedTickers { get; } = [];

    public async Task<IReadOnlyList<FundamentalsDiscoveryCandidate>> DiscoverCandidatesAsync(
        Fibra fibra, CancellationToken ct)
    {
        // Economatica uses a short ticker code that is usually the BMV ticker without
        // its 2-digit series suffix (e.g. "FHIPO14" → "FHIPO", "FVIA16" → "FVIA").
        // We try several forms in order and keep the first that yields PDFs. If a form
        // 404s or the page has no reports, we move on; if none resolve, return empty
        // (no throw) so fibras absent from Economatica don't pollute the error log.
        foreach (var econTicker in BuildTickerForms(fibra))
        {
            ct.ThrowIfCancellationRequested();
            var candidates = await TryFormAsync(fibra, econTicker, ct);
            if (candidates.Count > 0)
                return candidates;
        }

        return [];
    }

    private async Task<List<FundamentalsDiscoveryCandidate>> TryFormAsync(
        Fibra fibra, string econTicker, CancellationToken ct)
    {
        var pageUrl = $"{BaseUrl}/{econTicker}/REPORTES%20TRIMESTRALES/";

        string html;
        try
        {
            html = await GetHtmlAsync(pageUrl, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Genuine caller cancellation → propagate; do not swallow.
            throw;
        }
        catch (Exception ex)
        {
            // 404, network failure, or HttpClient timeout (TaskCanceledException without
            // caller cancellation) → this form didn't resolve; log and try the next form.
            // Debug level keeps absent fibras out of the PipelineErrorLog while preserving
            // observability for genuine outages.
            logger?.LogDebug(ex, "Economatica form '{EconTicker}' did not resolve for {Ticker}", econTicker, fibra.Ticker);
            return [];
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

    // Ordered, de-duplicated ticker forms to probe on Economatica. Order matters:
    // the 2-digit-suffix strip is the proven primary; the rest are fallbacks.
    private static IEnumerable<string> BuildTickerForms(Fibra fibra)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ticker = fibra.Ticker?.Trim() ?? string.Empty;

        var forms = new List<string>();
        if (ticker.Length > 2)
            forms.Add(ticker[..^2]);                       // "FVIA16" → "FVIA"
        forms.Add(ticker.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9')); // strip all trailing digits
        forms.Add(ticker);                                 // full ticker as-is
        forms.AddRange((fibra.NameVariants ?? []).Select(NormalizeTickerForm)); // name variants as ticker-like codes

        foreach (var form in forms)
        {
            if (!string.IsNullOrWhiteSpace(form) && seen.Add(form))
                yield return form;
        }
    }

    // Reduce a name variant to an uppercase ASCII-alphanumeric code (Economatica paths are
    // codes, not long names). Accents are stripped: "Fibra Vía" → "FIBRAVIA". Only useful
    // when a variant happens to be a code; long names won't resolve and are skipped on 404.
    private static string NormalizeTickerForm(string value)
        => string.Concat((value ?? string.Empty)
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch =>
                System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) !=
                System.Globalization.UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(ch)))
            .ToUpperInvariant();

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
