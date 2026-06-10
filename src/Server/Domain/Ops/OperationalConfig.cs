namespace Domain.Ops;

public class OperationalConfig
{
    public int Id { get; set; } = 1;
    public decimal CommissionFactor { get; set; } = 0.006m;
    public int AvgPeriods { get; set; } = 4;
    public int NewsCadenceMinutes { get; set; } = 1440;
    public int FibraNewsMonths { get; set; } = 15;
    public int FundamentalsCadenceMinutes { get; set; } = 2880;
    public int DistributionCadenceMinutes { get; set; } = 1440;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }
    public bool TermsEnabled { get; set; }
    public string? TermsText { get; set; }
    public string? ContactEmail { get; set; }
    public int UniverseDegradationThresholdPct { get; set; } = 30;
}
