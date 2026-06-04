using System.Text.Json;
using Domain.Portfolio;
using Infrastructure.Persistence.Repositories.Portfolio;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Portfolio;

public class PortfolioRepositorySnapshotTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Fibra1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Fibra2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Fibra3 = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static PortfolioPosition MakePosition(
        Guid userId,
        Guid fibraId,
        int titulos = 100,
        decimal costoPromedio = 50m,
        DateTimeOffset? uploadedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        FibraId = fibraId,
        Titulos = titulos,
        CostoPromedio = costoPromedio,
        CostoTotalCompra = titulos * costoPromedio * 1.006m,
        UploadedAt = uploadedAt ?? DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task ArchivePortfolioAsync_ExistingPositions_CreatesSnapshotAndClearsPortfolio()
    {
        await using var db = CreateDb();
        db.PortfolioPositions.AddRange(
            MakePosition(UserA, Fibra1),
            MakePosition(UserA, Fibra2, titulos: 200, costoPromedio: 75m));
        await db.SaveChangesAsync();

        var repo = new PortfolioRepository(db);
        await repo.ArchivePortfolioAsync(UserA, CancellationToken.None);

        var positions = await repo.GetByUserIdAsync(UserA, CancellationToken.None);
        Assert.Empty(positions);

        var snapshot = await repo.GetSnapshotAsync(UserA, CancellationToken.None);
        Assert.NotNull(snapshot);
        Assert.NotEqual(default, snapshot.ArchivedAt);

        using var json = JsonDocument.Parse(snapshot.PositionsJson);
        Assert.Equal(2, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task RestoreSnapshotAsync_WithSnapshot_RebuildsPortfolioAndDeletesSnapshot()
    {
        await using var db = CreateDb();
        db.PortfolioPositions.AddRange(
            MakePosition(UserA, Fibra1),
            MakePosition(UserA, Fibra2, titulos: 200, costoPromedio: 75m));
        await db.SaveChangesAsync();

        var repo = new PortfolioRepository(db);
        await repo.ArchivePortfolioAsync(UserA, CancellationToken.None);
        var restored = await repo.RestoreSnapshotAsync(UserA, CancellationToken.None);

        Assert.True(restored);

        var positions = await repo.GetByUserIdAsync(UserA, CancellationToken.None);
        Assert.Equal(2, positions.Count);
        Assert.Contains(positions, p => p.FibraId == Fibra1);
        Assert.Contains(positions, p => p.FibraId == Fibra2);
        Assert.Null(await repo.GetSnapshotAsync(UserA, CancellationToken.None));
    }

    [Fact]
    public async Task UpsertPortfolioAsync_WithExistingPositions_CreatesSnapshotAndReplacesPortfolio()
    {
        await using var db = CreateDb();
        db.PortfolioPositions.AddRange(
            MakePosition(UserA, Fibra1),
            MakePosition(UserA, Fibra2));
        await db.SaveChangesAsync();

        var repo = new PortfolioRepository(db);
        await repo.UpsertPortfolioAsync(
            UserA,
            [MakePosition(UserA, Fibra3, titulos: 30, costoPromedio: 65m)],
            CancellationToken.None);

        var positions = await repo.GetByUserIdAsync(UserA, CancellationToken.None);
        Assert.Single(positions);
        Assert.Equal(Fibra3, positions[0].FibraId);

        var snapshot = await repo.GetSnapshotAsync(UserA, CancellationToken.None);
        Assert.NotNull(snapshot);

        using var json = JsonDocument.Parse(snapshot.PositionsJson);
        Assert.Equal(2, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task MergePositionsAsync_MergesExistingPositionsAndKeepsUnmentionedPositions()
    {
        await using var db = CreateDb();
        db.PortfolioPositions.AddRange(
            MakePosition(UserA, Fibra1, titulos: 100, costoPromedio: 50m),
            MakePosition(UserA, Fibra2, titulos: 40, costoPromedio: 80m));
        await db.SaveChangesAsync();

        var repo = new PortfolioRepository(db);
        await repo.MergePositionsAsync(
            UserA,
            [
                MakePosition(UserA, Fibra1, titulos: 25, costoPromedio: 60m),
                MakePosition(UserA, Fibra3, titulos: 10, costoPromedio: 90m),
            ],
            CancellationToken.None);

        var positions = await repo.GetByUserIdAsync(UserA, CancellationToken.None);
        Assert.Equal(3, positions.Count);

        var merged = Assert.Single(positions, p => p.FibraId == Fibra1);
        Assert.Equal(125, merged.Titulos);
        Assert.Equal(52m, Math.Round(merged.CostoPromedio, 0));
        Assert.Equal((100 * 50m * 1.006m) + (25 * 60m * 1.006m), merged.CostoTotalCompra);

        var untouched = Assert.Single(positions, p => p.FibraId == Fibra2);
        Assert.Equal(40, untouched.Titulos);

        Assert.Contains(positions, p => p.FibraId == Fibra3);
    }
}
