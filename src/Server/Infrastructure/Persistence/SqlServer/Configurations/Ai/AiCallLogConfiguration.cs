using Domain.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Ai;

public class AiCallLogConfiguration : IEntityTypeConfiguration<AiCallLog>
{
    public void Configure(EntityTypeBuilder<AiCallLog> builder)
    {
        builder.ToTable("AiCallLog", schema: "jobs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("newsequentialid()");
        builder.Property(x => x.Timestamp).HasColumnName("timestamp").IsRequired();
        builder.Property(x => x.Operation)
            .HasColumnName("operation")
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.ModelId)
            .HasColumnName("model_id")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(x => x.PromptLength).HasColumnName("prompt_length");
        builder.Property(x => x.DurationMs).HasColumnName("duration_ms");
        builder.Property(x => x.Success).HasColumnName("success");
        builder.Property(x => x.RequestRaw).HasColumnName("request_raw");
        builder.Property(x => x.ResponseRaw).HasColumnName("response_raw");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message");
        builder.Property(x => x.Context).HasColumnName("context");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("getutcdate()");

        builder.HasIndex(x => new { x.Operation, x.CreatedAt })
            .HasDatabaseName("IX_AiCallLog_Operation_CreatedAt");
    }
}
