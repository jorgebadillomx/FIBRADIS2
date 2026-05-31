using System.Diagnostics;
using Application.Ai;
using Application.Fundamentals;
using Application.News;
using Domain.Ai;
using Domain.News;

namespace Infrastructure.Integrations.Ai;

public class RoutingKpiExtractorService(
    GeminiKpiExtractorService gemini,
    DeepSeekKpiExtractorService deepSeek,
    IAiProviderConfigRepository providerRepo,
    IAiCallLogRepository callLogRepo) : IKpiExtractorService
{
    public async Task<KpiExtractionResult> ExtractAsync(string markdownContent, CancellationToken ct, Guid? relatedEntityId = null)
    {
        var config = await providerRepo.GetConfigAsync(ct);
        var sw = Stopwatch.StartNew();
        var rawData = AiCallRawData.Begin();

        KpiExtractionResult result;
        try
        {
            result = config.Provider switch
            {
                AiProvider.Gemini => await gemini.ExtractAsync(markdownContent, ct),
                AiProvider.DeepSeek => await deepSeek.ExtractAsync(markdownContent, ct),
                _ => new KpiExtractionResult(null, null, null, null, null, null, null, null, null, null, null, null, null, $"Proveedor de IA no soportado: {config.Provider}.", false),
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            await TryLogAsync("KpiExtraction", config.Provider.ToString(), config.ModelId,
                markdownContent.Length, sw.ElapsedMilliseconds, false,
                rawData.RequestBody, rawData.ResponseBody, ex.Message, relatedEntityId);
            throw;
        }

        sw.Stop();
        await TryLogAsync("KpiExtraction", config.Provider.ToString(), config.ModelId,
            markdownContent.Length, sw.ElapsedMilliseconds, result.Success,
            rawData.RequestBody, rawData.ResponseBody,
            result.Success ? null : result.ExtractionNotes, relatedEntityId);

        return result;
    }

    private async Task TryLogAsync(string operation, string provider, string modelId,
        int promptLength, long durationMs, bool success,
        string? requestRaw, string? responseRaw, string? errorMessage, Guid? relatedEntityId = null)
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
                Context = relatedEntityId.HasValue ? $"{{\"recordId\":\"{relatedEntityId}\"}}" : null,
            }, CancellationToken.None);
        }
        catch { /* never let logging break the call */ }
    }
}
