namespace Application.Market;

public interface IBmvSchedule
{
    bool IsTradingHours(DateTimeOffset utcNow);
}
