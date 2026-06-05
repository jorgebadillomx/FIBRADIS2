using Domain.Auth;
using Domain.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Portfolio;

public class UserOpportunityWeightsConfiguration : IEntityTypeConfiguration<UserOpportunityWeights>
{
    public void Configure(EntityTypeBuilder<UserOpportunityWeights> builder)
    {
        builder.ToTable("UserOpportunityWeights", "portfolio");
        builder.HasKey(s => s.UserId);

        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.WeightsJson).HasColumnName("weights_json").HasColumnType("text");
        builder.Property(s => s.Profile).HasColumnName("profile").HasMaxLength(50).IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<UserOpportunityWeights>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
