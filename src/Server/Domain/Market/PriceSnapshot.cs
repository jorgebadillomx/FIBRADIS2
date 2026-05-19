namespace Domain.Market;

public class PriceSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FibraId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal? LastPrice { get; set; }
    public decimal? DailyChange { get; set; }
    public decimal? DailyChangePct { get; set; }
    public long? Volume { get; set; }
    public decimal? Week52High { get; set; }
    public decimal? Week52Low { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public MarketDataStatus Status { get; set; }
    public string? ErrorReason { get; set; }
}
