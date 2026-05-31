using Domain.Catalog;
using Domain.Fundamentals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Fundamentals;

public class FundamentalSourceManifestConfiguration : IEntityTypeConfiguration<FundamentalSourceManifest>
{
    public void Configure(EntityTypeBuilder<FundamentalSourceManifest> builder)
    {
        builder.ToTable("FundamentalSourceManifest", schema: "fundamentals");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("newsequentialid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.SourceName)
            .HasColumnName("source_name")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.FibraId)
            .HasColumnName("fibra_id");

        builder.Property(x => x.SourceTitle)
            .HasColumnName("source_title")
            .HasColumnType("nvarchar(300)")
            .IsRequired();

        builder.Property(x => x.Period)
            .HasColumnName("period")
            .HasColumnType("varchar(10)");

        builder.Property(x => x.ReportType)
            .HasColumnName("report_type")
            .HasColumnType("varchar(30)")
            .IsRequired();

        builder.Property(x => x.DiscoveryStatus)
            .HasColumnName("discovery_status")
            .HasColumnType("varchar(40)")
            .IsRequired();

        builder.Property(x => x.PackageUrl)
            .HasColumnName("package_url")
            .HasColumnType("nvarchar(500)")
            .IsRequired();

        builder.Property(x => x.DownloadUrl)
            .HasColumnName("download_url")
            .HasColumnType("nvarchar(1000)");

        builder.Property(x => x.DownloadSignature)
            .HasColumnName("download_signature")
            .HasColumnType("nvarchar(500)");

        builder.Property(x => x.PdfUrl)
            .HasColumnName("pdf_url")
            .HasColumnType("nvarchar(1000)");

        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasColumnType("nvarchar(260)");

        builder.Property(x => x.SourcePublishedAt)
            .HasColumnName("source_published_at");

        builder.Property(x => x.FirstSeenAt)
            .HasColumnName("first_seen_at");

        builder.Property(x => x.LastSeenAt)
            .HasColumnName("last_seen_at");

        builder.Property(x => x.LastDecision)
            .HasColumnName("last_decision")
            .HasColumnType("varchar(40)")
            .IsRequired();

        builder.Property(x => x.LastDecisionReason)
            .HasColumnName("last_decision_reason")
            .HasColumnType("nvarchar(500)");

        builder.Property(x => x.LastProcessedRecordId)
            .HasColumnName("last_processed_record_id");

        builder.Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasColumnType("nvarchar(500)");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("getutcdate()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("getutcdate()");

        builder.HasIndex(x => new { x.SourceName, x.PackageUrl })
            .IsUnique()
            .HasDatabaseName("UX_FundamentalSourceManifest_SourceName_PackageUrl");

        builder.HasIndex(x => new { x.FibraId, x.Period, x.ReportType })
            .HasDatabaseName("IX_FundamentalSourceManifest_FibraId_Period_ReportType");

        builder.HasOne<Fibra>()
            .WithMany()
            .HasForeignKey(x => x.FibraId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
