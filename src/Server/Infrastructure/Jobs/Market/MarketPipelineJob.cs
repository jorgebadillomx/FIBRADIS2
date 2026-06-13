using System.Text.Json;
using Application.Catalog;
using Application.Jobs;
using Application.Market;
using Domain.Jobs;
using Domain.Market;
using Infrastructure.Integrations.Yahoo;
using Infrastructure.Time;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.Market;

public class MarketPipelineJob(
    IBmvSchedule bmvSchedule,
    ITimeService timeService,
    IFibraRepository fibraRepo,
    IYahooFinanceClient yahooClient,
    IMarketRepository marketRepo,
    IPipelineErrorLogRepository pipelineErrorLogRepo,
    IPipelineRunLogRepository pipelineRunLogRepo,
    ILogger<MarketPipelineJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default) =>
        await ExecuteAsync(forceRun: false, ct);

    public async Task ExecuteAsync(bool forceRun, CancellationToken ct = default)
    {
        var startedAt = timeService.UtcNow;
        var status = "Failed";
        var processed = 0;
        var errors = 0;
        var critical = 0;
        var totalFibras = 0;
        string? details = null;
        try
        {
            if (!forceRun && !bmvSchedule.IsTradingHours(startedAt))
            {
                logger.LogDebug("Outside BMV hours, skipping market pipeline");
                status = "Completed";
                details = JsonSerializer.Serialize(new
                {
                    processed,
                    errors,
                    critical,
                    totalFibras,
                    skipped = true,
                    reason = "outside-trading-hours",
                });
                return;
            }

            var fibras = await fibraRepo.GetAllActiveAsync(ct);
            totalFibras = fibras.Count;
            if (fibras.Count == 0)
            {
                logger.LogDebug("No active fibras found, skipping market pipeline");
                status = "Completed";
                details = JsonSerializer.Serialize(new
                {
                    processed,
                    errors,
                    critical,
                    totalFibras,
                    skipped = true,
                    reason = "no-active-fibras",
                });
                return;
            }

            var capturedAt = timeService.UtcNow;
            IReadOnlyList<YahooQuoteResult> quotes = [];
            var batchFailed = false;

            try
            {
                quotes = await yahooClient.GetQuotesAsync(fibras.Select(f => f.YahooTicker), ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Yahoo Finance batch request failed for all fibras");
                var batchErrorType = ex.GetType().Name;
                var batchAiContext = $"El pipeline de mercado intentó obtener cotizaciones para {fibras.Count} FIBRAs activas desde Yahoo Finance y falló en la solicitud batch inicial. No se recibieron datos utilizables antes de procesar tickers individuales. Revise conectividad HTTP, throttling o cambios en la respuesta del proveedor.";
                try
                {
                    await pipelineErrorLogRepo.LogErrorAsync(new PipelineErrorLog
                    {
                        Pipeline = "Market",
                        Timestamp = capturedAt,
                        ErrorType = batchErrorType.Length > 100 ? batchErrorType[..100] : batchErrorType,
                        Message = BuildExceptionChain(ex) is var batchChain && batchChain.Length > 500 ? batchChain[..500] : batchChain,
                        Context = JsonSerializer.Serialize(new
                        {
                            totalTickers = fibras.Count,
                            yahooTickers = fibras.Select(f => f.YahooTicker).ToArray(),
                            exceptionChain = BuildExceptionChain(ex),
                        }),
                        AiContext = batchAiContext.Length > 800 ? batchAiContext[..800] : batchAiContext,
                    }, ct);
                }
                catch (Exception logEx)
                {
                    logger.LogWarning(logEx, "Failed to write pipeline error log entry for batch failure");
                }

                batchFailed = true;
            }

            var quotesBySymbol = quotes.ToDictionary(
                q => q.Symbol,
                q => q,
                StringComparer.OrdinalIgnoreCase);

            foreach (var fibra in fibras)
            {
                PriceSnapshot snapshot;

                if (!batchFailed && quotesBySymbol.TryGetValue(fibra.YahooTicker, out var quote))
                {
                    snapshot = new PriceSnapshot
                    {
                        FibraId = fibra.Id,
                        Ticker = fibra.Ticker,
                        LastPrice = quote.LastPrice,
                        DailyChange = quote.DailyChange,
                        DailyChangePct = quote.DailyChangePct,
                        Volume = quote.Volume,
                        Week52High = quote.Week52High,
                        Week52Low = quote.Week52Low,
                        CapturedAt = capturedAt,
                        Status = MarketDataStatus.Processed,
                    };

                    await marketRepo.AddPriceSnapshotAsync(snapshot, ct);

                    processed++;
                }
                else
                {
                    var prev = await marketRepo.GetLastSnapshotsAsync(fibra.Id, 1, ct);
                    var prevFailed = prev.FirstOrDefault()?.Status is MarketDataStatus.Error or MarketDataStatus.Critical;
                    var snapshotStatus = prevFailed ? MarketDataStatus.Critical : MarketDataStatus.Error;
                    var reason = batchFailed ? "Batch request failed" : $"Symbol {fibra.Ticker}.MX not found in response";

                    snapshot = new PriceSnapshot
                    {
                        FibraId = fibra.Id,
                        Ticker = fibra.Ticker,
                        CapturedAt = capturedAt,
                        Status = snapshotStatus,
                        ErrorReason = reason,
                    };

                    await marketRepo.AddPriceSnapshotAsync(snapshot, ct);
                    var tickerAiContext = $"El pipeline de mercado procesó un lote de {fibras.Count} FIBRAs y no pudo actualizar el precio de {fibra.Ticker} usando el símbolo {fibra.YahooTicker}. El estado previo indicaba {(prevFailed ? "fallas recientes" : "operación normal")} y por eso se marcó el snapshot como {snapshotStatus}. El paso exacto que falló fue la resolución de la cotización dentro de la respuesta de Yahoo Finance.";
                    try
                    {
                        await pipelineErrorLogRepo.LogErrorAsync(new PipelineErrorLog
                        {
                            Pipeline = "Market",
                            Timestamp = capturedAt,
                            ErrorType = batchFailed ? "BatchRequestFailed" : "MissingTickerQuote",
                            Message = reason,
                            Context = JsonSerializer.Serialize(new
                            {
                                fibra.Id,
                                fibra.Ticker,
                                fibra.YahooTicker,
                                capturedAt,
                                batchFailed,
                            }),
                            AiContext = tickerAiContext.Length > 800 ? tickerAiContext[..800] : tickerAiContext,
                        }, ct);
                    }
                    catch (Exception logEx)
                    {
                        logger.LogWarning(logEx, "Failed to write pipeline error log entry for ticker {Ticker}", fibra.Ticker);
                    }

                    if (snapshotStatus == MarketDataStatus.Critical) critical++;
                    else errors++;
                }
            }

            if (!batchFailed && processed > 0)
            {
                var retentionCutoff = DateOnly.FromDateTime(timeService.UtcNow.UtcDateTime).AddDays(-1);
                try
                {
                    await marketRepo.DeleteOldPriceSnapshotsAsync(retentionCutoff, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to delete old price snapshots before cutoff {Cutoff}",
                        retentionCutoff);
                }
            }

            logger.LogInformation(
                "Market pipeline complete — processed: {Processed}, errors: {Errors}, critical: {Critical}",
                processed, errors, critical);

            status = "Completed";
            details = JsonSerializer.Serialize(new
            {
                processed,
                errors,
                critical,
                totalFibras,
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
                processed,
                errors,
                critical,
                totalFibras,
            });
            throw;
        }
        finally
        {
            try
            {
                await pipelineRunLogRepo.AddAsync(new PipelineRunLog
                {
                    Pipeline = "Market",
                    StartedAt = startedAt,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Status = status,
                    ItemsProcessed = processed,
                    ErrorCount = errors + critical,
                    Details = details,
                }, CancellationToken.None);
            }
            catch (Exception logEx)
            {
                logger.LogWarning(logEx, "Failed to write PipelineRunLog for Market pipeline");
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
