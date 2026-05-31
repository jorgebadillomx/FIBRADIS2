namespace SharedApiContracts.Catalog;

public sealed record CreateFibraRequest(
    string Ticker,
    string YahooTicker,
    string FullName,
    string ShortName,
    string Sector,
    string Market,
    string Currency,
    string? SiteUrl,
    string? InvestorUrl,
    string? ReportsUrl,
    IReadOnlyList<string>? NameVariants,
    string? Description);
