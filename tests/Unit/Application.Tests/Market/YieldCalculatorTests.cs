using Application.Market;
using Domain.Market;

namespace Application.Tests.Market;

public class YieldCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 5, 19);

    private static Distribution Dist(string ticker, DateOnly date, decimal amount) => new()
    {
        Id = Guid.NewGuid(),
        FibraId = Guid.NewGuid(),
        Ticker = ticker,
        PaymentDate = date,
        AmountPerUnit = amount,
    };

    [Fact]
    public void Calculate_FourQuarterlyPayments_ReturnsCorrectYield()
    {
        // Todos dentro de la ventana de 365 días (cutoff = 2025-05-19)
        var dists = new[]
        {
            Dist("FUNO11", new DateOnly(2025, 7, 1),  0.361m),
            Dist("FUNO11", new DateOnly(2025, 10, 1), 0.372m),
            Dist("FUNO11", new DateOnly(2026, 1, 1),  0.378m),
            Dist("FUNO11", new DateOnly(2026, 4, 1),  0.384m),
        };
        const decimal lastPrice = 21.50m;

        var result = YieldCalculator.Calculate(dists, lastPrice, Today);

        // TTM = 0.361 + 0.372 + 0.378 + 0.384 = 1.495; yield = 1.495 / 21.50 = 0.0695
        Assert.NotNull(result);
        Assert.Equal(Math.Round(1.495m / 21.50m, 4), result!.Value);
    }

    [Fact]
    public void Calculate_TwoSemiannualPayments_ReturnsAnnualizedYield()
    {
        var dists = new[]
        {
            Dist("TERRA13", new DateOnly(2025, 6, 16),  0.55m),
            Dist("TERRA13", new DateOnly(2025, 12, 15), 0.55m),
        };
        const decimal lastPrice = 20.00m;

        var result = YieldCalculator.Calculate(dists, lastPrice, Today);

        // 2 pagos, intervalo ~182 días → paymentsPerYear≈2 → annualized = 0.55*2 = 1.10
        // yield = 1.10 / 20.00 = 0.055
        Assert.NotNull(result);
        Assert.True(result!.Value > 0.04m && result.Value < 0.07m,
            $"Yield esperado ~0.055, obtenido {result.Value}");
    }

    [Fact]
    public void Calculate_NoDistributions_ReturnsNull()
    {
        var result = YieldCalculator.Calculate([], 21.50m, Today);
        Assert.Null(result);
    }

    [Fact]
    public void Calculate_NullLastPrice_ReturnsNull()
    {
        var dists = new[] { Dist("FUNO11", new DateOnly(2025, 12, 15), 0.384m) };
        var result = YieldCalculator.Calculate(dists, null, Today);
        Assert.Null(result);
    }

    [Fact]
    public void Calculate_ZeroLastPrice_ReturnsNull()
    {
        var dists = new[] { Dist("FUNO11", new DateOnly(2025, 12, 15), 0.384m) };
        var result = YieldCalculator.Calculate(dists, 0m, Today);
        Assert.Null(result);
    }

    [Fact]
    public void Calculate_SinglePayment_ReturnsNonNullYield()
    {
        var dists = new[] { Dist("FIBRAMQ12", new DateOnly(2025, 12, 15), 0.152m) };
        const decimal lastPrice = 15.00m;

        var result = YieldCalculator.Calculate(dists, lastPrice, Today);

        // 1 pago → annualized = 0.152 (sin extrapolación)
        Assert.NotNull(result);
        Assert.Equal(Math.Round(0.152m / 15.00m, 4), result!.Value);
    }

    [Fact]
    public void Calculate_ThreeQuarterlyPaymentsInYear_AnnualizesByThreeNotFour()
    {
        // TERRA13: 3 pagos trimestrales dentro de los últimos 12 meses
        var dists = new[]
        {
            Dist("TERRA13", Today.AddDays(-270), 0.18m),
            Dist("TERRA13", Today.AddDays(-180), 0.18m),
            Dist("TERRA13", Today.AddDays(-90),  0.18m),
        };
        const decimal lastPrice = 20.00m;

        var result = YieldCalculator.Calculate(dists, lastPrice, Today);

        // TTM = 3 * 0.18 = 0.54; yield = 0.54 / 20.00 = 0.027
        Assert.NotNull(result);
        Assert.Equal(Math.Round(0.54m / 20.00m, 4), result!.Value);
    }

    [Fact]
    public void Calculate_FutureDatedDistributions_AreIgnored()
    {
        // Una distribución con fecha futura (error de captura / aviso anticipado) NO debe contar
        // en el TTM (trailing twelve months): solo pagos ya realizados.
        var dists = new[]
        {
            Dist("FUNO11", Today.AddDays(-90), 0.384m),  // ya pagada → cuenta
            Dist("FUNO11", Today.AddDays(30),  0.390m),  // futura → se ignora
        };

        var result = YieldCalculator.Calculate(dists, 21.50m, Today);

        Assert.NotNull(result);
        Assert.Equal(Math.Round(0.384m / 21.50m, 4), result!.Value);
    }

    [Fact]
    public void Calculate_DistributionsOutsideOneYear_AreIgnored()
    {
        var dists = new[]
        {
            Dist("FUNO11", new DateOnly(2024, 3, 17), 0.361m), // fuera del año
            Dist("FUNO11", new DateOnly(2025, 12, 15), 0.384m), // dentro del año
        };

        var result = YieldCalculator.Calculate(dists, 21.50m, Today);

        // Solo 1 pago dentro del año
        Assert.NotNull(result);
        Assert.Equal(Math.Round(0.384m / 21.50m, 4), result!.Value);
    }
}
