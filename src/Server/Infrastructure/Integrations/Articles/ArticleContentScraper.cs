using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
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

            var hostname = Uri.TryCreate(effectiveUrl, UriKind.Absolute, out var u) ? u.Host : null;
            var bodyText = await ExtractBodyTextAsync(html, hostname);
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
    internal static async Task<string?> ExtractBodyTextAsync(string html, string? hostname = null)
    {
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        RemoveBoilerplateNodes(document);

        // Phase 1: selectores del catálogo por sitio (mayor especificidad)
        if (hostname != null && SiteExtractionCatalog.TryGetSelectors(hostname, out var catalogSelectors))
        {
            foreach (var selector in catalogSelectors)
            {
                var el = document.QuerySelector(selector);
                if (el == null) continue;
                var text = NormalizeText(el.TextContent);
                if (PassesQualityGate(text)) return text;
            }
        }

        // Phase 2: contenedores semánticos HTML5
        var fragment = TryExtractSemantic(document);

        // Phase 3: selectores de clase CMS (WordPress, Drupal, editoriales en español)
        fragment ??= TryExtractByContentClass(document);

        // Phase 4: densidad de párrafos como último recurso
        fragment ??= ExtractFromParagraphs(document);

        if (!string.IsNullOrWhiteSpace(fragment) && PassesQualityGate(fragment))
            return fragment;

        // Phase 5: og:description / meta description (umbral mínimo = 50 chars)
        // Útil para SPAs, muros de pago parciales y portales con contenido en JS
        var metaText = ExtractMetaDescription(document);
        if (metaText != null)
            return metaText;

        return null;
    }

    private static void RemoveBoilerplateNodes(IDocument document)
    {
        // Scripts, estilos e iframes no aportan texto editorial
        document.QuerySelectorAll("script, style, noscript, iframe, svg")
            .ToList().ForEach(n => n.Remove());

        const string SemanticBoilerplate =
            "nav, header, footer, aside, " +
            "[role=navigation], [role=banner], [role=contentinfo], " +
            "[role=complementary], [role=search]";

        // Clases de contenido no editorial — exactas para evitar falsos positivos
        const string ClassBoilerplate =
            ".related, .related-articles, .related-posts, .related-content, .related-news, " +
            ".newsletter, .newsletter-cta, .newsletter-signup, " +
            ".subscribe, .subscription, .subscription-cta, " +
            ".cookie-banner, .cookie-notice, .cookie-consent, " +
            ".share-bar, .share-buttons, .social-share, .social-links, .social, " +
            ".sidebar, .widget, .advertisement, .ads, .ad-unit, " +
            ".promo, .promo-box, .tags, .tags-section, .tag-cloud, " +
            ".comments, .comments-section, " +
            "[data-component=related-articles], [data-component=newsletter]";

        var combined = SemanticBoilerplate + ", " + ClassBoilerplate;
        document.QuerySelectorAll(combined).ToList().ForEach(n => n.Remove());
    }

    private static string? TryExtractSemantic(IDocument document)
    {
        var el = document.QuerySelector("article")
            ?? document.QuerySelector("[itemprop=articleBody]")
            ?? document.QuerySelector("main");

        if (el == null) return null;
        var text = NormalizeText(el.TextContent);
        return PassesQualityGate(text) ? text : null;
    }

    private static string? TryExtractByContentClass(IDocument document)
    {
        var selectors = new[]
        {
            // WordPress / genéricos
            ".article-body", ".article-content", ".article__body", ".article__content",
            ".entry-content", ".entry__content", ".post-content", ".post__content",
            ".story-body", ".story__body", ".content-body",
            // Editoriales en español / latinoamérica
            ".nota-body", ".nota-cuerpo", ".nota-contenido", ".cuerpo-nota",
            ".articulo-cuerpo", ".editorial-content",
            // Drupal
            ".field-body", ".field--name-body",
            // CMS adicionales detectados en feeds de FIBRAs
            ".notaContainer__body", ".story__content",
            ".textonota", ".articulo",
        };

        foreach (var sel in selectors)
        {
            var el = document.QuerySelector(sel);
            if (el == null) continue;
            var text = NormalizeText(el.TextContent);
            if (PassesQualityGate(text)) return text;
        }
        return null;
    }

    private static string? ExtractFromParagraphs(IDocument document)
    {
        var useful = document.QuerySelectorAll("p")
            .Select(p => NormalizeText(p.TextContent))
            .Where(t => t.Length >= MinParagraphLength && !IsNavigationParagraph(t))
            .ToList();

        return useful.Count >= 2 ? string.Join(" ", useful) : null;
    }

    private static string? ExtractMetaDescription(IDocument document)
    {
        const int MinMetaLength = 50;
        string?[] selectors =
        [
            document.QuerySelector("meta[property='og:description']")?.GetAttribute("content"),
            document.QuerySelector("meta[name='description']")?.GetAttribute("content"),
            document.QuerySelector("meta[name='twitter:description']")?.GetAttribute("content"),
        ];
        foreach (var raw in selectors)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var text = NormalizeText(raw);
            if (text.Length >= MinMetaLength &&
                !string.Equals(text, "Google News", StringComparison.OrdinalIgnoreCase))
                return text;
        }
        return null;
    }

    private static bool IsNavigationParagraph(string text)
    {
        if (text.Count(c => c == '|') >= 2) return true;
        if (NavKeywordRegex().IsMatch(text)) return true;
        return false;
    }

    private static string NormalizeText(string text)
    {
        text = TagRegex().Replace(text, " ");
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

    // Detecta palabras de navegación/chrome en texto de párrafos
    [GeneratedRegex(
        """(?:^|\s)(?:buscar|search|suscr[íi]bete|subscribe|iniciar\s+sesi[oó]n|login|cerrar\s+sesi[oó]n|logout|s[íi]guenos|follow\s+us|compartir|share\s+this|ver\s+m[aá]s|leer\s+m[aá]s|read\s+more|ver\s+todos|see\s+all)(?:\s|[.,!?]|$)""",
        RegexOptions.IgnoreCase)]
    private static partial Regex NavKeywordRegex();

    [GeneratedRegex("""<[^>]+>""")]
    private static partial Regex TagRegex();

    [GeneratedRegex("""\s{2,}""")]
    private static partial Regex WhitespaceRegex();
}
