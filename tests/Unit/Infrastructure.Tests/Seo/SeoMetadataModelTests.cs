using Domain.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Infrastructure.Tests.Seo;

public class SeoMetadataModelTests
{
    [Fact]
    public void AppDbContext_MapsSeoMetadataToSeoSchema_WithExpectedColumns()
    {
        using var context = CreateContext();

        var seoEntity = Assert.IsAssignableFrom<IEntityType>(context.Model.FindEntityType(typeof(SeoMetadata)));
        var table = StoreObjectIdentifier.Table("SeoMetadata", "seo");

        Assert.Equal("seo", seoEntity.GetSchema());
        Assert.Equal("SeoMetadata", seoEntity.GetTableName());
        Assert.Equal("id", seoEntity.FindProperty(nameof(SeoMetadata.Id))!.GetColumnName(table));
        Assert.Equal("page_type", seoEntity.FindProperty(nameof(SeoMetadata.PageType))!.GetColumnName(table));
        Assert.Equal("entity_key", seoEntity.FindProperty(nameof(SeoMetadata.EntityKey))!.GetColumnName(table));
        Assert.Equal("title", seoEntity.FindProperty(nameof(SeoMetadata.Title))!.GetColumnName(table));
        Assert.Equal("meta_description", seoEntity.FindProperty(nameof(SeoMetadata.MetaDescription))!.GetColumnName(table));
        Assert.Equal("canonical_path", seoEntity.FindProperty(nameof(SeoMetadata.CanonicalPath))!.GetColumnName(table));
        Assert.Equal("og_title", seoEntity.FindProperty(nameof(SeoMetadata.OgTitle))!.GetColumnName(table));
        Assert.Equal("og_description", seoEntity.FindProperty(nameof(SeoMetadata.OgDescription))!.GetColumnName(table));
        Assert.Equal("og_type", seoEntity.FindProperty(nameof(SeoMetadata.OgType))!.GetColumnName(table));
        Assert.Equal("og_image_url", seoEntity.FindProperty(nameof(SeoMetadata.OgImageUrl))!.GetColumnName(table));
        Assert.Equal("og_locale", seoEntity.FindProperty(nameof(SeoMetadata.OgLocale))!.GetColumnName(table));
        Assert.Equal("twitter_card", seoEntity.FindProperty(nameof(SeoMetadata.TwitterCard))!.GetColumnName(table));
        Assert.Equal("robots_directives", seoEntity.FindProperty(nameof(SeoMetadata.RobotsDirectives))!.GetColumnName(table));
        Assert.Equal("json_ld", seoEntity.FindProperty(nameof(SeoMetadata.JsonLd))!.GetColumnName(table));
        Assert.Equal("is_active", seoEntity.FindProperty(nameof(SeoMetadata.IsActive))!.GetColumnName(table));
        Assert.Equal("updated_at", seoEntity.FindProperty(nameof(SeoMetadata.UpdatedAt))!.GetColumnName(table));
        Assert.Equal("updated_by", seoEntity.FindProperty(nameof(SeoMetadata.UpdatedBy))!.GetColumnName(table));
        Assert.Equal("title_is_overridden", seoEntity.FindProperty(nameof(SeoMetadata.TitleIsOverridden))!.GetColumnName(table));
        Assert.Equal("meta_description_is_overridden", seoEntity.FindProperty(nameof(SeoMetadata.MetaDescriptionIsOverridden))!.GetColumnName(table));
        Assert.Equal("canonical_path_is_overridden", seoEntity.FindProperty(nameof(SeoMetadata.CanonicalPathIsOverridden))!.GetColumnName(table));
        Assert.Equal("og_title_is_overridden", seoEntity.FindProperty(nameof(SeoMetadata.OgTitleIsOverridden))!.GetColumnName(table));
        Assert.Equal("og_description_is_overridden", seoEntity.FindProperty(nameof(SeoMetadata.OgDescriptionIsOverridden))!.GetColumnName(table));
        Assert.Equal("og_type_is_overridden", seoEntity.FindProperty(nameof(SeoMetadata.OgTypeIsOverridden))!.GetColumnName(table));
        Assert.Equal("og_image_url_is_overridden", seoEntity.FindProperty(nameof(SeoMetadata.OgImageUrlIsOverridden))!.GetColumnName(table));
        Assert.Equal("og_locale_is_overridden", seoEntity.FindProperty(nameof(SeoMetadata.OgLocaleIsOverridden))!.GetColumnName(table));
        Assert.Equal("twitter_card_is_overridden", seoEntity.FindProperty(nameof(SeoMetadata.TwitterCardIsOverridden))!.GetColumnName(table));
        Assert.Equal("robots_directives_is_overridden", seoEntity.FindProperty(nameof(SeoMetadata.RobotsDirectivesIsOverridden))!.GetColumnName(table));
        Assert.Equal("json_ld_is_overridden", seoEntity.FindProperty(nameof(SeoMetadata.JsonLdIsOverridden))!.GetColumnName(table));
    }

    [Fact]
    public void AppDbContext_ConfiguresSeoMetadataUniqueIndex_AndPageTypeConversion()
    {
        using var context = CreateContext();

        var seoEntity = Assert.IsAssignableFrom<IEntityType>(context.Model.FindEntityType(typeof(SeoMetadata)));
        var uniqueIndex = Assert.Single(
            seoEntity.GetIndexes(),
            index => index.Properties.Count == 2 &&
                     index.Properties[0].Name == nameof(SeoMetadata.PageType) &&
                     index.Properties[1].Name == nameof(SeoMetadata.EntityKey));

        Assert.True(uniqueIndex.IsUnique);
        Assert.Equal("UX_SeoMetadata_PageType_EntityKey", uniqueIndex.GetDatabaseName());
        Assert.Equal("nvarchar(16)", seoEntity.FindProperty(nameof(SeoMetadata.PageType))!.GetColumnType());
        Assert.Equal(16, seoEntity.FindProperty(nameof(SeoMetadata.PageType))!.GetMaxLength());
        Assert.Equal(256, seoEntity.FindProperty(nameof(SeoMetadata.EntityKey))!.GetMaxLength());
        Assert.Equal(120, seoEntity.FindProperty(nameof(SeoMetadata.Title))!.GetMaxLength());
        Assert.Equal(120, seoEntity.FindProperty(nameof(SeoMetadata.OgTitle))!.GetMaxLength());
        Assert.Equal(160, seoEntity.FindProperty(nameof(SeoMetadata.MetaDescription))!.GetMaxLength());
        Assert.Equal(256, seoEntity.FindProperty(nameof(SeoMetadata.CanonicalPath))!.GetMaxLength());
        Assert.Equal(160, seoEntity.FindProperty(nameof(SeoMetadata.OgDescription))!.GetMaxLength());
        Assert.Equal(512, seoEntity.FindProperty(nameof(SeoMetadata.OgImageUrl))!.GetMaxLength());
        Assert.Equal(16, seoEntity.FindProperty(nameof(SeoMetadata.OgLocale))!.GetMaxLength());
        Assert.Equal(32, seoEntity.FindProperty(nameof(SeoMetadata.TwitterCard))!.GetMaxLength());
        Assert.Equal(256, seoEntity.FindProperty(nameof(SeoMetadata.RobotsDirectives))!.GetMaxLength());
        Assert.Equal(256, seoEntity.FindProperty(nameof(SeoMetadata.UpdatedBy))!.GetMaxLength());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=LAPBADIS;Database=fibradis_model_tests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options);
    }
}
