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
            .HasDefaultValueSql("NEWID()")
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
            .HasColumnType("text");

        builder.Property(x => x.PdfReference)
            .HasColumnName("pdf_reference")
            .HasColumnType("varchar(500)");

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
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ConfirmedAt)
            .HasColumnName("confirmed_at");

        builder.Property(x => x.MarkdownContent)
            .HasColumnName("markdown_content")
            .HasColumnType("text");

        builder.Property(x => x.FieldNotesJson)
            .HasColumnName("FieldNotesJson")
            .HasColumnType("text");

        builder.Property(x => x.AiAnalysisJson)
            .HasColumnName("ai_analysis_json")
            .HasColumnType("text");

        builder.Property(x => x.ErrorReason)
            .HasColumnName("error_reason")
            .HasColumnType("varchar(500)");

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(x => x.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("varchar(100)");

        builder.HasOne<Fibra>()
            .WithMany()
            .HasForeignKey(r => r.FibraId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.FibraId, x.Period, x.Status })
            .HasDatabaseName("IX_FundamentalRecord_FibraId_Period_Status");
    }
}
