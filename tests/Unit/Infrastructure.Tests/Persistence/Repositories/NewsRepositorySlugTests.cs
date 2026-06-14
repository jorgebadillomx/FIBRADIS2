using Domain.News;
using Domain.Seo;
using Infrastructure.Persistence.Repositories.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories;

public class NewsRepositorySlugTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static NewsArticle CreateArticle(string title, string? slug = null, NewsArticleStatus status = NewsArticleStatus.Processed, DateTimeOffset? publishedAt = null, DateTimeOffset? deletedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        TitleNormalized = title.ToLowerInvariant(),
        Slug = slug,
        Source = "Fuente",
        PublishedAt = publishedAt ?? DateTimeOffset.UtcNow.AddHours(-1),
        Url = $"https://example.com/{Guid.NewGuid():N}",
        Snippet = $"Snippet de {title}",
        Status = status,
        CapturedAt = DateTimeOffset.UtcNow,
        DeletedAt = deletedAt,
    };

    [Fact]
    public async Task GetBySlugAsync_HappyPath_ReturnsArticle()
    {
        await using var db = CreateDbContext();
        var article = CreateArticle("FUNO11 reporta resultados", slug: "funo11-reporta-resultados");
        db.NewsArticles.Add(article);
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);
        var result = await repo.GetBySlugAsync("funo11-reporta-resultados");

        Assert.NotNull(result);
        Assert.Equal(article.Id, result.Id);
    }

    [Fact]
    public async Task GetBySlugAsync_SlugNotFound_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new NewsRepository(db);

        Assert.Null(await repo.GetBySlugAsync("slug-inexistente"));
    }

    [Fact]
    public async Task GetBySlugAsync_DeletedArticle_ReturnsNull()
    {
        await using var db = CreateDbContext();
        db.NewsArticles.Add(CreateArticle("Borrado", slug: "borrado", deletedAt: DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);

        Assert.Null(await repo.GetBySlugAsync("borrado"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetBySlugAsync_NullOrEmpty_ReturnsNull(string? slug)
    {
        await using var db = CreateDbContext();
        db.NewsArticles.Add(CreateArticle("Cualquiera", slug: "cualquiera"));
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);

        Assert.Null(await repo.GetBySlugAsync(slug!));
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_NoDuplicate_ReturnsFreshSlug()
    {
        await using var db = CreateDbContext();
        var repo = new NewsRepository(db);

        var slug = await repo.GenerateUniqueSlugAsync("FUNO11 reporta resultados del 2T25");

        Assert.Equal("funo11-reporta-resultados-del-2t25", slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_DuplicateExists_ReturnsSuffixedSlug()
    {
        await using var db = CreateDbContext();
        db.NewsArticles.Add(CreateArticle("FUNO11 reporta resultados del 2T25", slug: "funo11-reporta-resultados-del-2t25"));
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);
        var slug = await repo.GenerateUniqueSlugAsync("FUNO11 reporta resultados del 2T25");

        Assert.Equal("funo11-reporta-resultados-del-2t25-2", slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_ExcludeId_ReturnsSameSlugForOwner()
    {
        await using var db = CreateDbContext();
        var article = CreateArticle("FUNO11 reporta resultados del 2T25", slug: "funo11-reporta-resultados-del-2t25");
        db.NewsArticles.Add(article);
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);
        var slug = await repo.GenerateUniqueSlugAsync("FUNO11 reporta resultados del 2T25", article.Id);

        // El dueño del slug no colisiona consigo mismo — backfill idempotente
        Assert.Equal("funo11-reporta-resultados-del-2t25", slug);
    }

    [Fact]
    public async Task GetArticlesWithoutSlugAsync_ReturnsOnlyNullSlugNotDeleted_RespectsBatchSize()
    {
        await using var db = CreateDbContext();
        db.NewsArticles.AddRange(
            CreateArticle("Sin slug 1"),
            CreateArticle("Sin slug 2"),
            CreateArticle("Sin slug 3"),
            CreateArticle("Con slug", slug: "con-slug"),
            CreateArticle("Borrado sin slug", deletedAt: DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);
        var batch = await repo.GetArticlesWithoutSlugAsync(batchSize: 2);

        Assert.Equal(2, batch.Count);
        Assert.All(batch, a => Assert.Null(a.Slug));
        Assert.All(batch, a => Assert.Null(a.DeletedAt));
    }

    [Fact]
    public async Task GetArticlesForSitemapAsync_FiltersAndOrdersByPublishedAtDesc()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        db.NewsArticles.AddRange(
            CreateArticle("Reciente", slug: "reciente", publishedAt: now.AddHours(-1)),
            CreateArticle("Antigua", slug: "antigua", publishedAt: now.AddDays(-2)),
            CreateArticle("Sin slug", publishedAt: now),
            CreateArticle("Pendiente", slug: "pendiente", status: NewsArticleStatus.Pending, publishedAt: now),
            CreateArticle("Borrada", slug: "borrada", deletedAt: now, publishedAt: now));
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);
        var rows = await repo.GetArticlesForSitemapAsync(limit: 500);

        Assert.Equal(2, rows.Count);
        Assert.Equal("reciente", rows[0].Slug);
        Assert.Equal("antigua", rows[1].Slug);
    }

    [Fact]
    public async Task GetArticlesForSitemapAsync_RespectsLimit()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            db.NewsArticles.Add(CreateArticle($"Nota {i}", slug: $"nota-{i}", publishedAt: now.AddHours(-i)));
        }
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);
        var rows = await repo.GetArticlesForSitemapAsync(limit: 3);

        Assert.Equal(3, rows.Count);
        Assert.Equal("nota-0", rows[0].Slug); // la más reciente primero
    }

    [Fact]
    public async Task GetArticlesForSitemapPageAsync_ExcludesNoindexAndSoftDeletedRows_AndReturnsTotal()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var visible = CreateArticle("Visible", slug: "visible", publishedAt: now.AddHours(-1));
        var noindex = CreateArticle("Noindex", slug: "noindex", publishedAt: now.AddHours(-2));
        var deleted = CreateArticle("Deleted", slug: "deleted", publishedAt: now.AddHours(-3), deletedAt: now);

        db.NewsArticles.AddRange(visible, noindex, deleted);
        db.SeoMetadata.Add(new SeoMetadata
        {
            Id = Guid.NewGuid(),
            PageType = SeoPageType.News,
            EntityKey = "noindex",
            Title = "SEO de prueba",
            MetaDescription = "Descripción de prueba",
            CanonicalPath = "/noticias/noindex",
            OgTitle = "SEO de prueba",
            OgDescription = "Descripción de prueba",
            OgType = "article",
            OgImageUrl = "https://example.com/og.png",
            OgLocale = "es_MX",
            TwitterCard = "summary_large_image",
            RobotsDirectives = "noindex,follow",
            JsonLd = null,
            IsActive = true,
            UpdatedAt = now,
            UpdatedBy = "test",
        });
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);
        var (items, total) = await repo.GetArticlesForSitemapPageAsync(page: 1, pageSize: 10);

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("visible", items[0].Slug);
    }

    [Theory]
    [InlineData("Paged", "paged-2")]
    [InlineData("Fibras", "fibras-2")]
    [InlineData("Related", "related-2")]
    public async Task GenerateUniqueSlugAsync_ReservedRouteLiteral_ReturnsSuffixedSlug(string title, string expected)
    {
        // 'paged'/'fibras'/'related' son literales de ruta: la ruta literal ganaría sobre /{slug}
        // y el artículo quedaría inalcanzable (o devolvería otro contenido con 200)
        await using var db = CreateDbContext();
        var repo = new NewsRepository(db);

        var slug = await repo.GenerateUniqueSlugAsync(title);

        Assert.Equal(expected, slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_GuidShapedTitle_ReturnsSuffixedSlug()
    {
        // Un slug GUID-parseable lo capturaría la ruta /{id:guid} y el artículo quedaría en 404
        await using var db = CreateDbContext();
        var repo = new NewsRepository(db);

        var slug = await repo.GenerateUniqueSlugAsync("aaaaaaaa-0000-0000-0000-000000000001");

        Assert.False(Guid.TryParse(slug, out _));
        Assert.Equal("aaaaaaaa-0000-0000-0000-000000000001-2", slug);
    }

    [Fact]
    public async Task AddWithLinksAsync_AutoGeneratesSlug()
    {
        await using var db = CreateDbContext();
        var repo = new NewsRepository(db);
        var article = CreateArticle("FUNO11 reporta resultados del 2T25");

        await repo.AddWithLinksAsync(article, []);

        Assert.Equal("funo11-reporta-resultados-del-2t25", article.Slug);
    }

    [Fact]
    public async Task AddWithLinksAsync_DuplicateTitle_GeneratesSuffixedSlug()
    {
        await using var db = CreateDbContext();
        var repo = new NewsRepository(db);

        await repo.AddWithLinksAsync(CreateArticle("FUNO11 reporta resultados del 2T25"), []);
        var second = CreateArticle("FUNO11 reporta resultados del 2T25");
        await repo.AddWithLinksAsync(second, []);

        Assert.Equal("funo11-reporta-resultados-del-2t25-2", second.Slug);
    }
}
