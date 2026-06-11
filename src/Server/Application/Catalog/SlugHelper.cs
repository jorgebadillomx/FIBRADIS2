using System.Globalization;
using System.Text;

namespace Application.Catalog;

/// <summary>
/// Normalización Unicode compartida para generadores de slugs.
/// Produce texto lowercase sin marcas combinantes, listo para aplicar
/// la estrategia de slugify específica de cada dominio.
/// </summary>
public static class SlugHelper
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
}
