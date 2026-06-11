using Domain.Market;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence;

public class DistributionConfigurationTests
{
    [Fact]
    public async Task ExDividendDate_CanBePersistedAndRead()
    {
        await using var db = CreateDbContext();
        var dist = BuildDistribution(exDate: new DateOnly(2026, 6, 8));
        db.Distributions.Add(dist);
        await db.SaveChangesAsync();

        var loaded = await db.Distributions.FindAsync(dist.Id);

        Assert.NotNull(loaded);
        Assert.Equal(new DateOnly(2026, 6, 8), loaded.ExDividendDate);
    }

    [Fact]
    public async Task TaxableAmount_CanBePersistedAndRead()
    {
        await using var db = CreateDbContext();
        var dist = BuildDistribution(taxable: 0.44m);
        db.Distributions.Add(dist);
        await db.SaveChangesAsync();

        var loaded = await db.Distributions.FindAsync(dist.Id);

        Assert.NotNull(loaded);
        Assert.Equal(0.44m, loaded.TaxableAmount);
    }

    [Fact]
    public async Task CapitalReturnAmount_CanBePersistedAndRead()
    {
        await using var db = CreateDbContext();
        var dist = BuildDistribution(capital: 0.22m);
        db.Distributions.Add(dist);
        await db.SaveChangesAsync();

        var loaded = await db.Distributions.FindAsync(dist.Id);

        Assert.NotNull(loaded);
        Assert.Equal(0.22m, loaded.CapitalReturnAmount);
    }

    [Fact]
    public async Task AvisoUrl_CanBePersistedAndRead()
    {
        await using var db = CreateDbContext();
        var dist = BuildDistribution(avisoUrl: "https://bmv.com.mx/avisos/123.pdf");
        db.Distributions.Add(dist);
        await db.SaveChangesAsync();

        var loaded = await db.Distributions.FindAsync(dist.Id);

        Assert.NotNull(loaded);
        Assert.Equal("https://bmv.com.mx/avisos/123.pdf", loaded.AvisoUrl);
    }

    [Fact]
    public async Task NewColumns_AreNullableAndDefaultToNull()
    {
        await using var db = CreateDbContext();
        var dist = BuildDistribution();
        db.Distributions.Add(dist);
        await db.SaveChangesAsync();

        var loaded = await db.Distributions.FindAsync(dist.Id);

        Assert.NotNull(loaded);
        Assert.Null(loaded.ExDividendDate);
        Assert.Null(loaded.TaxableAmount);
        Assert.Null(loaded.CapitalReturnAmount);
        Assert.Null(loaded.AvisoUrl);
    }

    private static Distribution BuildDistribution(
        DateOnly? exDate = null,
        decimal? taxable = null,
        decimal? capital = null,
        string? avisoUrl = null)
        => new()
        {
            Id = Guid.NewGuid(),
            FibraId = Guid.NewGuid(),
            Ticker = "TEST11",
            PaymentDate = new DateOnly(2026, 6, 15),
            ExDividendDate = exDate,
            AmountPerUnit = 0.50m,
            TaxableAmount = taxable,
            CapitalReturnAmount = capital,
            AvisoUrl = avisoUrl,
            Currency = "MXN",
            Source = "test",
            CapturedAt = DateTimeOffset.UtcNow,
        };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}
