using System.Net;
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

        IReadOnlyDictionary<string, Snapshot?>? snapshots = null;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                snapshots = await yahooQuotes.GetSnapshotAsync(symbols, ct);
                break;
            }
            catch (Exception ex) when (Is429(ex))
            {
                if (attempt == 1)
                {
                    logger.LogWarning("Yahoo Finance devolvió 429 en batch; reintentando en 60s");
                    await Task.Delay(TimeSpan.FromSeconds(60), ct);
                }
                else
                {
                    logger.LogWarning("Yahoo Finance devolvió 429 en segundo intento batch; cambiando a modo individual ({Count} tickers)", symbols.Count);
                    return await GetQuotesIndividuallyAsync(symbols, ct);
                }
            }
        }

        snapshots ??= new Dictionary<string, Snapshot?>();

        var nullSymbols = snapshots.Where(kv => kv.Value is null).Select(kv => kv.Key).ToList();
        if (nullSymbols.Count > 0)
            logger.LogWarning("Yahoo devolvió null para {Count}/{Total} símbolos: {Symbols}",
                nullSymbols.Count, snapshots.Count, string.Join(", ", nullSymbols));

        return BuildResults(snapshots);
    }

    private async Task<IReadOnlyList<YahooQuoteResult>> GetQuotesIndividuallyAsync(
        IList<string> symbols,
        CancellationToken ct)
    {
        var results = new List<YahooQuoteResult>(symbols.Count);
        for (var i = 0; i < symbols.Count; i++)
        {
            if (i > 0)
                await Task.Delay(TimeSpan.FromSeconds(10), ct);

            var symbol = symbols[i];
            try
            {
                var snapshots = await yahooQuotes.GetSnapshotAsync([symbol], ct);
                results.AddRange(BuildResults(snapshots));
                logger.LogDebug("Yahoo individual OK: {Symbol} ({Index}/{Total})", symbol, i + 1, symbols.Count);
            }
            catch (Exception ex) when (Is429(ex))
            {
                logger.LogWarning("Yahoo devolvió 429 para {Symbol} en modo individual; esperando 30s adicionales", symbol);
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Error obteniendo cotización individual para {Symbol}", symbol);
            }
        }

        logger.LogInformation("Modo individual completado: {Ok}/{Total} tickers obtenidos", results.Count, symbols.Count);
        return results;
    }

    private static List<YahooQuoteResult> BuildResults(IReadOnlyDictionary<string, Snapshot?> snapshots)
    {
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

        const int maxRetries = 3;
        YahooQuotesApi.History? history = null;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await historyClient.Inner.GetHistoryAsync(yahooTicker);
                if (result.HasValue)
                {
                    history = result.Value;
                    break;
                }
            }
            catch (Exception ex) when (Is429(ex))
            {
                logger.LogWarning("Yahoo devolvió 429 para {Ticker}, intento {Attempt}/{MaxRetries}", yahooTicker, attempt, maxRetries);
            }

            if (attempt < maxRetries)
                await Task.Delay(TimeSpan.FromSeconds(attempt * 30), ct);
        }

        if (history is null)
        {
            logger.LogWarning("Yahoo no devolvió historial para {Ticker} tras {MaxRetries} intentos", yahooTicker, maxRetries);
            return [];
        }

        var ticks = history.Ticks;
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

    private static bool Is429(Exception ex)
    {
        var current = ex;
        while (current is not null)
        {
            if (current is HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests })
                return true;
            current = current.InnerException;
        }
        return false;
    }
}
