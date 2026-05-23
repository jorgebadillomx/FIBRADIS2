using Application.News;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.News;

public class NewsBodyTextRetryJob(
    INewsRepository newsRepo,
    IArticleContentScraper articleContentScraper,
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
                skipped++;
            }
        }

        logger.LogInformation(
            "Body-text retry complete — updated: {Updated}, still-null: {Skipped}",
            updated, skipped);
    }
}
