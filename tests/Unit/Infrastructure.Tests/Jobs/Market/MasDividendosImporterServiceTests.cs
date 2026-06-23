using Domain.Catalog;
using Domain.Market;
using Infrastructure.Integrations.MasDividendos;
using Infrastructure.Jobs.Market;
using Infrastructure.Persistence.Repositories.Market;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs.Market;

public class MasDividendosImporterServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly DateOnly FuturePago = Today.AddDays(30);
    private static readonly DateOnly FutureExDate = Today.AddDays(29);
    private static readonly DateOnly PastPago = Today.AddDays(-30);

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static Fibra CreateFibra(Guid id) => new()
    {
        Id = id,
        Ticker = "FCFE18",
        YahooTicker = "FCFE18.MX",
        FullName = "CFE Fibra E",
        ShortName = "CFE Fibra E",
        Sector = "Infraestructura",
        Market = "BMV",
        Currency = "MXN",
        State = FibraState.Active,
    };

    private static MasDividendosRecord Record(string ticker, string? monto, DateOnly? fechaPago, DateOnly? exDate) =>
        new("1", "CFE Capital", ticker, monto, "Resultado fiscal", fechaPago, exDate, null);

    private static MasDividendosImporterService CreateService(AppDbContext db, params MasDividendosRecord[] records) =>
        new(
            new FakeMasDividendosClient(records),
            new MarketRepository(db),
            NullLogger<MasDividendosImporterService>.Instance);

    [Fact]
    public async Task FutureEvent_WithNoExistingRow_InsertsAnnouncedDistribution()
    {
        var fibraId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var service = CreateService(db, Record("FCFE18", "$0.61", FuturePago, FutureExDate));

        var result = await service.ImportAsync([CreateFibra(fibraId)]);

        Assert.Equal(1, result.Inserted);
        var stored = await db.Distributions.SingleAsync(d => d.FibraId == fibraId);
        Assert.Equal("masdividendos", stored.Source);
        Assert.Equal(FuturePago, stored.PaymentDate);
        Assert.Equal(0.61m, stored.AmountPerUnit);
    }

    [Fact]
    public async Task PastEvent_WithNoExistingRow_DoesNotInsert()
    {
        var fibraId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var service = CreateService(db, Record("FCFE18", "$0.61", PastPago, PastPago.AddDays(-1)));

        var result = await service.ImportAsync([CreateFibra(fibraId)]);

        Assert.Equal(0, result.Inserted);
        Assert.Equal(0, await db.Distributions.CountAsync());
    }

    [Fact]
    public async Task FutureEvent_WithExistingRow_EnrichesWithoutDuplicating()
    {
        var fibraId = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.Distributions.Add(new Distribution
        {
            Id = Guid.NewGuid(),
            FibraId = fibraId,
            Ticker = "FCFE18",
            PaymentDate = FuturePago,
            AmountPerUnit = 0.62m,
            Currency = "MXN",
            Source = "yahoo",
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, Record("FCFE18", "$0.61", FuturePago, FutureExDate));
        var result = await service.ImportAsync([CreateFibra(fibraId)]);

        Assert.Equal(0, result.Inserted);
        Assert.Equal(1, await db.Distributions.CountAsync(d => d.FibraId == fibraId));
        var stored = await db.Distributions.SingleAsync(d => d.FibraId == fibraId);
        Assert.Equal("yahoo", stored.Source); // no se altera la procedencia confirmada
        Assert.Equal(FutureExDate, stored.ExDividendDate); // enriquecido
    }

    [Fact]
    public async Task FutureEvent_WithUnparseableAmount_DoesNotInsert()
    {
        var fibraId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var service = CreateService(db, Record("FCFE18", null, FuturePago, FutureExDate));

        var result = await service.ImportAsync([CreateFibra(fibraId)]);

        Assert.Equal(0, result.Inserted);
        Assert.Equal(0, await db.Distributions.CountAsync());
    }

    private sealed class FakeMasDividendosClient(IReadOnlyList<MasDividendosRecord> records) : IMasDividendosClient
    {
        public Task<IReadOnlyList<MasDividendosRecord>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(records);
    }
}
