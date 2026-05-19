using Domain.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.News;

public class NewsArticleFibraConfiguration : IEntityTypeConfiguration<NewsArticleFibra>
{
    public void Configure(EntityTypeBuilder<NewsArticleFibra> builder)
    {
        builder.ToTable("NewsArticleFibra", schema: "news");
        builder.HasKey(x => new { x.NewsArticleId, x.FibraId });

        builder.Property(x => x.NewsArticleId).HasColumnName("news_article_id");
        builder.Property(x => x.FibraId).HasColumnName("fibra_id");

        builder.HasOne(x => x.NewsArticle)
            .WithMany(a => a.FibraLinks)
            .HasForeignKey(x => x.NewsArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.FibraId)
            .HasDatabaseName("IX_NewsArticleFibra_FibraId");
    }
}
