using Domain.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Portfolio;

public class UserFavoriteConfiguration : IEntityTypeConfiguration<UserFavorite>
{
    public void Configure(EntityTypeBuilder<UserFavorite> builder)
    {
        builder.ToTable("UserFavorites", "portfolio");
        builder.HasKey(e => new { e.UserId, e.FibraId });

        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.FibraId).HasColumnName("fibra_id").IsRequired();
        builder.Property(e => e.AddedAt).HasColumnName("added_at").IsRequired();
    }
}
