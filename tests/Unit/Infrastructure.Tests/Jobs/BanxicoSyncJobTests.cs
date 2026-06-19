using Application.Integrations;
using Application.Jobs;
using Application.Ops;
using Domain.Jobs;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Tests.Jobs;

public class BanxicoSyncJobTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRateExists_UpdatesRepository()
    {
        var repo = new FakeOperationalConfigRepository();
        var client = new FakeBanxicoClient(cetes: 9.5m, tiie: null);
        var logger = new ListLogger<BanxicoSyncJob>();
        var job = new BanxicoSyncJob(client, repo, new NullRunLogRepo(), new NullErrorLogRepo(), logger);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, repo.UpdateCalls);
        Assert.Equal(9.5m, repo.LastRate);
        Assert.NotNull(repo.LastUpdatedAt);
        Assert.Contains(logger.Messages, message => message.Contains("no se obtuvo tasa TIIE 28d", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Messages, message => message.Contains("CETES=9.5", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRateIsNull_DoesNotUpdateRepository()
    {
        var repo = new FakeOperationalConfigRepository();
        var client = new FakeBanxicoClient(cetes: null, tiie: null);
        var logger = new ListLogger<BanxicoSyncJob>();
        var job = new BanxicoSyncJob(client, repo, new NullRunLogRepo(), new NullErrorLogRepo(), logger);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Equal(0, repo.UpdateCalls);
        Assert.Contains(logger.Messages, message => message.Contains("no se obtuvo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTiieAvailable_UpdatesTiieRate()
    {
        var repo = new FakeOperationalConfigRepository();
        var client = new FakeBanxicoClient(cetes: null, tiie: 10.25m);
        var logger = new ListLogger<BanxicoSyncJob>();
        var job = new BanxicoSyncJob(client, repo, new NullRunLogRepo(), new NullErrorLogRepo(), logger);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, repo.TiieUpdateCalls);
        Assert.Equal(10.25m, repo.LastTiieRate);
        Assert.NotNull(repo.LastUpdatedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBothAvailable_UpdatesBothRates()
    {
        var repo = new FakeOperationalConfigRepository();
        var client = new FakeBanxicoClient(cetes: 9.5m, tiie: 10.25m);
        var logger = new ListLogger<BanxicoSyncJob>();
        var job = new BanxicoSyncJob(client, repo, new NullRunLogRepo(), new NullErrorLogRepo(), logger);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, repo.UpdateCalls);
        Assert.Equal(1, repo.TiieUpdateCalls);
        Assert.Equal(9.5m, repo.LastRate);
        Assert.Equal(10.25m, repo.LastTiieRate);
    }

    private sealed class FakeBanxicoClient(decimal? cetes, decimal? tiie) : IBanxicoClient
    {
        public Task<decimal?> GetCetes28dAsync(CancellationToken ct = default) => Task.FromResult(cetes);
        public Task<decimal?> GetTiie28dAsync(CancellationToken ct = default) => Task.FromResult(tiie);
        public Task<IReadOnlyList<(DateOnly Periodo, decimal InpcIndex)>> GetInpcHistoryAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<(DateOnly Periodo, decimal InpcIndex)>>(Array.Empty<(DateOnly, decimal)>());
    }

    private sealed class FakeOperationalConfigRepository : IOperationalConfigRepository
    {
        public int UpdateCalls { get; private set; }
        public int TiieUpdateCalls { get; private set; }
        public decimal? LastRate { get; private set; }
        public decimal? LastTiieRate { get; private set; }
        public DateTimeOffset? LastUpdatedAt { get; private set; }

        public Task<Domain.Ops.OperationalConfig> GetAsync(CancellationToken ct = default)
            => Task.FromResult(new Domain.Ops.OperationalConfig());

        public Task UpdateCetesRateAsync(decimal rate, DateTimeOffset updatedAt, CancellationToken ct = default)
        {
            UpdateCalls++;
            LastRate = rate;
            LastUpdatedAt = updatedAt;
            return Task.CompletedTask;
        }

        public Task UpdateTiieRateAsync(decimal rate, DateTimeOffset updatedAt, CancellationToken ct = default)
        {
            TiieUpdateCalls++;
            LastTiieRate = rate;
            LastUpdatedAt = updatedAt;
            return Task.CompletedTask;
        }

        public Task UpdateOrganizationSameAsAsync(string? organizationSameAsJson, string actor, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task UpdateAsync(
            decimal? commissionFactor,
            int? avgPeriods,
            int? newsCadenceMinutes,
            int? fibraNewsMonths,
            int? distributionCadenceMinutes,
            bool? termsEnabled,
            string? termsText,
            string? contactEmail,
            string actor,
            int? fundamentalsCadenceMinutes = null,
            int? universeDegradationThresholdPct = null,
            decimal? isrRetentionRate = null,
            decimal? ivaRate = null,
            CancellationToken ct = default)
            => Task.CompletedTask;
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
        public List<string> Messages { get; } = new();

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
