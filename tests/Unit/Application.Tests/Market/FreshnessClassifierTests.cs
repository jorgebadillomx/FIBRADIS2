using Application.Market;
using Domain.Market;

namespace Application.Tests.Market;

public class FreshnessClassifierTests
{
    private static readonly DateTimeOffset _now = new(2026, 5, 19, 20, 0, 0, TimeSpan.Zero);

    private static PriceSnapshot MakeSnapshot(
        DateTimeOffset capturedAt,
        MarketDataStatus status = MarketDataStatus.Processed,
        decimal? lastPrice = 10m)
        => new() { FibraId = Guid.NewGuid(), LastPrice = lastPrice, CapturedAt = capturedAt, Status = status };

    [Fact]
    public void Fresh_WhenAgeUnder20MinAndMarketOpen()
    {
        var snap = MakeSnapshot(_now - TimeSpan.FromMinutes(10));
        Assert.Equal("fresh", FreshnessClassifier.Classify(snap, isMarketOpen: true, _now));
    }

    [Fact]
    public void Stale_WhenAgeBetween20MinAnd6HAndMarketOpen()
    {
        var snap = MakeSnapshot(_now - TimeSpan.FromMinutes(90));
        Assert.Equal("stale", FreshnessClassifier.Classify(snap, isMarketOpen: true, _now));
    }

    [Fact]
    public void Critical_WhenAge6HOrMoreAndMarketOpen()
    {
        var snap = MakeSnapshot(_now - TimeSpan.FromHours(7));
        Assert.Equal("critical", FreshnessClassifier.Classify(snap, isMarketOpen: true, _now));
    }

    [Fact]
    public void Critical_WhenStatusCriticalAndMarketOpenEvenIfAgeUnder20Min()
    {
        var snap = MakeSnapshot(_now - TimeSpan.FromMinutes(5), MarketDataStatus.Critical);
        Assert.Equal("critical", FreshnessClassifier.Classify(snap, isMarketOpen: true, _now));
    }

    [Fact]
    public void OffHours_WhenMarketClosed_RegardlessOfAge()
    {
        var snap = MakeSnapshot(_now - TimeSpan.FromHours(8));
        Assert.Equal("off-hours", FreshnessClassifier.Classify(snap, isMarketOpen: false, _now));
    }

    [Fact]
    public void Null_WhenSnapshotIsNull()
    {
        Assert.Null(FreshnessClassifier.Classify(null, isMarketOpen: true, _now));
    }

    [Fact]
    public void Null_WhenLastPriceIsNull()
    {
        var snap = MakeSnapshot(_now - TimeSpan.FromMinutes(5), lastPrice: null);
        Assert.Null(FreshnessClassifier.Classify(snap, isMarketOpen: true, _now));
    }
}
