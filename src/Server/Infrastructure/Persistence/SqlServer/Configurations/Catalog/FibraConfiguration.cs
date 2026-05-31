using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Infrastructure.Persistence.SqlServer.Configurations.Catalog;

public class FibraConfiguration : IEntityTypeConfiguration<Fibra>
{
    private static readonly JsonSerializerOptions _jsonOpts = new();

    private static readonly ValueComparer<List<string>> _listComparer = new(
        (a, b) => a != null && b != null && a.SequenceEqual(b),
        v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
        v => v.ToList());

    public void Configure(EntityTypeBuilder<Fibra> builder)
    {
        builder.ToTable("Fibra", schema: "catalog");
        builder.HasKey(f => f.Id);
        builder.HasIndex(f => f.Ticker).IsUnique().HasDatabaseName("UX_Fibra_Ticker");

        builder.Property(f => f.Ticker).HasMaxLength(20).IsRequired().HasColumnName("ticker");
        builder.Property(f => f.YahooTicker).HasMaxLength(32).IsRequired().HasColumnName("yahoo_ticker");
        builder.Property(f => f.FullName).HasMaxLength(256).IsRequired().HasColumnName("full_name");
        builder.Property(f => f.ShortName).HasMaxLength(64).IsRequired().HasColumnName("short_name");
        builder.Property(f => f.Sector).HasMaxLength(64).IsRequired().HasColumnName("sector");
        builder.Property(f => f.Market).HasMaxLength(32).IsRequired().HasColumnName("market");
        builder.Property(f => f.Currency).HasMaxLength(8).IsRequired().HasColumnName("currency");
        builder.Property(f => f.State).HasConversion<string>().HasMaxLength(16).HasColumnName("state");
        builder.Property(f => f.SiteUrl).HasMaxLength(512).HasColumnName("site_url");
        builder.Property(f => f.InvestorUrl).HasMaxLength(512).HasColumnName("investor_url");
        builder.Property(f => f.ReportsUrl).HasMaxLength(512).HasColumnName("reports_url");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");

        builder.Property(f => f.Description)
            .HasColumnType("nvarchar(max)")
            .HasColumnName("description");

        // name_variants almacenado como JSON — editable desde Ops (Historia 5.3)
        builder.Property(f => f.NameVariants)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOpts),
                v => JsonSerializer.Deserialize<List<string>>(v, _jsonOpts) ?? new())
            .HasColumnType("nvarchar(max)")
            .HasColumnName("name_variants")
            .Metadata.SetValueComparer(_listComparer);
    }
}
