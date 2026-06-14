using Application.Seo;
using Domain.Seo;
using Infrastructure.Persistence.Repositories.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories.Seo;

public class RedirectRepositoryTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveRedirectsAndNormalizesPaths()
    {
        await using var db = CreateDbContext();
        var repo = new RedirectRepository(db);
        await repo.AddAsync(CreateRedirect("/Blog/", "/Noticias/", 301, true), CancellationToken.None);
        await repo.AddAsync(CreateRedirect("/Catalogo/", "/Fibras/", 302, false), CancellationToken.None);

        var result = await repo.GetActiveAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("/blog", result[0].FromPath);
        Assert.Equal("/noticias", result[0].ToPath);
        Assert.Equal(301, result[0].StatusCode);
    }

    [Fact]
    public async Task GetByFromPathAsync_NormalizesTheLookupPath()
    {
        await using var db = CreateDbContext();
        var repo = new RedirectRepository(db);
        await repo.AddAsync(CreateRedirect("/Blog/", "/Noticias/", 301, true), CancellationToken.None);

        var result = await repo.GetByFromPathAsync("/BLOG/", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("/blog", result!.FromPath);
        Assert.Equal("/noticias", result.ToPath);
    }

    [Fact]
    public async Task UpdateAsync_NormalizesStoredValues()
    {
        await using var db = CreateDbContext();
        var repo = new RedirectRepository(db);
        var redirect = CreateRedirect("/Blog/", "/Noticias/", 301, true);
        await repo.AddAsync(redirect, CancellationToken.None);

        redirect.FromPath = "/Aviso-De-Privacidad/";
        redirect.ToPath = "/Privacidad/";
        redirect.Notes = "  Redirección manual  ";

        await repo.UpdateAsync(redirect, CancellationToken.None);

        var persisted = await repo.GetByIdAsync(redirect.Id, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal("/aviso-de-privacidad", persisted!.FromPath);
        Assert.Equal("/privacidad", persisted.ToPath);
        Assert.Equal("Redirección manual", persisted.Notes);
    }

    private static UrlRedirect CreateRedirect(string fromPath, string toPath, int statusCode, bool isActive) => new()
    {
        Id = Guid.NewGuid(),
        FromPath = fromPath,
        ToPath = toPath,
        StatusCode = statusCode,
        IsActive = isActive,
        Notes = null,
        CreatedAt = DateTimeOffset.UtcNow,
        CreatedBy = "adminops@test.com",
        UpdatedAt = DateTimeOffset.UtcNow,
        UpdatedBy = "adminops@test.com",
    };
}
