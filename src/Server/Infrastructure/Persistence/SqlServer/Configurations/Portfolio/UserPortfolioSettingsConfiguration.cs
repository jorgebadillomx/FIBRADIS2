using Domain.Auth;
using Domain.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Portfolio;

public class UserPortfolioSettingsConfiguration : IEntityTypeConfiguration<UserPortfolioSettings>
{
    public void Configure(EntityTypeBuilder<UserPortfolioSettings> builder)
    {
        builder.ToTable("UserPortfolioSettings", "portfolio");
        builder.HasKey(s => s.UserId);

        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.ColumnConfigJson).HasColumnName("column_config_json").HasColumnType("text");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<UserPortfolioSettings>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
