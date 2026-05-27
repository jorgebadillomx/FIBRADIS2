using Application.Fundamentals;
using Application.News;
using Domain.News;

namespace Infrastructure.Integrations.Ai;

public class RoutingKpiExtractorService(
    GeminiKpiExtractorService gemini,
    DeepSeekKpiExtractorService deepSeek,
    IAiProviderConfigRepository providerRepo) : IKpiExtractorService
{
    public async Task<KpiExtractionResult> ExtractAsync(string markdownContent, CancellationToken ct)
    {
        var config = await providerRepo.GetConfigAsync(ct);
        return config.Provider switch
        {
            AiProvider.Gemini => await gemini.ExtractAsync(markdownContent, ct),
            AiProvider.DeepSeek => await deepSeek.ExtractAsync(markdownContent, ct),
            _ => new KpiExtractionResult(null, null, null, null, null, null, null, null, null, null, null, null, null, $"Proveedor de IA no soportado: {config.Provider}.", false),
        };
    }
}
