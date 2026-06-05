using Domain.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Ops;

public class OperationalConfigConfiguration : IEntityTypeConfiguration<OperationalConfig>
{
    public void Configure(EntityTypeBuilder<OperationalConfig> builder)
    {
        builder.ToTable("OperationalConfig", schema: "ops");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CommissionFactor)
            .HasColumnName("commission_factor")
            .HasPrecision(10, 6);
        builder.Property(x => x.AvgPeriods).HasColumnName("avg_periods");
        builder.Property(x => x.NewsCadenceMinutes).HasColumnName("news_cadence_minutes");
        builder.Property(x => x.FibraNewsMonths).HasColumnName("fibra_news_months");
        builder.Property(x => x.FundamentalsCadenceMinutes).HasColumnName("fundamentals_cadence_minutes");
        builder.Property(x => x.DistributionCadenceMinutes).HasColumnName("distribution_cadence_minutes");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);
        builder.Property(x => x.TermsEnabled).HasColumnName("terms_enabled").HasDefaultValue(false);
        builder.Property(x => x.TermsText).HasColumnName("terms_text");
        builder.Property(x => x.ContactEmail).HasColumnName("contact_email").HasMaxLength(256);

        builder.HasData(new OperationalConfig
        {
            Id = 1,
            CommissionFactor = 0.006m,
            AvgPeriods = 4,
            NewsCadenceMinutes = 1440,
            FibraNewsMonths = 15,
            FundamentalsCadenceMinutes = 1440,
            DistributionCadenceMinutes = 1440,
            UpdatedAt = new DateTimeOffset(2026, 5, 24, 0, 0, 0, TimeSpan.Zero),
            UpdatedBy = "system",
            TermsEnabled = false,
            ContactEmail = "contacto@fibradis.mx",
        });
    }
}
