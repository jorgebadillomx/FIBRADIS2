using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Application.Fundamentals;

namespace Infrastructure.Integrations.PdfDiscovery;

public partial class AmefibraDiscoveryClient(HttpClient http) : IAmefibraDiscoveryClient
{
    private const string BaseUrl = "https://amefibra.com/reportes-de-fibras/";
    private bool _warmedUp;

    public async Task<IReadOnlyList<AmefibraListingItem>> GetListingItemsAsync(CancellationToken ct)
    {
        await WarmupAsync(ct);

        var firstPage = await GetHtmlAsync(BaseUrl, ct);
        var pageCount = await GetPageCountAsync(firstPage);
        var items = await ParseListingItemsAsync(firstPage);

        for (var page = 2; page <= pageCount; page++)
        {
            var html = await GetHtmlAsync($"{BaseUrl}?cp={page}", ct);
            items.AddRange(await ParseListingItemsAsync(html));
        }

        // El sitemap incluye paquetes que no aparecen en el listing paginado (ej. FIBRAs nuevas o reportes recientes)
        var sitemapItems = await GetSitemapItemsAsync(ct);
        var knownUrls = items.Select(x => x.PackageUrl).ToHashSet(StringComparer.OrdinalIgnoreCase);
        items.AddRange(sitemapItems.Where(x => !knownUrls.Contains(x.PackageUrl)));

        return items
            .GroupBy(x => x.PackageUrl, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    private async Task<List<AmefibraListingItem>> GetSitemapItemsAsync(CancellationToken ct)
    {
        const string SitemapUrl = "https://amefibra.com/wpdmpro-sitemap.xml";
        string xml;
        try
        {
            xml = await GetHtmlAsync(SitemapUrl, ct);
        }
        catch
        {
            return [];
        }

        var items = new List<AmefibraListingItem>();
        foreach (Match m in SitemapLocRegex().Matches(xml))
        {
            var url = m.Groups[1].Value.Trim();
            if (!url.Contains("/download/", StringComparison.OrdinalIgnoreCase))
                continue;

            var slug = url.TrimEnd('/').Split('/').Last();
            items.Add(new AmefibraListingItem(SlugToTitle(slug), url, null));
        }
        return items;
    }

    private static string SlugToTitle(string slug)
        => string.Join(" ", slug.Split('-').Select(w =>
            w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w));

    public async Task<AmefibraPackageDetails> GetPackageDetailsAsync(string packageUrl, CancellationToken ct)
    {
        var html = await GetHtmlAsync(packageUrl, ct);
        var document = await ToDocumentAsync(html);

        var downloadUrl = document.QuerySelector("a.wpdm-download-link")?.GetAttribute("data-downloadurl");
        var publishedRaw = document.QuerySelectorAll(".list-group-item")
            .FirstOrDefault(item => item.TextContent.Contains("Fecha de creación", StringComparison.OrdinalIgnoreCase))
            ?.QuerySelector(".badge")
            ?.TextContent
            ?.Trim();

        return new AmefibraPackageDetails(
            packageUrl,
            downloadUrl,
            AmefibraTitleParser.ParseSpanishDate(publishedRaw));
    }

    public async Task<(byte[] Content, string? PdfUrl, string? FileName)> DownloadPdfAsync(string packageUrl, string downloadUrl, CancellationToken ct)
    {
        using var request = CreateRequest(downloadUrl, packageUrl);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var finalUrl = response.RequestMessage?.RequestUri?.ToString();
        var fileName = AmefibraTitleParser.GetFileNameFromUrl(finalUrl);
        var content = await response.Content.ReadAsByteArrayAsync(ct);
        return (content, finalUrl ?? downloadUrl, fileName);
    }

    private async Task WarmupAsync(CancellationToken ct)
    {
        if (_warmedUp) return;
        _warmedUp = true;
        try
        {
            using var request = CreateRequest(BaseUrl, BaseUrl);
            request.Method = HttpMethod.Head;
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch
        {
            // Warmup is best-effort; proceed even if the portal rejects HEAD
        }
    }

    private async Task<string> GetHtmlAsync(string url, CancellationToken ct)
    {
        using var request = CreateRequest(url, BaseUrl);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static HttpRequestMessage CreateRequest(string url, string referer)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        request.Headers.TryAddWithoutValidation("Accept-Language", "es-MX,es;q=0.9,en;q=0.8");
        request.Headers.Referrer = new Uri(referer);
        return request;
    }

    private static async Task<IDocument> ToDocumentAsync(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(html));
    }

    private static async Task<int> GetPageCountAsync(string html)
    {
        var document = await ToDocumentAsync(html);
        var numbers = document.QuerySelectorAll(".pagination a, .pagination span, .page-numbers a, .page-numbers span")
            .Select(x => x.TextContent.Trim())
            .Where(x => int.TryParse(x, out _))
            .Select(int.Parse)
            .DefaultIfEmpty(1);

        return numbers.Max();
    }

    private static async Task<List<AmefibraListingItem>> ParseListingItemsAsync(string html)
    {
        var document = await ToDocumentAsync(html);
        return document.QuerySelectorAll(".link-template-default.card")
            .Select(card =>
            {
                var link = card.QuerySelector("h3.package-title a");
                var downloadLink = card.QuerySelector("a.wpdm-download-link");
                var title = link?.TextContent.Trim();
                var packageUrl = link?.GetAttribute("href");
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(packageUrl))
                    return null;

                return new AmefibraListingItem(
                    title,
                    packageUrl,
                    downloadLink?.GetAttribute("data-downloadurl"));
            })
            .Where(x => x is not null)
            .Cast<AmefibraListingItem>()
            .ToList();
    }

    [GeneratedRegex(@"<loc><!\[CDATA\[([^\]]+)\]\]></loc>")]
    private static partial Regex SitemapLocRegex();
}
