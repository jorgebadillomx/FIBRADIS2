namespace Domain.Fundamentals;

public class FundamentalRecord
{
    public Guid Id { get; init; }
    public Guid FibraId { get; init; }
    public string Period { get; init; } = "";
    public string Status { get; set; } = "";
    public string ProcessingMode { get; init; } = "manual";
    public decimal? CapRate { get; init; }
    public decimal? NavPerCbfi { get; init; }
    public decimal? Ltv { get; init; }
    public decimal? NoiMargin { get; init; }
    public decimal? FfoMargin { get; init; }
    public decimal? QuarterlyDistribution { get; init; }
    public string? Summary { get; init; }
    public string? PdfReference { get; set; }
    public DateTimeOffset? PdfUploadedAt { get; set; }
    public bool IsPossibleUpdate { get; init; }
    public string? ImportedBy { get; init; }
    public string? ConfirmedBy { get; set; }
    public DateTimeOffset CapturedAt { get; init; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? ErrorReason { get; init; }
}
