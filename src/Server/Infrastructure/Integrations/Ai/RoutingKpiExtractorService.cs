using System.Diagnostics;
using System.Text.Json;
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
    private const int PreviewChars = 2000;

    public async Task<KpiExtractionResult> ExtractAsync(string markdownContent, CancellationToken ct)
    {
        var config = await providerRepo.GetConfigAsync(ct);
        var sw = Stopwatch.StartNew();
        var preview = markdownContent.Length > PreviewChars ? markdownContent[..PreviewChars] : markdownContent;

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
                markdownContent.Length, sw.ElapsedMilliseconds, false, preview, null, ex.Message);
            throw;
        }

        sw.Stop();
        await TryLogAsync("KpiExtraction", config.Provider.ToString(), config.ModelId,
            markdownContent.Length, sw.ElapsedMilliseconds, result.Success,
            preview, JsonSerializer.Serialize(result), result.Success ? null : result.ExtractionNotes);

        return result;
    }

    private async Task TryLogAsync(string operation, string provider, string modelId,
        int promptLength, long durationMs, bool success, string? inputPreview, string? responseRaw, string? errorMessage)
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
                InputPreview = inputPreview,
                ResponseRaw = responseRaw,
                ErrorMessage = errorMessage,
            }, CancellationToken.None);
        }
        catch { /* never let logging break the call */ }
    }
}
