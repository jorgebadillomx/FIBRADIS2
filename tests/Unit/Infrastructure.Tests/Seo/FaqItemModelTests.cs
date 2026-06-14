using Domain.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Infrastructure.Tests.Seo;

public class FaqItemModelTests
{
    [Fact]
    public void AppDbContext_MapsFaqItemToSeoSchema_WithExpectedColumns()
    {
        using var context = CreateContext();

        var entity = Assert.IsAssignableFrom<IEntityType>(context.Model.FindEntityType(typeof(FaqItem)));
        var table = StoreObjectIdentifier.Table("FaqItem", "seo");

        Assert.Equal("seo", entity.GetSchema());
        Assert.Equal("FaqItem", entity.GetTableName());
        Assert.Equal("id", entity.FindProperty(nameof(FaqItem.Id))!.GetColumnName(table));
        Assert.Equal("page_type", entity.FindProperty(nameof(FaqItem.PageType))!.GetColumnName(table));
        Assert.Equal("entity_key", entity.FindProperty(nameof(FaqItem.EntityKey))!.GetColumnName(table));
        Assert.Equal("question", entity.FindProperty(nameof(FaqItem.Question))!.GetColumnName(table));
        Assert.Equal("answer", entity.FindProperty(nameof(FaqItem.Answer))!.GetColumnName(table));
        Assert.Equal("display_order", entity.FindProperty(nameof(FaqItem.Order))!.GetColumnName(table));
        Assert.Equal("is_active", entity.FindProperty(nameof(FaqItem.IsActive))!.GetColumnName(table));
        Assert.Equal("updated_at", entity.FindProperty(nameof(FaqItem.UpdatedAt))!.GetColumnName(table));
        Assert.Equal("updated_by", entity.FindProperty(nameof(FaqItem.UpdatedBy))!.GetColumnName(table));
    }

    [Fact]
    public void AppDbContext_ConfiguresFaqItemIndexes_AndLengths()
    {
        using var context = CreateContext();

        var entity = Assert.IsAssignableFrom<IEntityType>(context.Model.FindEntityType(typeof(FaqItem)));

        var orderIndex = Assert.Single(
            entity.GetIndexes(),
            index => index.Properties.Count == 3 &&
                     index.Properties[0].Name == nameof(FaqItem.PageType) &&
                     index.Properties[1].Name == nameof(FaqItem.EntityKey) &&
                     index.Properties[2].Name == nameof(FaqItem.Order));
        Assert.Equal("IX_FaqItem_PageType_EntityKey_Order", orderIndex.GetDatabaseName());
        Assert.False(orderIndex.IsUnique);

        var uniqueIndex = Assert.Single(
            entity.GetIndexes(),
            index => index.Properties.Count == 3 &&
                     index.Properties[0].Name == nameof(FaqItem.PageType) &&
                     index.Properties[1].Name == nameof(FaqItem.EntityKey) &&
                     index.Properties[2].Name == nameof(FaqItem.Question));
        Assert.True(uniqueIndex.IsUnique);
        Assert.Equal("UX_FaqItem_PageType_EntityKey_Question", uniqueIndex.GetDatabaseName());

        Assert.Equal("nvarchar(16)", entity.FindProperty(nameof(FaqItem.PageType))!.GetColumnType());
        Assert.Equal(16, entity.FindProperty(nameof(FaqItem.PageType))!.GetMaxLength());
        Assert.Equal(256, entity.FindProperty(nameof(FaqItem.EntityKey))!.GetMaxLength());
        Assert.Equal(256, entity.FindProperty(nameof(FaqItem.Question))!.GetMaxLength());
        Assert.Equal("nvarchar(max)", entity.FindProperty(nameof(FaqItem.Answer))!.GetColumnType());
        Assert.Equal(256, entity.FindProperty(nameof(FaqItem.UpdatedBy))!.GetMaxLength());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            // Cadena sintácticamente válida para el provider SqlServer; el model-building NO abre
            // conexión, así que no depende de un servidor real (portable a CI / otras máquinas).
            .UseSqlServer("Server=localhost;Database=fibradis_model_tests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options);
    }
}
