using Domain.Portfolio;
using Infrastructure.Persistence.Repositories.Portfolio;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Portfolio;

public class PortfolioRepositoryEditTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Fibra1 = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static PortfolioPosition MakePosition(Guid userId, Guid fibraId, int titulos = 100, decimal costoPromedio = 50m) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        FibraId = fibraId,
        Titulos = titulos,
        CostoPromedio = costoPromedio,
        CostoTotalCompra = titulos * costoPromedio * 1.006m,
        UploadedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task GetPosition_ExistingPosition_ReturnsPosition()
    {
        await using var db = CreateDb();
        db.PortfolioPositions.Add(MakePosition(UserA, Fibra1));
        await db.SaveChangesAsync();

        var repo = new PortfolioRepository(db);
        var result = await repo.GetPositionAsync(UserA, Fibra1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(UserA, result.UserId);
        Assert.Equal(Fibra1, result.FibraId);
    }

    [Fact]
    public async Task GetPosition_WrongUser_ReturnsNull()
    {
        await using var db = CreateDb();
        db.PortfolioPositions.Add(MakePosition(UserA, Fibra1));
        await db.SaveChangesAsync();

        var repo = new PortfolioRepository(db);
        var result = await repo.GetPositionAsync(UserB, Fibra1, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePosition_ChangesPersistedCorrectly()
    {
        await using var db = CreateDb();
        db.PortfolioPositions.Add(MakePosition(UserA, Fibra1, titulos: 100, costoPromedio: 50m));
        await db.SaveChangesAsync();

        var repo = new PortfolioRepository(db);
        var position = await repo.GetPositionAsync(UserA, Fibra1, CancellationToken.None);
        Assert.NotNull(position);

        position.Titulos = 600;
        position.CostoPromedio = 48m;
        position.CostoTotalCompra = 600 * 48m * 1.006m;
        await repo.UpdatePositionAsync(position, CancellationToken.None);

        var updated = await repo.GetPositionAsync(UserA, Fibra1, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(600, updated.Titulos);
        Assert.Equal(48m, updated.CostoPromedio);
        Assert.Equal(600 * 48m * 1.006m, updated.CostoTotalCompra);
    }

    [Fact]
    public async Task DeletePosition_RemovesFromDb()
    {
        await using var db = CreateDb();
        db.PortfolioPositions.Add(MakePosition(UserA, Fibra1));
        await db.SaveChangesAsync();

        var repo = new PortfolioRepository(db);
        var deleted = await repo.DeletePositionAsync(UserA, Fibra1, CancellationToken.None);

        Assert.True(deleted);
        var remaining = await repo.GetByUserIdAsync(UserA, CancellationToken.None);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DeletePosition_WrongUser_ReturnsFalse()
    {
        await using var db = CreateDb();
        db.PortfolioPositions.Add(MakePosition(UserA, Fibra1));
        await db.SaveChangesAsync();

        var repo = new PortfolioRepository(db);
        var deleted = await repo.DeletePositionAsync(UserB, Fibra1, CancellationToken.None);

        Assert.False(deleted);
        var remaining = await repo.GetByUserIdAsync(UserA, CancellationToken.None);
        Assert.Single(remaining);
    }
}
