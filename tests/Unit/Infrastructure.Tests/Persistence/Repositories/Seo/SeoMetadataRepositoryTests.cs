using Application.Seo;
using Domain.Seo;
using Infrastructure.Persistence.Repositories.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories.Seo;

public class SeoMetadataRepositoryTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static SeoMetadata CreateSeoMetadata(
        SeoPageType pageType = SeoPageType.StaticPage,
        string entityKey = "/contacto",
        string title = "Contacto | FIBRADIS") => new()
    {
        Id = Guid.NewGuid(),
        PageType = pageType,
        EntityKey = entityKey,
        Title = title,
        MetaDescription = "Texto de prueba para la descripción SEO del contacto.",
        CanonicalPath = entityKey,
        OgTitle = title,
        OgDescription = "Texto de prueba para OG.",
        OgType = "website",
        OgImageUrl = "https://fibrasinmobiliarias.com/og-image.png",
        OgLocale = "es_MX",
        TwitterCard = "summary_large_image",
        RobotsDirectives = "index,follow",
        JsonLd = "{\"@context\":\"https://schema.org\"}",
        IsActive = true,
        UpdatedAt = DateTimeOffset.UtcNow,
        UpdatedBy = "adminops@test.com",
    };

    [Fact]
    public async Task GetAsync_ReturnsRowByCompositeKey()
    {
        await using var db = CreateDbContext();
        db.SeoMetadata.Add(CreateSeoMetadata());
        await db.SaveChangesAsync();

        var repo = new SeoMetadataRepository(db);

        var result = await repo.GetAsync(SeoPageType.StaticPage, "/contacto", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("/contacto", result!.EntityKey);
        Assert.Equal("Contacto | FIBRADIS", result.Title);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByPageTypeAndSearch()
    {
        await using var db = CreateDbContext();
        db.SeoMetadata.AddRange(
            CreateSeoMetadata(SeoPageType.StaticPage, "/contacto", "Contacto | FIBRADIS"),
            CreateSeoMetadata(SeoPageType.StaticPage, "/acerca", "Acerca de FIBRADIS"),
            CreateSeoMetadata(SeoPageType.News, "noticia-1", "Noticias | FIBRADIS"));
        await db.SaveChangesAsync();

        var repo = new SeoMetadataRepository(db);

        var result = await repo.GetAllAsync(new SeoMetadataQuery(SeoPageType.StaticPage, "Contacto"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("/contacto", result[0].EntityKey);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrueForExistingRow()
    {
        await using var db = CreateDbContext();
        db.SeoMetadata.Add(CreateSeoMetadata());
        await db.SaveChangesAsync();

        var repo = new SeoMetadataRepository(db);

        var exists = await repo.ExistsAsync(SeoPageType.StaticPage, "/contacto", CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalseForMissingRow()
    {
        await using var db = CreateDbContext();
        var repo = new SeoMetadataRepository(db);

        var exists = await repo.ExistsAsync(SeoPageType.StaticPage, "/contacto", CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task UpsertAsync_RespectsOverrideFlags_WhenOverrideModeIsFalse()
    {
        await using var db = CreateDbContext();
        var existing = CreateSeoMetadata();
        existing.Title = "Título manual";
        existing.TitleIsOverridden = true;
        existing.MetaDescription = "Descripción manual";
        existing.MetaDescriptionIsOverridden = false;
        db.SeoMetadata.Add(existing);
        await db.SaveChangesAsync();

        var repo = new SeoMetadataRepository(db);
        var incoming = CreateSeoMetadata(title: "Título regenerado");
        incoming.MetaDescription = "Descripción regenerada";

        await repo.UpsertAsync(incoming, overrideMode: false, CancellationToken.None);

        var persisted = await repo.GetAsync(SeoPageType.StaticPage, "/contacto", CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal("Título manual", persisted!.Title);
        Assert.Equal("Descripción regenerada", persisted.MetaDescription);
    }

    [Fact]
    public async Task UpsertAsync_OverwritesAllFields_WhenOverrideModeIsTrue()
    {
        await using var db = CreateDbContext();
        var existing = CreateSeoMetadata();
        existing.Title = "Título manual";
        existing.TitleIsOverridden = true;
        db.SeoMetadata.Add(existing);
        await db.SaveChangesAsync();

        var repo = new SeoMetadataRepository(db);
        var incoming = CreateSeoMetadata(title: "Título regenerado");

        await repo.UpsertAsync(incoming, overrideMode: true, CancellationToken.None);

        var persisted = await repo.GetAsync(SeoPageType.StaticPage, "/contacto", CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal("Título regenerado", persisted!.Title);
    }

    [Fact]
    public async Task GetExistingKeysAsync_ReturnsOnlyMatches()
    {
        await using var db = CreateDbContext();
        db.SeoMetadata.Add(CreateSeoMetadata());
        await db.SaveChangesAsync();

        var repo = new SeoMetadataRepository(db);
        var keys = new[]
        {
            (SeoPageType.StaticPage, "/contacto"),
            (SeoPageType.News, "noticia-1"),
        };

        var existing = await repo.GetExistingKeysAsync(keys, CancellationToken.None);

        Assert.Single(existing);
        Assert.Contains(existing, key => key.PageType == SeoPageType.StaticPage && key.EntityKey == "/contacto");
    }
}
