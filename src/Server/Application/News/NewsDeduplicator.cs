using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.News;

public static partial class NewsDeduplicator
{
    public static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var withoutDiacritics = RemoveDiacritics(title).ToLowerInvariant();
        var withoutPunctuation = NonWordCharactersRegex().Replace(withoutDiacritics, " ");
        return WhitespaceRegex().Replace(withoutPunctuation, " ").Trim();
    }

    public static bool MatchesBlocklist(string title, string? snippet, IReadOnlyList<string> terms)
    {
        if (terms.Count == 0)
            return false;

        var normalizedTitle = NormalizeTitle(title);
        var normalizedSnippet = NormalizeTitle(snippet ?? string.Empty);

        foreach (var term in terms)
        {
            var normalizedTerm = NormalizeTitle(term);
            if (string.IsNullOrEmpty(normalizedTerm))
                continue;

            if (normalizedTitle.Contains(normalizedTerm, StringComparison.Ordinal)
                || normalizedSnippet.Contains(normalizedTerm, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<RssItem> Filter(
        IReadOnlyList<RssItem> items,
        IReadOnlySet<string> existingUrls,
        IReadOnlyList<string> recentTitles,
        IReadOnlyList<string> blocklistTerms)
        => FilterSeparatingDuplicates(items, existingUrls, recentTitles, blocklistTerms).Fresh;

    public static (IReadOnlyList<RssItem> Fresh, IReadOnlyList<RssItem> TitleDuplicates) FilterSeparatingDuplicates(
        IReadOnlyList<RssItem> items,
        IReadOnlySet<string> existingUrls,
        IReadOnlyList<string> recentTitles,
        IReadOnlyList<string> blocklistTerms)
    {
        var fresh = new List<RssItem>();
        var titleDuplicates = new List<RssItem>();
        var seenUrlsInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recentTitleSet = new HashSet<string>(recentTitles, StringComparer.Ordinal);
        var seenTitlesInBatch = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in items)
        {
            if (MatchesBlocklist(item.Title, item.Snippet, blocklistTerms))
                continue;

            // URL-duplicate: skip entirely (no storage value)
            if (existingUrls.Contains(item.Url) || !seenUrlsInBatch.Add(item.Url))
                continue;

            var normalizedTitle = NormalizeTitle(item.Title);
            if (!string.IsNullOrEmpty(normalizedTitle)
                && (recentTitleSet.Contains(normalizedTitle) || !seenTitlesInBatch.Add(normalizedTitle)))
            {
                // Title-duplicate with a new URL: save as deleted to preserve the record
                titleDuplicates.Add(item);
                continue;
            }

            fresh.Add(item);
        }

        return (fresh, titleDuplicates);
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[^\w\s]", RegexOptions.CultureInvariant)]
    private static partial Regex NonWordCharactersRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
