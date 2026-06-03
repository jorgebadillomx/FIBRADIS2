namespace Domain.Portfolio;

public class UserPortfolioSettings
{
    public Guid UserId { get; set; }
    public string? ColumnConfigJson { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
