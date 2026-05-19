using Domain.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Market;

public class PriceSnapshotConfiguration : IEntityTypeConfiguration<PriceSnapshot>
{
    public void Configure(EntityTypeBuilder<PriceSnapshot> builder)
    {
        builder.ToTable("PriceSnapshot", schema: "market");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.FibraId).HasColumnName("fibra_id").IsRequired();
        builder.Property(p => p.Ticker).HasMaxLength(20).IsRequired().HasColumnName("ticker");
        builder.Property(p => p.LastPrice).HasColumnName("last_price").HasColumnType("decimal(18,6)");
        builder.Property(p => p.DailyChange).HasColumnName("daily_change").HasColumnType("decimal(18,6)");
        builder.Property(p => p.DailyChangePct).HasColumnName("daily_change_pct").HasColumnType("decimal(10,4)");
        builder.Property(p => p.Volume).HasColumnName("volume");
        builder.Property(p => p.Week52High).HasColumnName("week52_high").HasColumnType("decimal(18,6)");
        builder.Property(p => p.Week52Low).HasColumnName("week52_low").HasColumnType("decimal(18,6)");
        builder.Property(p => p.CapturedAt).HasColumnName("captured_at").IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16).HasColumnName("status");
        builder.Property(p => p.ErrorReason).HasMaxLength(512).HasColumnName("error_reason");

        builder.HasIndex(p => new { p.FibraId, p.CapturedAt })
            .HasDatabaseName("IX_PriceSnapshot_FibraId_CapturedAt");
    }
}
