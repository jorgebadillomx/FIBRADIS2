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

        // P1: word-boundary check prevents "manual" matching "anual"
        if (AnnualKeywordRegex().IsMatch(lower))
            return (null, "annual");

        // P4: Spanish ordinal form — "1er-trimestre-2024", "2do-trimestre-2024", etc.
        var ordinal = OrdinalTrimestralRegex().Match(lower);
        if (ordinal.Success)
        {
            var quarter = int.Parse(ordinal.Groups["ord"].Value);
            var year = ParseYear(ordinal.Groups["oy"].Value);
            if (quarter >= 1 && quarter <= 4 && year >= 2018)
                return ($"Q{quarter}-{year}", "quarterly");
        }

        // P5: year/month in path — "/2025/02/" → Q1-2025
        var pathDate = PathDateRegex().Match(lower);
        if (pathDate.Success)
        {
            var year = int.Parse(pathDate.Groups["py"].Value);
            var month = int.Parse(pathDate.Groups["pm"].Value);
            var quarter = (month - 1) / 3 + 1;
            if (year is >= 2018 and <= 2035)
                return ($"Q{quarter}-{year}", "quarterly");
        }

        // Combined quarter+year pattern (handles "1q26", "Q1-2024", "4T25")
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
        // P3: validate year lower bound; P-dead: guard quarter is always 1-4 by regex
        if (quarter is < 1 or > 4 || year < 2018)
            return null;

        return $"Q{quarter}-{year}";
    }

    // P2: consistent [2018, 2035] range for both 2-digit and 4-digit years
    private static int ParseYear(string raw)
    {
        var y = int.Parse(raw);
        var full = raw.Length == 2 ? 2000 + y : y;
        return full is >= 2018 and <= 2035 ? full : 0;
    }

    // P1: word-boundary match prevents "manual" and "biannual" false positives
    [GeneratedRegex(@"\b(annual|anual)\b")]
    private static partial Regex AnnualKeywordRegex();

    // P4: "1er-trimestre-2024", "2do-trimestre-2025", "3er-trimestre-26", "4to-trimestre-2024"
    [GeneratedRegex(@"(?<ord>[1-4])(?:er|do|ro|to)-trimestre-(?<oy>20\d{2}|\d{2})")]
    private static partial Regex OrdinalTrimestralRegex();

    // P5: year/month in URL path like "/2025/02/" or "/2024/11/"
    [GeneratedRegex(@"/(?<py>20\d{2})/(?<pm>0[1-9]|1[0-2])/")]
    private static partial Regex PathDateRegex();

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
