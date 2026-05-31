using Domain.Catalog;
using Domain.Fundamentals;
using Infrastructure.Persistence.Repositories.Fundamentals;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories;

public class FundamentalesRepositorySummaryTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static Fibra CreateFibra(Guid id, string ticker = "FUNO11") => new()
    {
        Id = id,
        Ticker = ticker,
        FullName = "Fibra Test",
        ShortName = "Test",
        Sector = "Diversificado",
        Market = "BMV",
        Currency = "MXN",
        State = FibraState.Active,
        NameVariants = [],
        CreatedAt = DateTime.UtcNow,
    };

    private static FundamentalRecord CreateRecord(Guid fibraId, string period, string status = "processed") => new()
    {
        Id = Guid.NewGuid(),
        FibraId = fibraId,
        Period = period,
        Status = status,
        ProcessingMode = "manual",
        CapRate = 0.08m,
        CapturedAt = DateTimeOffset.UtcNow,
        ConfirmedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task GetSummaryLatestAsync_Returns_OneRowPerFibra()
    {
        await using var db = CreateDbContext();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        db.Fibras.AddRange(CreateFibra(id1, "FUNO11"), CreateFibra(id2, "FMTY14"));
        db.FundamentalRecords.AddRange(
            CreateRecord(id1, "Q3-2024"),
            CreateRecord(id2, "Q3-2024"));
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetSummaryLatestAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetSummaryLatestAsync_Returns_MostRecentPeriodPerFibra()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId, "FUNO11"));
        db.FundamentalRecords.AddRange(
            CreateRecord(fibraId, "Q1-2024"),
            CreateRecord(fibraId, "Q3-2024"),
            CreateRecord(fibraId, "Q2-2025"));
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetSummaryLatestAsync();

        Assert.Single(result);
        Assert.Equal("Q2-2025", result[0].Record.Period);
    }

    [Fact]
    public async Task GetSummaryByPeriodAsync_Returns_OnlyMatchingPeriod()
    {
        await using var db = CreateDbContext();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        db.Fibras.AddRange(CreateFibra(id1, "FUNO11"), CreateFibra(id2, "FMTY14"));
        db.FundamentalRecords.AddRange(
            CreateRecord(id1, "Q3-2024"),
            CreateRecord(id2, "Q3-2024"),
            CreateRecord(id1, "Q1-2024"));
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetSummaryByPeriodAsync("Q3-2024");

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal("Q3-2024", r.Record.Period));
    }

    [Fact]
    public async Task GetSummaryByPeriodAsync_Returns_EmptyList_WhenNoPeriodData()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId, "FUNO11"));
        db.FundamentalRecords.Add(CreateRecord(fibraId, "Q3-2024"));
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetSummaryByPeriodAsync("Q1-2099");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllProcessedPeriodsAsync_Returns_DistinctPeriods()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId, "FUNO11"));
        db.FundamentalRecords.AddRange(
            CreateRecord(fibraId, "Q1-2024"),
            CreateRecord(fibraId, "Q1-2024"),
            CreateRecord(fibraId, "Q3-2024"));
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetAllProcessedPeriodsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetSummaryLatestAsync_Excludes_NonProcessed_And_SoftDeleted()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId, "FUNO11"));
        var pending = CreateRecord(fibraId, "Q3-2024", "pending");
        var softDeleted = new FundamentalRecord
        {
            Id = Guid.NewGuid(),
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "processed",
            ProcessingMode = "manual",
            CapturedAt = DateTimeOffset.UtcNow,
            DeletedAt = DateTimeOffset.UtcNow,
        };
        db.FundamentalRecords.AddRange(pending, softDeleted);
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetSummaryLatestAsync();

        Assert.Empty(result);
    }
}
