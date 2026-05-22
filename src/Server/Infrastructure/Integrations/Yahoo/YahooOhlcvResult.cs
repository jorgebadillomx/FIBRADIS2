namespace Infrastructure.Integrations.Yahoo;

public sealed record YahooOhlcvResult(
    DateOnly Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume);
