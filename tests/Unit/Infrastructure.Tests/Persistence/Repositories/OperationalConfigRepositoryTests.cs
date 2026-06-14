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
        Assert.Equal(2880, config.FundamentalsCadenceMinutes);
        Assert.Null(config.Cetes28dRate);
        Assert.Null(config.Cetes28dRateUpdatedAt);
        Assert.Null(config.Tiie28dRate);
        Assert.Null(config.Tiie28dRateUpdatedAt);
        Assert.Null(config.OrganizationSameAsJson);
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
        Assert.Equal(2880, config.FundamentalsCadenceMinutes);
        Assert.Equal("system", config.UpdatedBy);
        Assert.Null(config.Cetes28dRate);
        Assert.Null(config.Cetes28dRateUpdatedAt);
        Assert.Null(config.Tiie28dRate);
        Assert.Null(config.Tiie28dRateUpdatedAt);
    }

    [Fact]
    public async Task UpdateCetesRateAsync_UpdatesRateAndTimestamp()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);
        var updatedAt = new DateTimeOffset(2026, 6, 12, 18, 30, 0, TimeSpan.Zero);

        await repo.UpdateCetesRateAsync(9.5m, updatedAt);

        var config = await db.OperationalConfigs.SingleAsync();

        Assert.Equal(9.5m, config.Cetes28dRate);
        Assert.Equal(updatedAt, config.Cetes28dRateUpdatedAt);
        Assert.Equal(updatedAt, config.UpdatedAt);
    }

    [Fact]
    public async Task UpdateTiieRateAsync_UpdatesTiieColumns()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);
        var updatedAt = new DateTimeOffset(2026, 6, 12, 18, 30, 0, TimeSpan.Zero);

        await repo.UpdateTiieRateAsync(10.25m, updatedAt);

        var config = await db.OperationalConfigs.SingleAsync();

        Assert.Equal(10.25m, config.Tiie28dRate);
        Assert.Equal(updatedAt, config.Tiie28dRateUpdatedAt);
        Assert.Equal(updatedAt, config.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_CommissionFactor_UpdatesAndAudits()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        await repo.UpdateAsync(0.008m, null, null, null, null, null, null, null, "adminops@test.com");

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

        await repo.UpdateAsync(null, 6, null, null, null, null, null, null, "adminops@test.com");

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

        await repo.UpdateAsync(null, null, 1440, null, null, null, null, null, "adminops@test.com");

        config = await db.OperationalConfigs.SingleAsync();
        var audit = await db.ConfigAuditLogs.SingleAsync();

        Assert.Equal(1440, config.NewsCadenceMinutes);
        Assert.Equal("news_cadence_minutes", audit.FieldName);
        Assert.Equal("60", audit.PreviousValue);
        Assert.Equal("1440", audit.NewValue);
    }

    [Fact]
    public async Task UpdateAsync_FundamentalsCadenceMinutes_UpdatesAndAudits()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        await repo.UpdateAsync(null, null, null, null, null, null, null, null, "adminops@test.com", 720);

        var config = await db.OperationalConfigs.SingleAsync();
        var audit = await db.ConfigAuditLogs.SingleAsync();

        Assert.Equal(720, config.FundamentalsCadenceMinutes);
        Assert.Equal("fundamentals_cadence_minutes", audit.FieldName);
        Assert.Equal("2880", audit.PreviousValue);
        Assert.Equal("720", audit.NewValue);
    }

    [Fact]
    public async Task UpdateAsync_NoChanges_CreatesNoAuditEntries()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        await repo.UpdateAsync(0.006m, 4, 1440, null, null, null, null, null, "adminops@test.com");

        Assert.Empty(db.ConfigAuditLogs);
    }

    [Fact]
    public async Task UpdateAsync_MultipleFields_CreatesSeparateAuditEntries()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        await repo.UpdateAsync(0.008m, 6, null, null, null, null, null, null, "adminops@test.com");

        var audits = await db.ConfigAuditLogs
            .OrderBy(x => x.FieldName)
            .ToListAsync();

        Assert.Equal(2, audits.Count);
        Assert.Collection(
            audits,
            entry => Assert.Equal("avg_periods", entry.FieldName),
            entry => Assert.Equal("commission_factor", entry.FieldName));
    }

    [Fact]
    public async Task UpdateAsync_TermsEnabled_UpdatesAndAudits()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        await repo.UpdateAsync(null, null, null, null, null, true, null, null, "adminops@test.com");

        var config = await db.OperationalConfigs.SingleAsync();
        var audit = await db.ConfigAuditLogs.SingleAsync();

        Assert.True(config.TermsEnabled);
        Assert.Equal("terms_enabled", audit.FieldName);
        Assert.Equal("false", audit.PreviousValue);
        Assert.Equal("true", audit.NewValue);
    }

    [Fact]
    public async Task UpdateAsync_ContactEmail_UpdatesAndAudits()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        await repo.UpdateAsync(null, null, null, null, null, null, null, "nuevo@fibradis.mx", "adminops@test.com");

        var config = await db.OperationalConfigs.SingleAsync();
        var audit = await db.ConfigAuditLogs.SingleAsync();

        Assert.Equal("nuevo@fibradis.mx", config.ContactEmail);
        Assert.Equal("contact_email", audit.FieldName);
        Assert.Equal("contacto@fibradis.mx", audit.PreviousValue);
        Assert.Equal("nuevo@fibradis.mx", audit.NewValue);
    }

    [Fact]
    public async Task UpdateOrganizationSameAsAsync_UpdatesJsonAndAudits()
    {
        await using var db = CreateDbContext();
        var repo = new OperationalConfigRepository(db);

        await repo.UpdateOrganizationSameAsAsync(
            """["https://www.youtube.com/@fibradis","https://www.instagram.com/fibradis"]""",
            "adminops@test.com");

        var config = await db.OperationalConfigs.SingleAsync();
        var audit = await db.ConfigAuditLogs.SingleAsync();

        Assert.Equal("""["https://www.youtube.com/@fibradis","https://www.instagram.com/fibradis"]""", config.OrganizationSameAsJson);
        Assert.Equal("organization_same_as_json", audit.FieldName);
        Assert.Equal("null", audit.PreviousValue);
        Assert.Equal("""["https://www.youtube.com/@fibradis","https://www.instagram.com/fibradis"]""", audit.NewValue);
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
