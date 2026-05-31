namespace Domain.Ops;

public class OperationalConfig
{
    public int Id { get; set; } = 1;
    public decimal CommissionFactor { get; set; } = 0.006m;
    public int AvgPeriods { get; set; } = 4;
    public int NewsCadenceMinutes { get; set; } = 60;
    public int FibraNewsMonths { get; set; } = 15;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }
}
