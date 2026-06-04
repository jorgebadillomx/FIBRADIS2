using Domain.Ops;
using Infrastructure.Persistence.Repositories.Ops;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories;

public class OperationalConfigRepositoryTests
{
    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenNoRow()
    {
        await using var db = CreateDbContext(ensureCreated: false);
        var repo = new OperationalConfigRepository(db);

        var config = await repo.GetAsync();

        Assert.Equal(0.006m, config.CommissionFactor);
        Assert.Equal(4, config.AvgPeriods);
        Assert.Equal(1440, config.NewsCadenceMinutes);
    }

    [Fact]
    public async Task GetAsync_ReturnsSeedRow()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        var config = await repo.GetAsync();

        Assert.Equal(1, config.Id);
        Assert.Equal(0.006m, config.CommissionFactor);
        Assert.Equal(4, config.AvgPeriods);
        Assert.Equal(1440, config.NewsCadenceMinutes);
        Assert.Equal("system", config.UpdatedBy);
    }

    [Fact]
    public async Task UpdateAsync_CommissionFactor_UpdatesAndAudits()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        await repo.UpdateAsync(0.008m, null, null, null, null, null, null, null, null, "adminops@test.com");

        var config = await db.OperationalConfigs.SingleAsync();
        var audit = await db.ConfigAuditLogs.SingleAsync();

        Assert.Equal(0.008m, config.CommissionFactor);
        Assert.Equal("adminops@test.com", config.UpdatedBy);
        Assert.Equal("commission_factor", audit.FieldName);
        Assert.Equal("0.006", audit.PreviousValue);
        Assert.Equal("0.008", audit.NewValue);
    }

    [Fact]
    public async Task UpdateAsync_AvgPeriods_UpdatesAndAudits()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        await repo.UpdateAsync(null, 6, null, null, null, null, null, null, null, "adminops@test.com");

        var config = await db.OperationalConfigs.SingleAsync();
        var audit = await db.ConfigAuditLogs.SingleAsync();

        Assert.Equal(6, config.AvgPeriods);
        Assert.Equal("avg_periods", audit.FieldName);
        Assert.Equal("4", audit.PreviousValue);
        Assert.Equal("6", audit.NewValue);
    }

    [Fact]
    public async Task UpdateAsync_NewsCadenceMinutes_UpdatesAndAudits()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        var config = await db.OperationalConfigs.SingleAsync();
        config.NewsCadenceMinutes = 60;
        await db.SaveChangesAsync();

        await repo.UpdateAsync(null, null, 1440, null, null, null, null, null, null, "adminops@test.com");

        config = await db.OperationalConfigs.SingleAsync();
        var audit = await db.ConfigAuditLogs.SingleAsync();

        Assert.Equal(1440, config.NewsCadenceMinutes);
        Assert.Equal("news_cadence_minutes", audit.FieldName);
        Assert.Equal("60", audit.PreviousValue);
        Assert.Equal("1440", audit.NewValue);
    }

    [Fact]
    public async Task UpdateAsync_NoChanges_CreatesNoAuditEntries()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        await repo.UpdateAsync(0.006m, 4, 1440, null, null, null, null, null, null, "adminops@test.com");

        Assert.Empty(db.ConfigAuditLogs);
    }

    [Fact]
    public async Task UpdateAsync_MultipleFields_CreatesSeparateAuditEntries()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        await repo.UpdateAsync(0.008m, 6, null, null, null, null, null, null, null, "adminops@test.com");

        var audits = await db.ConfigAuditLogs
            .OrderBy(x => x.FieldName)
            .ToListAsync();

        Assert.Equal(2, audits.Count);
        Assert.Collection(
            audits,
            entry => Assert.Equal("avg_periods", entry.FieldName),
            entry => Assert.Equal("commission_factor", entry.FieldName));
    }

    private static AppDbContext CreateDbContext(bool ensureCreated = true)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var db = new AppDbContext(options);
        if (ensureCreated)
        {
            db.Database.EnsureCreated();
        }

        return db;
    }
}
