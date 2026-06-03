using Application.Catalog;
using Application.Market;
using Domain.Catalog;
using Domain.Market;
using Infrastructure.Integrations.Yahoo;
using Infrastructure.Jobs.Market;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs.Market;

public class DailySnapshotHistoricalJobTests
{
    private static readonly Fibra _fibraFuno = new()
    {
        Id = Guid.NewGuid(), Ticker = "FUNO11", YahooTicker = "FUNO11.MX",
        Currency = "MXN", State = FibraState.Active
    };
    private static readonly Fibra _fibraFmty = new()
    {
        Id = Guid.NewGuid(), Ticker = "FMTY14", YahooTicker = "FMTY14.MX",
        Currency = "MXN", State = FibraState.Active
    };

    private static DailySnapshotHistoricalJob Build(
        FakeHistoricalFibraRepository fibraRepo,
        FakeHistoricalYahooClient yahoo,
        FakeHistoricalMarketRepository marketRepo)
        => new(fibraRepo, yahoo, marketRepo, NullLogger<DailySnapshotHistoricalJob>.Instance);

    [Fact]
    public async Task WhenYahooReturnsCandles_UpsertsAllSnapshots()
    {
        var marketRepo = new FakeHistoricalMarketRepository();
        var candles = new[]
        {
            new YahooOhlcvResult(new DateOnly(2024, 1, 2), 10m, 11m, 9m, 10.5m, 1000L),
            new YahooOhlcvResult(new DateOnly(2024, 1, 3), 10.5m, 11.5m, 10m, 11m, 2000L),
        };
        var yahoo = new FakeHistoricalYahooClient(("FUNO11.MX", candles));

        var job = Build(
            new FakeHistoricalFibraRepository([_fibraFuno]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Equal(2, marketRepo.UpsertedDailies.Count);
        Assert.All(marketRepo.UpsertedDailies, snapshot => Assert.Equal(_fibraFuno.Id, snapshot.FibraId));
    }

    [Fact]
    public async Task WhenCandlesAlreadyExist_ReportsSkipped()
    {
        var marketRepo = new FakeHistoricalMarketRepository(alwaysInsert: false);
        var candles = new[]
        {
            new YahooOhlcvResult(new DateOnly(2024, 1, 2), 10m, 11m, 9m, 10.5m, 1000L),
        };
        var yahoo = new FakeHistoricalYahooClient(("FUNO11.MX", candles));

        var job = Build(
            new FakeHistoricalFibraRepository([_fibraFuno]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Single(marketRepo.UpsertedDailies);
        Assert.Equal(1, marketRepo.FalseResultCount);
    }

    [Fact]
    public async Task WhenCandleHasZeroClose_DiscardedCandle()
    {
        var marketRepo = new FakeHistoricalMarketRepository();
        var candles = new[]
        {
            new YahooOhlcvResult(new DateOnly(2024, 1, 2), 10m, 11m, 9m, 0m, 1000L),
        };
        var yahoo = new FakeHistoricalYahooClient(("FUNO11.MX", candles));

        var job = Build(
            new FakeHistoricalFibraRepository([_fibraFuno]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Empty(marketRepo.UpsertedDailies);
    }

    [Fact]
    public async Task WhenCandleHasZeroOpen_UsesCloseAsFallback()
    {
        var marketRepo = new FakeHistoricalMarketRepository();
        var candles = new[]
        {
            new YahooOhlcvResult(new DateOnly(2024, 1, 2), 0m, 11m, 9m, 10.5m, 1000L),
        };
        var yahoo = new FakeHistoricalYahooClient(("FUNO11.MX", candles));

        var job = Build(
            new FakeHistoricalFibraRepository([_fibraFuno]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Single(marketRepo.UpsertedDailies);
        Assert.Equal(10.5m, marketRepo.UpsertedDailies[0].Open);
    }

    [Fact]
    public async Task WhenOneTickerFails_OtherTickersAreProcessed()
    {
        var marketRepo = new FakeHistoricalMarketRepository();
        var candles = new[]
        {
            new YahooOhlcvResult(new DateOnly(2024, 1, 2), 10m, 11m, 9m, 10.5m, 1000L),
        };
        var yahoo = new FakeHistoricalYahooClient(
            ("FUNO11.MX", candles),
            throwForTicker: "FMTY14.MX");

        var job = Build(
            new FakeHistoricalFibraRepository([_fibraFuno, _fibraFmty]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Single(marketRepo.UpsertedDailies);
        Assert.Equal("FUNO11", marketRepo.UpsertedDailies[0].Ticker);
    }

    [Fact]
    public async Task WhenNoActiveFibras_DoesNotCallYahoo()
    {
        var yahoo = new FakeHistoricalYahooClient();

        var job = Build(
            new FakeHistoricalFibraRepository([]),
            yahoo,
            new FakeHistoricalMarketRepository());

        await job.ExecuteAsync();

        Assert.Equal(0, yahoo.CallCount);
    }
}

internal sealed class FakeHistoricalFibraRepository(IReadOnlyList<Fibra> fibras) : IFibraRepository
{
    public Task AddAsync(Fibra fibra, CancellationToken ct = default) => Task.CompletedTask;

    public Task UpdateAsync(Fibra fibra, CancellationToken ct = default) => Task.CompletedTask;

    public Task<bool> ExistsByTickerAsync(string ticker, CancellationToken ct = default) => Task.FromResult(false);

    public Task<(IReadOnlyList<Fibra> Items, int Total)> GetActivePagedAsync(
        FibraFilter filter, CancellationToken ct = default)
        => Task.FromResult<(IReadOnlyList<Fibra>, int)>((fibras, fibras.Count));

    public Task<Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default)
        => Task.FromResult(fibras.FirstOrDefault(f => f.Ticker == ticker));

    public Task<Fibra?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(fibras.FirstOrDefault(f => f.Id == id));

    public Task<IReadOnlyList<Fibra>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Fibra>>([]);

    public Task<IReadOnlyList<Fibra>> GetAllActiveAsync(CancellationToken ct = default)
        => Task.FromResult(fibras);
}

internal sealed class FakeHistoricalYahooClient : IYahooFinanceClient
{
    private readonly Dictionary<string, IReadOnlyList<YahooOhlcvResult>> _data = [];
    private readonly string? _throwForTicker;

    public int CallCount { get; private set; }

    public FakeHistoricalYahooClient(
        (string ticker, IReadOnlyList<YahooOhlcvResult> candles)? entry = null,
        string? throwForTicker = null)
    {
        if (entry is not null)
            _data[entry.Value.ticker] = entry.Value.candles;
        _throwForTicker = throwForTicker;
    }

    public FakeHistoricalYahooClient(params (string ticker, IReadOnlyList<YahooOhlcvResult> candles)[] entries)
    {
        foreach (var (ticker, candles) in entries)
            _data[ticker] = candles;
    }

    public Task<IReadOnlyList<YahooQuoteResult>> GetQuotesAsync(
        IEnumerable<string> yahooTickers, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<YahooQuoteResult>>([]);

    public Task<IReadOnlyList<YahooDividendResult>> GetDividendHistoryAsync(
        string yahooTicker, DateOnly from, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<YahooDividendResult>>([]);

    public Task<IReadOnlyList<YahooOhlcvResult>> GetOhlcvHistoryAsync(
        string yahooTicker, DateOnly from, CancellationToken ct = default)
    {
        CallCount++;
        if (_throwForTicker is not null && yahooTicker == _throwForTicker)
            throw new InvalidOperationException($"Simulated failure for {yahooTicker}");

        _data.TryGetValue(yahooTicker, out var result);
        return Task.FromResult(result ?? (IReadOnlyList<YahooOhlcvResult>)[]);
    }
}

internal sealed class FakeHistoricalMarketRepository : IMarketRepository
{
    private readonly bool _alwaysInsert;

    public FakeHistoricalMarketRepository(bool alwaysInsert = true) => _alwaysInsert = alwaysInsert;

    public List<DailySnapshot> UpsertedDailies { get; } = [];
    public int FalseResultCount { get; private set; }

    public Task AddPriceSnapshotAsync(PriceSnapshot snapshot, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<PriceSnapshot>> GetLastSnapshotsAsync(
        Guid fibraId, int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PriceSnapshot>>([]);

    public Task<bool> UpsertDailySnapshotAsync(DailySnapshot snapshot, CancellationToken ct = default)
    {
        if (snapshot.Close is not > 0)
            return Task.FromResult(false);

        var normalized = new DailySnapshot
        {
            FibraId = snapshot.FibraId,
            Ticker = snapshot.Ticker,
            Date = snapshot.Date,
            Open = snapshot.Open is > 0 ? snapshot.Open : snapshot.Close,
            High = snapshot.High is > 0 ? snapshot.High : snapshot.Close,
            Low = snapshot.Low is > 0 ? snapshot.Low : snapshot.Close,
            Close = snapshot.Close,
            Volume = snapshot.Volume,
        };

        UpsertedDailies.Add(normalized);
        if (!_alwaysInsert) FalseResultCount++;
        return Task.FromResult(_alwaysInsert);
    }

    public Task DeleteOldPriceSnapshotsAsync(DateOnly cutoff, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<PriceSnapshot>> GetLatestSnapshotPerFibraAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PriceSnapshot>>([]);

    public Task<IReadOnlyList<DailySnapshot>> GetDailySnapshotsAsync(
        Guid fibraId, int days, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DailySnapshot>>([]);

    public Task<IReadOnlyList<Distribution>> GetDistributionsAsync(
        Guid fibraId, int? maxDays = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Distribution>>([]);

    public Task<IReadOnlyList<Distribution>> GetDistributionsByFibrasAsync(
        IReadOnlyList<Guid> fibraIds,
        int days,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Distribution>>([]);

    public Task<IReadOnlyDictionary<Guid, decimal>> GetWeek52AvgByFibrasAsync(
        IReadOnlyList<Guid> fibraIds,
        int days = 365,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());

    public Task AddDistributionAsync(Distribution dist, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<bool> UpsertDistributionAsync(Distribution dist, CancellationToken ct = default)
        => Task.FromResult(true);
}
