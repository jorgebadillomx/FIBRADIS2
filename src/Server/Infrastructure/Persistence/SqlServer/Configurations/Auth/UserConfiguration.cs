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
        builder.Property(u => u.Apodo).HasMaxLength(50);
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(u => u.HasAcceptedTerms);
        builder.Property(u => u.TermsAcceptedAt);
        builder.Property(u => u.Pago).HasPrecision(18, 2);
        builder.Property(u => u.FechaPago);
        builder.Property(u => u.EmailConfirmedAt).HasColumnName("email_confirmed_at");
        builder.Property(u => u.TrialEndsAt).HasColumnName("trial_ends_at");
        builder.Property(u => u.SubscriptionType)
               .HasColumnName("subscription_type")
               .HasMaxLength(16)
               .HasConversion<string>();
        builder.Property(u => u.SubscriptionStartedAt).HasColumnName("subscription_started_at");
        builder.Property(u => u.SubscriptionEndsAt).HasColumnName("subscription_ends_at");
        builder.Property(u => u.HowDidYouHear)
               .HasColumnName("how_did_you_hear")
               .HasMaxLength(32)
               .HasConversion<string>();
        builder.HasMany(u => u.RefreshTokens)
               .WithOne(rt => rt.User)
               .HasForeignKey(rt => rt.UserId);
    }
}
