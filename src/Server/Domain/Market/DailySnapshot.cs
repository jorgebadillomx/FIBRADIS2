namespace Domain.Market;

public class DailySnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FibraId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? Close { get; set; }
    public long? Volume { get; set; }

    public void MergeUpdate(DailySnapshot incoming)
    {
        // Open is the first price of the day — never overwritten after initial insert
        High = High.HasValue && incoming.High.HasValue
            ? Math.Max(High.Value, incoming.High.Value)
            : High ?? incoming.High;
        Low = Low.HasValue && incoming.Low.HasValue
            ? Math.Min(Low.Value, incoming.Low.Value)
            : Low ?? incoming.Low;
        Close = incoming.Close;
        Volume = incoming.Volume;
    }
}
