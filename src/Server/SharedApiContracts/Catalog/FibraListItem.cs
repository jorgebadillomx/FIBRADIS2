namespace SharedApiContracts.Catalog;

public record FibraListItem(
    Guid Id,
    string Ticker,
    string FullName,
    string ShortName,
    string Sector,
    string Market,
    string Currency,
    string State,
    string? SiteUrl);
