namespace Infrastructure.Integrations.Yahoo;

public interface IYahooFinanceClient
{
    Task<IReadOnlyList<YahooQuoteResult>> GetQuotesAsync(
        IEnumerable<string> yahooTickers,
        CancellationToken ct = default);

    Task<IReadOnlyList<YahooDividendResult>> GetDividendHistoryAsync(
        string yahooTicker,
        DateOnly from,
        CancellationToken ct = default);

    Task<IReadOnlyList<YahooOhlcvResult>> GetOhlcvHistoryAsync(
        string yahooTicker,
        DateOnly from,
        CancellationToken ct = default);
}
