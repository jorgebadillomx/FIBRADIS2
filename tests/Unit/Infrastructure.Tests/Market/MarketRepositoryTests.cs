using Domain.Market;
using Infrastructure.Persistence.Repositories.Market;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Market;

public class MarketRepositoryTests
{
    private sealed class DbScope : IAsyncDisposable
    {
        public AppDbContext Db { get; }
        private DbScope(AppDbContext db) => Db = db;

        public static DbScope Create()
        {
            var name = $"fibradis_market_tests_{Guid.NewGuid():N}";
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer($"Server=LAPBADIS;Database={name};Trusted_Connection=True;TrustServerCertificate=True")
                .Options;
            var db = new AppDbContext(opts);
            db.Database.EnsureCreated();
            return new DbScope(db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.Database.EnsureDeletedAsync();
            await Db.DisposeAsync();
        }
    }

    private static DailySnapshot CreateSnapshot(Guid fibraId, DateOnly date, decimal close = 100m) => new()
    {
        Id = Guid.NewGuid(),
        FibraId = fibraId,
        Ticker = "FUNO11",
        Date = date,
        Close = close,
    };

    [Fact]
    public async Task GetLatestDailySnapshotDateAsync_WhenRecordsExist_ReturnsMaxDate()
    {
        var fibraId = Guid.NewGuid();
        await using var scope = DbScope.Create();
        scope.Db.DailySnapshots.AddRange(
            CreateSnapshot(fibraId, new DateOnly(2026, 6, 8)),
            CreateSnapshot(fibraId, new DateOnly(2026, 6, 10)),
            CreateSnapshot(fibraId, new DateOnly(2026, 6, 9)));
        await scope.Db.SaveChangesAsync();

        var repo = new MarketRepository(scope.Db);
        var result = await repo.GetLatestDailySnapshotDateAsync(fibraId, CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 6, 10), result);
    }

    [Fact]
    public async Task GetLatestDailySnapshotDateAsync_WhenNoRecords_ReturnsNull()
    {
        await using var scope = DbScope.Create();
        var repo = new MarketRepository(scope.Db);

        var result = await repo.GetLatestDailySnapshotDateAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAllDailySnapshotsAsync_DeletesAllRows()
    {
        var fibraId = Guid.NewGuid();
        await using var scope = DbScope.Create();
        scope.Db.DailySnapshots.AddRange(
            CreateSnapshot(fibraId, new DateOnly(2026, 6, 8)),
            CreateSnapshot(fibraId, new DateOnly(2026, 6, 9)));
        await scope.Db.SaveChangesAsync();

        var repo = new MarketRepository(scope.Db);
        await repo.DeleteAllDailySnapshotsAsync(CancellationToken.None);

        Assert.Equal(0, await scope.Db.DailySnapshots.CountAsync());
    }
}
