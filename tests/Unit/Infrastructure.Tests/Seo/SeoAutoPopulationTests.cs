using Application.Seo;
using Domain.Catalog;
using Domain.News;
using Domain.Seo;
using Infrastructure.Persistence.Repositories.Catalog;
using Infrastructure.Persistence.Repositories.Seo;
using Infrastructure.Persistence.Repositories.News;
using Infrastructure.Persistence.SqlServer;
using Infrastructure.Seo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Tests.Seo;

// AC-5/AC-6 de 12-1: las filas SeoMetadata se auto-llenan al crear contenido y se regeneran al
// actualizarlo sin pisar overrides manuales. Las deps SEO son opcionales en los repos; aquí se
// inyectan para ejercitar la ruta de auto-población.
public class SeoAutoPopulationTests
{
    private const string BaseUrl = "https://fibradis.mx";

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:BaseUrl"] = BaseUrl })
            .Build();

    private static (NewsRepository Repo, SeoMetadataRepository SeoRepo) CreateNewsRepo(AppDbContext db)
    {
        var seoRepo = new SeoMetadataRepository(db);
        return (new NewsRepository(db, seoRepo, new SeoDefaultsBuilder(), CreateConfig()), seoRepo);
    }

    private static (FibraRepository Repo, SeoMetadataRepository SeoRepo) CreateFibraRepo(AppDbContext db)
    {
        var seoRepo = new SeoMetadataRepository(db);
        return (new FibraRepository(db, seoRepo, new SeoDefaultsBuilder(), CreateConfig()), seoRepo);
    }

    private static NewsArticle CreateArticle(string title) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        TitleNormalized = title.ToLowerInvariant(),
        Source = "Fuente",
        PublishedAt = DateTimeOffset.UtcNow.AddHours(-1),
        Url = $"https://example.com/{Guid.NewGuid():N}",
        Snippet = $"Snippet sobre {title} con suficiente longitud para una descripción válida de SEO según la convención del proyecto.",
        Status = NewsArticleStatus.Processed,
        CapturedAt = DateTimeOffset.UtcNow,
    };

    private static Fibra CreateFibra(string ticker = "FUNO11") => new()
    {
        Id = Guid.NewGuid(),
        Ticker = ticker,
        YahooTicker = $"{ticker}.MX",
        FullName = $"Fibra {ticker}",
        ShortName = ticker,
        Sector = "Diversificado",
        Market = "BMV",
        Currency = "MXN",
        State = FibraState.Active,
        NameVariants = [ticker],
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task AddWithLinksAsync_AutoCreatesSeoRow_ForNews()
    {
        await using var db = CreateDbContext();
        var (repo, seoRepo) = CreateNewsRepo(db);
        var article = CreateArticle("FUNO11 reporta resultados del 2T25");

        await repo.AddWithLinksAsync(article, []);

        var row = await seoRepo.GetAsync(SeoPageType.News, article.Slug!);
        Assert.NotNull(row);
        Assert.Equal(article.Slug, row!.EntityKey);
        Assert.Equal("article", row.OgType);
        Assert.False(row.TitleIsOverridden);
        Assert.Equal($"/noticias/{article.Slug}", row.CanonicalPath);
    }

    [Fact]
    public async Task AddAsync_AutoCreatesSeoRow_ForFibra()
    {
        await using var db = CreateDbContext();
        var (repo, seoRepo) = CreateFibraRepo(db);

        await repo.AddAsync(CreateFibra("TERRA13"));

        var row = await seoRepo.GetAsync(SeoPageType.Fibra, "TERRA13");
        Assert.NotNull(row);
        Assert.Equal("TERRA13", row!.EntityKey);
        Assert.Contains("TERRA13", row.Title);
        Assert.False(row.MetaDescriptionIsOverridden);
    }

    [Fact]
    public async Task WithoutSeoDeps_AddDoesNotThrow_AndCreatesNoSeoRow()
    {
        await using var db = CreateDbContext();
        var newsRepo = new NewsRepository(db);
        var fibraRepo = new FibraRepository(db);

        await newsRepo.AddWithLinksAsync(CreateArticle("Sin deps SEO"), []);
        await fibraRepo.AddAsync(CreateFibra("DANHOS13"));

        Assert.Empty(await db.SeoMetadata.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_Fibra_RegeneratesButRespectsTitleOverride()
    {
        await using var db = CreateDbContext();
        var (repo, seoRepo) = CreateFibraRepo(db);
        var fibra = CreateFibra("FMTY14");
        await repo.AddAsync(fibra);

        // Simula edición manual de Ops: marca Title como override con un valor custom.
        var manual = await seoRepo.GetAsync(SeoPageType.Fibra, "FMTY14");
        manual!.Title = "Título editado a mano";
        manual.TitleIsOverridden = true;
        await seoRepo.UpsertAsync(manual, overrideMode: true);

        // Cambia el nombre y actualiza ⇒ regen (overrideMode:false) NO debe pisar el Title override.
        fibra.FullName = "Fibra Monterrey Renombrada";
        await repo.UpdateAsync(fibra);

        var row = await seoRepo.GetAsync(SeoPageType.Fibra, "FMTY14");
        Assert.Equal("Título editado a mano", row!.Title);
        // La description NO está override ⇒ se regenera con el nuevo nombre.
        Assert.Contains("Fibra Monterrey Renombrada", row.MetaDescription);
    }

    // Nota: el regen tras UpdateAiAnalysisAsync/UpdateSummaryAsync NO se puede unit-testear con
    // InMemory porque esos métodos usan ExecuteUpdateAsync (no soportado por el provider InMemory).
    // La mecánica de regen-respeta-override queda cubierta por UpdateAsync_Fibra_* (arriba); el path
    // de noticias se valida en integración sobre SQL Server real.
}
