using Domain.Seo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Seo;

public class SeoMetadataConfiguration : IEntityTypeConfiguration<SeoMetadata>
{
    public void Configure(EntityTypeBuilder<SeoMetadata> builder)
    {
        builder.ToTable("SeoMetadata", schema: "seo");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PageType).HasColumnName("page_type").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.EntityKey).HasColumnName("entity_key").HasMaxLength(256).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(120).IsRequired();
        builder.Property(x => x.MetaDescription).HasColumnName("meta_description").HasMaxLength(160).IsRequired();
        builder.Property(x => x.CanonicalPath).HasColumnName("canonical_path").HasMaxLength(256).IsRequired();
        builder.Property(x => x.OgTitle).HasColumnName("og_title").HasMaxLength(120).IsRequired();
        builder.Property(x => x.OgDescription).HasColumnName("og_description").HasMaxLength(160).IsRequired();
        builder.Property(x => x.OgType).HasColumnName("og_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.OgImageUrl).HasColumnName("og_image_url").HasMaxLength(512).IsRequired();
        builder.Property(x => x.OgLocale).HasColumnName("og_locale").HasMaxLength(16).IsRequired();
        builder.Property(x => x.TwitterCard).HasColumnName("twitter_card").HasMaxLength(32).IsRequired();
        builder.Property(x => x.RobotsDirectives).HasColumnName("robots_directives").HasMaxLength(256).IsRequired();
        builder.Property(x => x.JsonLd).HasColumnName("json_ld");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256).IsRequired();
        builder.Property(x => x.TitleIsOverridden).HasColumnName("title_is_overridden").HasDefaultValue(false);
        builder.Property(x => x.MetaDescriptionIsOverridden).HasColumnName("meta_description_is_overridden").HasDefaultValue(false);
        builder.Property(x => x.CanonicalPathIsOverridden).HasColumnName("canonical_path_is_overridden").HasDefaultValue(false);
        builder.Property(x => x.OgTitleIsOverridden).HasColumnName("og_title_is_overridden").HasDefaultValue(false);
        builder.Property(x => x.OgDescriptionIsOverridden).HasColumnName("og_description_is_overridden").HasDefaultValue(false);
        builder.Property(x => x.OgTypeIsOverridden).HasColumnName("og_type_is_overridden").HasDefaultValue(false);
        builder.Property(x => x.OgImageUrlIsOverridden).HasColumnName("og_image_url_is_overridden").HasDefaultValue(false);
        builder.Property(x => x.OgLocaleIsOverridden).HasColumnName("og_locale_is_overridden").HasDefaultValue(false);
        builder.Property(x => x.TwitterCardIsOverridden).HasColumnName("twitter_card_is_overridden").HasDefaultValue(false);
        builder.Property(x => x.RobotsDirectivesIsOverridden).HasColumnName("robots_directives_is_overridden").HasDefaultValue(false);
        builder.Property(x => x.JsonLdIsOverridden).HasColumnName("json_ld_is_overridden").HasDefaultValue(false);

        builder.HasIndex(x => new { x.PageType, x.EntityKey })
            .IsUnique()
            .HasDatabaseName("UX_SeoMetadata_PageType_EntityKey");
    }
}
