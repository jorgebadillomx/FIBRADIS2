using Domain.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.News;

public class AiModeConfigConfiguration : IEntityTypeConfiguration<AiModeConfig>
{
    public void Configure(EntityTypeBuilder<AiModeConfig> builder)
    {
        builder.ToTable("AiModeConfig", schema: "ai");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Mode)
            .HasColumnName("mode")
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);
        builder.Property(x => x.NewsModel)
            .HasColumnName("news_model")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PreviousMode)
            .HasColumnName("previous_mode")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.MinBodyTextLengthForAi)
            .HasColumnName("min_body_text_length_for_ai")
            .IsRequired()
            .HasDefaultValue(500);

        builder.HasData(new AiModeConfig
        {
            Id = 1,
            Mode = AiMode.Off,
            NewsModel = "gemini-2.5-pro",
            UpdatedAt = new DateTimeOffset(2026, 5, 19, 0, 0, 0, TimeSpan.Zero),
            UpdatedBy = "system",
            PreviousMode = null,
            MinBodyTextLengthForAi = 500,
        });
    }
}
