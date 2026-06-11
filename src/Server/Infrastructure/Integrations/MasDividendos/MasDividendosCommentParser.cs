using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

namespace Infrastructure.Integrations.MasDividendos;

public static class MasDividendosCommentParser
{
    private static readonly Regex PercentagePattern = new(
        @"(?<pct>\d+(?:[.,]\d+)?)%\s*(?<label>[^;,\n]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex DollarLabelPattern = new(
        @"\$(?<amount>[\d.,]+)\s*(?<label>[^;,\n$]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex LabelAmountPattern = new(
        @"(?<label>[A-Za-zÁÉÍÓÚÜÑáéíóúüñ\s]{4,60}):\s*\$?\s*(?<amount>[\d.,]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly string[] UnclassifiedMarkers =
    [
        "CONCEPTO A CONFIRMAR",
        "CONCEPTO NO MENCIONADO",
        "PENDIENTE DE CONFIRMAR",
        "POR CONFIRMAR",
    ];

    public static (decimal? taxable, decimal? capital) Parse(string comentario, decimal totalAmount)
        => Parse(comentario, (decimal?)totalAmount);

    public static (decimal? taxable, decimal? capital) Parse(string? comentario, decimal? totalAmount)
    {
        if (string.IsNullOrWhiteSpace(comentario))
            return (null, null);

        var normalized = NormalizeComment(comentario);
        if (IsUnclassified(normalized))
            return (null, null);

        var totals = new DistributionBreakdown();

        if (TryParsePercentagePattern(normalized, totalAmount, totals))
            return totals.ToTupleOrNull();

        if (TryParseLabelAmountPattern(normalized, totals))
            return totals.ToTupleOrNull();

        if (TryParseDollarLabelPattern(normalized, totals))
            return totals.ToTupleOrNull();

        if (TryParseSimpleLabels(normalized, totalAmount, totals))
            return totals.ToTupleOrNull();

        return (null, null);
    }

    private static bool TryParsePercentagePattern(string text, decimal? totalAmount, DistributionBreakdown totals)
    {
        var matches = PercentagePattern.Matches(text);
        if (matches.Count == 0)
            return false;

        if (totalAmount is null)
            return false;

        foreach (Match match in matches)
        {
            var pct = ParseDecimal(match.Groups["pct"].Value);
            if (pct is null)
                continue;

            var label = match.Groups["label"].Value;
            ApplyAmount(totals, label, totalAmount.Value * (pct.Value / 100m));
        }

        return true;
    }

    private static bool TryParseDollarLabelPattern(string text, DistributionBreakdown totals)
    {
        var matches = DollarLabelPattern.Matches(text);
        if (matches.Count == 0)
            return false;

        foreach (Match match in matches)
        {
            var amount = ParseDecimal(match.Groups["amount"].Value);
            if (amount is null)
                continue;

            ApplyAmount(totals, match.Groups["label"].Value, amount.Value);
        }

        return true;
    }

    private static bool TryParseLabelAmountPattern(string text, DistributionBreakdown totals)
    {
        var matches = LabelAmountPattern.Matches(text);
        if (matches.Count == 0)
            return false;

        foreach (Match match in matches)
        {
            var amount = ParseDecimal(match.Groups["amount"].Value);
            if (amount is null)
                continue;

            ApplyAmount(totals, match.Groups["label"].Value, amount.Value);
        }

        return true;
    }

    private static bool TryParseSimpleLabels(string text, decimal? totalAmount, DistributionBreakdown totals)
    {
        if (totalAmount is null)
            return false;

        var segments = text
            .Split([';', ',', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeComment)
            .Where(segment => segment.Length > 0)
            .ToList();

        if (segments.Count == 0)
            return false;

        var any = false;
        foreach (var segment in segments)
        {
            var category = Classify(segment);
            if (category is null)
                continue;

            ApplyAmount(totals, segment, totalAmount.Value);
            any = true;
        }

        return any;
    }

    private static void ApplyAmount(DistributionBreakdown totals, string label, decimal amount)
    {
        var category = Classify(label);
        if (category is null)
            return;

        if (category == DistributionCategory.Taxable)
            totals.Taxable = totals.Taxable.GetValueOrDefault() + amount;
        else
            totals.Capital = totals.Capital.GetValueOrDefault() + amount;
    }

    private static DistributionCategory? Classify(string label)
    {
        var normalized = NormalizeComment(label);

        if (normalized.Contains("CUFIN", StringComparison.OrdinalIgnoreCase))
            return DistributionCategory.Taxable;

        if (normalized.Contains("RESULTADO FISCAL", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("CUENTA DE UTILIDAD FISCAL NETA", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("DISTRIBUCION DE INTERESES", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("UTILIDADES", StringComparison.OrdinalIgnoreCase))
        {
            return DistributionCategory.Taxable;
        }

        if (normalized.Contains("REEMBOLSO DE CAPITAL", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("RETORNO DE CAPITAL", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("CUCA", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("CUENTA DE CAPITAL DE APORTACION", StringComparison.OrdinalIgnoreCase))
        {
            return DistributionCategory.Capital;
        }

        return null;
    }

    private static decimal? ParseDecimal(string value)
    {
        var normalized = value.Trim();
        var commaCount = normalized.Count(c => c == ',');
        if (commaCount == 1 && !normalized.Contains('.'))
            normalized = normalized.Replace(',', '.');
        else
            normalized = normalized.Replace(",", string.Empty);

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static string NormalizeComment(string value)
    {
        var normalized = value
            .Replace("Ã³", "ó", StringComparison.Ordinal)
            .Replace("Ã¡", "á", StringComparison.Ordinal)
            .Replace("Ã©", "é", StringComparison.Ordinal)
            .Replace("Ã­", "í", StringComparison.Ordinal)
            .Replace("Ãº", "ú", StringComparison.Ordinal)
            .Replace("Ã±", "ñ", StringComparison.Ordinal)
            .Replace("Ã¼", "ü", StringComparison.Ordinal)
            .Trim();

        var decomposed = normalized.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool IsUnclassified(string value)
        => UnclassifiedMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private enum DistributionCategory
    {
        Taxable,
        Capital,
    }

    private sealed class DistributionBreakdown
    {
        public decimal? Taxable { get; set; }
        public decimal? Capital { get; set; }

        public (decimal? taxable, decimal? capital) ToTupleOrNull()
            => (Taxable, Capital) is (null, null) ? (null, null) : (Taxable, Capital);
    }
}
