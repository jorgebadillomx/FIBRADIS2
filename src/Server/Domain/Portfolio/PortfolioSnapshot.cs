namespace Domain.Portfolio;

public class PortfolioSnapshot
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset ArchivedAt { get; set; }
    public string PositionsJson { get; set; } = string.Empty;
}
