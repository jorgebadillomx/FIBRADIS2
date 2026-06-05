using Infrastructure.Persistence.Repositories.Portfolio;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Portfolio;

public class UserFavoritesRepositoryTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    [Fact]
    public async Task GetFavoriteIds_Empty_ReturnsEmptyList()
    {
        await using var db = CreateDb();
        var repo = new UserFavoritesRepository(db);

        var result = await repo.GetFavoriteIdsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Add_AddsToList()
    {
        await using var db = CreateDb();
        var repo = new UserFavoritesRepository(db);
        var userId = Guid.NewGuid();
        var fibraId = Guid.NewGuid();

        await repo.AddAsync(userId, fibraId, CancellationToken.None);

        var result = await repo.GetFavoriteIdsAsync(userId, CancellationToken.None);

        Assert.Contains(fibraId, result);
    }

    [Fact]
    public async Task Add_Idempotent_DoesNotDuplicate()
    {
        await using var db = CreateDb();
        var repo = new UserFavoritesRepository(db);
        var userId = Guid.NewGuid();
        var fibraId = Guid.NewGuid();

        await repo.AddAsync(userId, fibraId, CancellationToken.None);
        await repo.AddAsync(userId, fibraId, CancellationToken.None);

        var result = await repo.GetFavoriteIdsAsync(userId, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(fibraId, result[0]);
    }

    [Fact]
    public async Task Remove_RemovesFromList()
    {
        await using var db = CreateDb();
        var repo = new UserFavoritesRepository(db);
        var userId = Guid.NewGuid();
        var fibraId = Guid.NewGuid();

        await repo.AddAsync(userId, fibraId, CancellationToken.None);
        await repo.RemoveAsync(userId, fibraId, CancellationToken.None);

        var result = await repo.GetFavoriteIdsAsync(userId, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Remove_Idempotent_DoesNotThrow()
    {
        await using var db = CreateDb();
        var repo = new UserFavoritesRepository(db);

        await repo.RemoveAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task GetFavoriteIds_ReturnsOnlyForUserId()
    {
        await using var db = CreateDb();
        var repo = new UserFavoritesRepository(db);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var fibraId = Guid.NewGuid();

        await repo.AddAsync(userA, fibraId, CancellationToken.None);

        var result = await repo.GetFavoriteIdsAsync(userB, CancellationToken.None);

        Assert.Empty(result);
    }
}
