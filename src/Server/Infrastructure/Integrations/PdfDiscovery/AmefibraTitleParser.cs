using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Application.Fundamentals;

namespace Infrastructure.Integrations.PdfDiscovery;

public static partial class AmefibraTitleParser
{
    public static AmefibraTitleParseResult Parse(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return new AmefibraTitleParseResult(null, null, "unknown", "pending-classification", "Título vacío.");

        var normalized = Normalize(title);

        if (normalized.Contains("reporte anual", StringComparison.Ordinal) || normalized.Contains("annual", StringComparison.Ordinal))
        {
            return new AmefibraTitleParseResult(
                ExtractFibraHint(title, normalized),
                null,
                "annual",
                "annual",
                null);
        }

        var quarter = TryGetQuarter(normalized);
        var year = TryGetYear(normalized);
        if (quarter is null || year is null)
        {
            return new AmefibraTitleParseResult(
                ExtractFibraHint(title, normalized),
                null,
                "unknown",
                "pending-classification",
                "No se pudo identificar un trimestre válido en el título.");
        }

        return new AmefibraTitleParseResult(
            ExtractFibraHint(title, normalized),
            $"Q{quarter}-{year}",
            "quarterly",
            "eligible",
            null);
    }

    public static DateTimeOffset? ParseSpanishDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (!DateTime.TryParse(raw, CultureInfo.GetCultureInfo("es-MX"), DateTimeStyles.AssumeUniversal, out var parsed))
            return null;

        return new DateTimeOffset(DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc));
    }

    public static string NormalizeDownloadSignature(string? downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl) || !Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri))
            return string.Empty;

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var stableQuery = query["wpdmdl"];
        return stableQuery is null
            ? uri.GetLeftPart(UriPartial.Path)
            : $"{uri.GetLeftPart(UriPartial.Path)}?wpdmdl={stableQuery}";
    }

    public static string? GetFileNameFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var fileName = Path.GetFileName(uri.LocalPath);
        return string.IsNullOrWhiteSpace(fileName) ? null : fileName;
    }

    private static string Normalize(string value)
    {
        var formD = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(ch));
        }

        return WhitespaceRegex().Replace(builder.ToString().Normalize(NormalizationForm.FormC), " ").Trim();
    }

    private static int? TryGetQuarter(string normalized)
    {
        var match = QuarterRegex().Match(normalized);
        if (!match.Success)
            return null;

        foreach (var groupName in new[] { "q1", "q2", "q3", "q4" })
        {
            if (int.TryParse(match.Groups[groupName].Value, out var quarter))
                return quarter;
        }

        return null;
    }

    private static int? TryGetYear(string normalized)
    {
        var fullYear = YearRegex().Match(normalized);
        if (fullYear.Success && int.TryParse(fullYear.Value, out var year))
            return year;

        var shortYear = ShortYearRegex().Match(normalized);
        if (shortYear.Success && int.TryParse(shortYear.Groups["yy"].Value, out var shortValue))
        {
            var candidate = 2000 + shortValue;
            return candidate is >= 2018 and <= 2035 ? candidate : null;
        }

        return null;
    }

    private static string? ExtractFibraHint(string originalTitle, string normalized)
    {
        var cleaned = normalized;
        cleaned = YearRegex().Replace(cleaned, " ");
        cleaned = ShortYearRegex().Replace(cleaned, " ");
        cleaned = QuarterRegex().Replace(cleaned, " ");
        cleaned = cleaned.Replace("reporte", " ", StringComparison.Ordinal);
        cleaned = cleaned.Replace("trimestral", " ", StringComparison.Ordinal);
        cleaned = cleaned.Replace("anual", " ", StringComparison.Ordinal);
        cleaned = cleaned.Replace("bolsa de valores", " ", StringComparison.Ordinal);
        cleaned = cleaned.Replace("bmv", " ", StringComparison.Ordinal);
        cleaned = WhitespaceRegex().Replace(cleaned, " ").Trim(' ', '-', '_', '/', '(', ')');

        if (string.IsNullOrWhiteSpace(cleaned))
            return null;

        return cleaned;
    }

    [GeneratedRegex(@"\b(?:(?:q(?<q1>[1-4]))|(?:(?<q2>[1-4])q)|(?:t(?<q3>[1-4]))|(?:(?<q4>[1-4])t))\b")]
    private static partial Regex QuarterRegex();

    [GeneratedRegex(@"\b20\d{2}\b")]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"(?<!\d)(?<yy>\d{2})(?!\d)")]
    private static partial Regex ShortYearRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
