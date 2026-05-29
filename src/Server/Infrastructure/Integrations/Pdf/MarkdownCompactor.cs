using System.Text.RegularExpressions;

namespace Infrastructure.Integrations.Pdf;

public static partial class MarkdownCompactor
{
    [GeneratedRegex(@"(\w+)-\r?\n(\w+)", RegexOptions.Multiline)]
    private static partial Regex HyphenBreakRegex();

    [GeneratedRegex(@"^[ \t]*\d{1,4}[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex PageNumberLineRegex();

    [GeneratedRegex(@"^[ \t]*[-=*_]{3,}[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex DecorativeSeparatorRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MultiSpaceRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultiBlankLineRegex();

    public static string Compact(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // 1. Join OCR hyphenated line breaks: "estruc-\ntura" → "estructura"
        text = HyphenBreakRegex().Replace(text, "$1$2");

        // 2. Remove page number lines (lines that are only 1-4 digits)
        text = PageNumberLineRegex().Replace(text, string.Empty);

        // 3. Remove decorative separators (lines of only ---, ===, ***, ___)
        text = DecorativeSeparatorRegex().Replace(text, string.Empty);

        // 4. Frequency-based dedup: remove lines appearing ≥3 times that contain no digits
        text = RemoveFrequentNonNumericLines(text, threshold: 3);

        // 5. Collapse multiple consecutive spaces/tabs to a single space
        text = MultiSpaceRegex().Replace(text, " ");

        // 6. Trim each line
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            lines[i] = lines[i].Trim();
        text = string.Join('\n', lines);

        // 7. Collapse 3+ consecutive blank lines to 2
        text = MultiBlankLineRegex().Replace(text, "\n\n");

        return text.Trim();
    }

    private static string RemoveFrequentNonNumericLines(string text, int threshold)
    {
        var lines = text.Split('\n');

        var frequency = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var key = line.Trim();
            if (key.Length == 0) continue;
            frequency.TryGetValue(key, out var count);
            frequency[key] = count + 1;
        }

        var toRemove = new HashSet<string>(
            frequency
                .Where(kv => kv.Value >= threshold && !ContainsDigit(kv.Key))
                .Select(kv => kv.Key),
            StringComparer.Ordinal);

        if (toRemove.Count == 0)
            return text;

        var result = new System.Text.StringBuilder(text.Length);
        foreach (var line in lines)
        {
            if (!toRemove.Contains(line.Trim()))
            {
                result.Append(line);
                result.Append('\n');
            }
        }

        return result.ToString();
    }

    private static bool ContainsDigit(string s)
    {
        foreach (var c in s)
            if (char.IsDigit(c)) return true;
        return false;
    }
}
