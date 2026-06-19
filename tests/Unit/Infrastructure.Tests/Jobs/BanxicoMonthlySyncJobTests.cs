using Application.Integrations;
using Application.Jobs;
using Application.Ops;
using Domain.Jobs;
using Domain.Ops;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Tests.Jobs;

public class BanxicoMonthlySyncJobTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTableEmpty_FetchesFrom25MonthsAgo()
    {
        var client = new FakeBanxicoClient([
            (new DateOnly(2024, 4, 1), 134.1258m),
        ]);
        var repo = new FakeInpcRepository();
        var logger = new ListLogger<BanxicoMonthlySyncJob>();
        var job = new BanxicoMonthlySyncJob(client, repo, new NullRunLogRepo(), new NullErrorLogRepo(), logger);

        var now = DateTime.UtcNow;
        var expectedFrom = new DateOnly(now.Year, now.Month, 1).AddMonths(-25);
        var expectedTo = DateOnly.FromDateTime(now);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Equal(expectedFrom, client.LastFrom);
        Assert.Equal(expectedTo, client.LastTo);
        Assert.Single(repo.Upserted);
        Assert.Equal(new DateOnly(2024, 4, 1), repo.Upserted[0].Periodo);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLastPeriodoExists_FetchesFromNextMonth()
    {
        var now = DateTime.UtcNow;
        var client = new FakeBanxicoClient([
            (new DateOnly(2025, 6, 1), 140.0000m),
        ]);
        var repo = new FakeInpcRepository
        {
            LatestPeriodo = new DateOnly(2025, 5, 1),
        };
        var logger = new ListLogger<BanxicoMonthlySyncJob>();
        var job = new BanxicoMonthlySyncJob(client, repo, new NullRunLogRepo(), new NullErrorLogRepo(), logger);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Equal(new DateOnly(2025, 6, 1), client.LastFrom);
        Assert.Equal(DateOnly.FromDateTime(now), client.LastTo);
        Assert.Single(repo.Upserted);
        Assert.Equal(new DateOnly(2025, 6, 1), repo.Upserted[0].Periodo);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyUpToDate_DoesNotCallBanxico()
    {
        var now = DateTime.UtcNow;
        var client = new FakeBanxicoClient([]);
        var repo = new FakeInpcRepository
        {
            LatestPeriodo = new DateOnly(now.Year, now.Month, 1),
        };
        var logger = new ListLogger<BanxicoMonthlySyncJob>();
        var job = new BanxicoMonthlySyncJob(client, repo, new NullRunLogRepo(), new NullErrorLogRepo(), logger);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Null(client.LastFrom);
        Assert.Empty(repo.Upserted);
        Assert.Contains(logger.Messages, message => message.Contains("al día", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_WhenBanxicoReturnsEmpty_LogsWarningAndReturns()
    {
        var client = new FakeBanxicoClient([]);
        var repo = new FakeInpcRepository();
        var logger = new ListLogger<BanxicoMonthlySyncJob>();
        var job = new BanxicoMonthlySyncJob(client, repo, new NullRunLogRepo(), new NullErrorLogRepo(), logger);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Empty(repo.Upserted);
        Assert.Contains(logger.Messages, message => message.Contains("no retornó datos", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_WhenMissingMonths_FetchesCatchUpRange()
    {
        var client = new FakeBanxicoClient([
            (new DateOnly(2025, 4, 1), 139.1000m),
            (new DateOnly(2025, 5, 1), 139.9000m),
        ]);
        var repo = new FakeInpcRepository
        {
            LatestPeriodo = new DateOnly(2025, 3, 1),
        };
        var logger = new ListLogger<BanxicoMonthlySyncJob>();
        var job = new BanxicoMonthlySyncJob(client, repo, new NullRunLogRepo(), new NullErrorLogRepo(), logger);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Equal(new DateOnly(2025, 4, 1), client.LastFrom);
        Assert.Equal(2, repo.Upserted.Count);
        Assert.Equal(new DateOnly(2025, 5, 1), repo.Upserted[^1].Periodo);
    }

    private sealed class FakeBanxicoClient(IReadOnlyList<(DateOnly Periodo, decimal InpcIndex)> history) : IBanxicoClient
    {
        public DateOnly? LastFrom { get; private set; }
        public DateOnly? LastTo { get; private set; }

        public Task<decimal?> GetCetes28dAsync(CancellationToken ct = default) => Task.FromResult<decimal?>(null);
        public Task<decimal?> GetTiie28dAsync(CancellationToken ct = default) => Task.FromResult<decimal?>(null);

        public Task<IReadOnlyList<(DateOnly Periodo, decimal InpcIndex)>> GetInpcHistoryAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken ct = default)
        {
            LastFrom = from;
            LastTo = to;
            return Task.FromResult(history);
        }
    }

    private sealed class FakeInpcRepository : IInpcRepository
    {
        public DateOnly? LatestPeriodo { get; set; }
        public List<InpcMonthlyEntry> Upserted { get; } = [];

        public Task<DateOnly?> GetLatestPeriodoAsync(CancellationToken ct = default)
            => Task.FromResult(LatestPeriodo);

        public Task UpsertManyAsync(IEnumerable<InpcMonthlyEntry> entries, CancellationToken ct = default)
        {
            Upserted.AddRange(entries);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<InpcMonthlyEntry>> GetLastAsync(int count, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<InpcMonthlyEntry>>(Upserted.TakeLast(count).ToList());

        public Task<IReadOnlyList<InpcMonthlyEntry>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<InpcMonthlyEntry>>(Upserted
                .Where(entry => entry.Periodo >= from && entry.Periodo <= to)
                .OrderBy(entry => entry.Periodo)
                .ToList());
    }

    private sealed class NullRunLogRepo : IPipelineRunLogRepository
    {
        public Task AddAsync(PipelineRunLog entry, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<PipelineRunLog>> GetRecentAsync(string? pipeline, int take, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PipelineRunLog>>([]);
        public Task<PipelineRunLog?> GetLastCompletedAsync(string pipeline, CancellationToken ct = default)
            => Task.FromResult<PipelineRunLog?>(null);
    }

    private sealed class NullErrorLogRepo : IPipelineErrorLogRepository
    {
        public Task LogErrorAsync(PipelineErrorLog entry, CancellationToken ct = default) => Task.CompletedTask;
        public Task<(IReadOnlyList<PipelineErrorLog> Items, int Total)> GetPagedAsync(string? pipeline, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<PipelineErrorLog>, int)>(([], 0));
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
