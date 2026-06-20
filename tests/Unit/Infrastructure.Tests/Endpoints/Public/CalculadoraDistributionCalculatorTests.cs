using Api.Endpoints.Public;
using Domain.Market;

namespace Infrastructure.Tests.Endpoints.Public;

public class CalculadoraDistributionCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsNulls_WhenNoDistributions()
    {
        var result = CalculadoraDistributionCalculator.Calculate([]);

        Assert.Null(result.UltimoPeriodo);
        Assert.Null(result.DistCbfi);
        Assert.Null(result.DistCbfiAnual);
    }

    [Fact]
    public void Calculate_ReturnsPaymentQuarterAndSumsTrailing12Months()
    {
        var distributions = new[]
        {
            Dist(new DateOnly(2026, 5, 10), 0.30m),
            Dist(new DateOnly(2026, 5, 20), 0.10m),
            Dist(new DateOnly(2026, 2, 15), 0.05m),
            Dist(new DateOnly(2025, 11, 15), 0.20m),
        };

        var result = CalculadoraDistributionCalculator.Calculate(distributions);

        Assert.Equal("Q2-2026", result.UltimoPeriodo);
        Assert.NotNull(result.DistCbfi);
        Assert.NotNull(result.DistCbfiAnual);
        Assert.Equal(0.40m, result.DistCbfi.Value);
        // Trailing 12 meses desde 2026-05-20: todos los 4 pagos están dentro → 0.30+0.10+0.05+0.20 = 0.65
        Assert.Equal(0.65m, result.DistCbfiAnual.Value);
    }

    [Fact]
    public void Calculate_ExcludesDistributionsOlderThan12Months()
    {
        var distributions = new[]
        {
            Dist(new DateOnly(2026, 5, 15), 0.45m),
            Dist(new DateOnly(2026, 2, 15), 0.45m),
            Dist(new DateOnly(2025, 11, 15), 0.45m),
            Dist(new DateOnly(2025, 8, 15), 0.45m),
            Dist(new DateOnly(2025, 5, 14), 0.45m), // exactamente fuera del rango (< cutoff 2025-05-15)
        };

        var result = CalculadoraDistributionCalculator.Calculate(distributions);

        // Solo los 4 pagos dentro de 12 meses → 0.45 × 4 = 1.80
        Assert.Equal(1.80m, result.DistCbfiAnual!.Value);
    }

    private static Distribution Dist(DateOnly paymentDate, decimal amount) => new()
    {
        Id = Guid.NewGuid(),
        FibraId = Guid.NewGuid(),
        Ticker = "TEST",
        PaymentDate = paymentDate,
        AmountPerUnit = amount,
        Currency = "MXN",
        Source = "test",
        CapturedAt = DateTimeOffset.UtcNow,
    };
}
