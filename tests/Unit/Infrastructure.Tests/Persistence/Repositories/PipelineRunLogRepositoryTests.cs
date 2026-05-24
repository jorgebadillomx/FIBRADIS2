using Domain.Jobs;
using Infrastructure.Persistence.Repositories.Jobs;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories;

public class PipelineRunLogRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsEntry()
    {
        await using var db = CreateDbContext();
        var repo = new PipelineRunLogRepository(db);
        var entry = new PipelineRunLog
        {
            Id = Guid.NewGuid(),
            Pipeline = "Market",
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow.AddMinutes(1),
            Status = "Completed",
            ItemsProcessed = 10,
            ErrorCount = 0,
            Details = "{\"processed\":10,\"errors\":0}",
        };

        await repo.AddAsync(entry);

        var persisted = await db.PipelineRunLogs.SingleAsync();
        Assert.Equal("Market", persisted.Pipeline);
        Assert.Equal("Completed", persisted.Status);
        Assert.Equal(10, persisted.ItemsProcessed);
    }

    [Fact]
    public async Task GetRecentAsync_WithPipeline_ReturnsLatestFiveDescending()
    {
        await using var db = CreateDbContext();
        SeedRunLogs(db);
        var repo = new PipelineRunLogRepository(db);

        var result = await repo.GetRecentAsync("Market", 5);

        Assert.Equal(5, result.Count);
        Assert.All(result, entry => Assert.Equal("Market", entry.Pipeline));
        Assert.True(result.Zip(result.Skip(1)).All(pair => pair.First.StartedAt >= pair.Second.StartedAt));
        Assert.Equal(1, result[0].ItemsProcessed);
        Assert.Equal(5, result[^1].ItemsProcessed);
    }

    [Fact]
    public async Task GetRecentAsync_WithoutPipeline_ReturnsLatestFiveAcrossAllPipelines()
    {
        await using var db = CreateDbContext();
        SeedRunLogs(db);
        var repo = new PipelineRunLogRepository(db);

        var result = await repo.GetRecentAsync(null, 5);

        Assert.Equal(5, result.Count);
        Assert.Contains(result, entry => entry.Pipeline == "News");
        Assert.Contains(result, entry => entry.Pipeline == "Distribution");
        Assert.True(result.Zip(result.Skip(1)).All(pair => pair.First.StartedAt >= pair.Second.StartedAt));
    }

    [Fact]
    public async Task GetLastCompletedAsync_IgnoresQueuedAndReturnsMostRecentCompletedOrFailed()
    {
        await using var db = CreateDbContext();
        var baseTime = new DateTimeOffset(2026, 5, 23, 12, 0, 0, TimeSpan.Zero);
        db.PipelineRunLogs.AddRange(
            new PipelineRunLog
            {
                Id = Guid.NewGuid(),
                Pipeline = "Market",
                StartedAt = baseTime.AddMinutes(-15),
                CompletedAt = baseTime.AddMinutes(-10),
                Status = "Completed",
                ItemsProcessed = 3,
                ErrorCount = 0,
            },
            new PipelineRunLog
            {
                Id = Guid.NewGuid(),
                Pipeline = "Market",
                StartedAt = baseTime.AddMinutes(-5),
                Status = "Queued",
                TriggeredBy = "adminops@test.com",
            },
            new PipelineRunLog
            {
                Id = Guid.NewGuid(),
                Pipeline = "Market",
                StartedAt = baseTime.AddMinutes(-3),
                CompletedAt = baseTime,
                Status = "Failed",
                ItemsProcessed = 1,
                ErrorCount = 2,
            });
        await db.SaveChangesAsync();

        var repo = new PipelineRunLogRepository(db);
        var result = await repo.GetLastCompletedAsync("Market");

        Assert.NotNull(result);
        Assert.Equal("Failed", result!.Status);
        Assert.NotEqual("Queued", result.Status);
        Assert.Equal(2, result.ErrorCount);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static void SeedRunLogs(AppDbContext db)
    {
        var baseTime = new DateTimeOffset(2026, 5, 23, 12, 0, 0, TimeSpan.Zero);
        db.PipelineRunLogs.AddRange(
            Enumerable.Range(1, 8).Select(index => new PipelineRunLog
            {
                Id = Guid.NewGuid(),
                Pipeline = "Market",
                StartedAt = baseTime.AddMinutes(-(index + 10)),
                CompletedAt = baseTime.AddMinutes(-(index + 9)),
                Status = "Completed",
                ItemsProcessed = index,
                ErrorCount = 0,
            }));

        db.PipelineRunLogs.AddRange(
            new PipelineRunLog
            {
                Id = Guid.NewGuid(),
                Pipeline = "News",
                StartedAt = baseTime.AddMinutes(-2),
                CompletedAt = baseTime.AddMinutes(-1),
                Status = "Completed",
                ItemsProcessed = 12,
                ErrorCount = 1,
            },
            new PipelineRunLog
            {
                Id = Guid.NewGuid(),
                Pipeline = "Distribution",
                StartedAt = baseTime.AddMinutes(-4),
                CompletedAt = baseTime.AddMinutes(-3),
                Status = "Completed",
                ItemsProcessed = 5,
                ErrorCount = 0,
            });

        db.SaveChanges();
    }
}
