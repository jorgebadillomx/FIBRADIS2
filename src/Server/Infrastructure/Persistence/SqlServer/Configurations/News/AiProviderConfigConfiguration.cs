using Domain.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.News;

public class AiProviderConfigConfiguration : IEntityTypeConfiguration<AiProviderConfig>
{
    public void Configure(EntityTypeBuilder<AiProviderConfig> builder)
    {
        builder.ToTable("AiProviderConfig", schema: "ai");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Provider)
            .HasColumnName("provider")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(x => x.ModelId)
            .HasColumnName("model_id")
            .HasMaxLength(128);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);

        builder.HasData(new AiProviderConfig
        {
            Id = 1,
            Provider = AiProvider.Gemini,
            ModelId = "gemini-2.5-flash",
            UpdatedAt = new DateTimeOffset(2026, 5, 22, 0, 0, 0, TimeSpan.Zero),
            UpdatedBy = "system",
        });
    }
}
