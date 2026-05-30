using System.Diagnostics;
using Application.Ai;
using Application.News;
using Domain.Ai;
using Domain.News;

namespace Infrastructure.Integrations.Ai;

public class RoutingAiSummaryService(
    GeminiAiSummaryService gemini,
    DeepSeekAiSummaryService deepSeek,
    IAiProviderConfigRepository providerRepo,
    IAiCallLogRepository callLogRepo) : IAiSummaryService
{
    public async Task<string?> GenerateSummaryAsync(
        string title,
        string? snippet,
        string? bodyText = null,
        AiContentType contentType = AiContentType.News,
        CancellationToken ct = default)
    {
        var config = await providerRepo.GetConfigAsync(ct);
        var sw = Stopwatch.StartNew();
        var promptLength = title.Length + (snippet?.Length ?? 0) + (bodyText?.Length ?? 0);
        var rawData = AiCallRawData.Begin();
        var operationName = contentType == AiContentType.News ? "NewsSummary" : contentType.ToString();

        string? result;
        try
        {
            result = config.Provider switch
            {
                AiProvider.Gemini => await gemini.GenerateSummaryAsync(title, snippet, bodyText, contentType, ct),
                AiProvider.DeepSeek => await deepSeek.GenerateSummaryAsync(title, snippet, bodyText, contentType, ct),
                _ => throw new AiProviderConfigurationException($"Proveedor de IA no soportado: {config.Provider}. Configure un proveedor válido desde Ops."),
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            await TryLogAsync(operationName, config.Provider.ToString(), config.ModelId,
                promptLength, sw.ElapsedMilliseconds, false,
                rawData.RequestBody, rawData.ResponseBody, ex.Message);
            throw;
        }

        sw.Stop();
        await TryLogAsync(operationName, config.Provider.ToString(), config.ModelId,
            promptLength, sw.ElapsedMilliseconds, result is not null,
            rawData.RequestBody, rawData.ResponseBody, null);

        return result;
    }

    private async Task TryLogAsync(string operation, string provider, string modelId,
        int promptLength, long durationMs, bool success,
        string? requestRaw, string? responseRaw, string? errorMessage)
    {
        try
        {
            await callLogRepo.AddAsync(new AiCallLog
            {
                Timestamp = DateTimeOffset.UtcNow,
                Operation = operation,
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
