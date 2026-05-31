using System.Net.Http.Json;
using System.Text.Json;
using Application.Fundamentals;
using Application.News;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.Ai;

public class DeepSeekKpiExtractorService(
    HttpClient httpClient,
    IConfiguration configuration,
    IAiProviderConfigRepository providerRepo,
    IAiPromptRepository promptRepo,
    ILogger<DeepSeekKpiExtractorService> logger) : IKpiExtractorService
{
    private const string BaseUrl = "https://api.deepseek.com/chat/completions";
    private const int MaxMarkdownChars = 40_000;
    // Reasoning models (e.g. DeepSeek-R1) consume reasoning_tokens against max_tokens.
    // 2048 was exhausted entirely by reasoning, leaving no budget for the actual JSON response.
    private const int MaxOutputTokens = 8000;

    public async Task<KpiExtractionResult> ExtractAsync(string markdownContent, CancellationToken ct, Guid? relatedEntityId = null)
    {
        var apiKey = configuration["DeepSeek:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("DeepSeek:ApiKey no está configurado. La extracción IA de KPIs no está disponible.");
            return new KpiExtractionResult(null, null, null, null, null, null, null, null, null, null, null, null, null, "DeepSeek no está configurado.", false);
        }

        if (markdownContent.Length > MaxMarkdownChars)
        {
            logger.LogWarning(
                "Markdown truncado para extracción KPI con DeepSeek. Original: {OriginalChars} chars, enviando: {MaxChars} chars.",
                markdownContent.Length, MaxMarkdownChars);
            markdownContent = markdownContent[..MaxMarkdownChars];
        }

        var providerConfig = await providerRepo.GetConfigAsync(ct);
        var promptTemplate = await GetPromptAsync(ct);
        var prompt = promptTemplate.Replace("{markdown_content}", markdownContent, StringComparison.Ordinal);

        logger.LogInformation(
            "Llamando a DeepSeek para extracción KPI. Modelo: {Model}, prompt total: {PromptChars} chars.",
            providerConfig.ModelId, prompt.Length);

        var body = new
        {
            model = providerConfig.ModelId,
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = MaxOutputTokens,
            response_format = new { type = "json_object" },
        };

        using var response = await SendRequestAsync(apiKey, body, providerConfig.ModelId, ct);
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var root = doc.RootElement;
        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            logger.LogWarning("DeepSeek devolvió respuesta sin choices. Modelo: {Model}.", providerConfig.ModelId);
            return new KpiExtractionResult(null, null, null, null, null, null, null, null, null, null, null, null, null, "DeepSeek no devolvió choices en la respuesta.", false);
        }

        var firstChoice = choices[0];
        if (firstChoice.TryGetProperty("finish_reason", out var finishReasonProp))
        {
            var finishReason = finishReasonProp.GetString();
            if (finishReason == "length")
            {
                logger.LogError(
                    "DeepSeek truncó la respuesta por límite de tokens (finish_reason=length). Modelo: {Model}. " +
                    "Aumenta MaxOutputTokens o reduce el prompt. completion_tokens gastados: {Tokens}.",
                    providerConfig.ModelId,
                    root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("completion_tokens", out var ct2) ? ct2.GetInt32() : -1);
            }
        }

        var raw = firstChoice
            .TryGetProperty("message", out var message) && message.TryGetProperty("content", out var contentProp)
            ? contentProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return new KpiExtractionResult(null, null, null, null, null, null, null, null, null, null, null, null, null, "DeepSeek devolvió una respuesta vacía.", false);
        }

        var result = KpiExtractionJsonParser.Parse(raw, logger, "DeepSeek");
        if (!result.Success)
        {
            logger.LogWarning(
                "DeepSeek extracción KPI sin datos útiles. Modelo: {Model}. Raw response (primeros 1000 chars): {Raw}",
                providerConfig.ModelId,
                raw.Length > 1000 ? raw[..1000] + "…" : raw);
        }

        return result;
    }

    private async Task<string> GetPromptAsync(CancellationToken ct)
    {
        var stored = await promptRepo.GetPromptAsync(AiPromptTemplateDefaults.KpiExtractionContentType, ct);
        return stored?.PromptTemplate ?? AiPromptTemplateDefaults.KpiExtraction;
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string apiKey, object body, string model, CancellationToken ct)
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
                logger.LogError("DeepSeek rechazó la credencial durante extracción KPI. Modelo {Model}. Status: {StatusCode}. Body: {Body}", model, (int)response.StatusCode, responseBody);
                throw new InvalidOperationException("La credencial de DeepSeek fue rechazada.");
            }

            logger.LogError("DeepSeek devolvió error durante extracción KPI. Modelo {Model}. Status: {StatusCode}. Body: {Body}", model, (int)response.StatusCode, responseBody);
            throw new InvalidOperationException($"DeepSeek devolvió error HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al llamar a DeepSeek para extracción de KPIs con modelo {Model}.", model);
            throw;
        }
    }
}
