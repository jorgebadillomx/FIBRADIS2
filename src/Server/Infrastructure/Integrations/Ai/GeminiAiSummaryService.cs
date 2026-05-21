using System.Net.Http.Json;
using System.Text.Json;
using Application.News;
using Domain.News;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.Ai;

public class GeminiAiSummaryService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GeminiAiSummaryService> logger) : IAiSummaryService
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private const string DefaultNewsModel = "gemini-2.5-flash";
    private const string DefaultDocumentModel = "gemini-2.5-pro";

    public async Task<string?> GenerateSummaryAsync(
        string title,
        string? snippet,
        AiContentType contentType = AiContentType.News,
        CancellationToken ct = default)
    {
        var apiKey = configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Gemini:ApiKey no está configurado. El resumen AI no está disponible.");
            return null;
        }

        var model = contentType switch
        {
            AiContentType.Document => configuration["Gemini:DocumentModel"] ?? DefaultDocumentModel,
            _ => configuration["Gemini:NewsModel"] ?? DefaultNewsModel,
        };

        var prompt = string.IsNullOrWhiteSpace(snippet)
            ? $"""
              Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces) con amplio conocimiento del mercado inmobiliario y bursátil de México.
              Redacta un resumen profesional en español de máximo 3 oraciones sobre esta noticia:
              Título: {title}
              Incluye: el hecho central, su relevancia para el sector de FIBRAs o bienes raíces en México, y una perspectiva analítica breve para el inversor. Responde solo con el resumen, sin preámbulos.
              """
            : $"""
              Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces) con amplio conocimiento del mercado inmobiliario y bursátil de México.
              Redacta un resumen profesional en español de máximo 3 oraciones sobre esta noticia:
              Título: {title}
              Fragmento: {snippet}
              Incluye: el hecho central, su relevancia para el sector de FIBRAs o bienes raíces en México, y una perspectiva analítica breve para el inversor. Responde solo con el resumen, sin preámbulos.
              """;

        var url = $"{BaseUrl}/{model}:generateContent?key={apiKey}";
        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } },
            },
            generationConfig = new { maxOutputTokens = 256 },
        };

        using var response = await SendRequestAsync(url, body, model, ct);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            logger.LogWarning("Gemini devolvió respuesta sin candidatos para modelo {Model}", model);
            throw new InvalidOperationException("Gemini devolvió una respuesta vacía sin candidatos.");
        }

        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content))
            throw new InvalidOperationException("Gemini devolvió una respuesta sin contenido generado.");

        if (!content.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
            throw new InvalidOperationException("Gemini devolvió una respuesta sin partes de contenido.");

        if (!parts[0].TryGetProperty("text", out var textElement))
            throw new InvalidOperationException("Gemini devolvió una respuesta sin texto en la primera parte.");

        var text = textElement.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Gemini devolvió una respuesta con texto vacío.");
        }

        return text.Trim();
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        string url,
        object body,
        string model,
        CancellationToken ct)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(url, body, ct);
            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al llamar a Gemini API con modelo {Model}", model);
            throw;
        }
    }
}
