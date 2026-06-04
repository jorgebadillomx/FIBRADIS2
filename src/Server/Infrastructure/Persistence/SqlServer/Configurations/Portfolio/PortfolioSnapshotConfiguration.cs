using Domain.Auth;
using Domain.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Portfolio;

public class PortfolioSnapshotConfiguration : IEntityTypeConfiguration<PortfolioSnapshot>
{
    public void Configure(EntityTypeBuilder<PortfolioSnapshot> builder)
    {
        builder.ToTable("PortfolioSnapshots", schema: "portfolio");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.ArchivedAt).HasColumnName("archived_at").IsRequired();
        builder.Property(s => s.PositionsJson).HasColumnName("positions_json").HasColumnType("text").IsRequired();

        builder.HasIndex(s => s.UserId).IsUnique().HasDatabaseName("UX_PortfolioSnapshots_UserId");

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<PortfolioSnapshot>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
