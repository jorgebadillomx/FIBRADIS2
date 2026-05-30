using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.News;
using Domain.News;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.Ai;

public class DeepSeekNewsAnalysisService(
    HttpClient httpClient,
    IConfiguration configuration,
    IAiProviderConfigRepository providerRepo,
    IAiPromptRepository promptRepo,
    ILogger<DeepSeekNewsAnalysisService> logger) : IAiNewsAnalysisService
{
    private const string BaseUrl = "https://api.deepseek.com/chat/completions";
    private const int MaxOutputTokens = 2000;
    private const int MaxPromptBodyChars = 12000;

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<NewsAiAnalysis?> GenerateAnalysisAsync(
        string title,
        string? snippet,
        string? bodyText,
        CancellationToken ct = default)
    {
        var apiKey = configuration["DeepSeek:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("DeepSeek:ApiKey no está configurado. El análisis IA de noticias no está disponible.");
            return null;
        }

        var providerConfig = await providerRepo.GetConfigAsync(ct);
        var prompt = await BuildPromptAsync(title, snippet, bodyText, ct);

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

        var raw = ExtractTextFromDeepSeekResponse(doc.RootElement, providerConfig.ModelId);
        if (raw is null) return null;

        return DeserializeAnalysis(raw, providerConfig.ModelId);
    }

    private string? ExtractTextFromDeepSeekResponse(JsonElement root, string model)
    {
        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            logger.LogWarning("DeepSeek devolvió respuesta sin choices para análisis de noticias. Modelo: {Model}", model);
            return null;
        }

        var first = choices[0];
        if (!first.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var contentElement))
        {
            logger.LogWarning("DeepSeek devolvió mensaje sin contenido para análisis de noticias. Modelo: {Model}", model);
            return null;
        }

        var text = contentElement.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogWarning("DeepSeek devolvió texto vacío para análisis de noticias. Modelo: {Model}", model);
            return null;
        }

        return text;
    }

    private NewsAiAnalysis? DeserializeAnalysis(string raw, string model)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<NewsAiAnalysisJson>(raw, DeserializeOptions);
            if (dto is null) return null;
            return MapToAnalysis(dto);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "DeepSeek devolvió JSON malformado para análisis de noticias. Modelo: {Model}. Raw (primeros 500): {Raw}",
                model, raw.Length > 500 ? raw[..500] + "…" : raw);
            return null;
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string apiKey, object body, string model, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(body);

            var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode) return response;

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                logger.LogError("DeepSeek rechazó la credencial en análisis de noticias. Modelo: {Model}. Status: {Status}. Body: {Body}",
                    model, (int)response.StatusCode, responseBody);
                throw new AiProviderConfigurationException(
                    "La credencial de DeepSeek fue rechazada. Verifique DeepSeek:ApiKey.");
            }

            logger.LogError("DeepSeek devolvió error en análisis de noticias. Modelo: {Model}. Status: {Status}. Body: {Body}",
                model, (int)response.StatusCode, responseBody);
            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not AiProviderConfigurationException)
        {
            logger.LogError(ex, "Error al llamar a DeepSeek para análisis de noticias. Modelo: {Model}", model);
            throw;
        }
    }

    private async Task<string> BuildPromptAsync(string title, string? snippet, string? bodyText, CancellationToken ct)
    {
        var template = (await promptRepo.GetPromptAsync(AiPromptTemplateDefaults.NewsAnalysisContentType, ct))?.PromptTemplate
            ?? AiPromptTemplateDefaults.NewsAnalysis;

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
            .Replace("{title}", title, StringComparison.Ordinal)
            .Replace("{snippet_section}", snippetSection, StringComparison.Ordinal)
            .Replace("{body_section}", bodySection, StringComparison.Ordinal);
    }

    private static NewsAiAnalysis MapToAnalysis(NewsAiAnalysisJson dto) => new(
        IsRelevant: dto.IsRelevant,
        RelevanceReason: dto.RelevanceReason,
        Headline: dto.Headline,
        Impact: dto.Impact ?? "nulo",
        SectorTags: dto.SectorTags ?? [],
        Subsector: dto.Subsector,
        AffectedFibers: dto.AffectedFibers ?? [],
        KeyFacts: dto.KeyFacts ?? [],
        KeyFigures: (dto.KeyFigures ?? []).Select(f => new NewsKeyFigure(f.Label ?? "", f.ValueText ?? "", f.Importance ?? "baja")).ToList(),
        SummaryMarkdown: dto.SummaryMarkdown,
        InvestorTakeaway: dto.InvestorTakeaway,
        Confidence: dto.Confidence,
        ExtractionNotes: dto.ExtractionNotes
    );

    private sealed class NewsAiAnalysisJson
    {
        public bool IsRelevant { get; set; }
        public string? RelevanceReason { get; set; }
        public string? Headline { get; set; }
        public string? Impact { get; set; }
        public List<string>? SectorTags { get; set; }
        public string? Subsector { get; set; }
        public List<string>? AffectedFibers { get; set; }
        public List<string>? KeyFacts { get; set; }
        public List<NewsKeyFigureJson>? KeyFigures { get; set; }
        public string? SummaryMarkdown { get; set; }
        public string? InvestorTakeaway { get; set; }
        public double Confidence { get; set; }
        public string? ExtractionNotes { get; set; }
    }

    private sealed class NewsKeyFigureJson
    {
        public string? Label { get; set; }
        public string? ValueText { get; set; }
        public string? Importance { get; set; }
    }
}
