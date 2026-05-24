using Domain.News;
using Infrastructure.Integrations.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.News;

public class AiPromptConfiguration : IEntityTypeConfiguration<AiPrompt>
{
    public void Configure(EntityTypeBuilder<AiPrompt> builder)
    {
        builder.ToTable("AiPrompt", schema: "ai");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.PromptTemplate)
            .HasColumnName("prompt_template")
            .IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.ContentType)
            .IsUnique()
            .HasDatabaseName("UQ_AiPrompt_ContentType");

        builder.HasData(
            new AiPrompt
            {
                Id = 1,
                ContentType = AiPromptTemplateDefaults.NewsContentType,
                PromptTemplate = AiPromptTemplateDefaults.News,
                UpdatedAt = new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.Zero),
                UpdatedBy = "system",
            },
            new AiPrompt
            {
                Id = 2,
                ContentType = AiPromptTemplateDefaults.DocumentContentType,
                PromptTemplate = AiPromptTemplateDefaults.Document,
                UpdatedAt = new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.Zero),
                UpdatedBy = "system",
            });
    }
}
