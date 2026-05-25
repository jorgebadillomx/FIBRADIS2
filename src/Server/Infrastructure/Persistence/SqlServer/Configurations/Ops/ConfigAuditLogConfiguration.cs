using Domain.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Ops;

public class ConfigAuditLogConfiguration : IEntityTypeConfiguration<ConfigAuditLog>
{
    public void Configure(EntityTypeBuilder<ConfigAuditLog> builder)
    {
        builder.ToTable("ConfigAuditLog", schema: "ops");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Actor).HasColumnName("actor").HasMaxLength(256).IsRequired();
        builder.Property(x => x.ChangedAt).HasColumnName("changed_at");
        builder.Property(x => x.FieldName).HasColumnName("field_name").HasMaxLength(64).IsRequired();
        builder.Property(x => x.PreviousValue).HasColumnName("previous_value").HasMaxLength(512);
        builder.Property(x => x.NewValue).HasColumnName("new_value").HasMaxLength(512);

        builder.HasIndex(x => x.ChangedAt);
    }
}
