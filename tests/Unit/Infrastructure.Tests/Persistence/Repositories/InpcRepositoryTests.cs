using Domain.Ops;
using Infrastructure.Persistence.Repositories.Ops;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories;

public class InpcRepositoryTests
{
    [Fact]
    public async Task GetLatestPeriodoAsync_WhenEmpty_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new InpcRepository(db);

        var result = await repo.GetLatestPeriodoAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestPeriodoAsync_WhenHasRecords_ReturnsMaxPeriodo()
    {
        await using var db = CreateDbContext();
        db.InpcMonthlyEntries.AddRange(
            new InpcMonthlyEntry { Periodo = new DateOnly(2024, 4, 1), InpcIndex = 134.1258m, CapturedAt = DateTimeOffset.UtcNow },
            new InpcMonthlyEntry { Periodo = new DateOnly(2024, 6, 1), InpcIndex = 136.0000m, CapturedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var repo = new InpcRepository(db);
        var result = await repo.GetLatestPeriodoAsync();

        Assert.Equal(new DateOnly(2024, 6, 1), result);
    }

    [Fact]
    public async Task UpsertManyAsync_WhenPeriodoExists_UpdatesInpcIndex()
    {
        await using var db = CreateDbContext();
        db.InpcMonthlyEntries.Add(new InpcMonthlyEntry
        {
            Periodo = new DateOnly(2024, 4, 1),
            InpcIndex = 134.1258m,
            CapturedAt = new DateTimeOffset(2024, 4, 30, 12, 0, 0, TimeSpan.Zero),
        });
        await db.SaveChangesAsync();

        var repo = new InpcRepository(db);
        var updatedAt = new DateTimeOffset(2026, 6, 12, 18, 30, 0, TimeSpan.Zero);
        await repo.UpsertManyAsync([
            new InpcMonthlyEntry
            {
                Periodo = new DateOnly(2024, 4, 1),
                InpcIndex = 135.0000m,
                CapturedAt = updatedAt,
            }
        ]);

        var entry = await db.InpcMonthlyEntries.SingleAsync();
        Assert.Equal(135.0000m, entry.InpcIndex);
        Assert.Equal(updatedAt, entry.CapturedAt);
    }

    [Fact]
    public async Task UpsertManyAsync_WhenPeriodoNew_InsertsRecord()
    {
        await using var db = CreateDbContext();
        var repo = new InpcRepository(db);
        var capturedAt = new DateTimeOffset(2026, 6, 12, 18, 30, 0, TimeSpan.Zero);

        await repo.UpsertManyAsync([
            new InpcMonthlyEntry
            {
                Periodo = new DateOnly(2024, 4, 1),
                InpcIndex = 134.1258m,
                CapturedAt = capturedAt,
            }
        ]);

        var entry = await db.InpcMonthlyEntries.SingleAsync();
        Assert.Equal(new DateOnly(2024, 4, 1), entry.Periodo);
        Assert.Equal(134.1258m, entry.InpcIndex);
        Assert.Equal(capturedAt, entry.CapturedAt);
    }

    [Fact]
    public async Task GetLastAsync_ReturnsDescendingOrderedEntries()
    {
        await using var db = CreateDbContext();
        db.InpcMonthlyEntries.AddRange(
            new InpcMonthlyEntry { Periodo = new DateOnly(2024, 4, 1), InpcIndex = 134.1258m, CapturedAt = DateTimeOffset.UtcNow },
            new InpcMonthlyEntry { Periodo = new DateOnly(2024, 5, 1), InpcIndex = 135.5190m, CapturedAt = DateTimeOffset.UtcNow },
            new InpcMonthlyEntry { Periodo = new DateOnly(2024, 6, 1), InpcIndex = 136.0000m, CapturedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var repo = new InpcRepository(db);
        var result = await repo.GetLastAsync(2);

        Assert.Collection(
            result,
            entry => Assert.Equal(new DateOnly(2024, 6, 1), entry.Periodo),
            entry => Assert.Equal(new DateOnly(2024, 5, 1), entry.Periodo));
    }

    [Fact]
    public async Task GetRangeAsync_WhenEmpty_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var repo = new InpcRepository(db);

        var result = await repo.GetRangeAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 1));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRangeAsync_WhenHasRecords_ReturnsAscendingOrderedEntries()
    {
        await using var db = CreateDbContext();
        db.InpcMonthlyEntries.AddRange(
            new InpcMonthlyEntry { Periodo = new DateOnly(2024, 6, 1), InpcIndex = 136.0000m, CapturedAt = DateTimeOffset.UtcNow },
            new InpcMonthlyEntry { Periodo = new DateOnly(2024, 4, 1), InpcIndex = 134.1258m, CapturedAt = DateTimeOffset.UtcNow },
            new InpcMonthlyEntry { Periodo = new DateOnly(2024, 5, 1), InpcIndex = 135.5190m, CapturedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var repo = new InpcRepository(db);
        var result = await repo.GetRangeAsync(new DateOnly(2024, 4, 1), new DateOnly(2024, 6, 1));

        Assert.Collection(
            result,
            entry => Assert.Equal(new DateOnly(2024, 4, 1), entry.Periodo),
            entry => Assert.Equal(new DateOnly(2024, 5, 1), entry.Periodo),
            entry => Assert.Equal(new DateOnly(2024, 6, 1), entry.Periodo));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
