using System.Text.Json;
using Application.Catalog;
using Application.Jobs;
using Application.News;
using Domain.Catalog;
using Domain.Jobs;
using Domain.News;
using Infrastructure.Integrations.Articles;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.News;

public class NewsPipelineJob(
    IFibraRepository fibraRepo,
    INewsRepository newsRepo,
    IBlocklistRepository blocklistRepo,
    IRssClient rssClient,
    IAiModeRepository aiModeRepo,
    IAiProviderConfigRepository aiProviderConfigRepo,
    IOgImageScraper ogImageScraper,
    IArticleContentScraper articleContentScraper,
    IAiNewsAnalysisService analysisService,
    IPipelineErrorLogRepository pipelineErrorLogRepo,
    IPipelineRunLogRepository pipelineRunLogRepo,
    ILogger<NewsPipelineJob> logger)
{
    private static readonly string[] GeneralQueries =
    [
        "FIBRAs Mexico BMV",
        "mercado inmobiliario México renta",
    ];

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var status = "Failed";
        var fetched = 0;
        var filteredIn = 0;
        var saved = 0;
        var errors = 0;
        string? details = null;

        try
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
                try
                {
                    await pipelineErrorLogRepo.LogErrorAsync(new PipelineErrorLog
                    {
                        Pipeline = "News",
                        Timestamp = DateTimeOffset.UtcNow,
                        ErrorType = ex.GetType().Name.Length > 100 ? ex.GetType().Name[..100] : ex.GetType().Name,
                        Message = $"Fallo leyendo AiModeConfig, usando Off como fallback: {ex.Message}",
                        AiContext = "El pipeline de noticias no pudo leer la configuración de modo IA. Se usó modo Off como fallback para esta ejecución.",
                    }, CancellationToken.None);
                }
                catch (Exception logEx) { logger.LogWarning(logEx, "No se pudo guardar PipelineErrorLog para fallo de AiModeConfig."); }
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
            var (filteredItems, titleDuplicates) = NewsDeduplicator.FilterSeparatingDuplicates(allItems, existingUrls, recentTitles, blocklistTerms);

            fetched = allItems.Count;
            filteredIn = filteredItems.Count;

            foreach (var dup in titleDuplicates)
            {
                try
                {
                    var dupArticle = new NewsArticle
                    {
                        Title = dup.Title,
                        TitleNormalized = NewsDeduplicator.NormalizeTitle(dup.Title),
                        Source = dup.Source,
                        PublishedAt = dup.PublishedAt,
                        Url = dup.Url,
                        Snippet = dup.Snippet,
                        Status = NewsArticleStatus.Processed,
                        CapturedAt = DateTimeOffset.UtcNow,
                        DeletedAt = DateTimeOffset.UtcNow,
                    };
                    var dupFibraIds = NewsAssociator.Associate(dup, fibraMatchInfos);
                    await newsRepo.AddWithLinksAsync(dupArticle, dupFibraIds, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to save title-duplicate news article '{Url}'", dup.Url);
                }
            }

            var providerConfig = await aiProviderConfigRepo.GetConfigAsync(ct);

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
                    bodyText = NormalizeBodyText(bodyText);

                    string? aiSummary = null;
                    string? aiAnalysisJson = null;
                    var finalStatus = NewsArticleStatus.Processed;

                    if (currentMode == AiMode.On)
                    {
                        try
                        {
                            var analysis = await analysisService.GenerateAnalysisAsync(
                                item.Title, item.Snippet, bodyText, ct);
                            if (analysis is not null)
                            {
                                aiAnalysisJson = JsonSerializer.Serialize(analysis);
                                aiSummary = analysis.SummaryMarkdown;
                                finalStatus = NewsArticleStatus.Processed;
                            }
                            else
                            {
                                logger.LogWarning("AI analysis returned null for '{Url}'; article saved with Partial status", item.Url);
                                finalStatus = NewsArticleStatus.Partial;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "AI analysis failed for '{Url}'; article saved without analysis", item.Url);
                            var aiErrorType = ex.GetType().Name;
                            var aiContext = $"El pipeline de noticias falló al generar el análisis de IA para el artículo '{item.Title}' desde '{item.Url}' con fuente {item.Source}. El proveedor activo era {providerConfig.Provider}/{providerConfig.ModelId} y el artículo {(string.IsNullOrWhiteSpace(bodyText) ? "no tenía" : "sí tenía")} body_text disponible al momento del error. El artículo se guardará como Partial para permitir revisión operativa posterior.";
                            try
                            {
                                await pipelineErrorLogRepo.LogErrorAsync(new PipelineErrorLog
                                {
                                    Pipeline = "News",
                                    Timestamp = DateTimeOffset.UtcNow,
                                    ErrorType = aiErrorType.Length > 100 ? aiErrorType[..100] : aiErrorType,
                                    Message = ex.Message,
                                    Context = JsonSerializer.Serialize(new
                                    {
                                        item.Title,
                                        item.Url,
                                        item.Source,
                                        currentMode = currentMode.ToString(),
                                        provider = providerConfig.Provider.ToString(),
                                        model = providerConfig.ModelId,
                                        hasBodyText = !string.IsNullOrWhiteSpace(bodyText),
                                    }),
                                    AiContext = aiContext.Length > 800 ? aiContext[..800] : aiContext,
                                }, ct);
                            }
                            catch (Exception logEx)
                            {
                                logger.LogWarning(logEx, "Failed to write pipeline error log entry for AI analysis failure on '{Url}'", item.Url);
                            }
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
                        AiAnalysisJson = aiAnalysisJson,
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
                    var saveErrorType = ex.GetType().Name;
                    var saveAiContext = $"El pipeline de noticias falló al persistir el artículo '{item.Title}' desde la URL '{item.Url}' con fuente {item.Source}. El artículo ya había pasado blocklist y deduplicación, por lo que el fallo ocurrió en el guardado final o en la asociación con FIBRAs dentro de la base de datos. Revise restricciones, datos nulos inesperados o errores transitorios de persistencia.";
                    try
                    {
                        await pipelineErrorLogRepo.LogErrorAsync(new PipelineErrorLog
                        {
                            Pipeline = "News",
                            Timestamp = DateTimeOffset.UtcNow,
                            ErrorType = saveErrorType.Length > 100 ? saveErrorType[..100] : saveErrorType,
                            Message = ex.Message,
                            Context = JsonSerializer.Serialize(new
                            {
                                item.Title,
                                item.Url,
                                item.Source,
                            }),
                            AiContext = saveAiContext.Length > 800 ? saveAiContext[..800] : saveAiContext,
                        }, ct);
                    }
                    catch (Exception logEx)
                    {
                        logger.LogWarning(logEx, "Failed to write pipeline error log entry for save failure on '{Url}'", item.Url);
                    }
                    errors++;
                }
            }

            logger.LogInformation(
                "News pipeline complete — fetched: {Fetched}, filtered_in: {FilteredIn}, saved: {Saved}, errors: {Errors}",
                fetched,
                filteredIn,
                saved,
                errors);

            status = "Completed";
            details = JsonSerializer.Serialize(new
            {
                fetched,
                filteredIn,
                saved,
                errors,
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            details ??= JsonSerializer.Serialize(new
            {
                fetched,
                filteredIn,
                saved,
                errors,
            });
            throw;
        }
        finally
        {
            try
            {
                await pipelineRunLogRepo.AddAsync(new PipelineRunLog
                {
                    Pipeline = "News",
                    StartedAt = startedAt,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Status = status,
                    ItemsProcessed = saved,
                    ErrorCount = errors,
                    Details = details,
                }, CancellationToken.None);
            }
            catch (Exception logEx)
            {
                logger.LogWarning(logEx, "Failed to write PipelineRunLog for News pipeline");
            }
        }
    }

    private static IEnumerable<string> BuildFibraQueries(Fibra fibra)
    {
        yield return $"{fibra.Ticker} FIBRA";

        foreach (var variant in fibra.NameVariants.Where(v => !string.Equals(v, fibra.Ticker, StringComparison.OrdinalIgnoreCase)))
            yield return $"{variant} FIBRA México";
    }

    private static string? NormalizeBodyText(string? bodyText)
    {
        var normalized = NewsTextNormalizer.Normalize(bodyText);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

}
