using System.Net.Http.Json;
using System.Text.Json;
using Application.Fundamentals;
using Application.News;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.Ai;

public class GeminiKpiExtractorService(
    HttpClient httpClient,
    IConfiguration configuration,
    IAiProviderConfigRepository providerRepo,
    IAiPromptRepository promptRepo,
    ILogger<GeminiKpiExtractorService> logger) : IKpiExtractorService
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private const int MaxMarkdownChars = 80_000;
    private const int MaxOutputTokens = 2048;

    public async Task<KpiExtractionResult> ExtractAsync(string markdownContent, CancellationToken ct, Guid? relatedEntityId = null)
    {
        var apiKey = configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Gemini:ApiKey no está configurado. La extracción IA de KPIs no está disponible.");
            return new KpiExtractionResult(null, null, null, null, null, null, null, null, null, null, null, null, null, "Gemini no está configurado.", false);
        }

        if (markdownContent.Length > MaxMarkdownChars)
        {
            logger.LogWarning(
                "Markdown truncado para extracción KPI con Gemini. Original: {OriginalChars} chars, enviando: {MaxChars} chars.",
                markdownContent.Length, MaxMarkdownChars);
            markdownContent = markdownContent[..MaxMarkdownChars];
        }

        var providerConfig = await providerRepo.GetConfigAsync(ct);
        var url = $"{BaseUrl}/{providerConfig.ModelId}:generateContent?key={apiKey}";
        var promptTemplate = await GetPromptAsync(ct);
        var prompt = promptTemplate.Replace("{markdown_content}", markdownContent, StringComparison.Ordinal);

        logger.LogInformation(
            "Llamando a Gemini para extracción KPI. Modelo: {Model}, prompt total: {PromptChars} chars.",
            providerConfig.ModelId, prompt.Length);

        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } },
            },
            generationConfig = new
            {
                maxOutputTokens = MaxOutputTokens,
                responseMimeType = "application/json",
            },
        };

        using var response = await SendRequestAsync(url, body, providerConfig.ModelId, ct);
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var root = doc.RootElement;
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            logger.LogWarning("Gemini devolvió respuesta sin candidatos (posiblemente bloqueado por safety filters).");
            return new KpiExtractionResult(null, null, null, null, null, null, null, null, null, null, null, null, null, "Gemini no devolvió candidatos (safety filter o respuesta vacía).", false);
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.GetArrayLength() == 0)
        {
            logger.LogWarning("Gemini devolvió candidato sin contenido extraíble.");
            return new KpiExtractionResult(null, null, null, null, null, null, null, null, null, null, null, null, null, "Gemini devolvió candidato sin contenido.", false);
        }

        var raw = parts[0].TryGetProperty("text", out var textProp) ? textProp.GetString() : null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return new KpiExtractionResult(null, null, null, null, null, null, null, null, null, null, null, null, null, "Gemini devolvió una respuesta vacía.", false);
        }

        var result = KpiExtractionJsonParser.Parse(raw, logger, "Gemini");
        if (!result.Success)
        {
            logger.LogWarning(
                "Gemini extracción KPI sin datos útiles. Raw response (primeros 1000 chars): {Raw}",
                raw.Length > 1000 ? raw[..1000] + "…" : raw);
        }

        return result;
    }

    private async Task<string> GetPromptAsync(CancellationToken ct)
    {
        var stored = await promptRepo.GetPromptAsync(AiPromptTemplateDefaults.KpiExtractionContentType, ct);
        return stored?.PromptTemplate ?? AiPromptTemplateDefaults.KpiExtraction;
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string url, object body, string model, CancellationToken ct)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(url, body, ct);

            if (response.IsSuccessStatusCode)
                return response;

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                logger.LogError("Gemini rechazó la credencial durante extracción KPI. Modelo: {Model}. Status: {StatusCode}. Body: {Body}", model, (int)response.StatusCode, responseBody);
                throw new InvalidOperationException("La credencial de Gemini fue rechazada.");
            }

            logger.LogError("Gemini devolvió error durante extracción KPI. Modelo: {Model}. Status: {StatusCode}. Body: {Body}", model, (int)response.StatusCode, responseBody);
            throw new InvalidOperationException($"Gemini devolvió error HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al llamar a Gemini para extracción de KPIs con modelo {Model}.", model);
            throw;
        }
    }
}
