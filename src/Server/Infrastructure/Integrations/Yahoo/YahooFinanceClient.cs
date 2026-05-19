using System.Text.Json;
using System.Text.Json.Nodes;

namespace Infrastructure.Integrations.Yahoo;

public class YahooFinanceClient(HttpClient httpClient) : IYahooFinanceClient
{
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<YahooQuoteResult>> GetQuotesAsync(
        IEnumerable<string> yahooTickers,
        CancellationToken ct = default)
    {
        var symbols = string.Join(",", yahooTickers);
        if (string.IsNullOrEmpty(symbols))
            return [];

        var response = await httpClient.GetAsync(
            $"/v7/finance/quote?symbols={Uri.EscapeDataString(symbols)}&fields=regularMarketPrice,regularMarketChange,regularMarketChangePercent,regularMarketVolume,fiftyTwoWeekHigh,fiftyTwoWeekLow,regularMarketOpen,regularMarketDayHigh,regularMarketDayLow",
            ct);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        return ParseQuotes(json);
    }

    private static IReadOnlyList<YahooQuoteResult> ParseQuotes(string json)
    {
        try
        {
            var root = JsonNode.Parse(json);
            var results = root?["quoteResponse"]?["result"]?.AsArray();
            if (results is null)
                return [];

            var quotes = new List<YahooQuoteResult>();
            foreach (var item in results)
            {
                if (item is null) continue;
                var symbol = item["symbol"]?.GetValue<string>() ?? string.Empty;
                quotes.Add(new YahooQuoteResult(
                    Symbol: symbol,
                    LastPrice: GetDecimal(item, "regularMarketPrice"),
                    DailyChange: GetDecimal(item, "regularMarketChange"),
                    DailyChangePct: GetDecimal(item, "regularMarketChangePercent"),
                    Volume: GetLong(item, "regularMarketVolume"),
                    Week52High: GetDecimal(item, "fiftyTwoWeekHigh"),
                    Week52Low: GetDecimal(item, "fiftyTwoWeekLow"),
                    Open: GetDecimal(item, "regularMarketOpen"),
                    DayHigh: GetDecimal(item, "regularMarketDayHigh"),
                    DayLow: GetDecimal(item, "regularMarketDayLow")));
            }
            return quotes;
        }
        catch
        {
            return [];
        }
    }

    private static decimal? GetDecimal(JsonNode? node, string key)
    {
        try { return node?[key]?.GetValue<decimal>(); }
        catch { return null; }
    }

    private static long? GetLong(JsonNode? node, string key)
    {
        try { return node?[key]?.GetValue<long>(); }
        catch { return null; }
    }
}
