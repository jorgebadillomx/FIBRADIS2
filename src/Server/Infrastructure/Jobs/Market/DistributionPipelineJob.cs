using Application.Catalog;
using Application.Market;
using Domain.Market;
using Hangfire;
using Infrastructure.Integrations.Yahoo;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.Market;

public class DistributionPipelineJob(
    IFibraRepository fibraRepo,
    IYahooFinanceClient yahooClient,
    IMarketRepository marketRepo,
    ILogger<DistributionPipelineJob> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 0)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var historyStart = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5));
        var fibras = await fibraRepo.GetAllActiveAsync(ct);
        if (fibras.Count == 0)
        {
            logger.LogDebug("No active fibras found, skipping distribution pipeline");
            return;
        }

        int inserted = 0, skipped = 0, errors = 0;

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
                errors++;
            }
        }

        logger.LogInformation(
            "Distribution pipeline complete — inserted: {Inserted}, skipped: {Skipped}, errors: {Errors}",
            inserted, skipped, errors);
    }
}
