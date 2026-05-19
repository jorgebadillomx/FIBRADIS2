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
            // FUNO11 — pago trimestral ~0.38 MXN/CBFI
            D("FUNO11", new DateOnly(2025, 3, 17),  0.3610m),
            D("FUNO11", new DateOnly(2025, 6, 16),  0.3720m),
            D("FUNO11", new DateOnly(2025, 9, 15),  0.3780m),
            D("FUNO11", new DateOnly(2025, 12, 15), 0.3840m),
            // DANHOS13 — pago trimestral ~0.22 MXN/CBFI
            D("DANHOS13", new DateOnly(2025, 3, 17),  0.2150m),
            D("DANHOS13", new DateOnly(2025, 6, 16),  0.2200m),
            D("DANHOS13", new DateOnly(2025, 9, 15),  0.2250m),
            D("DANHOS13", new DateOnly(2025, 12, 15), 0.2300m),
            // TERRA13 — 3 pagos en 2025
            D("TERRA13", new DateOnly(2025, 6, 16),  0.1750m),
            D("TERRA13", new DateOnly(2025, 9, 15),  0.1800m),
            D("TERRA13", new DateOnly(2025, 12, 15), 0.1820m),
            // FIBRAMQ12 — 2 pagos en segundo semestre 2025
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
