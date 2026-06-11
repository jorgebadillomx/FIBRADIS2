using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Catalog;

/// <summary>
/// Construye el slug canónico de una ficha de FIBRA: <c>slugify(FullName)-ticker</c>
/// (ej. "Fibra Uno" + "FUNO11" → "fibra-uno-funo11"). El ticker SIEMPRE va al final
/// como último segmento — permite resolver la fibra extrayendo el texto después del
/// último guión, sin columna Slug en BD ni migración.
/// </summary>
public static partial class FibraSlug
{
    public static string Build(string fullName, string ticker)
    {
        var namePart = Slugify(fullName);
        var tickerPart = ticker.ToLowerInvariant();
        return string.IsNullOrEmpty(namePart) ? tickerPart : $"{namePart}-{tickerPart}";
    }

    // DEBE producir exactamente el mismo resultado que buildFibraSlug en
    // src/Web/Main/src/shared/lib/fibra-slug.ts — si divergen, el 301 del
    // middleware y la canonicalización client-side entran en loop de redirecciones.
    // Por eso los runs de no-alfanuméricos colapsan a UN guión (semántica del
    // regex TS [^a-z0-9]+), en vez de espacios→guión + strip del resto.
    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(ch);
        }

        var clean = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        return NonAlphanumericRuns().Replace(clean, "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRuns();
}
