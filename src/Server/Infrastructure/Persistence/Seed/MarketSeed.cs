using Domain.Market;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Persistence.Seed;

public static class MarketSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Distribution>().HasData(
            // ── FUNO11 — pago trimestral (Mar, Jun, Sep, Dic) ──────────────────
            D("FUNO11", new DateOnly(2021, 3, 15),  0.3100m),
            D("FUNO11", new DateOnly(2021, 6, 14),  0.3150m),
            D("FUNO11", new DateOnly(2021, 9, 13),  0.3180m),
            D("FUNO11", new DateOnly(2021, 12, 13), 0.3200m),
            D("FUNO11", new DateOnly(2022, 3, 14),  0.3300m),
            D("FUNO11", new DateOnly(2022, 6, 13),  0.3380m),
            D("FUNO11", new DateOnly(2022, 9, 12),  0.3420m),
            D("FUNO11", new DateOnly(2022, 12, 12), 0.3460m),
            D("FUNO11", new DateOnly(2023, 3, 13),  0.3500m),
            D("FUNO11", new DateOnly(2023, 6, 12),  0.3580m),
            D("FUNO11", new DateOnly(2023, 9, 11),  0.3620m),
            D("FUNO11", new DateOnly(2023, 12, 11), 0.3660m),
            D("FUNO11", new DateOnly(2024, 3, 18),  0.3680m),
            D("FUNO11", new DateOnly(2024, 6, 17),  0.3720m),
            D("FUNO11", new DateOnly(2024, 9, 16),  0.3760m),
            D("FUNO11", new DateOnly(2024, 12, 16), 0.3800m),
            D("FUNO11", new DateOnly(2025, 3, 17),  0.3610m),
            D("FUNO11", new DateOnly(2025, 6, 16),  0.3720m),
            D("FUNO11", new DateOnly(2025, 9, 15),  0.3780m),
            D("FUNO11", new DateOnly(2025, 12, 15), 0.3840m),

            // ── DANHOS13 — pago trimestral ──────────────────────────────────────
            D("DANHOS13", new DateOnly(2021, 3, 15),  0.1900m),
            D("DANHOS13", new DateOnly(2021, 6, 14),  0.1950m),
            D("DANHOS13", new DateOnly(2021, 9, 13),  0.1980m),
            D("DANHOS13", new DateOnly(2021, 12, 13), 0.2000m),
            D("DANHOS13", new DateOnly(2022, 3, 14),  0.2020m),
            D("DANHOS13", new DateOnly(2022, 6, 13),  0.2050m),
            D("DANHOS13", new DateOnly(2022, 9, 12),  0.2080m),
            D("DANHOS13", new DateOnly(2022, 12, 12), 0.2100m),
            D("DANHOS13", new DateOnly(2023, 3, 13),  0.2100m),
            D("DANHOS13", new DateOnly(2023, 6, 12),  0.2130m),
            D("DANHOS13", new DateOnly(2023, 9, 11),  0.2160m),
            D("DANHOS13", new DateOnly(2023, 12, 11), 0.2180m),
            D("DANHOS13", new DateOnly(2024, 3, 18),  0.2180m),
            D("DANHOS13", new DateOnly(2024, 6, 17),  0.2200m),
            D("DANHOS13", new DateOnly(2024, 9, 16),  0.2220m),
            D("DANHOS13", new DateOnly(2024, 12, 16), 0.2250m),
            D("DANHOS13", new DateOnly(2025, 3, 17),  0.2150m),
            D("DANHOS13", new DateOnly(2025, 6, 16),  0.2200m),
            D("DANHOS13", new DateOnly(2025, 9, 15),  0.2250m),
            D("DANHOS13", new DateOnly(2025, 12, 15), 0.2300m),

            // ── TERRA13 — pago trimestral (Jun, Sep, Dic + Mar desde 2022) ──────
            D("TERRA13", new DateOnly(2021, 6, 14),  0.1500m),
            D("TERRA13", new DateOnly(2021, 9, 13),  0.1530m),
            D("TERRA13", new DateOnly(2021, 12, 13), 0.1550m),
            D("TERRA13", new DateOnly(2022, 3, 14),  0.1580m),
            D("TERRA13", new DateOnly(2022, 6, 13),  0.1600m),
            D("TERRA13", new DateOnly(2022, 9, 12),  0.1620m),
            D("TERRA13", new DateOnly(2022, 12, 12), 0.1640m),
            D("TERRA13", new DateOnly(2023, 3, 13),  0.1650m),
            D("TERRA13", new DateOnly(2023, 6, 12),  0.1680m),
            D("TERRA13", new DateOnly(2023, 9, 11),  0.1700m),
            D("TERRA13", new DateOnly(2023, 12, 11), 0.1720m),
            D("TERRA13", new DateOnly(2024, 3, 18),  0.1730m),
            D("TERRA13", new DateOnly(2024, 6, 17),  0.1750m),
            D("TERRA13", new DateOnly(2024, 9, 16),  0.1770m),
            D("TERRA13", new DateOnly(2024, 12, 16), 0.1790m),
            D("TERRA13", new DateOnly(2025, 6, 16),  0.1750m),
            D("TERRA13", new DateOnly(2025, 9, 15),  0.1800m),
            D("TERRA13", new DateOnly(2025, 12, 15), 0.1820m),

            // ── FIBRAMQ12 — pago semestral/trimestral ───────────────────────────
            D("FIBRAMQ12", new DateOnly(2021, 6, 14),  0.1100m),
            D("FIBRAMQ12", new DateOnly(2021, 12, 13), 0.1150m),
            D("FIBRAMQ12", new DateOnly(2022, 6, 13),  0.1200m),
            D("FIBRAMQ12", new DateOnly(2022, 12, 12), 0.1250m),
            D("FIBRAMQ12", new DateOnly(2023, 6, 12),  0.1300m),
            D("FIBRAMQ12", new DateOnly(2023, 12, 11), 0.1350m),
            D("FIBRAMQ12", new DateOnly(2024, 6, 17),  0.1400m),
            D("FIBRAMQ12", new DateOnly(2024, 12, 16), 0.1450m),
            D("FIBRAMQ12", new DateOnly(2025, 9, 15),  0.1480m),
            D("FIBRAMQ12", new DateOnly(2025, 12, 15), 0.1520m)
        );
    }

    private static Distribution D(string ticker, DateOnly paymentDate, decimal amount)
        => new()
        {
            Id = GuidFromKey($"dist:{ticker}:{paymentDate:yyyy-MM-dd}"),
            FibraId = GuidFromTicker(ticker),
            Ticker = ticker,
            PaymentDate = paymentDate,
            AmountPerUnit = amount,
            Currency = "MXN",
            Source = "seed",
            CapturedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };

    private static Guid GuidFromKey(string key)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash);
    }

    private static Guid GuidFromTicker(string ticker)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(ticker));
        return new Guid(hash);
    }
}
