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

    private static Fibra CreateFibraInSector(string ticker, string sector, FibraState state = FibraState.Active)
    {
        var f = CreateFibra(ticker, state);
        f.Sector = sector;
        return f;
    }

    [Fact]
    public async Task GetActiveBySectorAsync_ReturnsSameSector_ExcludingSelf()
    {
        await using var db = CreateDbContext();
        var self = CreateFibraInSector("FUNO11", "Diversificado");
        db.Fibras.AddRange(
            self,
            CreateFibraInSector("FNOVA17", "Diversificado"),
            CreateFibraInSector("FPLUS16", "Diversificado"));
        await db.SaveChangesAsync();

        var repo = new FibraRepository(db);
        var result = await repo.GetActiveBySectorAsync("Diversificado", self.Id, 6, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, f => f.Id == self.Id);
        Assert.All(result, f => Assert.Equal("Diversificado", f.Sector));
        Assert.Contains(result, f => f.Ticker == "FNOVA17");
        Assert.Contains(result, f => f.Ticker == "FPLUS16");
    }

    [Fact]
    public async Task GetActiveBySectorAsync_ExcludesOtherSectors()
    {
        await using var db = CreateDbContext();
        var self = CreateFibraInSector("FUNO11", "Diversificado");
        db.Fibras.AddRange(
            self,
            CreateFibraInSector("TERRA13", "Industrial"),
            CreateFibraInSector("DANHOS13", "Comercial"));
        await db.SaveChangesAsync();

        var repo = new FibraRepository(db);
        var result = await repo.GetActiveBySectorAsync("Diversificado", self.Id, 6, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveBySectorAsync_ExcludesInactive()
    {
        await using var db = CreateDbContext();
        var self = CreateFibraInSector("FUNO11", "Diversificado");
        db.Fibras.AddRange(
            self,
            CreateFibraInSector("FNOVA17", "Diversificado"),
            CreateFibraInSector("FPLUS16", "Diversificado", FibraState.Inactive));
        await db.SaveChangesAsync();

        var repo = new FibraRepository(db);
        var result = await repo.GetActiveBySectorAsync("Diversificado", self.Id, 6, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("FNOVA17", result[0].Ticker);
    }

    [Fact]
    public async Task GetActiveBySectorAsync_RespectsCount()
    {
        await using var db = CreateDbContext();
        var self = CreateFibraInSector("FUNO11", "Industrial");
        db.Fibras.AddRange(
            self,
            CreateFibraInSector("TERRA13", "Industrial"),
            CreateFibraInSector("FMTY14", "Industrial"),
            CreateFibraInSector("VESTA15", "Industrial"),
            CreateFibraInSector("NEXT25", "Industrial"));
        await db.SaveChangesAsync();

        var repo = new FibraRepository(db);
        var result = await repo.GetActiveBySectorAsync("Industrial", self.Id, 2, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetActiveBySectorAsync_ZeroCountOrEmptySector_ReturnsEmpty()
    {
        await using var db = CreateDbContext();
        var self = CreateFibraInSector("FUNO11", "Diversificado");
        db.Fibras.AddRange(self, CreateFibraInSector("FNOVA17", "Diversificado"));
        await db.SaveChangesAsync();

        var repo = new FibraRepository(db);

        Assert.Empty(await repo.GetActiveBySectorAsync("Diversificado", self.Id, 0, CancellationToken.None));
        Assert.Empty(await repo.GetActiveBySectorAsync("", self.Id, 6, CancellationToken.None));
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
