using Domain.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.News;

public class BlocklistTermConfiguration : IEntityTypeConfiguration<BlocklistTerm>
{
    public void Configure(EntityTypeBuilder<BlocklistTerm> builder)
    {
        builder.ToTable("BlocklistTerm", schema: "news");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Term).HasColumnName("term").HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => x.Term)
            .IsUnique()
            .HasDatabaseName("IX_BlocklistTerm_Term");
    }
}
