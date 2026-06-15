namespace Application.Catalog;

/// <summary>
/// Construye el slug canónico de una ficha de FIBRA: <c>slugify(FullName)-ticker</c>
/// (ej. "Fibra Uno" + "FUNO11" → "fibra-uno-funo11"). El ticker SIEMPRE va al final
/// como último segmento — permite resolver la fibra extrayendo el texto después del
/// último guión, sin columna Slug en BD ni migración.
///
/// La estrategia de slugify vive en <see cref="SlugHelper.SlugifyToHyphens"/> (fuente única
/// compartida); esta clase solo compone <c>{slug}-{ticker}</c>. La paridad con el frontend
/// (<c>fibra-slug.ts</c>) se verifica con el corpus <c>slug-parity.fixture.json</c>.
/// </summary>
public static class FibraSlug
{
    public static string Build(string fullName, string ticker)
    {
        var namePart = SlugHelper.SlugifyToHyphens(fullName);
        var tickerPart = ticker.ToLowerInvariant();
        return string.IsNullOrEmpty(namePart) ? tickerPart : $"{namePart}-{tickerPart}";
    }
}
