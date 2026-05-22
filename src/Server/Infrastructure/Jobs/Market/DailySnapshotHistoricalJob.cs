using Application.Catalog;
using Application.Market;
using Domain.Market;
using Hangfire;
using Infrastructure.Integrations.Yahoo;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.Market;

public class DailySnapshotHistoricalJob(
    IFibraRepository fibraRepo,
    IYahooFinanceClient yahooClient,
    IMarketRepository marketRepo,
    ILogger<DailySnapshotHistoricalJob> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 0)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var historyStart = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5));
        var fibras = await fibraRepo.GetAllActiveAsync(ct);
        if (fibras.Count == 0)
        {
            logger.LogDebug("No active fibras found, skipping daily snapshot historical job");
            return;
        }

        int inserted = 0, skipped = 0, errors = 0;

        foreach (var fibra in fibras)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var candles = await yahooClient.GetOhlcvHistoryAsync(
                    fibra.YahooTicker, historyStart, ct);

                foreach (var candle in candles)
                {
                    if (candle.Close <= 0)
                    {
                        continue;
                    }

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
                logger.LogWarning(
                    ex,
                    "Failed to fetch OHLCV history for {Ticker} ({YahooTicker})",
                    fibra.Ticker,
                    fibra.YahooTicker);
                errors++;
            }
        }

        logger.LogInformation(
            "Daily snapshot historical job complete — inserted: {Inserted}, skipped: {Skipped}, errors: {Errors}",
            inserted,
            skipped,
            errors);
    }
}
