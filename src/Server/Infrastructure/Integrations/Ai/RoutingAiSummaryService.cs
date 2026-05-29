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
            await TryLogAsync(contentType.ToString(), config.Provider.ToString(), config.ModelId,
                promptLength, sw.ElapsedMilliseconds, false, null, ex.Message);
            throw;
        }

        sw.Stop();
        var response = result is not null && result.Length > 2000 ? result[..2000] : result;
        await TryLogAsync(contentType.ToString(), config.Provider.ToString(), config.ModelId,
            promptLength, sw.ElapsedMilliseconds, result is not null, response, null);

        return result;
    }

    private async Task TryLogAsync(string operation, string provider, string modelId,
        int promptLength, long durationMs, bool success, string? responseRaw, string? errorMessage)
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
                ResponseRaw = responseRaw,
                ErrorMessage = errorMessage,
            }, CancellationToken.None);
        }
        catch { /* never let logging break the call */ }
    }
}
