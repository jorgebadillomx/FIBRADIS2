using Application.Integrations;
using Application.Jobs;
using Application.Ops;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Tests.Jobs;

public class BanxicoSyncJobTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRateExists_UpdatesRepository()
    {
        var repo = new FakeOperationalConfigRepository();
        var client = new FakeBanxicoClient(9.5m);
        var logger = new ListLogger<BanxicoSyncJob>();
        var job = new BanxicoSyncJob(client, repo, logger);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, repo.UpdateCalls);
        Assert.Equal(9.5m, repo.LastRate);
        Assert.NotNull(repo.LastUpdatedAt);
        Assert.Contains(logger.Messages, message => message.Contains("actualizado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRateIsNull_DoesNotUpdateRepository()
    {
        var repo = new FakeOperationalConfigRepository();
        var client = new FakeBanxicoClient(null);
        var logger = new ListLogger<BanxicoSyncJob>();
        var job = new BanxicoSyncJob(client, repo, logger);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Equal(0, repo.UpdateCalls);
        Assert.Contains(logger.Messages, message => message.Contains("no se obtuvo", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeBanxicoClient(decimal? rate) : IBanxicoClient
    {
        public Task<decimal?> GetCetes28dAsync(CancellationToken ct = default) => Task.FromResult(rate);
    }

    private sealed class FakeOperationalConfigRepository : IOperationalConfigRepository
    {
        public int UpdateCalls { get; private set; }
        public decimal? LastRate { get; private set; }
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
            CancellationToken ct = default)
            => Task.CompletedTask;
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
