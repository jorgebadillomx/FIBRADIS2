using Domain.Market;

namespace Domain.Tests.Market;

public class DailySnapshotTests
{
    [Fact]
    public void MergeUpdate_PreservesOpen()
    {
        var existing = new DailySnapshot { Open = 100m, High = 105m, Low = 98m, Close = 102m, Volume = 1000L };
        var incoming = new DailySnapshot { Open = 99m, High = 110m, Low = 97m, Close = 108m, Volume = 2000L };

        existing.MergeUpdate(incoming);

        Assert.Equal(100m, existing.Open);
    }

    [Fact]
    public void MergeUpdate_TakesHigherHigh_WhenIncomingIsHigher()
    {
        var existing = new DailySnapshot { High = 105m, Low = 98m, Close = 102m, Volume = 1000L };
        var incoming = new DailySnapshot { High = 110m, Low = 97m, Close = 108m, Volume = 2000L };

        existing.MergeUpdate(incoming);

        Assert.Equal(110m, existing.High);
    }

    [Fact]
    public void MergeUpdate_KeepsExistingHigh_WhenExistingIsHigher()
    {
        var existing = new DailySnapshot { High = 115m, Low = 98m, Close = 102m, Volume = 1000L };
        var incoming = new DailySnapshot { High = 110m, Low = 97m, Close = 108m, Volume = 2000L };

        existing.MergeUpdate(incoming);

        Assert.Equal(115m, existing.High);
    }

    [Fact]
    public void MergeUpdate_TakesLowerLow_WhenIncomingIsLower()
    {
        var existing = new DailySnapshot { High = 105m, Low = 98m, Close = 102m, Volume = 1000L };
        var incoming = new DailySnapshot { High = 110m, Low = 95m, Close = 108m, Volume = 2000L };

        existing.MergeUpdate(incoming);

        Assert.Equal(95m, existing.Low);
    }

    [Fact]
    public void MergeUpdate_KeepsExistingLow_WhenExistingIsLower()
    {
        var existing = new DailySnapshot { High = 105m, Low = 93m, Close = 102m, Volume = 1000L };
        var incoming = new DailySnapshot { High = 110m, Low = 98m, Close = 108m, Volume = 2000L };

        existing.MergeUpdate(incoming);

        Assert.Equal(93m, existing.Low);
    }

    [Fact]
    public void MergeUpdate_UpdatesCloseAndVolume()
    {
        var existing = new DailySnapshot { Close = 102m, Volume = 1000L };
        var incoming = new DailySnapshot { Close = 108m, Volume = 2000L };

        existing.MergeUpdate(incoming);

        Assert.Equal(108m, existing.Close);
        Assert.Equal(2000L, existing.Volume);
    }

    [Fact]
    public void MergeUpdate_WhenExistingHighIsNull_TakesIncomingHigh()
    {
        var existing = new DailySnapshot { High = null, Low = 98m, Close = 102m, Volume = 1000L };
        var incoming = new DailySnapshot { High = 110m, Low = 97m, Close = 108m, Volume = 2000L };

        existing.MergeUpdate(incoming);

        Assert.Equal(110m, existing.High);
    }

    [Fact]
    public void MergeUpdate_WhenIncomingLowIsNull_KeepsExistingLow()
    {
        var existing = new DailySnapshot { High = 105m, Low = 93m, Close = 102m, Volume = 1000L };
        var incoming = new DailySnapshot { High = 110m, Low = null, Close = 108m, Volume = 2000L };

        existing.MergeUpdate(incoming);

        Assert.Equal(93m, existing.Low);
    }
}
