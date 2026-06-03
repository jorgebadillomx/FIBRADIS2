using System.Text.RegularExpressions;

namespace Application.Fundamentals;

public static partial class OfficialSitePeriodParser
{
    // Parses a filename or URL segment into a normalized period like "Q1-2024".
    // Returns (Period, ReportType). ReportType is "pending-classification" when period cannot be inferred.
    public static (string? Period, string ReportType) Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, "pending-classification");

        var lower = text.ToLowerInvariant();

        if (lower.Contains("annual") || lower.Contains("anual"))
            return (null, "annual");

        // Try combined quarter+year pattern first (handles embedded filenames like "1q26", "Q1-2024")
        var combined = CombinedPeriodRegex().Match(lower);
        if (combined.Success)
        {
            int quarter;
            int year;

            if (combined.Groups["q1"].Success)
            {
                quarter = int.Parse(combined.Groups["q1"].Value);
                year = ParseYear(combined.Groups["y1"].Value);
            }
            else
            {
                quarter = int.Parse(combined.Groups["q2"].Value);
                year = ParseYear(combined.Groups["y2"].Value);
            }

            if (quarter >= 1 && quarter <= 4 && year >= 2018)
                return ($"Q{quarter}-{year}", "quarterly");
        }

        return (null, "pending-classification");
    }

    // Converts BMV's "{year}-{quarter}" segment to "Q{q}-{year}".
    // e.g. "2026-01" → "Q1-2026", "2025-04" → "Q4-2025"
    public static string? ParseBmvSegment(string? segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            return null;

        var match = BmvSegmentRegex().Match(segment);
        if (!match.Success)
            return null;

        var year = int.Parse(match.Groups["year"].Value);
        var quarter = int.Parse(match.Groups["quarter"].Value);
        if (quarter is < 1 or > 4)
            return null;

        return $"Q{quarter}-{year}";
    }

    private static int ParseYear(string raw)
    {
        if (raw.Length == 4)
            return int.Parse(raw);
        var y = int.Parse(raw); // 2-digit
        var candidate = 2000 + y;
        return candidate is >= 2018 and <= 2035 ? candidate : 0;
    }

    // Matches embedded quarter+year patterns in filenames:
    // Group q1+y1: "{q}[qt]{year}" — e.g. 1q26, 4T25, 3Q2025
    // Group q2+y2: "[qt]{q}-?{year}" — e.g. Q1-2024, T4-25, q1-26
    [GeneratedRegex(
        @"(?:(?<q1>[1-4])[qt](?<y1>20\d{2}|\d{2}))|(?:[qt](?<q2>[1-4])-?(?<y2>20\d{2}|\d{2}))",
        RegexOptions.IgnoreCase)]
    private static partial Regex CombinedPeriodRegex();

    [GeneratedRegex(@"(?<year>20\d{2})-0?(?<quarter>[1-4])")]
    private static partial Regex BmvSegmentRegex();
}
