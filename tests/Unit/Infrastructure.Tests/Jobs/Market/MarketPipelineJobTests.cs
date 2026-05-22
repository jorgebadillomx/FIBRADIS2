using Application.Catalog;
using Application.Market;
using Domain.Catalog;
using Domain.Market;
using Infrastructure.Integrations.Yahoo;
using Infrastructure.Jobs.Market;
using Infrastructure.Time;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs.Market;

public class MarketPipelineJobTests
{
    private static readonly DateTimeOffset _tradingUtc = new(2026, 5, 19, 15, 0, 0, TimeSpan.Zero); // 10am CDT
    private static readonly Fibra _fibraFuno = new() { Id = Guid.NewGuid(), Ticker = "FUNO11", YahooTicker = "FUNO11.MX", State = FibraState.Active };
    private static readonly Fibra _fibraFmty = new() { Id = Guid.NewGuid(), Ticker = "FMTY14", YahooTicker = "FMTY14.MX", State = FibraState.Active };

    private static MarketPipelineJob Build(
        FakeBmvSchedule bmv,
        FakeTimeService time,
        FakeFibraRepository fibraRepo,
        FakeYahooClient yahoo,
        FakeMarketRepository marketRepo)
        => new(bmv, time, fibraRepo, yahoo, marketRepo, NullLogger<MarketPipelineJob>.Instance);

    [Fact]
    public async Task ExecuteAsync_WhenOutsideTradingHours_DoesNotCallYahooClient()
    {
        var yahoo = new FakeYahooClient();
        var job = Build(
            new FakeBmvSchedule(isTradingHours: false),
            new FakeTimeService(_tradingUtc),
            new FakeFibraRepository([_fibraFuno]),
            yahoo,
            new FakeMarketRepository());

        await job.ExecuteAsync();

        Assert.Equal(0, yahoo.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAllFibrasSucceed_PersistsSnapshotsWithStatusProcessed()
    {
        var marketRepo = new FakeMarketRepository();
        var yahoo = new FakeYahooClient(
            new YahooQuoteResult("FUNO11.MX", 50m, 0.5m, 1.0m, 100_000L, 55m, 45m, 49m, 51m, 48m));

        var job = Build(
            new FakeBmvSchedule(true),
            new FakeTimeService(_tradingUtc),
            new FakeFibraRepository([_fibraFuno]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Single(marketRepo.AddedSnapshots);
        Assert.Equal(MarketDataStatus.Processed, marketRepo.AddedSnapshots[0].Status);
        Assert.Equal(50m, marketRepo.AddedSnapshots[0].LastPrice);
        Assert.Single(marketRepo.UpsertedDailies);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOneFibraFails_OtherFibrasSucceed()
    {
        var marketRepo = new FakeMarketRepository();
        var yahoo = new FakeYahooClient(
            new YahooQuoteResult("FUNO11.MX", 50m, 0.5m, 1.0m, 100_000L, 55m, 45m, 49m, 51m, 48m));
        // FMTY14.MX not in response → error

        var job = Build(
            new FakeBmvSchedule(true),
            new FakeTimeService(_tradingUtc),
            new FakeFibraRepository([_fibraFuno, _fibraFmty]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Equal(2, marketRepo.AddedSnapshots.Count);
        var funo = marketRepo.AddedSnapshots.First(s => s.Ticker == "FUNO11");
        var fmty = marketRepo.AddedSnapshots.First(s => s.Ticker == "FMTY14");
        Assert.Equal(MarketDataStatus.Processed, funo.Status);
        Assert.Equal(MarketDataStatus.Error, fmty.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFirstFailure_StatusIsError()
    {
        var marketRepo = new FakeMarketRepository(); // no previous snapshots
        var yahoo = new FakeYahooClient(); // empty response

        var job = Build(
            new FakeBmvSchedule(true),
            new FakeTimeService(_tradingUtc),
            new FakeFibraRepository([_fibraFuno]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Single(marketRepo.AddedSnapshots);
        Assert.Equal(MarketDataStatus.Error, marketRepo.AddedSnapshots[0].Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTwoConsecutiveFailures_StatusIsCritical()
    {
        var previousError = new PriceSnapshot
        {
            FibraId = _fibraFuno.Id,
            Ticker = "FUNO11",
            Status = MarketDataStatus.Error,
            CapturedAt = _tradingUtc.AddMinutes(-15),
        };
        var marketRepo = new FakeMarketRepository(previousError);
        var yahoo = new FakeYahooClient(); // empty response — second failure

        var job = Build(
            new FakeBmvSchedule(true),
            new FakeTimeService(_tradingUtc),
            new FakeFibraRepository([_fibraFuno]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Single(marketRepo.AddedSnapshots);
        Assert.Equal(MarketDataStatus.Critical, marketRepo.AddedSnapshots[0].Status);
    }

    [Fact]
    public async Task ExecuteAsync_DeletesOldPriceSnapshotsUsingCurrentUtcDate()
    {
        var marketRepo = new FakeMarketRepository();
        var yahoo = new FakeYahooClient(
            new YahooQuoteResult("FUNO11.MX", 50m, 0.5m, 1.0m, 100_000L, 55m, 45m, 49m, 51m, 48m));

        var job = Build(
            new FakeBmvSchedule(true),
            new FakeTimeService(_tradingUtc),
            new FakeFibraRepository([_fibraFuno]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Equal(new DateOnly(2026, 5, 18), marketRepo.DeleteOldPriceSnapshotsCutoff);
    }
}

// ── Fakes ────────────────────────────────────────────────────────────────────

internal sealed class FakeBmvSchedule(bool isTradingHours) : IBmvSchedule
{
    public bool IsTradingHours(DateTimeOffset utcNow) => isTradingHours;
}

internal sealed class FakeTimeService(DateTimeOffset utcNow) : ITimeService
{
    public DateTimeOffset UtcNow => utcNow;
}

internal sealed class FakeFibraRepository(IReadOnlyList<Fibra> fibras) : IFibraRepository
{
    public Task<(IReadOnlyList<Fibra> Items, int Total)> GetActivePagedAsync(
        FibraFilter filter, CancellationToken ct = default)
        => Task.FromResult<(IReadOnlyList<Fibra>, int)>((fibras, fibras.Count));

    public Task<Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default)
        => Task.FromResult(fibras.FirstOrDefault(f => f.Ticker == ticker));

    public Task<IReadOnlyList<Fibra>> GetAllActiveAsync(CancellationToken ct = default)
        => Task.FromResult(fibras);
}

internal sealed class FakeYahooClient(params YahooQuoteResult[] quotes) : IYahooFinanceClient
{
    public int CallCount { get; private set; }

    public Task<IReadOnlyList<YahooQuoteResult>> GetQuotesAsync(
        IEnumerable<string> yahooTickers, CancellationToken ct = default)
    {
        CallCount++;
        return Task.FromResult<IReadOnlyList<YahooQuoteResult>>(quotes);
    }

    public Task<IReadOnlyList<YahooDividendResult>> GetDividendHistoryAsync(
        string yahooTicker, DateOnly from, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<YahooDividendResult>>([]);

    public Task<IReadOnlyList<YahooOhlcvResult>> GetOhlcvHistoryAsync(
        string yahooTicker, DateOnly from, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<YahooOhlcvResult>>([]);
}

internal sealed class FakeMarketRepository : IMarketRepository
{
    private readonly PriceSnapshot? _existing;

    public FakeMarketRepository(PriceSnapshot? existing = null) => _existing = existing;

    public List<PriceSnapshot> AddedSnapshots { get; } = [];
    public List<DailySnapshot> UpsertedDailies { get; } = [];
    public DateOnly? DeleteOldPriceSnapshotsCutoff { get; private set; }

    public Task AddPriceSnapshotAsync(PriceSnapshot snapshot, CancellationToken ct = default)
    {
        AddedSnapshots.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PriceSnapshot>> GetLastSnapshotsAsync(
        Guid fibraId, int count, CancellationToken ct = default)
    {
        IReadOnlyList<PriceSnapshot> result = _existing is not null && _existing.FibraId == fibraId
            ? [_existing]
            : [];
        return Task.FromResult(result);
    }

    public Task<bool> UpsertDailySnapshotAsync(DailySnapshot snapshot, CancellationToken ct = default)
    {
        UpsertedDailies.Add(snapshot);
        return Task.FromResult(true);
    }

    public Task DeleteOldPriceSnapshotsAsync(DateOnly cutoff, CancellationToken ct = default)
    {
        DeleteOldPriceSnapshotsCutoff = cutoff;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PriceSnapshot>> GetLatestSnapshotPerFibraAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PriceSnapshot>>([]);

    public Task<IReadOnlyList<DailySnapshot>> GetDailySnapshotsAsync(Guid fibraId, int days, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DailySnapshot>>([]);

    public Task<IReadOnlyList<Distribution>> GetDistributionsAsync(Guid fibraId, int? maxDays = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Distribution>>([]);

    public Task AddDistributionAsync(Distribution dist, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<bool> UpsertDistributionAsync(Distribution dist, CancellationToken ct = default)
        => Task.FromResult(true);
}
