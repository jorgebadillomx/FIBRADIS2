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
    public void Calculate_UsesMostRecentQuarterAndSumsQuarterAndYear()
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
        Assert.Equal(0.45m, result.DistCbfiAnual.Value);
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
