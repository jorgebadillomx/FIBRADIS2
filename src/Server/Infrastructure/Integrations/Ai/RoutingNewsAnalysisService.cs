using System.Diagnostics;
using Application.Ai;
using Application.News;
using Domain.Ai;
using Domain.News;

namespace Infrastructure.Integrations.Ai;

public class RoutingNewsAnalysisService(
    GeminiNewsAnalysisService gemini,
    DeepSeekNewsAnalysisService deepSeek,
    IAiProviderConfigRepository providerRepo,
    IAiCallLogRepository callLogRepo) : IAiNewsAnalysisService
{
    public async Task<NewsAiAnalysis?> GenerateAnalysisAsync(
        string title,
        string? snippet,
        string? bodyText,
        CancellationToken ct = default)
    {
        var config = await providerRepo.GetConfigAsync(ct);
        var sw = Stopwatch.StartNew();
        var promptLength = title.Length + (snippet?.Length ?? 0) + (bodyText?.Length ?? 0);
        var rawData = AiCallRawData.Begin();

        NewsAiAnalysis? result;
        try
        {
            result = config.Provider switch
            {
                AiProvider.Gemini => await gemini.GenerateAnalysisAsync(title, snippet, bodyText, ct),
                AiProvider.DeepSeek => await deepSeek.GenerateAnalysisAsync(title, snippet, bodyText, ct),
                _ => throw new AiProviderConfigurationException($"Proveedor de IA no soportado: {config.Provider}. Configure un proveedor válido desde Ops."),
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            await TryLogAsync(config.Provider.ToString(), config.ModelId, promptLength,
                sw.ElapsedMilliseconds, false, rawData.RequestBody, rawData.ResponseBody, ex.Message);
            throw;
        }

        sw.Stop();
        await TryLogAsync(config.Provider.ToString(), config.ModelId, promptLength,
            sw.ElapsedMilliseconds, result is not null, rawData.RequestBody, rawData.ResponseBody, null);

        return result;
    }

    private async Task TryLogAsync(string provider, string modelId, int promptLength,
        long durationMs, bool success, string? requestRaw, string? responseRaw, string? errorMessage)
    {
        try
        {
            await callLogRepo.AddAsync(new AiCallLog
            {
                Timestamp = DateTimeOffset.UtcNow,
                Operation = "NewsAnalysis",
                Provider = provider,
                ModelId = modelId,
                PromptLength = promptLength,
                DurationMs = durationMs,
                Success = success,
                RequestRaw = requestRaw,
                ResponseRaw = responseRaw,
                ErrorMessage = errorMessage,
            }, CancellationToken.None);
        }
        catch { /* never let logging break the call */ }
    }
}
