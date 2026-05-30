using System.Linq;
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
    IAiProviderConfigRepository providerRepo,
    IAiPromptRepository promptRepo,
    ILogger<GeminiAiSummaryService> logger) : IAiSummaryService
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private const string DefaultDocumentModel = "gemini-2.5-pro";
    private const int NewsMaxOutputTokens = 2500;
    private const int RetryNewsMaxOutputTokens = 2500;
    private const int DocumentMaxOutputTokens = 768;
    private const int RetryDocumentMaxOutputTokens = 1024;
    private const int MinimumShortSummaryLength = 180;
    private const int MinimumLongSummaryLength = 320;
    private const int LongBodyThreshold = 2000;
    private const int MinimumShortSummarySentenceCount = 2;
    private const int MinimumLongSummarySentenceCount = 4;
    private const int MaxPromptBodyChars = 12000;

    public async Task<string?> GenerateSummaryAsync(
        string title,
        string? snippet,
        string? bodyText = null,
        AiContentType contentType = AiContentType.News,
        CancellationToken ct = default)
    {
        var apiKey = configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Gemini:ApiKey no está configurado. El resumen AI no está disponible.");
            return null;
        }

        var providerConfig = await providerRepo.GetConfigAsync(ct);
        var model = contentType == AiContentType.Document
            ? DefaultDocumentModel
            : providerConfig.ModelId;
        var maxOutputTokens = contentType == AiContentType.Document ? DocumentMaxOutputTokens : NewsMaxOutputTokens;
        var retryMaxOutputTokens = contentType == AiContentType.Document ? RetryDocumentMaxOutputTokens : RetryNewsMaxOutputTokens;
        var summary = await GenerateSummaryCoreAsync(
            model,
            apiKey,
            await BuildPromptAsync(title, snippet, bodyText, contentType, requiresElaboration: false, ct),
            maxOutputTokens,
            ct);

        if (IsAcceptableSummary(summary, bodyText))
            return summary;

        logger.LogWarning(
            "Gemini devolvió un resumen corto o incompleto para modelo {Model}. Longitud: {Length}. Reintentando con prompt reforzado.",
            model,
            summary.Length);

        var retriedSummary = await GenerateSummaryCoreAsync(
            model,
            apiKey,
            await BuildPromptAsync(title, snippet, bodyText, contentType, requiresElaboration: true, ct),
            retryMaxOutputTokens,
            ct);

        if (IsAcceptableSummary(retriedSummary, bodyText))
            return retriedSummary;

        if (contentType == AiContentType.News && HasLongBody(bodyText))
            throw new InvalidOperationException("Gemini devolvió un resumen incompleto incluso tras múltiples intentos.");

        return retriedSummary.Length >= summary.Length ? retriedSummary : summary;
    }

    private async Task<string> GenerateSummaryCoreAsync(
        string model,
        string apiKey,
        string prompt,
        int maxOutputTokens,
        CancellationToken ct)
    {
        var url = $"{BaseUrl}/{model}:generateContent?key={apiKey}";
        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } },
            },
            generationConfig = new { maxOutputTokens },
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
            throw new InvalidOperationException("Gemini devolvió una respuesta con texto vacío.");

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

            if (response.IsSuccessStatusCode)
                return response;

            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                logger.LogError(
                    "Gemini rechazó la credencial para modelo {Model}. Status: {StatusCode}. Body: {Body}",
                    model,
                    (int)response.StatusCode,
                    responseBody);

                throw new AiProviderConfigurationException(
                    "La credencial de Gemini fue rechazada. Verifique Gemini:ApiKey y genere una nueva API key si la actual fue revocada o reportada como filtrada.");
            }

            logger.LogError(
                "Gemini devolvió error para modelo {Model}. Status: {StatusCode}. Body: {Body}",
                model,
                (int)response.StatusCode,
                responseBody);

            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al llamar a Gemini API con modelo {Model}", model);
            throw;
        }
    }

    private async Task<string> BuildPromptAsync(
        string title,
        string? snippet,
        string? bodyText,
        AiContentType contentType,
        bool requiresElaboration,
        CancellationToken ct)
    {
        var contentTypeKey = AiPromptTemplateDefaults.ResolveContentType(contentType);
        var template = (await promptRepo.GetPromptAsync(contentTypeKey, ct))?.PromptTemplate
            ?? AiPromptTemplateDefaults.GetTemplate(contentTypeKey);
        var strictness = requiresElaboration
            ? "La respuesta debe estar completa, cerrar la idea final y no puede quedar truncada. Escribe entre 6 y 8 oraciones completas, con un mínimo de 450 caracteres, análisis suficiente para un inversionista y sin frases telegráficas."
            : "Redacta un resumen analítico en español de entre 5 y 7 oraciones sobre esta noticia. Debe ser un texto sustancial, no una nota breve.";

        var preparedBody = string.IsNullOrWhiteSpace(bodyText)
            ? null
            : bodyText.Trim()[..Math.Min(bodyText.Trim().Length, MaxPromptBodyChars)];

        var bodySection = string.IsNullOrWhiteSpace(preparedBody)
            ? "Cuerpo completo no disponible."
            : $"Cuerpo del artículo: {preparedBody}";

        var snippetSection = string.IsNullOrWhiteSpace(snippet)
            ? "Fragmento RSS no disponible."
            : $"Fragmento RSS: {snippet}";

        return template
            .Replace("{strictness_instruction}", strictness, StringComparison.Ordinal)
            .Replace("{title}", title, StringComparison.Ordinal)
            .Replace("{snippet_section}", snippetSection, StringComparison.Ordinal)
            .Replace("{body_section}", bodySection, StringComparison.Ordinal);
    }

    private static bool IsAcceptableSummary(string summary, string? bodyText)
    {
        var hasLongBody = HasLongBody(bodyText);
        var minimumLength = hasLongBody ? MinimumLongSummaryLength : MinimumShortSummaryLength;
        var minimumSentenceCount = hasLongBody ? MinimumLongSummarySentenceCount : MinimumShortSummarySentenceCount;

        if (summary.Length < minimumLength)
            return false;

        var trimmed = summary.TrimEnd();
        if (!(trimmed.EndsWith('.') || trimmed.EndsWith('!') || trimmed.EndsWith('?')))
            return false;

        return CountSentenceTerminators(trimmed) >= minimumSentenceCount;
    }

    private static int CountSentenceTerminators(string text)
        => text.Count(c => c is '.' or '!' or '?');

    private static bool HasLongBody(string? bodyText)
        => !string.IsNullOrWhiteSpace(bodyText) && bodyText.Trim().Length >= LongBodyThreshold;
}
