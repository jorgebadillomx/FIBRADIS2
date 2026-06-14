using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Api.Middleware;

/// <summary>
/// Re-serializa un bloque JSON-LD proveniente de la BD (editable desde Ops) antes de inyectarlo
/// en el HTML del SPA. Compartido por los tres middlewares de metadata SEO para evitar divergencia.
///
/// Re-encodar con <see cref="JavaScriptEncoder"/> escapa <c>&lt;</c> como <c><</c>: impide que un
/// <c>&lt;/script&gt;</c> embebido en el contenido rompa el bloque (stored-XSS). Si el JSON-LD es
/// inválido (campo libre editado a mano), se omite el bloque en vez de propagar
/// <see cref="JsonException"/> — evita un 500 en la página por una sola fila mal formada.
/// </summary>
internal static class SeoJsonLd
{
    // Mismo encoder que usaban los builders inline: escapa control chars y < > & como \uXXXX,
    // deja pasar acentos/em-dash; el JSON-LD queda válido (RFC 8259) ante contenido scrapeado.
    private static readonly JsonSerializerOptions ReEncodeOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    /// <summary>
    /// Devuelve el bloque <c>&lt;script type="application/ld+json"&gt;…&lt;/script&gt;</c> con el
    /// JSON-LD re-serializado de forma segura, o cadena vacía si es null/whitespace/JSON inválido.
    /// </summary>
    public static string BuildScriptBlock(string? jsonLd)
    {
        if (string.IsNullOrWhiteSpace(jsonLd))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(jsonLd);
            var encoded = JsonSerializer.Serialize(doc.RootElement, ReEncodeOptions);
            return $"<script type=\"application/ld+json\">{encoded}</script>";
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}
