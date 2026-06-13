using Domain.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Ops;

public class InpcMonthlyConfiguration : IEntityTypeConfiguration<InpcMonthlyEntry>
{
    public void Configure(EntityTypeBuilder<InpcMonthlyEntry> b)
    {
        b.ToTable("InpcMonthly", "ops");
        b.HasKey(x => x.Periodo);

        b.Property(x => x.Periodo).HasColumnName("periodo");
        b.Property(x => x.InpcIndex).HasColumnName("inpc_index").HasColumnType("decimal(10,4)");
        b.Property(x => x.CapturedAt).HasColumnName("captured_at");
    }
}
