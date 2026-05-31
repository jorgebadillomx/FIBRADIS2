using Domain.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.News;

public class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> builder)
    {
        builder.ToTable("NewsArticle", schema: "news");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(512).IsRequired();
        builder.Property(x => x.TitleNormalized).HasColumnName("title_normalized").HasMaxLength(512).IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").HasMaxLength(256).IsRequired();
        builder.Property(x => x.PublishedAt).HasColumnName("published_at").IsRequired();
        builder.Property(x => x.Url).HasColumnName("url").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Snippet).HasColumnName("snippet").HasMaxLength(2048);
        builder.Property(x => x.BodyText).HasColumnName("body_text").HasColumnType("nvarchar(max)");
        builder.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(2048);
        builder.Property(x => x.AiSummary).HasColumnName("ai_summary").HasColumnType("nvarchar(max)");
        builder.Property(x => x.AiAnalysisJson).HasColumnName("ai_analysis_json").HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.CapturedAt).HasColumnName("captured_at").IsRequired();
        builder.Property(x => x.ErrorReason).HasColumnName("error_reason").HasMaxLength(512);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(x => x.Url)
            .IsUnique()
            .HasDatabaseName("IX_NewsArticle_Url");

        builder.HasIndex(x => new { x.TitleNormalized, x.CapturedAt })
            .HasDatabaseName("IX_NewsArticle_TitleNormalized_CapturedAt");
    }
}
