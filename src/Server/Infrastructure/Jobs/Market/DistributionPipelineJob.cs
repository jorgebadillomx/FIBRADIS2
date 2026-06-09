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
        string? details = null;
        try
        {
            var historyStart = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5));
            var fibras = await fibraRepo.GetAllActiveAsync(ct);
            if (fibras.Count == 0)
            {
                logger.LogDebug("No active fibras found, skipping distribution pipeline");
                status = "Completed";
                details = JsonSerializer.Serialize(new { processed = inserted, errors });
                return;
            }

            foreach (var fibra in fibras)
            {
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

            logger.LogInformation(
                "Distribution pipeline complete — inserted: {Inserted}, skipped: {Skipped}, errors: {Errors}",
                inserted, skipped, errors);

            status = "Completed";
            details = JsonSerializer.Serialize(new
            {
                processed = inserted,
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
                processed = inserted,
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
