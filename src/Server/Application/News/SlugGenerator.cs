using System.Text.RegularExpressions;
using Application.Catalog;

namespace Application.News;

/// <summary>
/// Genera slugs URL-safe a partir del título de un artículo de noticias
/// (ej. "FUNO11 reporta resultados del 2T25" → "funo11-reporta-resultados-del-2t25").
/// La unicidad NO se resuelve aquí — ver <c>INewsRepository.GenerateUniqueSlugAsync</c>.
///
/// DIVERGENCIA INTENCIONAL vs <see cref="Catalog.FibraSlug"/> / <see cref="Catalog.SlugHelper.SlugifyToHyphens"/>:
/// noticias usa la forma "espacio→guión, luego elimina el resto de no-alfanuméricos" (no
/// "colapsa runs a un guión"). Ej: "S.A." → "sa" aquí vs "s-a" en FibraSlug. Esto NO se unifica
/// a propósito: los slugs de noticias están <b>persistidos</b> en BD (columna Slug) y cambiarlos
/// rompería URLs ya indexadas y dispararía 301s. A diferencia de la fibra (slug derivado en C# y
/// TS), el slug de noticia se genera solo en backend y se busca por el valor almacenado, así que
/// no hay riesgo de loop de redirección entre lenguajes. Comparte <see cref="Catalog.SlugHelper.NormalizeText"/>
/// (la normalización Unicode) con el resto; solo difiere la estrategia de reemplazo.
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
