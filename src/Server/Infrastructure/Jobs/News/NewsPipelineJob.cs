using Application.Catalog;
using Application.News;
using Domain.Catalog;
using Domain.News;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.News;

public class NewsPipelineJob(
    IFibraRepository fibraRepo,
    INewsRepository newsRepo,
    IBlocklistRepository blocklistRepo,
    IRssClient rssClient,
    IAiModeRepository aiModeRepo,
    IOgImageScraper ogImageScraper,
    IArticleContentScraper articleContentScraper,
    IAiSummaryService summaryService,
    ILogger<NewsPipelineJob> logger)
{
    private static readonly string[] GeneralQueries =
    [
        "FIBRAs Mexico BMV",
        "mercado inmobiliario México renta",
    ];

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var currentMode = AiMode.Off;
        try
        {
            var config = await aiModeRepo.GetConfigAsync(ct);
            currentMode = config.Mode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read AI mode configuration; falling back to Off for this pipeline run.");
        }

        var fibras = await fibraRepo.GetAllActiveAsync(ct);
        var fibraMatchInfos = fibras
            .Select(f => new FibraMatchInfo(f.Id, f.Ticker, f.NameVariants.AsReadOnly()))
            .ToList();
        var blocklistTerms = await blocklistRepo.GetAllTermsAsync(ct);
        var since24h = DateTimeOffset.UtcNow.AddHours(-24);

        var allItems = new List<RssItem>();

        foreach (var fibra in fibras)
        {
            foreach (var query in BuildFibraQueries(fibra))
            {
                var items = await rssClient.FetchAsync(query, ct);
                allItems.AddRange(items);
            }
        }

        foreach (var query in GeneralQueries)
        {
            var items = await rssClient.FetchAsync(query, ct);
            allItems.AddRange(items);
        }

        var candidateUrls = allItems
            .Select(item => item.Url)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var existingUrls = new HashSet<string>(
            await newsRepo.GetExistingUrlsAsync(candidateUrls, ct),
            StringComparer.OrdinalIgnoreCase);
        var recentTitles = await newsRepo.GetRecentNormalizedTitlesAsync(since24h, ct);
        var filteredItems = NewsDeduplicator.Filter(allItems, existingUrls, recentTitles, blocklistTerms);

        int saved = 0, errors = 0;

        foreach (var item in filteredItems)
        {
            try
            {
                var imageUrl = !string.IsNullOrWhiteSpace(item.Url)
                    ? await ogImageScraper.TryGetOgImageAsync(item.Url, ct)
                    : null;
                var bodyText = !string.IsNullOrWhiteSpace(item.Url)
                    ? await articleContentScraper.TryGetArticleTextAsync(item.Url, ct)
                    : null;

                string? aiSummary = null;
                var finalStatus = NewsArticleStatus.Processed;

                if (currentMode == AiMode.On)
                {
                    try
                    {
                        aiSummary = await summaryService.GenerateSummaryAsync(
                            item.Title, item.Snippet, bodyText, AiContentType.News, ct);
                        if (aiSummary is not null)
                        {
                            finalStatus = NewsArticleStatus.Processed;
                        }
                        else
                        {
                            logger.LogWarning("AI summary returned null for '{Url}'; article saved with Partial status", item.Url);
                            finalStatus = NewsArticleStatus.Partial;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "AI summary failed for '{Url}'; article saved without summary", item.Url);
                        finalStatus = NewsArticleStatus.Partial;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }

                var article = new NewsArticle
                {
                    Title = item.Title,
                    TitleNormalized = NewsDeduplicator.NormalizeTitle(item.Title),
                    Source = item.Source,
                    PublishedAt = item.PublishedAt,
                    Url = item.Url,
                    Snippet = item.Snippet,
                    BodyText = bodyText,
                    ImageUrl = imageUrl,
                    AiSummary = aiSummary,
                    Status = finalStatus,
                    CapturedAt = DateTimeOffset.UtcNow,
                };
                var fibraIds = NewsAssociator.Associate(item, fibraMatchInfos);

                await newsRepo.AddWithLinksAsync(article, fibraIds, ct);
                saved++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save news article '{Url}'", item.Url);
                errors++;
            }
        }

        logger.LogInformation(
            "News pipeline complete — fetched: {Fetched}, filtered_in: {FilteredIn}, saved: {Saved}, errors: {Errors}",
            allItems.Count,
            filteredItems.Count,
            saved,
            errors);
    }

    private static IEnumerable<string> BuildFibraQueries(Fibra fibra)
    {
        yield return $"{fibra.Ticker} FIBRA";

        foreach (var variant in fibra.NameVariants.Where(v => !string.Equals(v, fibra.Ticker, StringComparison.OrdinalIgnoreCase)))
            yield return $"{variant} FIBRA México";
    }

}
