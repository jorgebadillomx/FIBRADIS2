using YahooQuotesApi;

namespace Infrastructure.Integrations.Yahoo;

public class YahooFinanceClient(YahooQuotes yahooQuotes) : IYahooFinanceClient
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
}
