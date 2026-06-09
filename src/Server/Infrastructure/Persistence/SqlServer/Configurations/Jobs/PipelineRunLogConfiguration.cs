using Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Jobs;

public class PipelineRunLogConfiguration : IEntityTypeConfiguration<PipelineRunLog>
{
    public void Configure(EntityTypeBuilder<PipelineRunLog> builder)
    {
        builder.ToTable("PipelineRunLog", schema: "jobs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("NEWID()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Pipeline)
            .HasColumnName("pipeline")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.ItemsProcessed)
            .HasColumnName("items_processed");

        builder.Property(x => x.ErrorCount)
            .HasColumnName("error_count");

        builder.Property(x => x.TriggeredBy)
            .HasColumnName("triggered_by")
            .HasColumnType("varchar(100)");

        builder.Property(x => x.Details)
            .HasColumnName("details")
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => new { x.Pipeline, x.StartedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_PipelineRunLog_Pipeline_StartedAt");
    }
}
