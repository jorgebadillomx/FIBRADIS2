using Application.Catalog;
using Application.Market;
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
    ILogger<MarketPipelineJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        if (!bmvSchedule.IsTradingHours(timeService.UtcNow))
        {
            logger.LogDebug("Outside BMV hours, skipping market pipeline");
            return;
        }

        var fibras = await fibraRepo.GetAllActiveAsync(ct);
        if (fibras.Count == 0)
        {
            logger.LogDebug("No active fibras found, skipping market pipeline");
            return;
        }

        var capturedAt = timeService.UtcNow;
        IReadOnlyList<YahooQuoteResult> quotes = [];
        bool batchFailed = false;

        try
        {
            quotes = await yahooClient.GetQuotesAsync(fibras.Select(f => f.YahooTicker), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yahoo Finance batch request failed for all fibras");
            batchFailed = true;
        }

        var quotesBySymbol = quotes.ToDictionary(
            q => q.Symbol,
            q => q,
            StringComparer.OrdinalIgnoreCase);

        int processed = 0, errors = 0, critical = 0;

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

                var daily = new DailySnapshot
                {
                    FibraId = fibra.Id,
                    Ticker = fibra.Ticker,
                    Date = DateOnly.FromDateTime(capturedAt.UtcDateTime),
                    Open = quote.Open,
                    High = quote.DayHigh,
                    Low = quote.DayLow,
                    Close = quote.LastPrice,
                    Volume = quote.Volume,
                };
                await marketRepo.UpsertDailySnapshotAsync(daily, ct);

                processed++;
            }
            else
            {
                var prev = await marketRepo.GetLastSnapshotsAsync(fibra.Id, 1, ct);
                bool prevFailed = prev.FirstOrDefault()?.Status is MarketDataStatus.Error or MarketDataStatus.Critical;
                var status = prevFailed ? MarketDataStatus.Critical : MarketDataStatus.Error;
                var reason = batchFailed ? "Batch request failed" : $"Symbol {fibra.Ticker}.MX not found in response";

                snapshot = new PriceSnapshot
                {
                    FibraId = fibra.Id,
                    Ticker = fibra.Ticker,
                    CapturedAt = capturedAt,
                    Status = status,
                    ErrorReason = reason,
                };

                await marketRepo.AddPriceSnapshotAsync(snapshot, ct);

                if (status == MarketDataStatus.Critical) critical++;
                else errors++;
            }
        }

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

        logger.LogInformation(
            "Market pipeline complete — processed: {Processed}, errors: {Errors}, critical: {Critical}",
            processed, errors, critical);
    }
}
