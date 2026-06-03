using Domain.Market;
using Infrastructure.Persistence.Repositories.Market;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Market;

public class MarketRepositoryWeek52AvgTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static DailySnapshot CreateSnapshot(
        Guid fibraId,
        string ticker,
        int daysAgo,
        decimal? close) => new()
    {
        Id = Guid.NewGuid(),
        FibraId = fibraId,
        Ticker = ticker,
        Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-daysAgo),
        Close = close,
    };

    [Fact]
    public async Task GetWeek52Avg_WithMultipleFibras_ReturnsAverageClosePerFibra()
    {
        var fibraUnoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fibraDosId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await using var db = CreateDbContext();
        db.DailySnapshots.AddRange(
            CreateSnapshot(fibraUnoId, "FUNO11", 10, 100m),
            CreateSnapshot(fibraUnoId, "FUNO11", 20, 110m),
            CreateSnapshot(fibraUnoId, "FUNO11", 30, 120m),
            CreateSnapshot(fibraUnoId, "FUNO11", 40, 130m),
            CreateSnapshot(fibraDosId, "FIHO12", 10, 200m),
            CreateSnapshot(fibraDosId, "FIHO12", 20, 220m),
            CreateSnapshot(fibraDosId, "FIHO12", 30, 240m),
            CreateSnapshot(fibraDosId, "FIHO12", 40, 260m));
        await db.SaveChangesAsync();

        var repo = new MarketRepository(db);

        var result = await repo.GetWeek52AvgByFibrasAsync([fibraUnoId, fibraDosId], 365, CancellationToken.None);

        Assert.Equal(115m, result[fibraUnoId]);
        Assert.Equal(230m, result[fibraDosId]);
    }

    [Fact]
    public async Task GetWeek52Avg_WithNoSnapshots_ReturnsEmptyDictionary()
    {
        await using var db = CreateDbContext();
        var repo = new MarketRepository(db);

        var result = await repo.GetWeek52AvgByFibrasAsync([Guid.NewGuid()], 365, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetWeek52Avg_ExcludesSnapshotsOlderThan365Days()
    {
        var fibraId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        await using var db = CreateDbContext();
        db.DailySnapshots.AddRange(
            CreateSnapshot(fibraId, "VESTA", 30, 100m),
            CreateSnapshot(fibraId, "VESTA", 400, 500m));
        await db.SaveChangesAsync();

        var repo = new MarketRepository(db);

        var result = await repo.GetWeek52AvgByFibrasAsync([fibraId], 365, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(100m, result[fibraId]);
    }

    [Fact]
    public async Task GetWeek52Avg_SkipsSnapshotsWithNullClose()
    {
        var fibraId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        await using var db = CreateDbContext();
        db.DailySnapshots.AddRange(
            CreateSnapshot(fibraId, "NEXT", 10, 140m),
            CreateSnapshot(fibraId, "NEXT", 20, null));
        await db.SaveChangesAsync();

        var repo = new MarketRepository(db);

        var result = await repo.GetWeek52AvgByFibrasAsync([fibraId], 365, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(140m, result[fibraId]);
    }
}
