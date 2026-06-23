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

    [Fact]
    public async Task InsertAnnounced_WhenNoExistingRow_InsertsMasDividendosSource()
    {
        var fibraId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var repo = new MarketRepository(db);

        var inserted = await repo.InsertAnnouncedDistributionIfAbsentAsync(
            fibraId,
            "FCFE18",
            new DateOnly(2026, 6, 30), // fecha de pago futura
            new DateOnly(2026, 6, 29), // ex-derecho
            0.61m,
            0.31m,
            0.30m,
            "https://www.bmv.com.mx/docs-pub/aviso.pdf",
            "MXN");

        Assert.True(inserted);
        var stored = await db.Distributions.SingleAsync(d => d.FibraId == fibraId);
        Assert.Equal("masdividendos", stored.Source);
        Assert.Equal(new DateOnly(2026, 6, 30), stored.PaymentDate);
        Assert.Equal(new DateOnly(2026, 6, 29), stored.ExDividendDate);
        Assert.Equal(0.61m, stored.AmountPerUnit);
        Assert.Equal(0.31m, stored.TaxableAmount);
    }

    [Fact]
    public async Task InsertAnnounced_WhenYahooRowMatchesByExDate_DoesNotDuplicate()
    {
        // Yahoo ya insertó la fila usando la ex-derecho como PaymentDate.
        var fibraId = Guid.NewGuid();
        var yahooExDate = new DateOnly(2026, 6, 29);
        var masFechaPago = new DateOnly(2026, 6, 30);

        await using var db = CreateDbContext();
        db.Distributions.Add(new Distribution
        {
            Id = Guid.NewGuid(),
            FibraId = fibraId,
            Ticker = "FCFE18",
            PaymentDate = yahooExDate,
            AmountPerUnit = 0.62m,
            Currency = "MXN",
            Source = "yahoo",
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var repo = new MarketRepository(db);
        var inserted = await repo.InsertAnnouncedDistributionIfAbsentAsync(
            fibraId, "FCFE18", masFechaPago, yahooExDate, 0.61m, null, null, null, "MXN");

        Assert.False(inserted);
        Assert.Equal(1, await db.Distributions.CountAsync(d => d.FibraId == fibraId));
    }

    [Fact]
    public async Task UpsertDistribution_ReconcilesAnnouncedRow_ByExDate_IntoSingleRow()
    {
        // Fila "anunciada" por masdividendos: PaymentDate = fecha de pago real, ExDividendDate = ex-derecho.
        var fibraId = Guid.NewGuid();
        var masFechaPago = new DateOnly(2026, 6, 30);
        var exDate = new DateOnly(2026, 6, 29);

        await using var db = CreateDbContext();
        db.Distributions.Add(new Distribution
        {
            Id = Guid.NewGuid(),
            FibraId = fibraId,
            Ticker = "FCFE18",
            PaymentDate = masFechaPago,
            ExDividendDate = exDate,
            AmountPerUnit = 0.50m,
            Currency = "MXN",
            Source = "masdividendos",
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var repo = new MarketRepository(db);

        // Yahoo confirma: usa la ex-derecho como PaymentDate y trae el monto real.
        var wasInserted = await repo.UpsertDistributionAsync(new Distribution
        {
            Id = Guid.NewGuid(),
            FibraId = fibraId,
            Ticker = "FCFE18",
            PaymentDate = exDate,
            AmountPerUnit = 0.62m,
            Currency = "MXN",
            Source = "yahoo",
            CapturedAt = DateTimeOffset.UtcNow,
        });

        Assert.False(wasInserted); // reconciliado, no insertado
        var stored = await db.Distributions.SingleAsync(d => d.FibraId == fibraId);
        Assert.Equal("yahoo", stored.Source);
        Assert.Equal(0.62m, stored.AmountPerUnit);     // monto de Yahoo (fuente de verdad)
        Assert.Equal(masFechaPago, stored.PaymentDate); // fecha de pago precisa preservada
        Assert.Equal(exDate, stored.ExDividendDate);
    }
}
