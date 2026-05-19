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
    ILogger<NewsPipelineJob> logger)
{
    private static readonly string[] GeneralQueries =
    [
        "FIBRAs Mexico BMV",
        "mercado inmobiliario México renta",
    ];

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var fibras = await fibraRepo.GetAllActiveAsync(ct);
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
                var article = new NewsArticle
                {
                    Title = item.Title,
                    TitleNormalized = NewsDeduplicator.NormalizeTitle(item.Title),
                    Source = item.Source,
                    PublishedAt = item.PublishedAt,
                    Url = item.Url,
                    Snippet = item.Snippet,
                    Status = NewsArticleStatus.Pending,
                    CapturedAt = DateTimeOffset.UtcNow,
                };

                await newsRepo.AddAsync(article, ct);
                saved++;
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
