using Infrastructure.Jobs.Market;

namespace Infrastructure.Tests.Jobs.Market;

public class BmvScheduleTests
{
    private readonly BmvSchedule _sut = new();

    // May 19 2026 is a Tuesday; Mexico City is in CDT (UTC-5) during May
    // 8:15am CDT = 13:15 UTC; 3:15pm CDT = 20:15 UTC

    [Fact]
    public void IsTradingHours_Tuesday10amCdmx_ReturnsTrue()
    {
        // 10:00 CDT = 15:00 UTC
        var utc = new DateTimeOffset(2026, 5, 19, 15, 0, 0, TimeSpan.Zero);
        Assert.True(_sut.IsTradingHours(utc));
    }

    [Fact]
    public void IsTradingHours_Tuesday4pmCdmx_ReturnsFalse()
    {
        // 16:00 CDT = 21:00 UTC
        var utc = new DateTimeOffset(2026, 5, 19, 21, 0, 0, TimeSpan.Zero);
        Assert.False(_sut.IsTradingHours(utc));
    }

    [Fact]
    public void IsTradingHours_Saturday10amCdmx_ReturnsFalse()
    {
        // Saturday May 23 2026, 10:00 CDT = 15:00 UTC
        var utc = new DateTimeOffset(2026, 5, 23, 15, 0, 0, TimeSpan.Zero);
        Assert.False(_sut.IsTradingHours(utc));
    }

    [Fact]
    public void IsTradingHours_Tuesday8_15amCdmx_ReturnsTrue()
    {
        // 8:15 CDT = 13:15 UTC — open boundary is inclusive
        var utc = new DateTimeOffset(2026, 5, 19, 13, 15, 0, TimeSpan.Zero);
        Assert.True(_sut.IsTradingHours(utc));
    }

    [Fact]
    public void IsTradingHours_Tuesday3_15pmCdmx_ReturnsFalse()
    {
        // 15:15 CDT = 20:15 UTC — close boundary is exclusive
        var utc = new DateTimeOffset(2026, 5, 19, 20, 15, 0, TimeSpan.Zero);
        Assert.False(_sut.IsTradingHours(utc));
    }

    [Fact]
    public void IsTradingHours_Tuesday8_14amCdmx_ReturnsFalse()
    {
        // 8:14 CDT = 13:14 UTC — one minute before open
        var utc = new DateTimeOffset(2026, 5, 19, 13, 14, 0, TimeSpan.Zero);
        Assert.False(_sut.IsTradingHours(utc));
    }
}
