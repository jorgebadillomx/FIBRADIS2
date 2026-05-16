using Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Auth;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken", schema: "auth");
        builder.HasKey(rt => rt.Id);
        builder.HasIndex(rt => new { rt.UserId, rt.RevokedAt })
               .HasDatabaseName("IX_RefreshToken_UserId_RevokedAt");
        builder.Property(rt => rt.TokenHash).HasMaxLength(512).IsRequired();
    }
}
