using System.Text.Json;
using Application.Jobs;
using Application.News;
using Domain.Jobs;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.News;

public class NewsBodyTextRetryJob(
    INewsRepository newsRepo,
    IArticleContentScraper articleContentScraper,
    IPipelineErrorLogRepository pipelineErrorLogRepo,
    ILogger<NewsBodyTextRetryJob> logger)
{
    private const int MaxArticles = 200;
    private const int DaysBack = 60;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var articles = await newsRepo.GetNullBodyTextArticlesAsync(MaxArticles, DaysBack, ct);
        logger.LogInformation("Body-text retry started — {Count} articles to process", articles.Count);

        int updated = 0, skipped = 0;

        foreach (var (id, url) in articles)
        {
            try
            {
                var bodyText = await articleContentScraper.TryGetArticleTextAsync(url, ct);
                if (!string.IsNullOrWhiteSpace(bodyText))
                {
                    await newsRepo.UpdateBodyTextAsync(id, bodyText, ct);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "body-text retry failed for '{Url}'", url);
                var retryErrorType = ex.GetType().Name;
                var retryAiContext = $"El job de reintento de body_text falló al re-scrapear el artículo {id} desde la URL '{url}'. El registro había sido seleccionado porque seguía con body_text nulo dentro de la ventana de {DaysBack} días y el fallo ocurrió durante la extracción del contenido. Revise bloqueo del sitio, timeout, DNS o cambios en la estructura HTML.";
                try
                {
                    await pipelineErrorLogRepo.LogErrorAsync(new PipelineErrorLog
                    {
                        Pipeline = "BodyTextRetry",
                        Timestamp = DateTimeOffset.UtcNow,
                        ErrorType = retryErrorType.Length > 100 ? retryErrorType[..100] : retryErrorType,
                        Message = ex.Message,
                        Context = JsonSerializer.Serialize(new { articleId = id, url }),
                        AiContext = retryAiContext.Length > 800 ? retryAiContext[..800] : retryAiContext,
                    }, ct);
                }
                catch (Exception logEx)
                {
                    logger.LogWarning(logEx, "Failed to write pipeline error log entry for article {ArticleId}", id);
                }
                skipped++;
            }
        }

        logger.LogInformation(
            "Body-text retry complete — updated: {Updated}, still-null: {Skipped}",
            updated, skipped);
    }
}
