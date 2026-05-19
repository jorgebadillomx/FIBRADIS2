namespace Infrastructure.Time;

public interface ITimeService
{
    DateTimeOffset UtcNow { get; }
}
