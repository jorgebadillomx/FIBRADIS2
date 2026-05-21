using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Application.News;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.GoogleNews;

public class GoogleNewsRssClient(HttpClient http, ILogger<GoogleNewsRssClient> logger) : IRssClient
{
    private const string BaseUrl = "https://news.google.com/rss/search";

    public async Task<IReadOnlyList<RssItem>> FetchAsync(string query, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}?q={Uri.EscapeDataString(query)}&hl=es-419&gl=MX&ceid=MX:es-419";

        try
        {
            var xml = await http.GetStringAsync(url, ct);
            var doc = XDocument.Parse(xml);

            return doc.Descendants("item")
                .Select(item =>
                {
                    var title = item.Element("title")?.Value ?? string.Empty;
                    var link = ExtractLink(item);
                    var snippet = StripHtml(item.Element("description")?.Value);
                    var source = item.Element("source")?.Value ?? string.Empty;
                    var pubDateStr = item.Element("pubDate")?.Value ?? string.Empty;
                    var publishedAt = ParsePublishedAt(pubDateStr, query, title);

                    return new RssItem(
                        Title: title,
                        Source: source,
                        PublishedAt: publishedAt == default ? DateTimeOffset.UtcNow : publishedAt,
                        Url: link,
                        Snippet: snippet);
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Url))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RSS fetch failed for query '{Query}'", query);
            return [];
        }
    }

    // Google News RSS puede emitir <link/> self-closing o <link href="..."/>.
    // Fallback: <guid isPermaLink="true"> contiene la URL real en esos casos.
    private static string ExtractLink(XElement item)
    {
        var linkElement = item.Element("link");

        // Caso normal: <link>https://...</link>
        var value = linkElement?.Value?.Trim();
        if (!string.IsNullOrEmpty(value)) return value;

        // Caso Atom-style: <link href="https://..."/>
        var hrefAttr = linkElement?.Attribute("href")?.Value?.Trim();
        if (!string.IsNullOrEmpty(hrefAttr)) return hrefAttr;

        // Fallback: <guid isPermaLink="true">https://...</guid>
        // Excluir URLs de news.google.com: son redirects internos de Google, no URLs reales del artículo.
        var guid = item.Element("guid");
        var isPermaLink = guid?.Attribute("isPermaLink")?.Value;
        if (isPermaLink is null or "true")
        {
            var guidValue = guid?.Value?.Trim();
            if (!string.IsNullOrEmpty(guidValue)
                && guidValue.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                && !guidValue.Contains("news.google.com", StringComparison.OrdinalIgnoreCase))
                return guidValue;
        }

        return string.Empty;
    }

    private static string? StripHtml(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var stripped = Regex.Replace(raw, "<[^>]+>", " ", RegexOptions.IgnoreCase);
        var decoded = WebUtility.HtmlDecode(stripped);
        var normalized = Regex.Replace(decoded, @"\s{2,}", " ").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private DateTimeOffset ParsePublishedAt(string pubDateStr, string query, string title)
    {
        if (DateTimeOffset.TryParse(pubDateStr, out var publishedAt))
            return publishedAt;

        if (string.IsNullOrWhiteSpace(pubDateStr))
        {
            logger.LogWarning(
                "RSS item missing pubDate for query '{Query}' and title '{Title}'",
                query,
                title);
        }
        else
        {
            logger.LogWarning(
                "RSS item with invalid pubDate '{PubDate}' for query '{Query}' and title '{Title}'",
                pubDateStr,
                query,
                title);
        }

        return DateTimeOffset.UtcNow;
    }
}
