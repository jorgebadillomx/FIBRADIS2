using Infrastructure.Persistence.Repositories.Ops;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories;

public class EditorialPageRepositoryTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    [Fact]
    public async Task GetAllAsync_ReturnsFivePages()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        var repo = new EditorialPageRepository(db);

        var pages = await repo.GetAllAsync(CancellationToken.None);

        Assert.Equal(5, pages.Count);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagesOrderedByDisplayOrder()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        var repo = new EditorialPageRepository(db);

        var pages = await repo.GetAllAsync(CancellationToken.None);

        Assert.Equal(
            ["que-son-las-fibras", "historia", "como-se-estructuran", "por-que-invertir", "regimen-fiscal"],
            pages.Select(page => page.Slug).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_NoPageHasNullOrEmptyContent()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        var repo = new EditorialPageRepository(db);

        var pages = await repo.GetAllAsync(CancellationToken.None);

        Assert.All(pages, page => Assert.False(string.IsNullOrWhiteSpace(page.Content)));
    }
}
