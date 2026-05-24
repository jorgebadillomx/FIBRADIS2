using Domain.Catalog;
using Domain.Fundamentals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Fundamentals;

public class FundamentalRecordConfiguration : IEntityTypeConfiguration<FundamentalRecord>
{
    public void Configure(EntityTypeBuilder<FundamentalRecord> builder)
    {
        builder.ToTable("FundamentalRecord", schema: "fundamentals");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("newsequentialid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.FibraId)
            .HasColumnName("fibra_id")
            .IsRequired();

        builder.Property(x => x.Period)
            .HasColumnName("period")
            .HasColumnType("varchar(10)")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.ProcessingMode)
            .HasColumnName("processing_mode")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.CapRate)
            .HasColumnName("cap_rate")
            .HasColumnType("decimal(18,6)");

        builder.Property(x => x.NavPerCbfi)
            .HasColumnName("nav_per_cbfi")
            .HasColumnType("decimal(18,6)");

        builder.Property(x => x.Ltv)
            .HasColumnName("ltv")
            .HasColumnType("decimal(18,6)");

        builder.Property(x => x.NoiMargin)
            .HasColumnName("noi_margin")
            .HasColumnType("decimal(18,6)");

        builder.Property(x => x.FfoMargin)
            .HasColumnName("ffo_margin")
            .HasColumnType("decimal(18,6)");

        builder.Property(x => x.QuarterlyDistribution)
            .HasColumnName("quarterly_distribution")
            .HasColumnType("decimal(18,6)");

        builder.Property(x => x.Summary)
            .HasColumnName("summary")
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.PdfReference)
            .HasColumnName("pdf_reference")
            .HasColumnType("nvarchar(500)");

        builder.Property(x => x.PdfUploadedAt)
            .HasColumnName("pdf_uploaded_at");

        builder.Property(x => x.IsPossibleUpdate)
            .HasColumnName("is_possible_update")
            .IsRequired();

        builder.Property(x => x.ImportedBy)
            .HasColumnName("imported_by")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.ConfirmedBy)
            .HasColumnName("confirmed_by")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.CapturedAt)
            .HasColumnName("captured_at")
            .HasDefaultValueSql("getutcdate()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ConfirmedAt)
            .HasColumnName("confirmed_at");

        builder.Property(x => x.ErrorReason)
            .HasColumnName("error_reason")
            .HasColumnType("nvarchar(500)");

        builder.HasOne<Fibra>()
            .WithMany()
            .HasForeignKey(r => r.FibraId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.FibraId, x.Period, x.Status })
            .HasDatabaseName("IX_FundamentalRecord_FibraId_Period_Status");
    }
}
