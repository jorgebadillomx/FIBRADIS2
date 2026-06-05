namespace Domain.Portfolio;

public class UserOpportunityWeights
{
    public Guid UserId { get; set; }
    public string? WeightsJson { get; set; }
    public string Profile { get; set; } = "default";
    public DateTimeOffset UpdatedAt { get; set; }
}
