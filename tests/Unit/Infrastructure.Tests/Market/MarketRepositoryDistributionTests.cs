using Domain.Market;
using Infrastructure.Persistence.Repositories.Market;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Market;

public class MarketRepositoryDistributionTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static Distribution CreateDistribution(Guid fibraId, string ticker, DateOnly paymentDate, decimal amount) => new()
    {
        Id = Guid.NewGuid(),
        FibraId = fibraId,
        Ticker = ticker,
        PaymentDate = paymentDate,
        AmountPerUnit = amount,
        Currency = "MXN",
        Source = "seed",
        CapturedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task GetDistributionsByRange_WithNoRows_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var repo = new MarketRepository(db);

        var result = await repo.GetDistributionsByRangeAsync(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDistributionsByRange_ReturnsOnlyItemsWithinRangeOrderedByDate()
    {
        var fibraId = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.Distributions.AddRange(
            CreateDistribution(fibraId, "FUNO11", new DateOnly(2026, 5, 31), 0.30m),
            CreateDistribution(fibraId, "FUNO11", new DateOnly(2026, 6, 10), 0.31m),
            CreateDistribution(fibraId, "FUNO11", new DateOnly(2026, 6, 20), 0.32m),
            CreateDistribution(fibraId, "FUNO11", new DateOnly(2026, 7, 1), 0.33m));
        await db.SaveChangesAsync();

        var repo = new MarketRepository(db);
        var result = await repo.GetDistributionsByRangeAsync(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));

        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2026, 6, 10), result[0].PaymentDate);
        Assert.Equal(new DateOnly(2026, 6, 20), result[1].PaymentDate);
    }

    [Fact]
    public async Task UpdateDistributionBreakdown_FallsBackToExDate_WhenPaymentDateMismatches()
    {
        // Yahoo Finance stores the ex-dividend date as PaymentDate.
        // MasDividendos provides the actual FechaPago (different date).
        // The fallback must match using FechaExDerecho == Distribution.PaymentDate.
        var fibraId = Guid.NewGuid();
        var yahooExDate = new DateOnly(2026, 6, 5);   // what Yahoo stored as PaymentDate
        var masFechaPago = new DateOnly(2026, 6, 20); // actual payment date from MasDividendos

        await using var db = CreateDbContext();
        db.Distributions.Add(CreateDistribution(fibraId, "FUNO11", yahooExDate, 0.31m));
        await db.SaveChangesAsync();

        var repo = new MarketRepository(db);

        // Primary lookup by masFechaPago fails; fallback by yahooExDate succeeds.
        var result = await repo.UpdateDistributionBreakdownAsync(
            fibraId,
            masFechaPago,
            yahooExDate,
            0.21m,
            0.10m,
            null);

        Assert.True(result);
        var stored = await db.Distributions.FirstAsync(d => d.FibraId == fibraId);
        Assert.Equal(0.21m, stored.TaxableAmount);
        Assert.Equal(0.10m, stored.CapitalReturnAmount);
        Assert.Equal(yahooExDate, stored.ExDividendDate);
    }

    [Fact]
    public async Task UpdateDistributionBreakdown_SeedsNullFields_AndUpdatesOnValueChange()
    {
        var fibraId = Guid.NewGuid();
        var paymentDate = new DateOnly(2026, 6, 20);

        await using var db = CreateDbContext();
        db.Distributions.Add(CreateDistribution(fibraId, "FUNO11", paymentDate, 0.31m));
        await db.SaveChangesAsync();

        var repo = new MarketRepository(db);

        var firstUpdate = await repo.UpdateDistributionBreakdownAsync(
            fibraId,
            paymentDate,
            new DateOnly(2026, 6, 18),
            0.21m,
            0.10m,
            "https://www.bmv.com.mx/docs-pub/aviso.pdf");

        Assert.True(firstUpdate);
        var stored = await db.Distributions.FirstAsync(d => d.FibraId == fibraId && d.PaymentDate == paymentDate);
        Assert.Equal(new DateOnly(2026, 6, 18), stored.ExDividendDate);
        Assert.Equal(0.21m, stored.TaxableAmount);
        Assert.Equal(0.10m, stored.CapitalReturnAmount);
        Assert.Equal("https://www.bmv.com.mx/docs-pub/aviso.pdf", stored.AvisoUrl);

        var secondUpdate = await repo.UpdateDistributionBreakdownAsync(
            fibraId,
            paymentDate,
            new DateOnly(2026, 6, 19),
            0.99m,
            0.88m,
            "https://www.bmv.com.mx/docs-pub/otro.pdf");

        Assert.True(secondUpdate);
        stored = await db.Distributions.FirstAsync(d => d.FibraId == fibraId && d.PaymentDate == paymentDate);
        Assert.Equal(new DateOnly(2026, 6, 19), stored.ExDividendDate);
        Assert.Equal(0.99m, stored.TaxableAmount);
        Assert.Equal(0.88m, stored.CapitalReturnAmount);
        Assert.Equal("https://www.bmv.com.mx/docs-pub/aviso.pdf", stored.AvisoUrl);
    }
}
