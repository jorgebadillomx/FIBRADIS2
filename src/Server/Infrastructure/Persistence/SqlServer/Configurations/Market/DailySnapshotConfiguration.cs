using Domain.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Market;

public class DailySnapshotConfiguration : IEntityTypeConfiguration<DailySnapshot>
{
    public void Configure(EntityTypeBuilder<DailySnapshot> builder)
    {
        builder.ToTable("DailySnapshot", schema: "market");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.FibraId).HasColumnName("fibra_id").IsRequired();
        builder.Property(d => d.Ticker).HasMaxLength(20).IsRequired().HasColumnName("ticker");
        builder.Property(d => d.Date).HasColumnName("date").IsRequired();
        builder.Property(d => d.Open).HasColumnName("open").HasColumnType("decimal(18,6)");
        builder.Property(d => d.High).HasColumnName("high").HasColumnType("decimal(18,6)");
        builder.Property(d => d.Low).HasColumnName("low").HasColumnType("decimal(18,6)");
        builder.Property(d => d.Close).HasColumnName("close").HasColumnType("decimal(18,6)");
        builder.Property(d => d.Volume).HasColumnName("volume");

        builder.HasIndex(d => new { d.FibraId, d.Date })
            .IsUnique()
            .HasDatabaseName("UX_DailySnapshot_FibraId_Date");
    }
}
