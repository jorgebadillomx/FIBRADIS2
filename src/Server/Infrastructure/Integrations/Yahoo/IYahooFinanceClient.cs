namespace Infrastructure.Integrations.Yahoo;

public interface IYahooFinanceClient
{
    Task<IReadOnlyList<YahooQuoteResult>> GetQuotesAsync(
        IEnumerable<string> yahooTickers,
        CancellationToken ct = default);
}
