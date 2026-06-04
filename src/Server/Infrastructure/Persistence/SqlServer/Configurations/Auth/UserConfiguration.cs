using Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Auth;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User", schema: "auth");
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UX_User_Email");
        builder.Property(u => u.Email).HasMaxLength(512).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(u => u.HasAcceptedTerms);
        builder.Property(u => u.TermsAcceptedAt);
        builder.Property(u => u.Pago).HasPrecision(18, 2);
        builder.Property(u => u.FechaPago);
        builder.HasMany(u => u.RefreshTokens)
               .WithOne(rt => rt.User)
               .HasForeignKey(rt => rt.UserId);
    }
}
