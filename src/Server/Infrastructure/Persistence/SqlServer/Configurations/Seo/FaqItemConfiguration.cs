using Domain.Seo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Seo;

public class FaqItemConfiguration : IEntityTypeConfiguration<FaqItem>
{
    public void Configure(EntityTypeBuilder<FaqItem> builder)
    {
        builder.ToTable("FaqItem", schema: "seo");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PageType).HasColumnName("page_type").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.EntityKey).HasColumnName("entity_key").HasMaxLength(256).IsRequired();
        builder.Property(x => x.Question).HasColumnName("question").HasMaxLength(256).IsRequired();
        builder.Property(x => x.Answer).HasColumnName("answer").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.Order).HasColumnName("display_order").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256).IsRequired();

        builder.HasIndex(x => new { x.PageType, x.EntityKey, x.Order })
            .HasDatabaseName("IX_FaqItem_PageType_EntityKey_Order");

        builder.HasIndex(x => new { x.PageType, x.EntityKey, x.Question })
            .IsUnique()
            .HasDatabaseName("UX_FaqItem_PageType_EntityKey_Question");
    }
}
