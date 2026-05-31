using Domain.Catalog;
using Domain.News;
using Infrastructure.Persistence.Repositories.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories;

public class NewsRepositoryPublicPagedTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static Fibra CreateFibra(Guid id, string ticker) => new()
    {
        Id = id,
        Ticker = ticker,
        YahooTicker = $"{ticker}.MX",
        FullName = $"Fibra {ticker}",
        ShortName = ticker,
        Sector = "Industrial",
        Market = "BMV",
        Currency = "MXN",
        State = FibraState.Active,
        NameVariants = [ticker],
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static NewsArticle CreateArticle(string title, DateTimeOffset publishedAt, NewsArticleStatus status = NewsArticleStatus.Processed) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        TitleNormalized = title.ToLowerInvariant(),
        Source = "Fuente",
        PublishedAt = publishedAt,
        Url = $"https://example.com/{Guid.NewGuid():N}",
        Snippet = $"Snippet de {title}",
        Status = status,
        CapturedAt = publishedAt,
    };

    [Fact]
    public async Task GetPagedPublicAsync_ReturnsProcessedArticlesOrderedAndTickerMap()
    {
        await using var db = CreateDbContext();
        var fibraA = CreateFibra(Guid.NewGuid(), "FUNO11");
        var fibraB = CreateFibra(Guid.NewGuid(), "DANHOS13");
        var newest = CreateArticle("FUNO11 anuncia expansión", DateTimeOffset.UtcNow.AddHours(-1));
        var middle = CreateArticle("Mercado reacciona a FIBRA", DateTimeOffset.UtcNow.AddHours(-2));
        var oldest = CreateArticle("Panorama industrial", DateTimeOffset.UtcNow.AddHours(-3));

        db.Fibras.AddRange(fibraA, fibraB);
        db.NewsArticles.AddRange(newest, middle, oldest);
        db.NewsArticleFibras.AddRange(
            new NewsArticleFibra { NewsArticleId = newest.Id, FibraId = fibraA.Id },
            new NewsArticleFibra { NewsArticleId = newest.Id, FibraId = fibraB.Id },
            new NewsArticleFibra { NewsArticleId = middle.Id, FibraId = fibraA.Id });
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);

        var (items, total, tickersByArticleId) = await repo.GetPagedPublicAsync(1, 20, null, null, CancellationToken.None);

        Assert.Equal(3, total);
        Assert.Collection(items,
            item => Assert.Equal(newest.Id, item.Id),
            item => Assert.Equal(middle.Id, item.Id),
            item => Assert.Equal(oldest.Id, item.Id));
        Assert.Equal(["DANHOS13", "FUNO11"], tickersByArticleId[newest.Id].Select(t => t.Ticker).ToArray());
        Assert.Equal(["FUNO11"], tickersByArticleId[middle.Id].Select(t => t.Ticker).ToArray());
        Assert.False(tickersByArticleId.ContainsKey(oldest.Id));
        Assert.All(tickersByArticleId[newest.Id], t => Assert.NotEqual(Guid.Empty, t.FibraId));
    }

    [Fact]
    public async Task GetPagedPublicAsync_AppliesTitleFilter()
    {
        await using var db = CreateDbContext();
        db.NewsArticles.AddRange(
            CreateArticle("FUNO11 supera expectativas", DateTimeOffset.UtcNow.AddHours(-1)),
            CreateArticle("Sector hotelero se ajusta", DateTimeOffset.UtcNow.AddHours(-2)),
            CreateArticle("Otra nota sobre FUNO11", DateTimeOffset.UtcNow.AddHours(-3)));
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);

        var (items, total, _) = await repo.GetPagedPublicAsync(1, 20, "FUNO11", null, CancellationToken.None);

        Assert.Equal(2, total);
        Assert.All(items, item => Assert.Contains("FUNO11", item.Title, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetPagedPublicAsync_AppliesFibraFilter()
    {
        await using var db = CreateDbContext();
        var fibraA = CreateFibra(Guid.NewGuid(), "FUNO11");
        var fibraB = CreateFibra(Guid.NewGuid(), "TERRA13");
        var articleA = CreateArticle("FUNO11 cierra trimestre", DateTimeOffset.UtcNow.AddHours(-1));
        var articleB = CreateArticle("TERRA13 firma contrato", DateTimeOffset.UtcNow.AddHours(-2));

        db.Fibras.AddRange(fibraA, fibraB);
        db.NewsArticles.AddRange(articleA, articleB);
        db.NewsArticleFibras.AddRange(
            new NewsArticleFibra { NewsArticleId = articleA.Id, FibraId = fibraA.Id },
            new NewsArticleFibra { NewsArticleId = articleB.Id, FibraId = fibraB.Id });
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);

        var (items, total, tickersByArticleId) = await repo.GetPagedPublicAsync(1, 20, null, fibraB.Id, CancellationToken.None);

        var single = Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal(articleB.Id, single.Id);
        Assert.Equal(["TERRA13"], tickersByArticleId[articleB.Id].Select(t => t.Ticker).ToArray());
        Assert.Equal(fibraB.Id, tickersByArticleId[articleB.Id][0].FibraId);
    }

    [Fact]
    public async Task GetPagedPublicAsync_ReturnsEmptyItemsButPreservesTotalForOutOfRangePage()
    {
        await using var db = CreateDbContext();
        db.NewsArticles.AddRange(
            CreateArticle("Nota 1", DateTimeOffset.UtcNow.AddHours(-1)),
            CreateArticle("Nota 2", DateTimeOffset.UtcNow.AddHours(-2)));
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);

        var (items, total, tickersByArticleId) = await repo.GetPagedPublicAsync(99, 20, null, null, CancellationToken.None);

        Assert.Empty(items);
        Assert.Equal(2, total);
        Assert.Empty(tickersByArticleId);
    }

    [Fact]
    public async Task GetPagedPublicAsync_ExcludesPendingDeletedAndPartialArticles()
    {
        await using var db = CreateDbContext();
        var deleted = CreateArticle("Deleted hidden", DateTimeOffset.UtcNow.AddHours(-5));
        deleted.DeletedAt = DateTimeOffset.UtcNow;

        db.NewsArticles.AddRange(
            CreateArticle("Processed visible", DateTimeOffset.UtcNow.AddHours(-1)),
            CreateArticle("Pending hidden", DateTimeOffset.UtcNow.AddHours(-2), NewsArticleStatus.Pending),
            CreateArticle("Partial hidden", DateTimeOffset.UtcNow.AddHours(-3), NewsArticleStatus.Partial),
            CreateArticle("Error hidden", DateTimeOffset.UtcNow.AddHours(-4), NewsArticleStatus.Error),
            deleted);
        await db.SaveChangesAsync();

        var repo = new NewsRepository(db);

        var (items, total, _) = await repo.GetPagedPublicAsync(1, 20, null, null, CancellationToken.None);

        var single = Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal("Processed visible", single.Title);
    }
}
