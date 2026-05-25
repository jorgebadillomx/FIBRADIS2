using Domain.Catalog;
using Infrastructure.Persistence.Repositories.Catalog;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories;

public class FibraRepositoryTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static Fibra CreateFibra(
        string ticker = "FUNO11",
        FibraState state = FibraState.Active) => new()
    {
        Id = Guid.NewGuid(),
        Ticker = ticker,
        YahooTicker = $"{ticker}.MX",
        FullName = $"Fibra {ticker}",
        ShortName = ticker,
        Sector = "Diversificado",
        Market = "BMV",
        Currency = "MXN",
        State = state,
        SiteUrl = $"https://{ticker.ToLowerInvariant()}.example.com",
        InvestorUrl = null,
        ReportsUrl = null,
        NameVariants = [ticker],
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task AddAsync_PersistsFibra()
    {
        await using var db = CreateDbContext();
        var repo = new FibraRepository(db);
        var fibra = CreateFibra();

        await repo.AddAsync(fibra, CancellationToken.None);

        var persisted = await repo.GetByTickerAsync("FUNO11", CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Equal(fibra.Id, persisted!.Id);
    }

    [Fact]
    public async Task AddAsync_DuplicateTicker_Throws()
    {
        await using var db = CreateDbContext();
        var repo = new FibraRepository(db);

        await repo.AddAsync(CreateFibra(), CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            repo.AddAsync(CreateFibra(), CancellationToken.None));
    }

    [Fact]
    public async Task ExistsByTickerAsync_ExistingTicker_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        db.Fibras.Add(CreateFibra());
        await db.SaveChangesAsync();

        var repo = new FibraRepository(db);
        var exists = await repo.ExistsByTickerAsync("FUNO11", CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByTickerAsync_NonExistentTicker_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var repo = new FibraRepository(db);

        var exists = await repo.ExistsByTickerAsync("NOEXISTE", CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsByTickerAsync_CaseInsensitive()
    {
        await using var db = CreateDbContext();
        db.Fibras.Add(CreateFibra());
        await db.SaveChangesAsync();

        var repo = new FibraRepository(db);
        var exists = await repo.ExistsByTickerAsync("funo11", CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        await using var db = CreateDbContext();
        var fibra = CreateFibra();
        db.Fibras.Add(fibra);
        await db.SaveChangesAsync();

        var repo = new FibraRepository(db);
        fibra.FullName = "Fibra Uno Actualizada";

        await repo.UpdateAsync(fibra, CancellationToken.None);

        var persisted = await repo.GetByTickerAsync("FUNO11", CancellationToken.None);
        Assert.Equal("Fibra Uno Actualizada", persisted!.FullName);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatesFibra()
    {
        await using var db = CreateDbContext();
        var fibra = CreateFibra();
        db.Fibras.Add(fibra);
        await db.SaveChangesAsync();

        var repo = new FibraRepository(db);
        fibra.State = FibraState.Inactive;

        await repo.UpdateAsync(fibra, CancellationToken.None);

        var active = await repo.GetAllActiveAsync(CancellationToken.None);
        Assert.DoesNotContain(active, item => item.Ticker == "FUNO11");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllIncludingInactive()
    {
        await using var db = CreateDbContext();
        db.Fibras.AddRange(
            CreateFibra("ACTIVA1", FibraState.Active),
            CreateFibra("INACTIVA1", FibraState.Inactive));
        await db.SaveChangesAsync();

        var repo = new FibraRepository(db);
        var all = await repo.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, all.Count);
        Assert.Contains(all, item => item.Ticker == "INACTIVA1" && item.State == FibraState.Inactive);
    }
}
