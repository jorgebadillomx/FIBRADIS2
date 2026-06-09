using Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Jobs;

public class PipelineErrorLogConfiguration : IEntityTypeConfiguration<PipelineErrorLog>
{
    public void Configure(EntityTypeBuilder<PipelineErrorLog> builder)
    {
        builder.ToTable("PipelineErrorLog", schema: "jobs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("NEWID()");
        builder.Property(x => x.Pipeline)
            .HasColumnName("pipeline")
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.Timestamp).HasColumnName("timestamp");
        builder.Property(x => x.ErrorType)
            .HasColumnName("error_type")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(x => x.Message)
            .HasColumnName("message")
            .IsRequired();
        builder.Property(x => x.Context).HasColumnName("context");
        builder.Property(x => x.AiContext)
            .HasColumnName("ai_context")
            .IsRequired();
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(x => new { x.Pipeline, x.CreatedAt })
            .HasDatabaseName("IX_PipelineErrorLog_Pipeline_CreatedAt");
    }
}
