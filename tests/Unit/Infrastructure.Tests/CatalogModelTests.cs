using Domain.Catalog;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Infrastructure.Tests;

public class CatalogModelTests
{
    [Fact]
    public void AppDbContext_SeedsAtLeastTenActiveFibras_ForPublicCatalog()
    {
        using var context = CreateContext();

        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var fibraEntity = designTimeModel.FindEntityType(typeof(Fibra));
        Assert.NotNull(fibraEntity);

        var seedData = fibraEntity!.GetSeedData();
        Assert.True(seedData.Count() >= 10);

        var funo = Assert.Single(seedData, item => Equals(item[nameof(Fibra.Ticker)], "FUNO11"));
        Assert.Equal("Fibra Uno", funo[nameof(Fibra.FullName)]);
        Assert.Equal(FibraState.Active.ToString(), funo[nameof(Fibra.State)]?.ToString());
        Assert.NotNull(funo[nameof(Fibra.NameVariants)]);
    }

    [Fact]
    public void AppDbContext_MapsFibraToCatalogSchema_WithExpectedSnakeCaseColumns()
    {
        using var context = CreateContext();

        var fibraEntity = Assert.IsAssignableFrom<IEntityType>(context.Model.FindEntityType(typeof(Fibra)));
        var table = StoreObjectIdentifier.Table("Fibra", "catalog");

        Assert.Equal("catalog", fibraEntity.GetSchema());
        Assert.Equal("Fibra", fibraEntity.GetTableName());
        Assert.Equal("ticker", fibraEntity.FindProperty(nameof(Fibra.Ticker))!.GetColumnName(table));
        Assert.Equal("full_name", fibraEntity.FindProperty(nameof(Fibra.FullName))!.GetColumnName(table));
        Assert.Equal("short_name", fibraEntity.FindProperty(nameof(Fibra.ShortName))!.GetColumnName(table));
        Assert.Equal("sector", fibraEntity.FindProperty(nameof(Fibra.Sector))!.GetColumnName(table));
        Assert.Equal("market", fibraEntity.FindProperty(nameof(Fibra.Market))!.GetColumnName(table));
        Assert.Equal("currency", fibraEntity.FindProperty(nameof(Fibra.Currency))!.GetColumnName(table));
        Assert.Equal("state", fibraEntity.FindProperty(nameof(Fibra.State))!.GetColumnName(table));
        Assert.Equal("site_url", fibraEntity.FindProperty(nameof(Fibra.SiteUrl))!.GetColumnName(table));
        Assert.Equal("investor_url", fibraEntity.FindProperty(nameof(Fibra.InvestorUrl))!.GetColumnName(table));
        Assert.Equal("reports_url", fibraEntity.FindProperty(nameof(Fibra.ReportsUrl))!.GetColumnName(table));
        Assert.Equal("name_variants", fibraEntity.FindProperty(nameof(Fibra.NameVariants))!.GetColumnName(table));
        Assert.Equal("created_at", fibraEntity.FindProperty(nameof(Fibra.CreatedAt))!.GetColumnName(table));
    }

    [Fact]
    public void AppDbContext_ConfiguresTickerAsUniqueIndex()
    {
        using var context = CreateContext();

        var fibraEntity = Assert.IsAssignableFrom<IEntityType>(context.Model.FindEntityType(typeof(Fibra)));
        var tickerIndex = Assert.Single(
            fibraEntity.GetIndexes(),
            index => index.Properties.Count == 1 &&
                     index.Properties[0].Name == nameof(Fibra.Ticker));

        Assert.True(tickerIndex.IsUnique);
        Assert.Equal("UX_Fibra_Ticker", tickerIndex.GetDatabaseName());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=LAPBADIS;Database=fibradis_model_tests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options);
    }
}
