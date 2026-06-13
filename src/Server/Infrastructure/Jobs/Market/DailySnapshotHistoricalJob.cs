using System.Text.Json;
using Application.Catalog;
using Application.Jobs;
using Application.Market;
using Domain.Catalog;
using Domain.Jobs;
using Domain.Market;
using Hangfire;
using Infrastructure.Integrations.Yahoo;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.Market;

public class DailySnapshotHistoricalJob(
    IFibraRepository fibraRepo,
    IYahooFinanceClient yahooClient,
    IMarketRepository marketRepo,
    IPipelineRunLogRepository runLogRepo,
    IPipelineErrorLogRepository errorLogRepo,
    ILogger<DailySnapshotHistoricalJob> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 0)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var status = "Failed";
        var inserted = 0;
        var errors = 0;

        try
        {
            (inserted, errors) = await ExecuteCoreAsync(ct);
            status = "Completed";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DailySnapshotHistoricalJob: error inesperado");
            errors++;
            await TryLogErrorAsync("unexpected", null, $"Error inesperado en DailySnapshotHistoricalJob: {ex.Message}", ex);
        }
        finally
        {
            await TryLogRunAsync(startedAt, status, inserted, errors);
        }
    }

    private async Task<(int Inserted, int Errors)> ExecuteCoreAsync(CancellationToken ct)
    {
        int inserted = 0, skipped = 0, errors = 0;
        var defaultHistoryStart = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-4));
        var fibras = await fibraRepo.GetAllActiveAsync(ct);
        if (fibras.Count == 0)
        {
            logger.LogDebug("No active fibras found, skipping daily snapshot historical job");
            return (inserted, errors);
        }

        async Task<(int inserted, int skipped, int errors)> ProcessFibraAsync(Fibra fibra, DateOnly historyStart)
        {
            var localInserted = 0;
            var localSkipped = 0;
            var localErrors = 0;

            try
            {
                ct.ThrowIfCancellationRequested();
                var candles = await yahooClient.GetOhlcvHistoryAsync(
                    fibra.YahooTicker, historyStart, ct);

                foreach (var candle in candles)
                {
                    if (candle.Close <= 0)
                        continue;

                    var snapshot = new DailySnapshot
                    {
                        FibraId = fibra.Id,
                        Ticker = fibra.Ticker,
                        Date = candle.Date,
                        Open = candle.Open > 0 ? candle.Open : candle.Close,
                        High = candle.High > 0 ? candle.High : candle.Close,
                        Low = candle.Low > 0 ? candle.Low : candle.Close,
                        Close = candle.Close,
                        Volume = candle.Volume,
                    };

                    var wasInserted = await marketRepo.UpsertDailySnapshotAsync(snapshot, ct);
                    if (wasInserted) localInserted++;
                    else localSkipped++;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to fetch OHLCV history for {Ticker} ({YahooTicker})",
                    fibra.Ticker,
                    fibra.YahooTicker);
                localErrors++;
                await TryLogErrorAsync(fibra.Ticker, fibra.YahooTicker, $"No se pudo obtener historial OHLCV para {fibra.Ticker} ({fibra.YahooTicker}): {ex.Message}", ex);
            }

            return (localInserted, localSkipped, localErrors);
        }

        foreach (var (fibra, index) in fibras.Select((f, i) => (f, i)))
        {
            if (index > 0)
                await Task.Delay(TimeSpan.FromSeconds(1.5), ct);

            var lastDate = await marketRepo.GetLatestDailySnapshotDateAsync(fibra.Id, ct);
            var result = await ProcessFibraAsync(fibra, lastDate ?? defaultHistoryStart);
            inserted += result.inserted;
            skipped += result.skipped;
            errors += result.errors;
        }

        var benchmarkTickers = new[] { "^MXX", "^GSPC" };
        foreach (var ticker in benchmarkTickers)
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5), ct);

            var benchmark = await fibraRepo.GetByTickerAsync(ticker, ct);
            if (benchmark is null)
            {
                logger.LogWarning("Benchmark {Ticker} not found in Fibras table, skipping", ticker);
                continue;
            }

            var lastDate = await marketRepo.GetLatestDailySnapshotDateAsync(benchmark.Id, ct);
            var result = await ProcessFibraAsync(benchmark, lastDate ?? defaultHistoryStart);
            inserted += result.inserted;
            skipped += result.skipped;
            errors += result.errors;
        }

        logger.LogInformation(
            "Daily snapshot historical job complete — inserted: {Inserted}, skipped: {Skipped}, errors: {Errors}",
            inserted,
            skipped,
            errors);

        return (inserted, errors);
    }

    private async Task TryLogRunAsync(DateTimeOffset startedAt, string status, int processed, int errors)
    {
        try
        {
            await runLogRepo.AddAsync(new PipelineRunLog
            {
                Pipeline = "DailySnapshot",
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                Status = status,
                ItemsProcessed = processed,
                ErrorCount = errors,
                Details = JsonSerializer.Serialize(new { inserted = processed, errors }),
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DailySnapshotHistoricalJob: fallo al escribir PipelineRunLog");
        }
    }

    private async Task TryLogErrorAsync(string ticker, string? yahooTicker, string message, Exception? ex)
    {
        try
        {
            await errorLogRepo.LogErrorAsync(new PipelineErrorLog
            {
                Pipeline = "DailySnapshot",
                Timestamp = DateTimeOffset.UtcNow,
                ErrorType = ex?.GetType().Name ?? "Error",
                Message = message.Length > 500 ? message[..500] : message,
                Context = JsonSerializer.Serialize(new { ticker, yahooTicker }),
                AiContext = $"DailySnapshotHistoricalJob falló al procesar {ticker} ({yahooTicker ?? "?"}) desde Yahoo Finance. {message} Revise conectividad con Yahoo Finance, throttling o cambios en el ticker.",
            }, CancellationToken.None);
        }
        catch (Exception logEx)
        {
            logger.LogWarning(logEx, "DailySnapshotHistoricalJob: fallo al escribir PipelineErrorLog");
        }
    }
}
