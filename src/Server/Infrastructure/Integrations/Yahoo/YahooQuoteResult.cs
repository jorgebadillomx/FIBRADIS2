namespace Infrastructure.Integrations.Yahoo;

public record YahooQuoteResult(
    string Symbol,
    decimal? LastPrice,
    decimal? DailyChange,
    decimal? DailyChangePct,
    long? Volume,
    decimal? Week52High,
    decimal? Week52Low,
    decimal? Open,
    decimal? DayHigh,
    decimal? DayLow);
