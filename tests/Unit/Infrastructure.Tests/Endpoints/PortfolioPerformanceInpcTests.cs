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
    public async Task BuildInpcSeriesAsync_WhenEntriesExist_InterpolatesDailyFromBaseDate()
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
        // El primer punto es la base → 0% exacto.
        Assert.Equal("2026-03-15", result[0].Date);
        Assert.Equal(0m, result[0].ValuePct);
        // El segundo punto se interpola día a día (no escalón mensual) → ~10% sobre la base interpolada.
        Assert.Equal("2026-04-15", result[1].Date);
        Assert.True(result[1].ValuePct is > 9m and < 11m, $"esperado ~10%, obtenido {result[1].ValuePct}");
        Assert.Equal(new DateOnly(2026, 2, 1), repo.LastFrom);
    }

    [Fact]
    public async Task BuildInpcSeriesAsync_WhenCurrentMonthUnpublished_DoesNotReturnFlatZero()
    {
        // Reproduce el bug del rango 30D: el rango cae entre el mes base y el mes en curso
        // (aún sin publicar). Con el escalón mensual todos los puntos daban 0; ahora la serie
        // debe ser estrictamente creciente, proyectando el mes no publicado con la última tasa.
        var portfolioSeries = new[]
        {
            new PortfolioPerformancePointDto("2026-05-10", 0m),
            new PortfolioPerformancePointDto("2026-05-25", 0m),
            new PortfolioPerformancePointDto("2026-06-10", 0m),
            new PortfolioPerformancePointDto("2026-06-20", 0m),
        };
        var repo = new FakeInpcRepository([
            new InpcMonthlyEntry { Periodo = new DateOnly(2026, 4, 1), InpcIndex = 100m, CapturedAt = DateTimeOffset.UtcNow },
            new InpcMonthlyEntry { Periodo = new DateOnly(2026, 5, 1), InpcIndex = 101m, CapturedAt = DateTimeOffset.UtcNow },
        ]);

        var result = await InvokeBuildInpcSeriesAsync(portfolioSeries, repo);

        Assert.NotNull(result);
        Assert.Equal(4, result!.Count);
        Assert.Equal(0m, result[0].ValuePct);
        // Estrictamente creciente: nunca plano en cero.
        for (var i = 1; i < result.Count; i++)
            Assert.True(result[i].ValuePct > result[i - 1].ValuePct,
                $"punto {i} ({result[i].ValuePct}) no es mayor que el anterior ({result[i - 1].ValuePct})");
    }

    [Fact]
    public async Task BuildInpcSeriesAsync_WhenDateBeyondLastPublished_Projects()
    {
        var portfolioSeries = new[]
        {
            new PortfolioPerformancePointDto("2026-05-15", 0m),
            new PortfolioPerformancePointDto("2026-07-15", 0m),
        };
        var repo = new FakeInpcRepository([
            new InpcMonthlyEntry { Periodo = new DateOnly(2026, 4, 1), InpcIndex = 100m, CapturedAt = DateTimeOffset.UtcNow },
            new InpcMonthlyEntry { Periodo = new DateOnly(2026, 5, 1), InpcIndex = 110m, CapturedAt = DateTimeOffset.UtcNow },
        ]);

        var result = await InvokeBuildInpcSeriesAsync(portfolioSeries, repo);

        Assert.NotNull(result);
        Assert.Equal(0m, result![0].ValuePct);
        // Julio está más allá del último publicado (mayo) → se proyecta con MoM=1.1, no se aplana.
        Assert.True(result[1].ValuePct is > 20m and < 22m, $"esperado ~21%, obtenido {result[1].ValuePct}");
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
