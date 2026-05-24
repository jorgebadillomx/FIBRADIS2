using Domain.Catalog;
using Domain.Fundamentals;
using Infrastructure.Persistence.Repositories.Fundamentals;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories;

public class FundamentalRepositoryTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static Fibra CreateFibra(Guid id) => new()
    {
        Id = id,
        Ticker = "FUNO11",
        FullName = "Fibra Uno",
        ShortName = "Fibra Uno",
        Sector = "Diversificado",
        Market = "BMV",
        Currency = "MXN",
        State = FibraState.Active,
        NameVariants = [],
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task AddAsync_PersistsRecord()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var record = new FundamentalRecord
        {
            Id = Guid.NewGuid(),
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "pending",
            ProcessingMode = "manual",
            CapRate = 0.08m,
            CapturedAt = DateTimeOffset.UtcNow,
        };

        await repo.AddAsync(record, CancellationToken.None);

        Assert.Equal(1, await db.FundamentalRecords.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenExists()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var id = Guid.NewGuid();
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = id,
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "pending",
            ProcessingMode = "manual",
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetByIdAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Q3-2024", result!.Period);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        await using var db = CreateDbContext();
        var repo = new FundamentalRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProcessedByFibraAndPeriodAsync_ReturnsOnlyProcessed()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        db.FundamentalRecords.AddRange(
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q3-2024",
                Status = "processed", ProcessingMode = "manual",
                CapturedAt = DateTimeOffset.UtcNow,
            },
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q3-2024",
                Status = "pending", ProcessingMode = "manual",
                CapturedAt = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetProcessedByFibraAndPeriodAsync(fibraId, "Q3-2024", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("processed", result!.Status);
    }

    [Fact]
    public async Task GetLatestProcessedByFibraAsync_ReturnsMostRecentProcessed()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var older = DateTimeOffset.UtcNow.AddDays(-2);
        var newer = DateTimeOffset.UtcNow.AddDays(-1);
        db.FundamentalRecords.AddRange(
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q2-2024",
                Status = "processed", ProcessingMode = "manual",
                CapturedAt = older,
            },
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q3-2024",
                Status = "processed", ProcessingMode = "manual",
                CapturedAt = newer,
            });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetLatestProcessedByFibraAsync(fibraId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Q3-2024", result!.Period);
    }

    [Fact]
    public async Task GetByFibraAsync_ReturnsAllRecordsOrderedByDateDesc()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        db.FundamentalRecords.AddRange(
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q1-2024",
                Status = "processed", ProcessingMode = "manual",
                CapturedAt = DateTimeOffset.UtcNow.AddDays(-5),
            },
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q3-2024",
                Status = "pending", ProcessingMode = "manual",
                CapturedAt = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var results = await repo.GetByFibraAsync(fibraId, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("Q3-2024", results[0].Period);
        Assert.Equal("Q1-2024", results[1].Period);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesStatusOnly()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var id = Guid.NewGuid();
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = id,
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "pending",
            ProcessingMode = "manual",
            CapRate = 0.09m,
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        await repo.UpdateStatusAsync(id, "processed", "jorge", DateTimeOffset.UtcNow, CancellationToken.None);

        var updated = await db.FundamentalRecords.FindAsync(id);
        Assert.NotNull(updated);
        Assert.Equal("processed", updated!.Status);
        Assert.Equal("jorge", updated.ConfirmedBy);
    }

    [Fact]
    public async Task UpdateStatusAsync_IsIdempotent_WhenAlreadyProcessed()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var id = Guid.NewGuid();
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = id,
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "processed",
            ProcessingMode = "manual",
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        await repo.UpdateStatusAsync(id, "processed", "jorge2", DateTimeOffset.UtcNow, CancellationToken.None);

        var record = await db.FundamentalRecords.FindAsync(id);
        Assert.Null(record!.ConfirmedBy);
    }
}
