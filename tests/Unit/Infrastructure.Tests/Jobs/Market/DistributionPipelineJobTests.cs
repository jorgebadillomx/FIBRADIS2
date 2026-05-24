using Application.Catalog;
using Application.Jobs;
using Application.Market;
using Domain.Catalog;
using Domain.Jobs;
using Domain.Market;
using Infrastructure.Integrations.Yahoo;
using Infrastructure.Jobs.Market;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs.Market;

public class DistributionPipelineJobTests
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

    private static DistributionPipelineJob Build(
        FakeDistFibraRepository fibraRepo,
        FakeDistYahooClient yahoo,
        FakeDistMarketRepository marketRepo)
        => new(
            fibraRepo,
            yahoo,
            marketRepo,
            new FakeDistributionPipelineErrorLogRepository(),
            new FakeDistributionPipelineRunLogRepository(),
            NullLogger<DistributionPipelineJob>.Instance);

    private static DistributionPipelineJob Build(
        FakeDistFibraRepository fibraRepo,
        FakeDistYahooClient yahoo,
        FakeDistMarketRepository marketRepo,
        FakeDistributionPipelineRunLogRepository runLogRepo)
        => new(
            fibraRepo,
            yahoo,
            marketRepo,
            new FakeDistributionPipelineErrorLogRepository(),
            runLogRepo,
            NullLogger<DistributionPipelineJob>.Instance);

    [Fact]
    public async Task ExecuteAsync_WhenYahooReturnsNoDividends_InsertsNothing()
    {
        var marketRepo = new FakeDistMarketRepository();
        var yahoo = new FakeDistYahooClient();

        var job = Build(
            new FakeDistFibraRepository([_fibraFuno]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Empty(marketRepo.UpsertedDistributions);
    }

    [Fact]
    public async Task ExecuteAsync_WhenYahooReturnsDividend_InsertsDistribution()
    {
        var marketRepo = new FakeDistMarketRepository(alwaysInsert: true);
        var div = new YahooDividendResult(new DateOnly(2024, 3, 18), 0.368m);
        var yahoo = new FakeDistYahooClient(("FUNO11.MX", [div]));

        var job = Build(
            new FakeDistFibraRepository([_fibraFuno]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Single(marketRepo.UpsertedDistributions);
        var dist = marketRepo.UpsertedDistributions[0];
        Assert.Equal("FUNO11", dist.Ticker);
        Assert.Equal(_fibraFuno.Id, dist.FibraId);
        Assert.Equal(new DateOnly(2024, 3, 18), dist.PaymentDate);
        Assert.Equal(0.368m, dist.AmountPerUnit);
        Assert.Equal("yahoo", dist.Source);
        Assert.Equal("MXN", dist.Currency);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDistributionAlreadyExists_SkipsInsert()
    {
        var marketRepo = new FakeDistMarketRepository(alwaysInsert: false);
        var div = new YahooDividendResult(new DateOnly(2024, 3, 18), 0.368m);
        var yahoo = new FakeDistYahooClient(("FUNO11.MX", [div]));

        var job = Build(
            new FakeDistFibraRepository([_fibraFuno]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        Assert.Single(marketRepo.UpsertedDistributions);
        // UpsertDistributionAsync was called but returned false — distribution was attempted but not inserted
        Assert.Equal(1, marketRepo.UpsertCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOneTickerFails_OtherTickersAreProcessed()
    {
        var marketRepo = new FakeDistMarketRepository(alwaysInsert: true);
        var div = new YahooDividendResult(new DateOnly(2024, 3, 18), 0.368m);
        // Only FUNO11 has data; FMTY14 will throw
        var yahoo = new FakeDistYahooClient(
            ("FUNO11.MX", [div]),
            throwForTicker: "FMTY14.MX");

        var job = Build(
            new FakeDistFibraRepository([_fibraFuno, _fibraFmty]),
            yahoo,
            marketRepo);

        await job.ExecuteAsync();

        // FUNO11 distribution inserted, FMTY14 error swallowed
        Assert.Single(marketRepo.UpsertedDistributions);
        Assert.Equal("FUNO11", marketRepo.UpsertedDistributions[0].Ticker);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoActiveFibras_DoesNotCallYahoo()
    {
        var yahoo = new FakeDistYahooClient();

        var job = Build(
            new FakeDistFibraRepository([]),
            yahoo,
            new FakeDistMarketRepository());

        await job.ExecuteAsync();

        Assert.Equal(0, yahoo.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoActiveFibras_WritesPipelineRunLogCompleted()
    {
        var runLogRepo = new FakeDistributionPipelineRunLogRepository();
        var job = Build(
            new FakeDistFibraRepository([]),
            new FakeDistYahooClient(),
            new FakeDistMarketRepository(),
            runLogRepo);

        await job.ExecuteAsync();

        Assert.Single(runLogRepo.Entries);
        var entry = runLogRepo.Entries[0];
        Assert.Equal("Completed", entry.Status);
        Assert.Equal(0, entry.ItemsProcessed);
        Assert.Null(entry.TriggeredBy);
    }
}

// ── Fakes ────────────────────────────────────────────────────────────────────

internal sealed class FakeDistFibraRepository(IReadOnlyList<Fibra> fibras) : IFibraRepository
{
    public Task<(IReadOnlyList<Fibra> Items, int Total)> GetActivePagedAsync(
        FibraFilter filter, CancellationToken ct = default)
        => Task.FromResult<(IReadOnlyList<Fibra>, int)>((fibras, fibras.Count));

    public Task<Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default)
        => Task.FromResult(fibras.FirstOrDefault(f => f.Ticker == ticker));

    public Task<Fibra?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(fibras.FirstOrDefault(f => f.Id == id));

    public Task<IReadOnlyList<Fibra>> GetAllActiveAsync(CancellationToken ct = default)
        => Task.FromResult(fibras);
}

internal sealed class FakeDistYahooClient : IYahooFinanceClient
{
    private readonly Dictionary<string, IReadOnlyList<YahooDividendResult>> _data = [];
    private readonly string? _throwForTicker;

    public int CallCount { get; private set; }

    public FakeDistYahooClient(
        (string ticker, IReadOnlyList<YahooDividendResult> divs)? entry = null,
        string? throwForTicker = null)
    {
        if (entry is not null)
            _data[entry.Value.ticker] = entry.Value.divs;
        _throwForTicker = throwForTicker;
    }

    public FakeDistYahooClient(params (string ticker, IReadOnlyList<YahooDividendResult> divs)[] entries)
    {
        foreach (var (ticker, divs) in entries)
            _data[ticker] = divs;
    }

    public Task<IReadOnlyList<YahooQuoteResult>> GetQuotesAsync(
        IEnumerable<string> yahooTickers, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<YahooQuoteResult>>([]);

    public Task<IReadOnlyList<YahooDividendResult>> GetDividendHistoryAsync(
        string yahooTicker, DateOnly from, CancellationToken ct = default)
    {
        CallCount++;
        if (_throwForTicker is not null && yahooTicker == _throwForTicker)
            throw new InvalidOperationException($"Simulated failure for {yahooTicker}");

        _data.TryGetValue(yahooTicker, out var result);
        return Task.FromResult(result ?? (IReadOnlyList<YahooDividendResult>)[]);
    }

    public Task<IReadOnlyList<YahooOhlcvResult>> GetOhlcvHistoryAsync(
        string yahooTicker, DateOnly from, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<YahooOhlcvResult>>([]);
}

internal sealed class FakeDistMarketRepository : IMarketRepository
{
    private readonly bool _alwaysInsert;

    public FakeDistMarketRepository(bool alwaysInsert = true) => _alwaysInsert = alwaysInsert;

    public List<Distribution> UpsertedDistributions { get; } = [];
    public int UpsertCallCount { get; private set; }

    public Task<bool> UpsertDistributionAsync(Distribution dist, CancellationToken ct = default)
    {
        UpsertCallCount++;
        UpsertedDistributions.Add(dist);
        return Task.FromResult(_alwaysInsert);
    }

    public Task AddPriceSnapshotAsync(PriceSnapshot snapshot, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<PriceSnapshot>> GetLastSnapshotsAsync(
        Guid fibraId, int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PriceSnapshot>>([]);

    public Task<bool> UpsertDailySnapshotAsync(DailySnapshot snapshot, CancellationToken ct = default)
        => Task.FromResult(true);

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

    public Task AddDistributionAsync(Distribution dist, CancellationToken ct = default)
        => Task.CompletedTask;
}

internal sealed class FakeDistributionPipelineErrorLogRepository : IPipelineErrorLogRepository
{
    public Task LogErrorAsync(PipelineErrorLog entry, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<(IReadOnlyList<PipelineErrorLog> Items, int Total)> GetPagedAsync(string? pipeline, int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult<(IReadOnlyList<PipelineErrorLog>, int)>(([], 0));
}

internal sealed class FakeDistributionPipelineRunLogRepository : IPipelineRunLogRepository
{
    public List<PipelineRunLog> Entries { get; } = [];

    public Task AddAsync(PipelineRunLog entry, CancellationToken ct = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PipelineRunLog>> GetRecentAsync(string? pipeline, int take, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PipelineRunLog>>([]);

    public Task<PipelineRunLog?> GetLastCompletedAsync(string pipeline, CancellationToken ct = default)
        => Task.FromResult<PipelineRunLog?>(null);
}
