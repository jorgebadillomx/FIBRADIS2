namespace Domain.Catalog;

public class Fibra
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string YahooTicker { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public FibraState State { get; set; }
    public string? SiteUrl { get; set; }
    public string? InvestorUrl { get; set; }
    public string? ReportsUrl { get; set; }
    public List<string> NameVariants { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public string? Description { get; set; }
}
