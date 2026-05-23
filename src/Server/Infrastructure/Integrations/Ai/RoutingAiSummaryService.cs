using Application.News;
using Domain.News;

namespace Infrastructure.Integrations.Ai;

public class RoutingAiSummaryService(
    GeminiAiSummaryService gemini,
    DeepSeekAiSummaryService deepSeek,
    IAiProviderConfigRepository providerRepo) : IAiSummaryService
{
    public async Task<string?> GenerateSummaryAsync(
        string title,
        string? snippet,
        string? bodyText = null,
        AiContentType contentType = AiContentType.News,
        CancellationToken ct = default)
    {
        var config = await providerRepo.GetConfigAsync(ct);
        return config.Provider switch
        {
            AiProvider.Gemini => await gemini.GenerateSummaryAsync(title, snippet, bodyText, contentType, ct),
            AiProvider.DeepSeek => await deepSeek.GenerateSummaryAsync(title, snippet, bodyText, contentType, ct),
            _ => throw new AiProviderConfigurationException($"Proveedor de IA no soportado: {config.Provider}. Configure un proveedor válido desde Ops."),
        };
    }
}
