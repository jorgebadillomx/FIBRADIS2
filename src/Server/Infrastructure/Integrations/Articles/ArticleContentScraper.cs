using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Application.News;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.Articles;

public partial class ArticleContentScraper(
    HttpClient http,
    IGoogleNewsUrlDecoder googleNewsUrlDecoder,
    ILogger<ArticleContentScraper> logger) : IArticleContentScraper
{
    private const int MaxStoredChars = 16000;
    private const int MinUsefulLength = 200;
    private const int MinParagraphLength = 60;

    public async Task<string?> TryGetArticleTextAsync(string url, CancellationToken ct = default)
    {
        try
        {
            var effectiveUrl = await ResolveSourceUrlAsync(url, ct);

            if (!Uri.TryCreate(effectiveUrl, UriKind.Absolute, out var parsedUrl) || !await IsAllowedHostAsync(parsedUrl, ct))
            {
                logger.LogDebug("article scraping skipped for '{Url}': private or loopback host", effectiveUrl);
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, effectiveUrl);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var html = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var bodyText = ExtractBodyText(html);
            if (string.IsNullOrWhiteSpace(bodyText))
                return null;

            return bodyText.Length > MaxStoredChars ? bodyText[..MaxStoredChars] : bodyText;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "article text extraction failed for '{Url}'", url);
            return null;
        }
    }

    // internal for unit testing
    internal static string? ExtractBodyText(string html)
    {
        // Remove noise: scripts, styles, svgs, HTML comments
        html = ScriptRegex().Replace(html, " ");
        html = StyleRegex().Replace(html, " ");
        html = SvgRegex().Replace(html, " ");
        html = HtmlCommentRegex().Replace(html, " ");

        // Remove boilerplate structural elements before content extraction
        html = NavBlockRegex().Replace(html, " ");
        html = HeaderBlockRegex().Replace(html, " ");
        html = FooterBlockRegex().Replace(html, " ");
        html = AsideBlockRegex().Replace(html, " ");

        // Phase 1: semantic content containers (editorial priority order)
        var fragment = TryExtractSemanticBlock(html);

        // Phase 2: paragraph-based fallback when no semantic container found
        fragment ??= ExtractFromParagraphs(html);

        if (string.IsNullOrWhiteSpace(fragment))
            return null;

        var text = StripTagsAndNormalize(fragment);

        return PassesQualityGate(text) ? text : null;
    }

    private static string? TryExtractSemanticBlock(string html)
    {
        // <article> — most specific editorial container
        var match = ArticleBlockRegex().Match(html);
        if (match.Success) return match.Value;

        // itemprop="articleBody" — structured data marker
        match = ArticleBodyPropRegex().Match(html);
        if (match.Success) return match.Value;

        // <main> — document landmark for primary content
        match = MainBlockRegex().Match(html);
        if (match.Success) return match.Value;

        return null;
    }

    private static string? ExtractFromParagraphs(string html)
    {
        var useful = ParagraphContentRegex()
            .Matches(html)
            .Select(m => StripTagsAndNormalize(m.Groups[1].Value))
            .Where(p => p.Length >= MinParagraphLength)
            .ToList();

        return useful.Count >= 2 ? string.Join(" ", useful) : null;
    }

    private static string StripTagsAndNormalize(string html)
    {
        var text = TagRegex().Replace(html, " ");
        text = WebUtility.HtmlDecode(text);
        return WhitespaceRegex().Replace(text, " ").Trim();
    }

    private static bool PassesQualityGate(string text)
        => text.Length >= MinUsefulLength
            && !string.Equals(text.Trim(), "Google News", StringComparison.OrdinalIgnoreCase);

    private async Task<string> ResolveSourceUrlAsync(string url, CancellationToken ct)
    {
        if (!url.Contains("news.google.com", StringComparison.OrdinalIgnoreCase))
            return url;

        var decodedUrl = await googleNewsUrlDecoder.TryDecodeAsync(url, ct);
        return string.IsNullOrWhiteSpace(decodedUrl) ? url : decodedUrl;
    }

    private async Task<bool> IsAllowedHostAsync(Uri uri, CancellationToken ct)
    {
        try
        {
            IPAddress[] addresses;
            if (IPAddress.TryParse(uri.Host, out var literalIp))
                addresses = [literalIp];
            else
                addresses = await Dns.GetHostAddressesAsync(uri.Host, ct);

            return addresses.Length > 0 && addresses.All(IsAllowedIp);
        }
        catch (SocketException ex)
        {
            logger.LogDebug(ex, "article scraping skipped for '{Host}': DNS resolution failed", uri.Host);
            return false;
        }
    }

    private static bool IsAllowedIp(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        if (IPAddress.IsLoopback(ip)) return false;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return !(b[0] == 10
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 169 && b[1] == 254));
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return !(ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || IPAddress.IsLoopback(ip));

        return false;
    }

    [GeneratedRegex("""<script\b[^>]*>[\s\S]*?</script>""", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptRegex();

    [GeneratedRegex("""<style\b[^>]*>[\s\S]*?</style>""", RegexOptions.IgnoreCase)]
    private static partial Regex StyleRegex();

    [GeneratedRegex("""<svg\b[^>]*>[\s\S]*?</svg>""", RegexOptions.IgnoreCase)]
    private static partial Regex SvgRegex();

    [GeneratedRegex("""<!--[\s\S]*?-->""")]
    private static partial Regex HtmlCommentRegex();

    [GeneratedRegex("""<nav\b[^>]*>[\s\S]*?</nav>""", RegexOptions.IgnoreCase)]
    private static partial Regex NavBlockRegex();

    [GeneratedRegex("""<header\b[^>]*>[\s\S]*?</header>""", RegexOptions.IgnoreCase)]
    private static partial Regex HeaderBlockRegex();

    [GeneratedRegex("""<footer\b[^>]*>[\s\S]*?</footer>""", RegexOptions.IgnoreCase)]
    private static partial Regex FooterBlockRegex();

    [GeneratedRegex("""<aside\b[^>]*>[\s\S]*?</aside>""", RegexOptions.IgnoreCase)]
    private static partial Regex AsideBlockRegex();

    [GeneratedRegex("""<article\b[^>]*>[\s\S]*?</article>""", RegexOptions.IgnoreCase)]
    private static partial Regex ArticleBlockRegex();

    // Captures div/section/article/span with itemprop="articleBody"; backreference ensures correct closing tag
    [GeneratedRegex("""<(div|section|article|span)\b[^>]+itemprop\s*=\s*"articleBody"[^>]*>[\s\S]*?</\1>""", RegexOptions.IgnoreCase)]
    private static partial Regex ArticleBodyPropRegex();

    [GeneratedRegex("""<main\b[^>]*>[\s\S]*?</main>""", RegexOptions.IgnoreCase)]
    private static partial Regex MainBlockRegex();

    [GeneratedRegex("""<p\b[^>]*>([\s\S]*?)</p>""", RegexOptions.IgnoreCase)]
    private static partial Regex ParagraphContentRegex();

    [GeneratedRegex("""<[^>]+>""")]
    private static partial Regex TagRegex();

    [GeneratedRegex("""\s{2,}""")]
    private static partial Regex WhitespaceRegex();
}
