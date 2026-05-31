using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.News;
using Domain.News;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.Ai;

public class GeminiNewsAnalysisService(
    HttpClient httpClient,
    IConfiguration configuration,
    IAiProviderConfigRepository providerRepo,
    IAiPromptRepository promptRepo,
    ILogger<GeminiNewsAnalysisService> logger) : IAiNewsAnalysisService
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private const int MaxOutputTokens = 2500;
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
        var apiKey = configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Gemini:ApiKey no está configurado. El análisis IA de noticias no está disponible.");
            return null;
        }

        var providerConfig = await providerRepo.GetConfigAsync(ct);
        var url = $"{BaseUrl}/{providerConfig.ModelId}:generateContent?key={apiKey}";
        var prompt = await BuildPromptAsync(title, snippet, bodyText, ct);

        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                maxOutputTokens = MaxOutputTokens,
                responseMimeType = "application/json",
            },
        };

        using var response = await SendRequestAsync(url, body, providerConfig.ModelId, ct);
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var raw = ExtractTextFromGeminiResponse(doc.RootElement, providerConfig.ModelId);
        if (raw is null) return null;

        return DeserializeAnalysis(raw, providerConfig.ModelId);
    }

    private string? ExtractTextFromGeminiResponse(JsonElement root, string model)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            logger.LogWarning("Gemini devolvió respuesta sin candidatos para análisis de noticias. Modelo: {Model}", model);
            return null;
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.GetArrayLength() == 0)
        {
            logger.LogWarning("Gemini devolvió candidato sin contenido para análisis de noticias. Modelo: {Model}", model);
            return null;
        }

        var text = parts[0].TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogWarning("Gemini devolvió texto vacío para análisis de noticias. Modelo: {Model}", model);
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
            logger.LogWarning(ex, "Gemini devolvió JSON malformado para análisis de noticias. Modelo: {Model}. Raw (primeros 500): {Raw}",
                model, raw.Length > 500 ? raw[..500] + "…" : raw);
            return null;
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string url, object body, string model, CancellationToken ct)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(url, body, ct);
            if (response.IsSuccessStatusCode) return response;

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                logger.LogError("Gemini rechazó la credencial en análisis de noticias. Modelo: {Model}. Status: {Status}. Body: {Body}",
                    model, (int)response.StatusCode, responseBody);
                throw new AiProviderConfigurationException(
                    "La credencial de Gemini fue rechazada. Verifique Gemini:ApiKey.");
            }

            logger.LogError("Gemini devolvió error en análisis de noticias. Modelo: {Model}. Status: {Status}. Body: {Body}",
                model, (int)response.StatusCode, responseBody);
            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not AiProviderConfigurationException)
        {
            logger.LogError(ex, "Error al llamar a Gemini para análisis de noticias. Modelo: {Model}", model);
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

        return template
            .Replace("{title}", title, StringComparison.Ordinal)
            .Replace("{snippet_section}", string.Empty, StringComparison.Ordinal)
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
        Confidence: dto.Confidence ?? 0.0,
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
        public double? Confidence { get; set; }
        public string? ExtractionNotes { get; set; }
    }

    private sealed class NewsKeyFigureJson
    {
        public string? Label { get; set; }
        public string? ValueText { get; set; }
        public string? Importance { get; set; }
    }
}
