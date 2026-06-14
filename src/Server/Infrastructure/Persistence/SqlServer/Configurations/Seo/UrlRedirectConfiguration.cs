using Domain.Seo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Seo;

public class UrlRedirectConfiguration : IEntityTypeConfiguration<UrlRedirect>
{
    public void Configure(EntityTypeBuilder<UrlRedirect> builder)
    {
        builder.ToTable("UrlRedirect", schema: "seo");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FromPath).HasColumnName("from_path").HasMaxLength(256).IsRequired();
        builder.Property(x => x.ToPath).HasColumnName("to_path").HasMaxLength(256).IsRequired();
        builder.Property(x => x.StatusCode).HasColumnName("status_code").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.Notes).HasColumnName("notes").HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(256).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256).IsRequired();

        builder.HasIndex(x => x.FromPath)
            .IsUnique()
            .HasDatabaseName("UX_UrlRedirect_FromPath");
    }
}
