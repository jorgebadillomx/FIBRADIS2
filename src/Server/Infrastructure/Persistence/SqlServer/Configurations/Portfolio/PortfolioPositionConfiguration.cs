using Domain.Auth;
using Domain.Catalog;
using Domain.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Portfolio;

public class PortfolioPositionConfiguration : IEntityTypeConfiguration<PortfolioPosition>
{
    public void Configure(EntityTypeBuilder<PortfolioPosition> builder)
    {
        builder.ToTable("PortfolioPositions", "portfolio");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(p => p.FibraId).HasColumnName("fibra_id").IsRequired();
        builder.Property(p => p.Titulos).HasColumnName("titulos").IsRequired();
        builder.Property(p => p.CostoPromedio).HasColumnName("costo_promedio").HasPrecision(18, 6).IsRequired();
        builder.Property(p => p.CostoTotalCompra).HasColumnName("costo_total_compra").HasPrecision(18, 6).IsRequired();
        builder.Property(p => p.UploadedAt).HasColumnName("uploaded_at").IsRequired();

        builder.HasIndex(p => new { p.UserId, p.FibraId })
               .IsUnique()
               .HasDatabaseName("UX_PortfolioPositions_UserId_FibraId");

        builder.HasOne<Fibra>()
               .WithMany()
               .HasForeignKey(p => p.FibraId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
