using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Catalog;

/// <summary>
/// Normalización Unicode compartida para generadores de slugs.
/// Produce texto lowercase sin marcas combinantes, listo para aplicar
/// la estrategia de slugify específica de cada dominio.
/// </summary>
public static partial class SlugHelper
{
    /// <summary>
    /// Normaliza el texto para slugificación: NFD → strip NonSpacingMark → NFC → lowercase.
    /// El resultado contiene letras base sin acentos y está en minúsculas, pero conserva
    /// espacios y puntuación — cada generador aplica su propia estrategia de reemplazo.
    /// </summary>
    public static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>
    /// Slug canónico estilo "kebab": <see cref="NormalizeText"/> y luego colapsa cada run de
    /// caracteres no-alfanuméricos a UN guión, recortando los guiones de los extremos.
    /// Es la semántica de slug de FIBRA y DEBE coincidir 1:1 con <c>buildFibraSlug</c> del
    /// frontend (<c>src/Web/Main/src/shared/lib/fibra-slug.ts</c>, regex <c>[^a-z0-9]+ → '-'</c>)
    /// — si divergen, el 301 del middleware y la canonicalización client-side entran en loop.
    /// El corpus de paridad vive en <c>slug-parity.fixture.json</c> y lo verifican ambos lenguajes.
    /// NOTA: <c>SlugGenerator</c> (noticias) NO usa esta estrategia a propósito (ver su doc).
    /// </summary>
    public static string SlugifyToHyphens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return NonAlphanumericRuns().Replace(NormalizeText(text), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRuns();
}
