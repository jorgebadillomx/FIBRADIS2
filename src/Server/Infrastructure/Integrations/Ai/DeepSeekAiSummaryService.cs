using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using Application.News;
using Domain.News;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.Ai;

public class DeepSeekAiSummaryService(
    HttpClient httpClient,
    IConfiguration configuration,
    IAiProviderConfigRepository providerRepo,
    IAiPromptRepository promptRepo,
    ILogger<DeepSeekAiSummaryService> logger) : IAiSummaryService
{
    private const string BaseUrl = "https://api.deepseek.com/chat/completions";
    private const string DefaultDocumentModel = "deepseek-v4-pro";
    private const int DefaultMaxOutputTokens = 768;
    private const int RetryMaxOutputTokens = 1024;
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
        var apiKey = configuration["DeepSeek:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("DeepSeek:ApiKey no está configurado. El resumen AI no está disponible.");
            return null;
        }

        var providerConfig = await providerRepo.GetConfigAsync(ct);
        var model = contentType == AiContentType.Document
            ? DefaultDocumentModel
            : providerConfig.ModelId;

        var summary = await GenerateSummaryCoreAsync(
            model,
            apiKey,
            await BuildPromptAsync(title, snippet, bodyText, contentType, requiresElaboration: false, ct),
            DefaultMaxOutputTokens,
            ct);

        if (IsAcceptableSummary(summary, bodyText))
            return summary;

        logger.LogWarning(
            "DeepSeek devolvió un resumen corto o incompleto para modelo {Model}. Longitud: {Length}. Reintentando con prompt reforzado.",
            model,
            summary.Length);

        var retriedSummary = await GenerateSummaryCoreAsync(
            model,
            apiKey,
            await BuildPromptAsync(title, snippet, bodyText, contentType, requiresElaboration: true, ct),
            RetryMaxOutputTokens,
            ct);

        if (IsAcceptableSummary(retriedSummary, bodyText))
            return retriedSummary;

        if (contentType == AiContentType.News && HasLongBody(bodyText))
            throw new InvalidOperationException("DeepSeek devolvió un resumen incompleto incluso tras múltiples intentos.");

        return retriedSummary.Length >= summary.Length ? retriedSummary : summary;
    }

    private async Task<string> GenerateSummaryCoreAsync(
        string model,
        string apiKey,
        string prompt,
        int maxTokens,
        CancellationToken ct)
    {
        var body = new
        {
            model,
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = maxTokens,
        };

        using var response = await SendRequestAsync(apiKey, body, model, ct);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;

        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            logger.LogWarning("DeepSeek devolvió respuesta sin choices para modelo {Model}", model);
            throw new InvalidOperationException("DeepSeek devolvió una respuesta vacía sin choices.");
        }

        var first = choices[0];
        if (!first.TryGetProperty("message", out var message))
            throw new InvalidOperationException("DeepSeek devolvió una respuesta sin mensaje.");

        if (!message.TryGetProperty("content", out var contentElement))
            throw new InvalidOperationException("DeepSeek devolvió un mensaje sin contenido.");

        var text = contentElement.GetString();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("DeepSeek devolvió una respuesta con texto vacío.");

        return text.Trim();
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        string apiKey,
        object body,
        string model,
        CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(body);

            var response = await httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
                return response;

            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                logger.LogError(
                    "DeepSeek rechazó la credencial para modelo {Model}. Status: {StatusCode}. Body: {Body}",
                    model,
                    (int)response.StatusCode,
                    responseBody);

                throw new AiProviderConfigurationException(
                    "La credencial de DeepSeek fue rechazada. Verifique DeepSeek:ApiKey.");
            }

            logger.LogError(
                "DeepSeek devolvió error para modelo {Model}. Status: {StatusCode}. Body: {Body}",
                model,
                (int)response.StatusCode,
                responseBody);

            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al llamar a DeepSeek API con modelo {Model}", model);
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
