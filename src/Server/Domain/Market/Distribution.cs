namespace Domain.Market;

public class Distribution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FibraId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public DateOnly? ExDividendDate { get; set; }
    public decimal AmountPerUnit { get; set; }
    public decimal? TaxableAmount { get; set; }
    public decimal? CapitalReturnAmount { get; set; }
    public string? AvisoUrl { get; set; }
    public string Currency { get; set; } = "MXN";
    public string Source { get; set; } = "seed";
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
}
