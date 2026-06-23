using System.Text.Json;
using Application.Catalog;
using Application.Jobs;
using Application.Market;
using Domain.Jobs;
using Domain.Market;
using Hangfire;
using Infrastructure.Integrations.Yahoo;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.Market;

public class DistributionPipelineJob(
    IFibraRepository fibraRepo,
    IYahooFinanceClient yahooClient,
    IMarketRepository marketRepo,
    IMasDividendosImporterService masDividendosImporter,
    IPipelineErrorLogRepository pipelineErrorLogRepo,
    IPipelineRunLogRepository pipelineRunLogRepo,
    ILogger<DistributionPipelineJob> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 0)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var status = "Failed";
        var inserted = 0;
        var skipped = 0;
        var errors = 0;
        var masDividendosUpdated = 0;
        var masDividendosSkipped = 0;
        var masDividendosUnmatched = 0;
        var masDividendosInserted = 0;
        string? details = null;
        try
        {
            // La ventana de descarga se decide POR FIBRA: una FIBRA sin distribuciones
            // requiere backfill histórico (4 años); una que ya tiene datos solo necesita
            // el mes en curso. Decidirlo con el conteo global dejaba sin histórico a
            // cualquier FIBRA agregada después de la carga inicial.
            var fibraIdsWithDistributions = await marketRepo.GetFibraIdsWithDistributionsAsync(ct);
            var backfillStart = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-4));
            var incrementalStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var historyEnd = new DateOnly(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month,
                DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month));
            var fibras = await fibraRepo.GetAllActiveAsync(ct);
            if (fibras.Count == 0)
            {
                logger.LogDebug("No active fibras found, skipping distribution pipeline");
                status = "Completed";
                details = JsonSerializer.Serialize(new { processed = inserted, errors, masDividendosUpdated, masDividendosSkipped, masDividendosUnmatched });
                return;
            }

            foreach (var (fibra, index) in fibras.Select((f, i) => (f, i)))
            {
                if (index > 0)
                    await Task.Delay(TimeSpan.FromSeconds(1.5), ct);

                var historyStart = fibraIdsWithDistributions.Contains(fibra.Id)
                    ? incrementalStart
                    : backfillStart;

                try
                {
                    ct.ThrowIfCancellationRequested();
                    var dividends = await yahooClient.GetDividendHistoryAsync(
                        fibra.YahooTicker, historyStart, ct);

                    foreach (var div in dividends)
                    {
                        var dist = new Distribution
                        {
                            Id = Guid.NewGuid(),
                            FibraId = fibra.Id,
                            Ticker = fibra.Ticker,
                            PaymentDate = div.PaymentDate,
                            AmountPerUnit = div.AmountPerUnit,
                            Currency = fibra.Currency,
                            Source = "yahoo",
                            CapturedAt = DateTimeOffset.UtcNow,
                        };

                        var wasInserted = await marketRepo.UpsertDistributionAsync(dist, ct);
                        if (wasInserted) inserted++;
                        else skipped++;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Failed to fetch dividend history for {Ticker} ({YahooTicker})",
                        fibra.Ticker, fibra.YahooTicker);
                    var distErrorType = ex.GetType().Name;
                    var distAiContext = $"El pipeline de distribuciones falló al descargar o persistir el historial de dividendos de {fibra.Ticker} usando {fibra.YahooTicker}. El proceso intentaba cubrir un rango histórico desde {historyStart:yyyy-MM-dd} y el error ocurrió dentro del ciclo por FIBRA, no en un fallo global del lote. Revise disponibilidad del ticker, formato del historial o colisiones al guardar distribuciones.";
                    var exChain = BuildExceptionChain(ex);
                    try
                    {
                        await pipelineErrorLogRepo.LogErrorAsync(new PipelineErrorLog
                        {
                            Pipeline = "Distribution",
                            Timestamp = DateTimeOffset.UtcNow,
                            ErrorType = distErrorType.Length > 100 ? distErrorType[..100] : distErrorType,
                            Message = exChain.Length > 500 ? exChain[..500] : exChain,
                            Context = JsonSerializer.Serialize(new
                            {
                                fibra.Id,
                                fibra.Ticker,
                                fibra.YahooTicker,
                                historyStart,
                                exceptionChain = exChain,
                            }),
                            AiContext = distAiContext.Length > 800 ? distAiContext[..800] : distAiContext,
                        }, ct);
                    }
                    catch (Exception logEx)
                    {
                        logger.LogWarning(logEx, "Failed to write pipeline error log entry for {Ticker}", fibra.Ticker);
                    }
                    errors++;
                }
            }

            try
            {
                var importResult = await masDividendosImporter.ImportAsync(fibras, ct);
                masDividendosUpdated += importResult.Updated;
                masDividendosSkipped += importResult.Skipped;
                masDividendosUnmatched += importResult.Unmatched;
                masDividendosInserted += importResult.Inserted;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "MasDividendos import failed during distribution pipeline");
                errors++;
            }

            logger.LogInformation(
                "Distribution pipeline complete — inserted: {Inserted}, skipped: {Skipped}, masdividendos updated: {MasUpdated}, masdividendos inserted: {MasInserted}, masdividendos skipped: {MasSkipped}, unmatched: {MasUnmatched}, errors: {Errors}",
                inserted, skipped, masDividendosUpdated, masDividendosInserted, masDividendosSkipped, masDividendosUnmatched, errors);

            status = "Completed";
            details = JsonSerializer.Serialize(new
            {
                processed = inserted,
                errors,
                masDividendos = new
                {
                    updated = masDividendosUpdated,
                    inserted = masDividendosInserted,
                    skipped = masDividendosSkipped,
                    unmatched = masDividendosUnmatched,
                },
                initialLoad = fibraIdsWithDistributions.Count == 0,
                backfillStart = backfillStart.ToString("yyyy-MM-dd"),
                incrementalStart = incrementalStart.ToString("yyyy-MM-dd"),
                historyEnd = historyEnd.ToString("yyyy-MM-dd"),
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
                processed = inserted,
                errors,
                masDividendos = new
                {
                    updated = masDividendosUpdated,
                    inserted = masDividendosInserted,
                    skipped = masDividendosSkipped,
                    unmatched = masDividendosUnmatched,
                },
            });
            throw;
        }
        finally
        {
            try
            {
                await pipelineRunLogRepo.AddAsync(new PipelineRunLog
                {
                    Pipeline = "Distribution",
                    StartedAt = startedAt,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Status = status,
                    ItemsProcessed = inserted,
                    ErrorCount = errors,
                    Details = details,
                }, CancellationToken.None);
            }
            catch (Exception logEx)
            {
                logger.LogWarning(logEx, "Failed to write PipelineRunLog for Distribution pipeline");
            }
        }
    }

    private static string BuildExceptionChain(Exception ex)
    {
        var parts = new List<string>();
        var current = ex;
        while (current != null)
        {
            parts.Add($"[{current.GetType().Name}] {current.Message}");
            current = current.InnerException;
        }
        return string.Join(" → ", parts);
    }
}
