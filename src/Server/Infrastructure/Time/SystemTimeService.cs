namespace Infrastructure.Time;

public class SystemTimeService : ITimeService
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
