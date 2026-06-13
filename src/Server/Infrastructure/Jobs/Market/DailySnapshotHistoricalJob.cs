using Application.Catalog;
using Application.Market;
using Domain.Catalog;
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
        var defaultHistoryStart = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-4));
        var fibras = await fibraRepo.GetAllActiveAsync(ct);
        if (fibras.Count == 0)
        {
            logger.LogDebug("No active fibras found, skipping daily snapshot historical job");
            return;
        }

        int inserted = 0, skipped = 0, errors = 0;

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
    }
}
