using System.Text.RegularExpressions;
using Application.Catalog;

namespace Application.News;

/// <summary>
/// Genera slugs URL-safe a partir del título de un artículo de noticias
/// (ej. "FUNO11 reporta resultados del 2T25" → "funo11-reporta-resultados-del-2t25").
/// La unicidad NO se resuelve aquí — ver <c>INewsRepository.GenerateUniqueSlugAsync</c>.
/// </summary>
public static partial class SlugGenerator
{
    public static string Generate(string text, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(text)) return "noticia";

        var clean = SlugHelper.NormalizeText(text);

        clean = clean.Replace(' ', '-');
        clean = InvalidChars().Replace(clean, "");
        clean = MultiHyphen().Replace(clean, "-").Trim('-');

        if (clean.Length > maxLength)
        {
            clean = clean[..maxLength].TrimEnd('-');
        }

        return string.IsNullOrEmpty(clean) ? "noticia" : clean;
    }

    [GeneratedRegex("[^a-z0-9-]")]
    private static partial Regex InvalidChars();

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultiHyphen();
}
