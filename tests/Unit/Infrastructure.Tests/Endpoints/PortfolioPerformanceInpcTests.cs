using System.Reflection;
using Application.Ops;
using Api.Endpoints.Private;
using Domain.Ops;
using SharedApiContracts.Portfolio;

namespace Infrastructure.Tests.Endpoints;

public class PortfolioPerformanceInpcTests
{
    [Fact]
    public async Task BuildInpcSeriesAsync_WhenNoInpcInRange_ReturnsNull()
    {
        var portfolioSeries = new[]
        {
            new PortfolioPerformancePointDto("2026-03-15", 0m),
            new PortfolioPerformancePointDto("2026-04-15", 5m),
        };
        var repo = new FakeInpcRepository([]);

        var result = await InvokeBuildInpcSeriesAsync(portfolioSeries, repo);

        Assert.Null(result);
        Assert.Equal(new DateOnly(2026, 2, 1), repo.LastFrom);
    }

    [Fact]
    public async Task BuildInpcSeriesAsync_WhenEntriesExist_NormalizesFromBaseMonth()
    {
        var portfolioSeries = new[]
        {
            new PortfolioPerformancePointDto("2026-03-15", 0m),
            new PortfolioPerformancePointDto("2026-04-15", 0m),
        };
        var repo = new FakeInpcRepository([
            new InpcMonthlyEntry { Periodo = new DateOnly(2026, 2, 1), InpcIndex = 100m, CapturedAt = DateTimeOffset.UtcNow },
            new InpcMonthlyEntry { Periodo = new DateOnly(2026, 3, 1), InpcIndex = 110m, CapturedAt = DateTimeOffset.UtcNow },
            new InpcMonthlyEntry { Periodo = new DateOnly(2026, 4, 1), InpcIndex = 121m, CapturedAt = DateTimeOffset.UtcNow },
        ]);

        var result = await InvokeBuildInpcSeriesAsync(portfolioSeries, repo);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("2026-03-15", result[0].Date);
        Assert.Equal(0m, result[0].ValuePct);
        Assert.Equal("2026-04-15", result[1].Date);
        Assert.Equal(10m, result[1].ValuePct);
        Assert.Equal(new DateOnly(2026, 2, 1), repo.LastFrom);
    }

    private static async Task<IReadOnlyList<PortfolioPerformancePointDto>?> InvokeBuildInpcSeriesAsync(
        IReadOnlyList<PortfolioPerformancePointDto> portfolioSeries,
        FakeInpcRepository repo)
    {
        var method = typeof(PortfolioEndpoints).GetMethod(
            "BuildInpcSeriesAsync",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var task = (Task<IReadOnlyList<PortfolioPerformancePointDto>?>)method!.Invoke(
            null,
            new object[] { portfolioSeries, repo, CancellationToken.None })!;

        return await task;
    }

    private sealed class FakeInpcRepository(IReadOnlyList<InpcMonthlyEntry> entries) : IInpcRepository
    {
        public DateOnly? LastFrom { get; private set; }
        public DateOnly? LastTo { get; private set; }

        public Task<DateOnly?> GetLatestPeriodoAsync(CancellationToken ct = default)
            => Task.FromResult<DateOnly?>(entries.Count > 0 ? entries[^1].Periodo : null);

        public Task UpsertManyAsync(IEnumerable<InpcMonthlyEntry> entries, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<InpcMonthlyEntry>> GetLastAsync(int count, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<InpcMonthlyEntry>>(entries.TakeLast(count).ToList());

        public Task<IReadOnlyList<InpcMonthlyEntry>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
        {
            LastFrom = from;
            LastTo = to;
            return Task.FromResult(entries);
        }
    }
}
