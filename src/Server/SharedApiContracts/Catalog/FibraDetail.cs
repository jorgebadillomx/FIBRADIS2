namespace SharedApiContracts.Catalog;

public record FibraDetail(
    Guid Id,
    string Ticker,
    string YahooTicker,
    string FullName,
    string ShortName,
    string Sector,
    string Market,
    string Currency,
    string State,
    string? SiteUrl,
    string? InvestorUrl,
    string? ReportsUrl,
    IReadOnlyList<string> NameVariants,
    DateTimeOffset CreatedAt,
    string? Description);
