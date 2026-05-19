using Domain.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Market;

public class DistributionConfiguration : IEntityTypeConfiguration<Distribution>
{
    public void Configure(EntityTypeBuilder<Distribution> builder)
    {
        builder.ToTable("Distribution", schema: "market");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.FibraId).HasColumnName("fibra_id").IsRequired();
        builder.Property(d => d.Ticker).HasMaxLength(20).IsRequired().HasColumnName("ticker");
        builder.Property(d => d.PaymentDate).HasColumnName("payment_date").IsRequired();
        builder.Property(d => d.AmountPerUnit).HasColumnName("amount_per_unit").HasColumnType("decimal(18,6)").IsRequired();
        builder.Property(d => d.Currency).HasMaxLength(10).IsRequired().HasColumnName("currency");
        builder.Property(d => d.Source).HasMaxLength(50).IsRequired().HasColumnName("source");
        builder.Property(d => d.CapturedAt).HasColumnName("captured_at").IsRequired();

        builder.HasIndex(d => new { d.FibraId, d.PaymentDate })
            .HasDatabaseName("IX_Distribution_FibraId_PaymentDate");
    }
}
