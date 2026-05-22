using Microsoft.Extensions.Logging;
using YahooQuotesApi;

namespace Infrastructure.Integrations.Yahoo;

public class YahooFinanceClient(
    YahooQuotes yahooQuotes,
    ILogger<YahooFinanceClient> logger,
    YahooQuotesHistory? historyClient = null) : IYahooFinanceClient
{
    public async Task<IReadOnlyList<YahooQuoteResult>> GetQuotesAsync(
        IEnumerable<string> yahooTickers,
        CancellationToken ct = default)
    {
        var symbols = yahooTickers.ToList();
        if (symbols.Count == 0)
            return [];

        var snapshots = await yahooQuotes.GetSnapshotAsync(symbols, ct);

        var results = new List<YahooQuoteResult>(snapshots.Count);
        foreach (var (symbol, snapshot) in snapshots)
        {
            if (snapshot is null) continue;
            results.Add(new YahooQuoteResult(
                Symbol: symbol,
                LastPrice: snapshot.RegularMarketPrice,
                DailyChange: snapshot.RegularMarketChange,
                DailyChangePct: (decimal)snapshot.RegularMarketChangePercent,
                Volume: snapshot.RegularMarketVolume,
                Week52High: snapshot.FiftyTwoWeekHigh,
                Week52Low: snapshot.FiftyTwoWeekLow,
                Open: snapshot.RegularMarketOpen,
                DayHigh: snapshot.RegularMarketDayHigh,
                DayLow: snapshot.RegularMarketDayLow));
        }
        return results;
    }

    public async Task<IReadOnlyList<YahooDividendResult>> GetDividendHistoryAsync(
        string yahooTicker,
        DateOnly from,
        CancellationToken ct = default)
    {
        if (historyClient is null)
        {
            logger.LogWarning("YahooQuotesHistory not configured; dividend history unavailable. Verify DI registration.");
            return [];
        }

        ct.ThrowIfCancellationRequested();
        var result = await historyClient.Inner.GetHistoryAsync(yahooTicker);
        if (!result.HasValue) return [];

        var dividends = result.Value.Dividends;
        if (dividends.IsDefaultOrEmpty) return [];

        var cutoff = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return dividends
            .Where(d => d.Date.ToDateTimeUtc() >= cutoff)
            .Where(d => d.Amount is > 0 and <= 1_000_000m)
            .Select(d => new YahooDividendResult(
                DateOnly.FromDateTime(d.Date.ToDateTimeUtc()),
                d.Amount))
            .OrderBy(d => d.PaymentDate)
            .ToList();
    }

    public async Task<IReadOnlyList<YahooOhlcvResult>> GetOhlcvHistoryAsync(
        string yahooTicker,
        DateOnly from,
        CancellationToken ct = default)
    {
        if (historyClient is null)
        {
            logger.LogWarning("YahooQuotesHistory not configured; OHLCV history unavailable. Verify DI registration.");
            return [];
        }

        ct.ThrowIfCancellationRequested();
        var result = await historyClient.Inner.GetHistoryAsync(yahooTicker);
        if (!result.HasValue) return [];

        var ticks = result.Value.Ticks;
        if (ticks.IsDefaultOrEmpty) return [];

        var cutoff = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return ticks
            .Where(t => t.Date.ToDateTimeUtc() >= cutoff && t.Close > 0)
            .Select(t => new YahooOhlcvResult(
                DateOnly.FromDateTime(t.Date.ToDateTimeUtc()),
                t.Open > 0 ? (decimal)t.Open : (decimal)t.Close,
                t.High > 0 ? (decimal)t.High : (decimal)t.Close,
                t.Low > 0 ? (decimal)t.Low : (decimal)t.Close,
                (decimal)t.Close,
                t.Volume))
            .OrderBy(c => c.Date)
            .ToList();
    }
}
